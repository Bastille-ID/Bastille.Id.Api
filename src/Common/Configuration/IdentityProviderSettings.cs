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
    using System;

    /// <summary>
    /// This class contains the identity provider settings.
    /// </summary>
    public class IdentityProviderSettings
    {
        /// <summary>
        /// Gets or sets the authority URI.
        /// </summary>
        /// <value>The authority URI.</value>
        public Uri AuthorityUri { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [require HTTPS metadata].
        /// </summary>
        /// <value><c>true</c> if [require HTTPS metadata]; otherwise, <c>false</c>.</value>
        public bool RequireHttpsMetadata { get; set; } = true;

        /// <summary>
        /// Gets or sets the API secret.
        /// </summary>
        /// <value>The API secret.</value>
        public string ApiSecret { get; set; }

        /// <summary>
        /// Gets or sets the cache minutes.
        /// </summary>
        /// <value>The cache minutes.</value>
        public int CacheMinutes { get; set; } = 10;
    }
}