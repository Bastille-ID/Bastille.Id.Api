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
    using Bastille.Id.Core.Organization;
    using Bastille.Id.Core.Security;
    using Bastille.Id.Models.Organization;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Talegen.Common.Messaging.Senders;
    using Vasont.AspnetCore.RedisClient;

    /// <summary>
    /// This controller class handles all organization and group related calls.
    /// </summary>
    [Authorize(Roles = SecurityDefaults.AdministratorRoleName)]
    [Route("[controller]")]
    public class OrganizationsController : ApiControllerBase
    {
        #region Private Fields

        /// <summary>
        /// The group service
        /// </summary>
        private readonly Lazy<OrganizationService> orgService;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationsController" /> class.
        /// </summary>
        /// <param name="appContext">The application context.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="distributedCache">The distributed cache.</param>
        /// <param name="emailSender">The email sender.</param>
        /// <param name="logger">The logger.</param>
        public OrganizationsController(ApplicationContext<ApplicationSettings> appContext, UserManager<User> userManager,
            IAdvancedDistributedCache distributedCache, IMessageSender emailSender, ILogger<OrganizationsController> logger)
            : base(appContext, userManager, distributedCache, emailSender, logger)
        {
            this.orgService = new Lazy<OrganizationService>(() => new OrganizationService(this.AppContext.DataContext, this.AppContext.ErrorManager));
        }

        /// <summary>
        /// Gets the service.
        /// </summary>
        /// <value>The service.</value>
        public OrganizationService Service => this.orgService.Value;

        /// <summary>
        /// Gets all available organizations
        /// </summary>
        /// <param name="filter">Contains the query filter model.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns the action result for the API request.</returns>
        [HttpGet]
        public async Task<IActionResult> Get(OrganizationQueryFilterModel filter, CancellationToken cancellationToken)
        {
            if (this.ModelState.IsValid)
            {
                await this.Service.BrowseAsync(filter, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult();
        }

        /// <summary>
        /// Retrieves all the details about a specific organization.
        /// </summary>
        /// <param name="id">Contains the unique identity of the organization to retrieve.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns the action result for the API request.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        {
            return this.SuccessOrFailResult(await this.Service.ReadAsync(id, cancellationToken));
        }

        /// <summary>
        /// Post a new organization to the database.
        /// </summary>
        /// <param name="model">Contains the client model containing new data.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns an action success result.</returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] OrganizationModel model, CancellationToken cancellationToken)
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
        /// Put an updated organization into the database.
        /// </summary>
        /// <param name="id">Contains the client identity to update.</param>
        /// <param name="model">Contains the client model containing changes.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns an action success result.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] OrganizationModel model, CancellationToken cancellationToken)
        {
            if (model != null && this.ModelState.IsValid)
            {
                model.OrganizationId = id;
                model = await this.Service.UpdateAsync(model, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(model);
        }

        /// <summary>
        /// Delete a specified organization from the database.
        /// </summary>
        /// <param name="id">Contains the organization identity to update.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns an action success result.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await this.Service.DeleteAsync(id, cancellationToken);
            return this.SuccessOrFailResult();
        }
    }
}