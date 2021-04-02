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
    using Microsoft.Extensions.Caching.Distributed;
    using Vasont.AspnetCore.RedisClient;

    /// <summary>
    /// This class implements a connection manager interface to track and manage connected users via the SignalR Notifications Hub.
    /// </summary>
    public class ConnectionManager : IConnectionManager
    {
        /// <summary>
        /// Contains an instance of the distributed cache within the system.
        /// </summary>
        private readonly IAdvancedDistributedCache distributedCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionManager" /> class.
        /// </summary>
        /// <param name="distributedCache">Contains an instance of the <see cref="IDistributedCache" /> object.</param>
        public ConnectionManager(IAdvancedDistributedCache distributedCache)
        {
            this.distributedCache = distributedCache;
        }

        /// <summary>
        /// Gets a list of online user external identities.
        /// </summary>
        public IEnumerable<string> OnlineUsers => this.distributedCache.FindKeys("notification_clients:*");

        /// <summary>
        /// This method is used to add a user's connection to the connection manager.
        /// </summary>
        /// <param name="tenantId">Contains the application instance tenant identity.</param>
        /// <param name="externalUserId">Contains the user's external user identity.</param>
        /// <param name="connectionId">Contains the SignalR connection identity.</param>
        public void AddConnection(string tenantId, string externalUserId, string connectionId)
        {
            string connectionKey = $"notification_clients:{tenantId}:{externalUserId}:{connectionId}";
            this.distributedCache.SetString(connectionKey, tenantId);
        }

        /// <summary>
        /// This method is used to add a user's connection to the connection manager.
        /// </summary>
        /// <param name="tenantId">Contains the application instance tenant identity.</param>
        /// <param name="externalUserId">Contains the user's external user identity.</param>
        /// <param name="connectionId">Contains the SignalR connection identity.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns a task.</returns>
        public async Task AddConnectionAsync(string tenantId, string externalUserId, string connectionId, CancellationToken cancellationToken = default)
        {
            string connectionKey = $"notification_clients:{tenantId}:{externalUserId}:{connectionId}";
            await this.distributedCache.SetStringAsync(connectionKey, tenantId, cancellationToken);
        }

        /// <summary>
        /// This method is used to return a list of tenant connection identities.
        /// </summary>
        /// <param name="tenantId">Contains the tenant identity.</param>
        /// <param name="excludeUserId">Contains a user identity to exclude from the connections returned.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns a list of connection identities associated with the tenant.</returns>
        public async Task<List<string>> FindTenantConnectionsAsync(string tenantId, string excludeUserId = "", CancellationToken cancellationToken = default)
        {
            string key = $"notification_clients:{tenantId}:*";
            List<string> keys = (await this.distributedCache.FindKeysAsync(key, cancellationToken)).ToList();

            if (!string.IsNullOrWhiteSpace(excludeUserId))
            {
                keys = keys.Where(key => !key.StartsWith($"notification_clients:{tenantId}:{excludeUserId}:", StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return keys.Select(result =>
            {
                string connectionId = result;
                int index = connectionId.LastIndexOf(':');

                if (index > 0)
                {
                    connectionId = connectionId[(index + 1)..];
                }

                return connectionId;
            }).ToList();
        }

        /// <summary>
        /// This method is used to return a list of tenant connection identities.
        /// </summary>
        /// <param name="tenantId">Contains the tenant identity.</param>
        /// <param name="excludeUserId">Contains a user identity to exclude from the connections returned.</param>
        /// <returns>Returns a list of connection identities associated with the tenant.</returns>
        public List<string> FindTenantConnections(string tenantId, string excludeUserId = "")
        {
            string key = $"notification_clients:{tenantId}:*";
            List<string> keys = this.distributedCache.FindKeys(key).ToList();

            if (!string.IsNullOrWhiteSpace(excludeUserId))
            {
                keys = keys.Where(key => !key.StartsWith($"notification_clients:{tenantId}:{excludeUserId}:", StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return keys.Select(result =>
            {
                string connectionId = result;
                int index = connectionId.LastIndexOf(':');

                if (index > 0)
                {
                    connectionId = connectionId[(index + 1)..];
                }

                return connectionId;
            }).ToList();
        }

        /// <summary>
        /// This method is used to get the connections of the specified user.
        /// </summary>
        /// <param name="tenantId">Contains the application instance tenant identity.</param>
        /// <param name="externalUserId">Contains the user identity to find.</param>
        /// <returns>Returns a hash set of connection identities for the specified user.</returns>
        public IEnumerable<string> GetConnections(string tenantId, string externalUserId)
        {
            return this.distributedCache.FindKeys($"notification_clients:{tenantId}:{externalUserId}:*");
        }

        /// <summary>
        /// This method is used to get the connections of the specified user.
        /// </summary>
        /// <param name="tenantId">Contains the application instance tenant identity.</param>
        /// <param name="externalUserId">Contains the user identity to find.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns a hash set of connection identities for the specified user.</returns>
        public async Task<IEnumerable<string>> GetConnectionsAsync(string tenantId, string externalUserId, CancellationToken cancellationToken = default)
        {
            return await this.distributedCache.FindKeysAsync($"notification_clients:{tenantId}:{externalUserId}:*", cancellationToken);
        }

        /// <summary>
        /// This method is used to remove a connection from the manager.
        /// </summary>
        /// <param name="connectionId">Contains the connection identity to remove.</param>
        /// <param name="tenantId">Contains an optional tenant identity to speed up lookup.</param>
        /// <param name="externalUserId">Contains an optional external user identity to speed up lookup.</param>
        public void RemoveConnection(string connectionId, string tenantId = "", string externalUserId = "")
        {
            // search pattern key
            string searchKey = BuildRemoveKey(connectionId, tenantId, externalUserId);
            List<string> results = this.distributedCache.FindKeys(searchKey).ToList();

            results.ForEach(key =>
            {
                this.distributedCache.Remove(key);
            });
        }

        /// <summary>
        /// This method is used to remove a connection from the manager.
        /// </summary>
        /// <param name="connectionId">Contains the connection identity to remove.</param>
        /// <param name="tenantId">Contains the application instance tenant identity.</param>
        /// <param name="externalUserId">Contains an optional external user identity to speed up lookup.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns a task object.</returns>
        public async Task RemoveConnectionAsync(string connectionId, string tenantId = "", string externalUserId = "", CancellationToken cancellationToken = default)
        {
            // search pattern key
            string searchKey = BuildRemoveKey(connectionId, tenantId, externalUserId);
            List<string> results = (await this.distributedCache.FindKeysAsync(searchKey, cancellationToken)).ToList();

            foreach (string key in results)
            {
                await this.distributedCache.RemoveAsync(key, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// This method is used to build the key to remove from the connections tracking.
        /// </summary>
        /// <param name="connectionId">Contains the connection identity.</param>
        /// <param name="tenantId">Contains the tenant identity.</param>
        /// <param name="externalUserId">Contains the external user identity.</param>
        /// <returns>Returns the combined key to find and remove.</returns>
        private static string BuildRemoveKey(string connectionId, string tenantId, string externalUserId)
        {
            // search pattern key
            string searchKey = "notification_clients:";

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                searchKey += tenantId + ":";
            }
            else
            {
                searchKey += "*:";
            }

            if (!string.IsNullOrWhiteSpace(externalUserId))
            {
                searchKey += externalUserId + ":";
            }
            else
            {
                searchKey += "*:";
            }

            searchKey += connectionId;

            return searchKey;
        }
    }
}