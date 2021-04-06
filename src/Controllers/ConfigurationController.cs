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

namespace Bastille.Id.Api.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Bastille.Id.Api.Common.Configuration;
    using Bastille.Id.Api.Common.Controllers;
    using Bastille.Id.Core;
    using Bastille.Id.Core.Data.Entities;
    using Bastille.Id.Core.Identity;
    using Bastille.Id.Models.Clients;
    using IdentityServer4.EntityFramework.DbContexts;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Talegen.Common.Messaging.Senders;
    using Vasont.AspnetCore.RedisClient;

    /// <summary>
    /// This class contains the configuration loader for clients using the resource.
    /// </summary>
    /// <seealso cref="Bastille.Id.Api.Common.Controllers.ApiControllerBase" />
    [Route("[controller]")]
    public class ConfigurationController : ApiControllerBase
    {
        #region Private Fields

        /// <summary>
        /// The client service
        /// </summary>
        private readonly Lazy<ClientService> clientService;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationController" /> class.
        /// </summary>
        /// <param name="appContext">The application context.</param>
        /// <param name="configurationDbContext"></param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="distributedCache">The distributed cache.</param>
        /// <param name="emailSender">The email sender.</param>
        /// <param name="logger">The logger.</param>
        public ConfigurationController(ApplicationContext<ApplicationSettings> appContext, ConfigurationDbContext configurationDbContext,
            UserManager<User> userManager, IAdvancedDistributedCache distributedCache, IMessageSender emailSender, ILogger<ClientsController> logger)
            : base(appContext, userManager, distributedCache, emailSender, logger)
        {
            this.clientService = new Lazy<ClientService>(() => new ClientService(new ClientServiceContext
            {
                Cache = distributedCache,
                ConfigurationDbContext = configurationDbContext,
                DataContext = appContext.DataContext,
                ErrorManager = appContext.ErrorManager,
                HttpContext = this.HttpContext,
                Principal = this.User,
                UserManager = this.UserManager
            }));
        }

        /// <summary>
        /// Gets the service.
        /// </summary>
        /// <value>The service.</value>
        public ClientService Service => this.clientService.Value;

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpGet("{clientId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetConfiguration(string clientId, CancellationToken cancellationToken)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();

            ClientModel client = await this.Service.ReadClientIdAsync(clientId, cancellationToken);

            if (client != null)
            {
                results.Add("authority", this.AppContext.Settings.IdentityProvider.AuthorityUri.ToString());
                results.Add("client_id", client.ClientId);
                results.Add("redirect_uri", client.RedirectUris.FirstOrDefault());

                if (client.AllowedGrantTypes.Contains("implicit"))
                {
                    results.Add("response_type", "id_token token");
                }
                else if (client.AllowedGrantTypes.Contains("hybrid"))
                {
                    results.Add("response_type", "code id_token");
                }
                else if (client.AllowedGrantTypes.Contains("code"))
                {
                    results.Add("response_type", "code");
                }

                results.Add("scope", client.AllowedScopes.Aggregate(string.Empty, (first, next) => first + " " + next));
                results.Add("post_logout_redirect_uri", client.PostLogoutRedirectUris.FirstOrDefault());
            }

            return this.SuccessOrFailResult(results);
        }
    }
}