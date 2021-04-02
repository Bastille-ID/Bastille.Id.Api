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
    using System.Linq;
    using System.Threading.Tasks;
    using Bastille.Id.Api.Common.Configuration;
    using Bastille.Id.Core.Security;
    using IdentityModel;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.Options;
    using Serilog;

    /// <summary>
    /// This class is the central notification hub for the application.
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        /// <summary>
        /// Contains an instance of the application settings.
        /// </summary>
        private readonly ApplicationSettings settings;

        /// <summary>
        /// Contains the connection manager instance.
        /// </summary>
        private readonly IConnectionManager connectionManager;

        /// <summary>
        /// Contains the hub context.
        /// </summary>
        private readonly IHubContext<NotificationHub> hubContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationHub" /> class.
        /// </summary>
        /// <param name="settings">Contains an instance of the application settings.</param>
        /// <param name="hubContext">Contains the hub context to use.</param>
        /// <param name="connectionManager">Contains the connection manager.</param>
        public NotificationHub(IOptions<ApplicationSettings> settings, IHubContext<NotificationHub> hubContext, IConnectionManager connectionManager)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            this.settings = settings.Value;
            this.hubContext = hubContext;
            this.connectionManager = connectionManager;
        }

        /// <summary>
        /// This method is called when a connection is established.
        /// </summary>
        /// <returns>Returns the task.</returns>
        public override async Task OnConnectedAsync()
        {
            if (this.Context != null && this.Context.User != null && this.Context.User.Identity != null && this.Context.User.Identity.IsAuthenticated)
            {
                string emailId = this.Context.User.Claims.FirstOrDefault(c => c.Type.Equals(JwtClaimTypes.Email, StringComparison.InvariantCultureIgnoreCase))?.Value ?? string.Empty;
                string externalUserId = this.Context.User.Claims.FirstOrDefault(c => c.Type.Equals(JwtClaimTypes.Subject, StringComparison.InvariantCultureIgnoreCase))?.Value ?? string.Empty;
                string tenantId = this.Context.User.Claims.FirstOrDefault(c => c.Type.Equals(SecurityDefaults.TenantClaimType, StringComparison.InvariantCultureIgnoreCase))?.Value ?? RefineHostToTenantKey(this.Context.GetHttpContext().Request.Host.Host);

                if (!string.IsNullOrWhiteSpace(externalUserId))
                {
                    Log.Information("Adding {0} user Id {1}:{2} to connection {3}.", tenantId, emailId, externalUserId, this.Context.ConnectionId);

                    if (!string.IsNullOrWhiteSpace(tenantId))
                    {
                        // add connection to tenant group
                        await this.Groups.AddToGroupAsync(this.Context.ConnectionId, tenantId);
                        await this.connectionManager.AddConnectionAsync(tenantId, externalUserId, this.Context.ConnectionId);
                    }
                    else
                    {
                        Log.Warning("No tenant host value was found in HTTP context during OnConnected hub call. The user connection cannot be tracked and notifications may not work.");
                    }
                }
                else
                {
                    Log.Warning("No external User identity value was found in JWT Claim during OnConnected hub call. The user connection cannot be tracked and notifications may not work.");
                }
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// This method is called with the connection is closed.
        /// </summary>
        /// <param name="exception">Contains an exception.</param>
        /// <returns>Returns the task.</returns>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (this.Context != null && this.Context.User != null && this.Context.User.Identity != null && this.Context.User.Identity.IsAuthenticated)
            {
                string emailId = this.Context.User.Claims.FirstOrDefault(c => c.Type.Equals(JwtClaimTypes.Email, StringComparison.InvariantCultureIgnoreCase))?.Value ?? string.Empty;
                string externalUserId = this.Context.User.Claims.FirstOrDefault(c => c.Type.Equals(JwtClaimTypes.Subject, StringComparison.InvariantCultureIgnoreCase))?.Value ?? string.Empty;
                string tenantId = this.Context.User.Claims.FirstOrDefault(c => c.Type.Equals(SecurityDefaults.TenantClaimType, StringComparison.InvariantCultureIgnoreCase))?.Value ?? RefineHostToTenantKey(this.Context.GetHttpContext().Request.Host.Host);

                if (!string.IsNullOrWhiteSpace(externalUserId))
                {
                    Log.Information("Removing {0} user Id {1}:{2} connection {3}.", tenantId, emailId, externalUserId, this.Context.ConnectionId);
                    await this.Groups.RemoveFromGroupAsync(this.Context.ConnectionId, tenantId);
                    await this.connectionManager.RemoveConnectionAsync(this.Context.ConnectionId, tenantId, externalUserId);
                }
                else
                {
                    Log.Warning("No external User identity value was found in JWT Claim during OnDisconnected hub call. The user connection cannot be tracked and notifications may not work.");
                }
            }

            if (exception != null)
            {
                Log.Warning(exception, "An exception was reported to the OnDisconnectedAsync method of Notification Hub.");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// This method is used to refine a host name into a tenant key.
        /// </summary>
        /// <param name="hostName">Contains the host name to refine.</param>
        /// <returns>Returns the refined tenant key.</returns>
        private static string RefineHostToTenantKey(string hostName)
        {
            string result = hostName ?? string.Empty;

            // if no host name specified default to debugging localhost
            if (string.IsNullOrWhiteSpace(result))
            {
                result = "localhost";
            }

            return result;
        }
    }
}