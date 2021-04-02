/*
 *
 * (c) Copyright Talegen, LLC.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * http://www.apache.org/licenses/LICENSE-2.0
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/

namespace Bastille.Id.Api.Common.Notifications
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Bastille.Id.Core;
    using Bastille.Id.Core.Data;
    using Bastille.Id.Models.Notifications;
    using Bastille.Id.Models.Notifications.Types;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.EntityFrameworkCore;
    using Serilog;
    using Talegen.Common.Core.Errors;
    using Talegen.Common.Core.Extensions;

    /// <summary>
    /// Implements the notification helper class.
    /// </summary>
    public class NotificationHelper : INotificationHelper
    {
        /// <summary>
        /// Contains the notification command to send on.
        /// </summary>
        private const string NotificationCommand = "notification";

        /// <summary>
        /// Contains the notification hub context.
        /// </summary>
        private readonly IHubContext<NotificationHub> hubContext;

        /// <summary>
        /// Contains the notification hub context.
        /// </summary>
        private readonly IConnectionManager connectionMananger;

        /// <summary>
        /// The error manager
        /// </summary>
        private readonly IErrorManager errorManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationHelper" /> class.
        /// </summary>
        /// <param name="context">Contains the hub context.</param>
        /// <param name="connectionManager">Contains a connection manager.</param>
        /// <param name="errorManager">Contains an instance of error manager.</param>
        public NotificationHelper(IHubContext<NotificationHub> context, IConnectionManager connectionManager, IErrorManager errorManager)
        {
            this.hubContext = context;
            this.connectionMananger = connectionManager;
            this.errorManager = errorManager;
        }

        /// <summary>
        /// This method is used to send a notification message to the specific connected user.
        /// </summary>
        /// <typeparam name="TModel">Contains the model data type included in the package.</typeparam>
        /// <param name="notification">Contains the notification model to send.</param>
        /// <param name="appContext">Contains an application context instance.</param>
        /// <param name="cancellationToken">Contains an optional cancellation token.</param>
        /// <returns>Returns a task.</returns>
        public async Task NotifyUserAsync<TModel>(NotificationModel<TModel, string> notification, ApplicationContext<ApplicationDbContext> appContext = null, CancellationToken cancellationToken = default)
        {
            Log.Debug("NotifyUserAsync Called");

            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            if (notification.Target != NotificationTarget.User)
            {
                throw new ArgumentOutOfRangeException(nameof(notification));
            }

            try
            {
                string clientKey = $"{notification.TenantId}:{notification.TargetId}";

                Log.Debug("Searching for signalR notification client {0}", clientKey);

                // this will send the notification to all clients connected with the target identity.
                IClientProxy clientProxy = this.hubContext.Clients.User(clientKey);

                if (clientProxy != null)
                {
                    Log.Information("Sending notification {0} to client {1}", notification.Type.ToString(), clientKey);

                    // execute the notification transfer to the client endpoint
                    await clientProxy.SendAsync(NotificationCommand, notification, cancellationToken: cancellationToken);

                    // if a context was specified...
                    if (appContext != null && notification.Model != null && notification.Model is NotificationMessageModel)
                    {
                        // store the notification message in the user's notification store database
                        NotificationService notificationService = new NotificationService(appContext);
                        await notificationService.StoreAsync(notification as NotificationModel<NotificationMessageModel, string>, NotificationState.Unread, cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    Log.Warning("The client key \"{0}\" was not found in hub client users.", clientKey);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Critical error occurred while retrieving connections for user {0}", notification.TargetId);
            }
        }

        /// <summary>
        /// This method is used to send a notification message to all the connected users.
        /// </summary>
        /// <typeparam name="TModel">Contains the model data type included in the package.</typeparam>
        /// <param name="notification">Contains the notification model to send.</param>
        /// <param name="appContext">Contains an application context instance.</param>
        /// <param name="excludeCurrentUser">Contains a value indicating whether the current user is excluded from receiving the signal message.</param>
        /// <param name="cancellationToken">Contains an optional cancellation token.</param>
        /// <returns>Returns a task.</returns>
        public async Task NotifyAllAsync<TModel>(NotificationModel<TModel, string> notification, ApplicationContext<ApplicationDbContext> appContext = null, bool excludeCurrentUser = false, CancellationToken cancellationToken = default)
        {
            Log.Debug("NotifyAllAsync Called");

            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            if (notification.Target != NotificationTarget.All)
            {
                throw new ArgumentOutOfRangeException(nameof(notification));
            }

            try
            {
                // because this is potentially a multi-tenant environment, the target identity value in the Notification model shall be the target tenant
                // idenfiier aka organization slug. We cannot use tenant Groups simply because there is no way to retrieve the tenant or connection information.
                string tenantId = !string.IsNullOrWhiteSpace(notification.TenantId) ? notification.TenantId : notification.TargetId;

                // TODO: get current user identity from access_token subject
                string currentUserId = appContext.CurrentUserId;

                IClientProxy groupProxy = null;
                Log.Debug("Attempting to find tenant group {0} with excludeCurrentUser = {1}", tenantId, excludeCurrentUser);

                // if we are not excluding the source user from the broadcast...
                if (!excludeCurrentUser)
                {
                    // notify everyone in the tenant group
                    groupProxy = this.hubContext.Clients.Group(tenantId);
                }
                else
                {
                    // otherwise, we need to find all connections tracked by tenant targetId and exclude the current external user id from results if set.
                    List<string> tenantConnectionIds = this.connectionMananger.FindTenantConnections(tenantId, currentUserId);

                    if (tenantConnectionIds.Any())
                    {
                        groupProxy = this.hubContext.Clients?.Clients(tenantConnectionIds);
                    }
                    else
                    {
                        Log.Information("No tenant connections found. Only {0} is connected apparently.", currentUserId);
                    }
                }

                if (groupProxy != null)
                {
                    Log.Information("Sending notification {0} to all clients on tenant {1}", notification.Type.ToString(), tenantId);

                    // execute the notification transfer to the client endpoint
                    await groupProxy.SendAsync(NotificationCommand, notification, cancellationToken: cancellationToken);

                    // if a context was specified...
                    if (appContext != null && notification.Model != null && notification.Model is NotificationMessageModel)
                    {
                        // store the notification message in the user's notification store database
                        NotificationService notificationService = new NotificationService(appContext);

                        // send the message to all active users, and exclude current user if specified.
                        List<string> notifyUsers = await appContext.DataContext.Users.Where(u => !excludeCurrentUser || u.Id != currentUserId.ToGuid()).Select(u => u.Id.ToString()).ToListAsync(cancellationToken);
                        await notificationService.StoreAsync(notification as NotificationModel<NotificationMessageModel, string>, NotificationState.Unread, notifyUsers, cancellationToken);
                    }
                }
                else
                {
                    Log.Error("Critical error occurred during SignalR connection send to Group TargetId: {0}. Group not found.", notification.TargetId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Critical error occurred while retrieving connections for Group Target Id {0}", notification.TargetId);
            }
        }
    }
}