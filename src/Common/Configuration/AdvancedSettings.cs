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
    /// <summary>
    /// This class contains advanced program settings.
    /// </summary>
    public class AdvancedSettings
    {
        /// <summary>
        /// Gets or sets the MinimumCompletionPortThreads count.
        /// </summary>
        public int MinimumCompletionPortThreads { get; set; } = 200;

        /// <summary>
        /// Gets or sets the maximum degree of parallelism.
        /// </summary>
        /// <value>The maximum degree of parallelism.</value>
        public int MaxDegreeOfParallelism { get; set; }

        /// <summary>
        /// Gets or sets the memory cache life minutes.
        /// </summary>
        /// <value>The memory cache life minutes.</value>
        public int MemoryCacheLifeMinutes { get; set; } = 10;

        /// <summary>
        /// Gets or sets a value indicating whether the server should force SSL only.
        /// </summary>
        public bool ForceSsl { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the diagnostics page is available.
        /// </summary>
        public bool ShowDiagnostics { get; set; }

        /// <summary>
        /// Gets or sets the name of the application data sub folder.
        /// </summary>
        /// <value>The name of the application data sub folder.</value>
        public string AppDataSubFolderName { get; set; } = "App_Data";

        /// <summary>
        /// Gets or sets a value indicating whether [minify json output].
        /// </summary>
        /// <value><c>true</c> if [minify json output]; otherwise, <c>false</c>.</value>
        public bool MinifyJsonOutput { get; set; }

        /// <summary>
        /// Gets or sets the JWT time skew in minutes.
        /// </summary>
        /// <value>The JWT time skew in minutes.</value>
        public int JwtTimeSkewInMinutes { get; set; }
    }
}