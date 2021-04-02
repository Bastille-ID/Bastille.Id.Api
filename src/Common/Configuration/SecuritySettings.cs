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
    using System.Collections.Generic;
    using Bastille.Id.Core.Configuration;

    /// <summary>
    /// Contains security related settings for the API and applications.
    /// </summary>
    public class SecuritySettings
    {
        /// <summary>
        /// Gets or sets the synchronization.
        /// </summary>
        /// <value>The synchronization.</value>
        public SynchronizationSettings Synchronization { get; set; }

        /// <summary>
        /// Gets or sets the allowed origins.
        /// </summary>
        /// <value>The allowed origins.</value>
        public List<string> AllowedOrigins { get; set; } = new List<string>();
    }
}