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
    using System.ComponentModel.DataAnnotations;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Bastille.Id.Api.Properties;
    using Bastille.Id.Core;
    using Bastille.Id.Core.Data;
    using Bastille.Id.Core.Data.Entities;
    using Bastille.Id.Models.Notifications;
    using Bastille.Id.Models.Notifications.Types;
    using Microsoft.EntityFrameworkCore;
    using Talegen.Common.Core.Errors;
    using Talegen.Common.Core.Extensions;

    /// <summary>
    /// This class implements the business logic needed for Notifications.
    /// </summary>
    public class NotificationService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationService" /> class.
        /// </summary>
        /// <param name="appContext">The application context.</param>
        public NotificationService(ApplicationContext<ApplicationDbContext> appContext)
        {
            this.AppContext = appContext;
        }

        /// <summary>
        /// Gets the application context.
        /// </summary>
        /// <value>The application context.</value>
        public ApplicationContext<ApplicationDbContext> AppContext { get; }

        /// <summary>
        /// This method is used to query a user's notification messages and return paginated results.
        /// </summary>
        /// <param name="filter">Contains the query parameters.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns the results of the query in a <see cref="NotificationStoreQueryModel" /> object.</returns>
        public async Task<NotificationStoreResultModel> FindNotificationsAsync(NotificationStoreQueryModel filter, CancellationToken cancellationToken = default)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            if (filter.Limit <= 0)
            {
                // we cannot allow an unlimited amount of components to be returned since there could be hundreds defaulting to 50 for now, this could be
                // changed in future to use user's default grid size
                filter.Limit = 50;
            }

            if (filter.Page <= 0)
            {
                filter.Page = 1;
            }

            return await this.ExecuteNotificationStoreQueryAsync(filter, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// This method is used to retrieve the total count of unread messages for the given user.
        /// </summary>
        /// <param name="state">Contains the state of messages to get a count for. Default is Unread messages.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns the total count of unread messages for the current user.</returns>
        public async Task<int> RetrieveTotalMessagesCountAsync(NotificationState state = NotificationState.Unread, CancellationToken cancellationToken = default)
        {
            return await this.AppContext.DataContext.Notifications
                .Where(n => n.UserId == this.AppContext.CurrentUserId.ToGuid() && n.State == state)
                .CountAsync(cancellationToken);
        }

        /// <summary>
        /// This method is used to retrieve the top number of notification messages for the current user. This will default to the top 10 messages. If count is
        /// zero, all notifications for the current user shall be returned.
        /// </summary>
        /// <param name="count">Contains the number of messages to return.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns a list of <see cref="NotificationStoreModel" /> objects.</returns>
        public async Task<List<NotificationStoreModel>> RetrieveTopNotificationsAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            var query = this.AppContext.DataContext.Notifications
                .Include(n => n.User)
                .Where(n => n.UserId == this.AppContext.CurrentUserId.ToGuid())
                .OrderByDescending(n => n.NotificationDate)
                .Select(n => new NotificationStoreModel
                {
                    NotificationId = n.NotificationId,
                    NotificationDate = n.NotificationDate,
                    UserId = n.UserId,
                    Alert = n.Alert,
                    State = n.State,
                    Target = n.Target,
                    Type = n.Type,
                    Message = new NotificationMessageModel
                    {
                        Summary = n.Summary,
                        HtmlBody = n.WebMessage,
                        Subject = n.Subject,
                        MetadataModel = n.Metadata
                    }
                });

            if (count > 0)
            {
                query = query.Take(count);
            }

            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// This method is used to retrieve an entity object by id.
        /// </summary>
        /// <param name="id">Contains the entity id to retrieve.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns the entity object if found.</returns>
        public async Task<Notification> ReadEntityAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Notification entity = await this.AppContext.DataContext.Notifications
                .Include(u => u.User)
                .FirstOrDefaultAsync(r => r.NotificationId == id && r.UserId == this.AppContext.CurrentUserId.ToGuid(), cancellationToken)
                .ConfigureAwait(false);

            if (entity == null)
            {
                this.AppContext.ErrorManager.CriticalNotFoundFormat(Resources.NotificationNotFoundErrorText, ErrorCategory.Application, id);
            }

            return entity;
        }

        /// <summary>
        /// This method is used to retrieve an entity object by model.
        /// </summary>
        /// <param name="model">Contains the model to retrieve as an entity.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns the entity object if found.</returns>
        public async Task<Notification> ReadEntityAsync(NotificationStoreModel model, CancellationToken cancellationToken = default)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return await this.ReadEntityAsync(model.NotificationId, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads the model asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Returns the notification model for the requested identifier.</returns>
        public async Task<NotificationStoreModel> ReadModelAsync(Guid id, CancellationToken cancellationToken = default)
        {
            NotificationStoreModel result = null;
            Notification entityFound = await this.ReadEntityAsync(id, cancellationToken).ConfigureAwait(false);

            if (entityFound != null)
            {
                result = entityFound.ToModel();
            }
            else
            {
                this.AppContext.ErrorManager.CriticalFormat(Resources.NotificationCannotBeFoundByIdErrorText, ErrorCategory.Application, id);
            }

            return result;
        }

        /// <summary>
        /// This method is used to convert a signal notification message model into a storage model.
        /// </summary>
        /// <param name="notification">Contains the signal notification message model to store in the database.</param>
        /// <param name="state">Contains the state of the new notification record. By default this is Unread.</param>
        /// <param name="usersToNotify">Contains a list of users to create notification messages for.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns a value indicating if the record was created.</returns>
        public async Task<bool> StoreAsync(NotificationModel<NotificationMessageModel, string> notification, NotificationState state = NotificationState.Unread, List<string> usersToNotify = null, CancellationToken cancellationToken = default)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            bool result = false;

            if (usersToNotify == null)
            {
                if (notification.Target == NotificationTarget.User && notification.TargetId != this.AppContext.CurrentUserId)
                {
                    Guid userId = await this.AppContext.DataContext.Users.Where(u => u.Id == notification.TargetId.ToGuid()).Select(u => u.Id).FirstOrDefaultAsync(cancellationToken);

                    if (userId != Guid.Empty)
                    {
                        usersToNotify = new List<string>() { userId.ToString() };
                    }
                }
                else
                {
                    usersToNotify = new List<string>() { this.AppContext.CurrentUserId };
                }
            }

            if (usersToNotify != null)
            {
                usersToNotify.ForEach(userId =>
                {
                    this.AppContext.DataContext.Notifications.Add(notification.ToEntity(userId.ToGuid(), state));
                });

                result = await this.SaveChangesAsync(cancellationToken) > 0;
            }

            return result;
        }

        /// <summary>
        /// This method is used to update the state of a notification message, allowing a user to set a Read, UnRead, or Archived state on a message.
        /// </summary>
        /// <param name="id">Contains the unique identity of the notification message.</param>
        /// <param name="model">Contains the model set the message to.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns a value indicating the record was updated successfully.</returns>
        public async Task<bool> UpdateStateAsync(Guid id, NotificationUpdateModel model, CancellationToken cancellationToken = default)
        {
            bool result = false;

            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            Notification entity = await this.ReadEntityAsync(id, cancellationToken).ConfigureAwait(false);

            if (entity != null)
            {
                entity.State = model.State;
                result = await this.SaveAsync(entity, EntityState.Modified, cancellationToken);
            }

            return result;
        }

        /// <summary>
        /// Deletes the specified notification.
        /// </summary>
        /// <param name="id">The notification record identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Returns true if the notification was successfully deleted.</returns>
        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            bool result = false;
            Notification entity = await this.ReadEntityAsync(id, cancellationToken).ConfigureAwait(false);

            if (entity != null)
            {
                result = await this.SaveAsync(entity, EntityState.Deleted, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        /// <summary>
        /// Deletes all of the notification messages for the current user.
        /// </summary>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns a value indicating the success of the execution.</returns>
        public async Task<bool> DeleteAllAsync(CancellationToken cancellationToken)
        {
            bool result = false;
            List<Notification> results = await this.AppContext.DataContext.Notifications.Where(n => n.UserId == this.AppContext.CurrentUserId.ToGuid()).ToListAsync(cancellationToken);

            if (results.Any())
            {
                this.AppContext.DataContext.Notifications.RemoveRange(results);
                result = await this.SaveChangesAsync(cancellationToken) > 0;
            }

            return result;
        }

        /// <summary>
        /// This method is used to execute an entity state save to the database.
        /// </summary>
        /// <param name="entity">Contains the entity to add, modify, or delete.</param>
        /// <param name="state">Contains the state of the entity to execute on the database.</param>
        /// <param name="cancellationToken">Contains an optional cancellation token.</param>
        /// <returns>Returns a value indicating whether the save execution was successful.</returns>
        public async Task<bool> SaveAsync(Notification entity, EntityState state = EntityState.Modified, CancellationToken cancellationToken = default)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            switch (state)
            {
                case EntityState.Added:
                    this.AppContext.DataContext.Notifications.Add(entity);
                    break;

                case EntityState.Deleted:
                    this.AppContext.DataContext.Notifications.Remove(entity);
                    break;
            }

            return await this.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0;
        }

        #region Private Methods

        /// <summary>
        /// This method is used to build the basic notification store queryable that will be used to determine result count as well as executed for component
        /// search results.
        /// </summary>
        /// <param name="filter">Contains the filter model that contains the filtering criteria for the query.</param>
        /// <returns>Returns an IQueryable for notifications that match the filtering criteria.</returns>
        private IQueryable<NotificationStoreModel> BuildQuery(NotificationStoreQueryModel filter)
        {
            // build the initial query. Retrieve all components that are in the specified folder and for which the current user has read or better permissions.
            // If filter component search string is null, skip contains(filter.ComponentSearch)
            var query = this.AppContext.DataContext.Notifications
                .Include(n => n.User)
                .AsNoTracking()
                .Where(n => n.UserId == this.AppContext.CurrentUserId.ToGuid())
                .Select(n => new NotificationStoreModel
                {
                    Alert = n.Alert,
                    Message = new NotificationMessageModel
                    {
                        HtmlBody = n.WebMessage,
                        Subject = n.Subject,
                        Summary = n.Summary,
                        MetadataModel = n.Metadata
                    },
                    NotificationDate = n.NotificationDate,
                    NotificationId = n.NotificationId,
                    State = n.State,
                    Target = n.Target,
                    Type = n.Type,
                    UserId = n.UserId
                })
                .AsQueryable();

            // if there is search text specified, use basic search capabilities...
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                query = query.Where(c => c.Message.Subject.Contains(filter.SearchText)
                || c.Message.Summary.Contains(filter.SearchText)
                || c.Message.HtmlBody.Contains(filter.SearchText));
            }

            if (filter.State.Any())
            {
                query = query.Where(c => filter.State.Contains(c.State));
            }

            if (filter.Alert.Any())
            {
                query = query.Where(c => filter.Alert.Contains(c.Alert));
            }

            if (filter.Sort.Any())
            {
                for (int i = 0; i < filter.Sort.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(filter.Sort[i]))
                    {
                        query = query.OrderByName(filter.Sort[i], filter.Dir.Length >= i ? filter.Dir[i] : SortDirection.Ascending);
                    }
                }
            }
            else
            {
                // if not specified, default sort is Name/Ascending
                query = query.OrderByName(nameof(Notification.NotificationDate), SortDirection.Descending);
            }

            // set paging parameters
            if (filter.Limit > 0)
            {
                query = filter.Page > 1 ? query.Skip((filter.Page - 1) * filter.Limit).Take(filter.Limit) : query.Take(filter.Limit);
            }

            return query;
        }

        /// <summary>
        /// This method is used to build the query LINQ for reuse between retrieval methods in the service class.
        /// </summary>
        /// <param name="filters">Contains the notifications store filter parameters object.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns a list of <see cref="NotificationStoreResultModel" /> based on query that uses filter parameters.</returns>
        private async Task<NotificationStoreResultModel> ExecuteNotificationStoreQueryAsync(NotificationStoreQueryModel filters, CancellationToken cancellationToken = default)
        {
            var query = this.BuildQuery(filters);

            // execute query, returning a list of new MinimalComponentModel objects with the permissions specified.
            var result = new NotificationStoreResultModel
            {
                Results = await query.ToListAsync(cancellationToken).ConfigureAwait(false)
            };

            // if the query returned less than the limit, and we're on the first page, we can use that count for the component results otherwise, we must run a
            // separate query to determine the total count
            result.TotalCount = filters.Page == 1 && result.Results.Count <= filters.Limit ? result.Results.Count : await query.CountAsync(cancellationToken);
            result.TotalUnreadCount = await this.RetrieveTotalMessagesCountAsync(NotificationState.Unread, cancellationToken);

            return result;
        }

        #endregion

        /// <summary>
        /// This method is used to execute the data context save changes and catch all validation errors.
        /// </summary>
        /// <param name="cancellationToken">Contains an optional cancellation token.</param>
        /// <returns>Returns the number of rows updated on success.</returns>
        protected virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            int resultValue = 0;

            try
            {
                resultValue = await this.AppContext.DataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException connEx)
            {
                this.AppContext.ErrorManager.Critical(connEx, ErrorCategory.Application);
            }
            catch (DbUpdateException ex)
            {
                var sqlEx = ex?.InnerException as SqlException;

                this.AppContext.ErrorManager.Critical(sqlEx?.InnerException ?? ex, ErrorCategory.Application);
            }
            catch (InvalidOperationException invalidEx)
            {
                this.AppContext.ErrorManager.Critical(invalidEx, ErrorCategory.Application);
            }
            catch (ValidationException validateEx)
            {
                this.AppContext.ErrorManager.Critical(validateEx, ErrorCategory.Application);
            }
            catch (Exception otherEx)
            {
                this.AppContext.ErrorManager.Critical(otherEx, ErrorCategory.Application);
            }

            return resultValue;
        }
    }
}