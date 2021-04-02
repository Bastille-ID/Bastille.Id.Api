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
    using System.Threading;
    using System.Threading.Tasks;
    using Bastille.Id.Api.Common.Configuration;
    using Bastille.Id.Api.Common.Controllers;
    using Bastille.Id.Core;
    using Bastille.Id.Core.Data.Entities;
    using Bastille.Id.Core.Identity;
    using Bastille.Id.Core.Security;
    using Bastille.Id.Models.Clients;
    using IdentityServer4.EntityFramework.DbContexts;
    using IdentityServer4.EntityFramework.Entities;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Talegen.Common.Messaging.Senders;
    using Vasont.AspnetCore.RedisClient;

    /// <summary>
    /// This controller class handles all Identity Server client management related calls.
    /// </summary>
    [Authorize(Roles = SecurityDefaults.AdministratorRoleName)]
    [Route("[controller]")]
    public class ClientsController : ApiControllerBase
    {
        #region Private Fields

        /// <summary>
        /// The client service
        /// </summary>
        private readonly Lazy<ClientService> clientService;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersController" /> class.
        /// </summary>
        /// <param name="appContext">The application context.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="distributedCache">The distributed cache.</param>
        /// <param name="emailSender">The email sender.</param>
        /// <param name="logger">The logger.</param>
        public ClientsController(ApplicationContext<ApplicationSettings> appContext, ConfigurationDbContext configurationDbContext,
            UserManager<User> userManager, IAdvancedDistributedCache distributedCache, IMessageSender emailSender,
            ILogger<ClientsController> logger)
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
        /// Gets all available clients
        /// </summary>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns the action result for the API request.</returns>
        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var results = await this.Service.ReadClientsAsync(cancellationToken);

            return this.SuccessOrFailResult(results);
        }

        /// <summary>
        /// Retrieves all the details about a specific client.
        /// </summary>
        /// <param name="id">Contains the unique identity of the client to retrieve.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns the action result for the API request.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
        {
            return this.SuccessOrFailResult(await this.Service.ReadAsync(id, cancellationToken));
        }

        /// <summary>
        /// Post a new client to the database.
        /// </summary>
        /// <param name="model">Contains the client model containing new data.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns an action success result.</returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ClientModel model, CancellationToken cancellationToken)
        {
            if (model != null && this.ModelState.IsValid)
            {
                model = await this.Service.CreateAsync(model, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(model);
        }

        /// <summary>
        /// Put an updated client into the database.
        /// </summary>
        /// <param name="id">Contains the client identity to update.</param>
        /// <param name="model">Contains the client model containing changes.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns an action success result.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] ClientModel model, CancellationToken cancellationToken)
        {
            if (model != null && this.ModelState.IsValid)
            {
                model.Id = id;
                model = await this.Service.UpdateAsync(model, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(model);
        }

        /// <summary>
        /// Delete a specified client from the database.
        /// </summary>
        /// <param name="id">Contains the client identity to update.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns an action success result.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await this.Service.DeleteAsync(id, cancellationToken);
            return this.SuccessOrFailResult();
        }

        /// <summary>
        /// Creates a new scope for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="scope">The scope.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPost("{id}/Scopes")]
        public async Task<IActionResult> PostScope(int id, [FromBody] string scope, CancellationToken cancellationToken)
        {
            ClientScope result = null;

            if (!string.IsNullOrWhiteSpace(scope) && this.ModelState.IsValid)
            {
                result = await this.Service.AddScopeAsync(id, scope, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(result);
        }

        /// <summary>
        /// Updates a scope for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="scopeId">The scope identifier.</param>
        /// <param name="scope">The scope.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPut("{id}/Scopes/{scopeId}")]
        public async Task<IActionResult> PutScope(int id, int scopeId, [FromBody] string scope, CancellationToken cancellationToken)
        {
            ClientScope scopeToUpdate = null;

            if (scope != null && this.ModelState.IsValid)
            {
                scopeToUpdate = await this.Service.UpdateScopeAsync(id, scopeId, scope, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(scopeToUpdate);
        }

        /// <summary>
        /// Deletes a scope for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="scopeId">The scope identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpDelete("{id}/Scopes/{scopeId}")]
        public async Task<IActionResult> DeleteScope(int id, int scopeId, CancellationToken cancellationToken)
        {
            if (this.ModelState.IsValid)
            {
                await this.Service.DeleteScopeAsync(id, scopeId, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult();
        }

        /// <summary>
        /// Creates a new client secret for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="model">The model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPost("{id}/Secrets")]
        public async Task<IActionResult> PostSecret(int id, [FromBody] ClientSecret model, CancellationToken cancellationToken)
        {
            ClientSecret result = null;

            if (model != null && this.ModelState.IsValid)
            {
                result = await this.Service.AddSecretAsync(id, model, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(result);
        }

        /// <summary>
        /// Updates a secret for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="secretId">The secret identifier.</param>
        /// <param name="model">The model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPut("{id}/Secrets/{secretId}")]
        public async Task<IActionResult> PutSecret(int id, int secretId, [FromBody] ClientSecret model, CancellationToken cancellationToken)
        {
            ClientSecret secretToUpdate = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.Id = secretId;
                secretToUpdate = await this.Service.UpdateSecretAsync(id, model, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(secretToUpdate);
        }

        /// <summary>
        /// Deletes a secret for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="secretId">The secret identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpDelete("{id}/Secrets/{secretId}")]
        public async Task<IActionResult> DeleteSecret(int id, int secretId, CancellationToken cancellationToken)
        {
            if (this.ModelState.IsValid)
            {
                await this.Service.DeleteSecretAsync(id, secretId, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult();
        }

        /// <summary>
        /// Creates a redirect for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="redirectUri">The redirect URI.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPost("{id}/Redirects")]
        public async Task<IActionResult> PostRedirect(int id, [FromBody] string redirectUri, CancellationToken cancellationToken)
        {
            ClientRedirectUri result = null;

            if (!string.IsNullOrWhiteSpace(redirectUri) && this.ModelState.IsValid)
            {
                result = await this.Service.AddRedirectAsync(id, redirectUri, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(result);
        }

        /// <summary>
        /// Updates a redirect for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="redirectId">The redirect identifier.</param>
        /// <param name="model">The redirect model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPut("{id}/Redirects/{redirectId}")]
        public async Task<IActionResult> PutRedirect(int id, int redirectId, [FromBody] ClientRedirectUri model, CancellationToken cancellationToken)
        {
            ClientRedirectUri redirectToUpdate = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.Id = redirectId;
                redirectToUpdate = await this.Service.UpdateRedirectAsync(id, model, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(redirectToUpdate);
        }

        /// <summary>
        /// Deletes a redirect for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="redirectId">The redirect identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpDelete("{id}/Redirects/{redirectId}")]
        public async Task<IActionResult> DeleteRedirect(int id, int redirectId, CancellationToken cancellationToken)
        {
            if (this.ModelState.IsValid)
            {
                await this.Service.DeleteRedirectAsync(id, redirectId, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult();
        }

        /// <summary>
        /// Creates a new allowed origin for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="originUri">The origin URI.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPost("{id}/Origins")]
        public async Task<IActionResult> PostOrigin(int id, [FromBody] string originUri, CancellationToken cancellationToken)
        {
            ClientCorsOrigin result = null;

            if (!string.IsNullOrWhiteSpace(originUri) && this.ModelState.IsValid)
            {
                result = await this.Service.AddOriginAsync(id, originUri, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(result);
        }

        /// <summary>
        /// Updates an allowed origin for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="originId">The origin identifier.</param>
        /// <param name="originUri">The origin URI.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPut("{id}/Origins/{originId}")]
        public async Task<IActionResult> PutOrigin(int id, int originId, [FromBody] ClientCorsOrigin model, CancellationToken cancellationToken)
        {
            ClientCorsOrigin originToUpdate = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.Id = originId;
                originToUpdate = await this.Service.UpdateOriginAsync(id, model, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(originToUpdate);
        }

        /// <summary>
        /// Deletes an allowed origin for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="originId">The origin identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpDelete("{id}/Origins/{originId}")]
        public async Task<IActionResult> DeleteOrigin(int id, int originId, CancellationToken cancellationToken)
        {
            if (this.ModelState.IsValid)
            {
                await this.Service.DeleteOriginAsync(id, originId, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult();
        }

        /// <summary>
        /// Creates a new Grant Type for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="grantType">Type of the grant.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPost("{id}/GrantTypes")]
        public async Task<IActionResult> PostGrantType(int id, [FromBody] string grantType, CancellationToken cancellationToken)
        {
            ClientGrantType result = null;

            if (!string.IsNullOrWhiteSpace(grantType) && this.ModelState.IsValid)
            {
                result = await this.Service.AddGrantTypeAsync(id, grantType, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(result);
        }

        /// <summary>
        /// Updates a Grant Type for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="grantTypeId">The grant type identifier.</param>
        /// <param name="model">Model of the grant.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPut("{id}/GrantTypes/{grantTypeId}")]
        public async Task<IActionResult> PutGrantType(int id, int grantTypeId, [FromBody] ClientGrantType model, CancellationToken cancellationToken)
        {
            ClientGrantType grantTypeToUpdate = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.Id = grantTypeId;
                grantTypeToUpdate = await this.Service.UpdateGrantTypeAsync(id, model, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(grantTypeToUpdate);
        }

        /// <summary>
        /// Deletes a Grant Type for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="grantTypeId">The grant type identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpDelete("{id}/GrantTypes/{grantTypeId}")]
        public async Task<IActionResult> DeleteGrantType(int id, int grantTypeId, CancellationToken cancellationToken)
        {
            if (this.ModelState.IsValid)
            {
                await this.Service.DeleteGrantTypeAsync(id, grantTypeId, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult();
        }

        /// <summary>
        /// Creates a new Logout Redirect for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="logoutUri">The logout URI.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPost("{id}/Logouts")]
        public async Task<IActionResult> PostLogout(int id, [FromBody] string logoutUri, CancellationToken cancellationToken)
        {
            ClientPostLogoutRedirectUri result = null;

            if (!string.IsNullOrWhiteSpace(logoutUri) && this.ModelState.IsValid)
            {
                result = await this.Service.AddPostLogoutRedirectAsync(id, logoutUri, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(result);
        }

        /// <summary>
        /// Updates a Logout Redirect for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="logoutId">The logout identifier.</param>
        /// <param name="model">The logout model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPut("{id}/Logouts/{logoutId}")]
        public async Task<IActionResult> PutLogout(int id, int logoutId, [FromBody] ClientPostLogoutRedirectUri model, CancellationToken cancellationToken)
        {
            ClientPostLogoutRedirectUri logoutToUpdate = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.Id = logoutId;
                logoutToUpdate = await this.Service.UpdatePostLogoutRedirectTypeAsync(id, model, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(logoutToUpdate);
        }

        /// <summary>
        /// Updates a Logout Redirect for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="logoutId">The logout identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpDelete("{id}/Logouts/{logoutId}")]
        public async Task<IActionResult> DeleteLogout(int id, int logoutId, CancellationToken cancellationToken)
        {
            if (this.ModelState.IsValid)
            {
                await this.Service.DeletePostLogoutRedirectTypeAsync(id, logoutId, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult();
        }

        /// <summary>
        /// Creates a new Property for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="model">The model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPost("{id}/Properties")]
        public async Task<IActionResult> PostProperty(int id, [FromBody] ClientProperty model, CancellationToken cancellationToken)
        {
            ClientProperty result = null;

            if (model != null && this.ModelState.IsValid)
            {
                result = await this.Service.AddPropertyAsync(id, model, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(result);
        }

        /// <summary>
        /// Updates a Property for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="propertyId">The property identifier.</param>
        /// <param name="model">The model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPut("{id}/Properties/{propertyId}")]
        public async Task<IActionResult> PutProperty(int id, int propertyId, [FromBody] ClientProperty model, CancellationToken cancellationToken)
        {
            ClientProperty propertyToUpdate = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.Id = propertyId;
                propertyToUpdate = await this.Service.UpdatePropertyAsync(id, model, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(propertyToUpdate);
        }

        /// <summary>
        /// Deletes a Property for the specified client.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="propertyId">The property identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpDelete("{id}/Properties/{propertyId}")]
        public async Task<IActionResult> DeleteProperty(int id, int propertyId, CancellationToken cancellationToken)
        {
            if (this.ModelState.IsValid)
            {
                await this.Service.DeletePropertyAsync(id, propertyId, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult();
        }
    }
}