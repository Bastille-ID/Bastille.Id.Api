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
    using Bastille.Id.Core.Data.Entities;
    using Bastille.Id.Models.Notifications;
    using Bastille.Id.Models.Notifications.Types;

    /// <summary>
    /// Contains a number of notification extension methods.
    /// </summary>
    public static class NotificationExtensions
    {
        /// <summary>
        /// This method is used to convert a signal notification message model to an entity object.
        /// </summary>
        /// <param name="notification">Contains the signal notification message model to convert.</param>
        /// <param name="ownerUserId">Contains the owner identity the notification record is related to.</param>
        /// <param name="state">Contains the state of the notification message. The default is Unread.</param>
        /// <returns>Returns a new <see cref="Notification" /> entity object.</returns>
        public static Notification ToEntity(this NotificationModel<NotificationMessageModel, string> notification, Guid ownerUserId, NotificationState state = NotificationState.Unread)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            return new Notification
            {
                NotificationId = notification.NotificationId,
                NotificationDate = notification.NotificationDate,
                Type = notification.Type,
                Alert = notification.Alert,
                State = state,
                Target = notification.Target,
                UserId = ownerUserId,
                Summary = notification.Model.Summary,
                WebMessage = notification.Model.HtmlBody,
                Subject = notification.Model.Subject,
                Metadata = notification.Model.MetadataModel
            };
        }

        /// <summary>
        /// Converts to model.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>Returns a new <see cref="NotificationStoreModel" /> model.</returns>
        public static NotificationStoreModel ToModel(this Notification entity)
        {
            NotificationStoreModel model = null;

            if (entity != null)
            {
                model = new NotificationStoreModel
                {
                    NotificationId = entity.NotificationId,
                    OrganizationId = entity.OrganizationId,
                    UserId = entity.UserId,
                    NotificationDate = entity.NotificationDate,
                    Alert = entity.Alert,
                    State = entity.State,
                    Target = entity.Target,
                    Type = entity.Type,
                    Message = new NotificationMessageModel
                    {
                        Summary = entity.Summary,
                        HtmlBody = entity.WebMessage,
                        Subject = entity.Subject,
                        MetadataModel = entity.Metadata
                    }
                };
            }

            return model;
        }

        /// <summary>
        /// Populates the specified entity.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="entity">The entity.</param>
        public static void Populate(this NotificationStoreModel model, Notification entity)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (entity != null)
            {
                entity.NotificationId = model.NotificationId;
                entity.UserId = model.UserId;
                entity.NotificationDate = model.NotificationDate;
                entity.Alert = model.Alert;
                entity.State = model.State;
                entity.Target = model.Target;
                entity.Type = model.Type;
                entity.Summary = model.Message.Summary;
                entity.WebMessage = model.Message.HtmlBody;
                entity.Subject = model.Message.Subject;
                entity.Metadata = model.Message.MetadataModel;
            }
        }

        /// <summary>
        /// Ingests the specified entity.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="entity">The entity.</param>
        public static void Ingest(this NotificationStoreModel model, Notification entity)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (entity != null)
            {
                model.NotificationId = entity.NotificationId;
                model.UserId = entity.UserId;
                model.NotificationDate = entity.NotificationDate;
                model.Alert = entity.Alert;
                model.State = entity.State;
                model.Type = entity.Type;
                model.Target = entity.Target;
                model.Message.Summary = entity.Summary;
                model.Message.HtmlBody = entity.WebMessage;
                model.Message.Subject = entity.Subject;
                model.Message.MetadataModel = entity.Metadata;
            }
        }
    }
}