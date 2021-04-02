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
    using IdentityModel;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.Options;
    using Serilog;
    using Configuration;

    /// <summary>
    /// This class is used to denote a user identity by the user's subject id.
    /// </summary>
    public class UserIdProvider : IUserIdProvider
    {
        /// <summary>
        /// Contains an instance of the application settings.
        /// </summary>
        private readonly ApplicationSettings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserIdProvider" /> class.
        /// </summary>
        /// <param name="settings">Contains an instance of the application settings.</param>
        public UserIdProvider(IOptions<ApplicationSettings> settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            this.settings = settings.Value;
        }

        /// <summary>
        /// This method is used to return the user identity used for denoting a user within the system.
        /// </summary>
        /// <param name="connection">Contains the hub connection.</param>
        /// <returns>Returns the user identity value from the subject identity claim.</returns>
        public virtual string GetUserId(HubConnectionContext connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            string hostName = connection.GetHttpContext()?.Request?.Host.Host ?? string.Empty;

            // if no host name specified default to debugging localhost
            if (string.IsNullOrWhiteSpace(hostName))
            {
                hostName = "localhost";
            }

            string externalUserId = connection.User?.FindFirst(JwtClaimTypes.Subject)?.Value ?? string.Empty;
            Log.Debug("UserIdProvider:GetUserId()={0}", hostName + ":" + externalUserId);
            return hostName + ":" + externalUserId;
        }
    }
}