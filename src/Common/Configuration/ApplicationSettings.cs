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

namespace Bastille.Id.Api.Common.Configuration
{
    using Bastille.Id.Core.Configuration;
    using Talegen.AspNetCore.Web.Configuration;
    using Talegen.Common.Messaging.Configuration;

    /// <summary>
    /// This class contains application settings derived from configuration inputs.
    /// </summary>
    public class ApplicationSettings
    {
        /// <summary>
        /// Gets or sets the security.
        /// </summary>
        /// <value>The security.</value>
        public SecuritySettings Security { get; set; }

        /// <summary>
        /// Gets or sets the application insights.
        /// </summary>
        /// <value>The application insights.</value>
        public ApplicationInsightSettings ApplicationInsights { get; set; }

        /// <summary>
        /// Gets or sets the advanced.
        /// </summary>
        /// <value>The advanced.</value>
        public AdvancedSettings Advanced { get; set; }

        /// <summary>
        /// Gets or sets the storage.
        /// </summary>
        /// <value>The storage.</value>
        public StorageSettings Storage { get; set; }

        /// <summary>
        /// Gets or sets an instance of the <see cref="MessagingSettings" /> class.
        /// </summary>
        public MessagingSettings Messaging { get; set; }

        /// <summary>
        /// Gets or sets the identity provider.
        /// </summary>
        /// <value>The identity provider.</value>
        public IdentityProviderSettings IdentityProvider { get; set; }

        /// <summary>
        /// Gets or sets the notifications.
        /// </summary>
        /// <value>The notifications.</value>
        public NotificationSettings Notifications { get; set; }
    }
}