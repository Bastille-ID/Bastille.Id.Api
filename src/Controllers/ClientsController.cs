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
        /// Initializes a new instance of the <see cref="ClientsController" /> class.
        /// </summary>
        /// <param name="appContext">The application context.</param>
        /// <param name="configurationDbContext">Contains the configuration database context.</param>
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
        /// <param name="model">The model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPost("{id}/Scopes")]
        public async Task<IActionResult> PostScope(int id, [FromBody] ClientScopeModel model, CancellationToken cancellationToken)
        {
            ClientScopeModel result = null;

            if (model != null && this.ModelState.IsValid)
            {
                result = await this.Service.AddScopeAsync(model, cancellationToken);
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
        /// <param name="model">The model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPut("{id}/Scopes/{scopeId}")]
        public async Task<IActionResult> PutScope(int id, int scopeId, [FromBody] ClientScopeModel model, CancellationToken cancellationToken)
        {
            ClientScopeModel scopeToUpdate = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.Id = scopeId;
                model.ClientId = id;
                scopeToUpdate = await this.Service.UpdateScopeAsync(model, cancellationToken);
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
        public async Task<IActionResult> PostSecret(int id, [FromBody] ClientSecretModel model, CancellationToken cancellationToken)
        {
            ClientSecretModel result = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.ClientId = id;
                result = await this.Service.AddSecretAsync(model, cancellationToken);
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
        public async Task<IActionResult> PutSecret(int id, int secretId, [FromBody] ClientSecretModel model, CancellationToken cancellationToken)
        {
            ClientSecretModel secretToUpdate = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.Id = secretId;
                model.ClientId = id;
                secretToUpdate = await this.Service.UpdateSecretAsync(model, cancellationToken);
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
        /// <param name="model">The redirect model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPost("{id}/Redirects")]
        public async Task<IActionResult> PostRedirect(int id, [FromBody] ClientRedirectUriModel model, CancellationToken cancellationToken)
        {
            ClientRedirectUriModel result = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.ClientId = id;
                result = await this.Service.AddRedirectAsync(model, cancellationToken);
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
        public async Task<IActionResult> PutRedirect(int id, int redirectId, [FromBody] ClientRedirectUriModel model, CancellationToken cancellationToken)
        {
            ClientRedirectUriModel redirectToUpdate = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.ClientId = id;
                model.Id = redirectId;
                redirectToUpdate = await this.Service.UpdateRedirectAsync(model, cancellationToken);
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
        /// <param name="model">The origin model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPost("{id}/Origins")]
        public async Task<IActionResult> PostOrigin(int id, [FromBody] ClientCorsOriginModel model, CancellationToken cancellationToken)
        {
            ClientCorsOriginModel result = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.ClientId = id;
                result = await this.Service.AddOriginAsync(model, cancellationToken);
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
        /// <param name="model">The origin model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPut("{id}/Origins/{originId}")]
        public async Task<IActionResult> PutOrigin(int id, int originId, [FromBody] ClientCorsOriginModel model, CancellationToken cancellationToken)
        {
            ClientCorsOriginModel originToUpdate = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.Id = originId;
                model.ClientId = id;
                originToUpdate = await this.Service.UpdateOriginAsync(model, cancellationToken);
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
        /// <param name="model">The grant model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPost("{id}/GrantTypes")]
        public async Task<IActionResult> PostGrantType(int id, [FromBody] ClientGrantTypeModel model, CancellationToken cancellationToken)
        {
            ClientGrantTypeModel result = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.ClientId = id;
                result = await this.Service.AddGrantTypeAsync(model, cancellationToken);
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
        public async Task<IActionResult> PutGrantType(int id, int grantTypeId, [FromBody] ClientGrantTypeModel model, CancellationToken cancellationToken)
        {
            ClientGrantTypeModel grantTypeToUpdate = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.ClientId = id;
                model.Id = grantTypeId;
                grantTypeToUpdate = await this.Service.UpdateGrantTypeAsync(model, cancellationToken);
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
        /// <param name="model">The logout URI model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPost("{id}/Logouts")]
        public async Task<IActionResult> PostLogout(int id, [FromBody] ClientLogoutRedirectUriModel model, CancellationToken cancellationToken)
        {
            ClientLogoutRedirectUriModel result = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.ClientId = id;
                result = await this.Service.AddPostLogoutRedirectAsync(model, cancellationToken);
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
        public async Task<IActionResult> PutLogout(int id, int logoutId, [FromBody] ClientLogoutRedirectUriModel model, CancellationToken cancellationToken)
        {
            ClientLogoutRedirectUriModel logoutToUpdate = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.ClientId = id;
                model.Id = logoutId;
                logoutToUpdate = await this.Service.UpdatePostLogoutRedirectTypeAsync(model, cancellationToken);
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
        public async Task<IActionResult> PostProperty(int id, [FromBody] ClientPropertyModel model, CancellationToken cancellationToken)
        {
            ClientPropertyModel result = null;

            if (model != null && this.ModelState.IsValid)
            {
                result = await this.Service.AddPropertyAsync(model, cancellationToken);
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
        public async Task<IActionResult> PutProperty(int id, int propertyId, [FromBody] ClientPropertyModel model, CancellationToken cancellationToken)
        {
            ClientPropertyModel propertyToUpdate = null;

            if (model != null && this.ModelState.IsValid)
            {
                model.ClientId = id;
                model.Id = propertyId;
                propertyToUpdate = await this.Service.UpdatePropertyAsync(model, cancellationToken);
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