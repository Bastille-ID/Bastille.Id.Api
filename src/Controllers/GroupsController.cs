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

namespace Bastille.Id.Server.Controllers.Api
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
    using Bastille.Id.Models.Security;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Talegen.Common.Messaging.Senders;
    using Vasont.AspnetCore.RedisClient;

    /// <summary>
    /// This controller class handles all client related calls.
    /// </summary>
    [Authorize(Roles = SecurityDefaults.AdministratorRoleName)]
    [Route("[controller]")]
    public class GroupsController : ApiControllerBase
    {
        #region Private Fields

        /// <summary>
        /// The group service
        /// </summary>
        private readonly Lazy<GroupService> groupService;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersController" /> class.
        /// </summary>
        /// <param name="appContext">The application context.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="distributedCache">The distributed cache.</param>
        /// <param name="emailSender">The email sender.</param>
        /// <param name="logger">The logger.</param>
        public GroupsController(ApplicationContext<ApplicationSettings> appContext,
            UserManager<User> userManager, IAdvancedDistributedCache distributedCache, IMessageSender emailSender, ILogger<LogsController> logger)
            : base(appContext, userManager, distributedCache, emailSender, logger)
        {
            this.groupService = new Lazy<GroupService>(() => new GroupService(this.AppContext.DataContext, this.AppContext.ErrorManager));
        }

        /// <summary>
        /// Gets the service.
        /// </summary>
        /// <value>The service.</value>
        public GroupService Service => this.groupService.Value;

        /// <summary>
        /// Gets all available active groups.
        /// </summary>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns the action result for the API request.</returns>
        [HttpGet]
        public async Task<IActionResult> Get(GroupQueryFilterModel filter, CancellationToken cancellationToken)
        {
            return this.SuccessOrFailResult(await this.Service.BrowseAsync(filter, cancellationToken));
        }

        /// <summary>
        /// Gets a specific group
        /// </summary>
        /// <param name="id">Contains the group identity.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns an action success result.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGroup(Guid id, CancellationToken cancellationToken)
        {
            return this.SuccessOrFailResult(await this.Service.ReadModelAsync(id, cancellationToken));
        }

        /// <summary>
        /// This method is used to get all users for the specified group.
        /// </summary>
        /// <param name="id">Contains the unique identity of the group.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns the action result.</returns>
        [HttpGet("{id}/Users")]
        public async Task<IActionResult> GetGroupUsers(Guid id, CancellationToken cancellationToken)
        {
            return this.SuccessOrFailResult(await this.Service.ReadUsersAsync(id, cancellationToken));
        }

        /// <summary>
        /// This method is used to get all managers for the specified organization.
        /// </summary>
        /// <param name="id">Contains the unique identity of the organization.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns the action result.</returns>
        [HttpGet("{id}/Owner")]
        public async Task<IActionResult> GetOwner(Guid id, CancellationToken cancellationToken)
        {
            return this.SuccessOrFailResult(await this.Service.ReadOwnerAsync(id, cancellationToken));
        }

        /// <summary>
        /// Post a new organization to the database.
        /// </summary>
        /// <param name="model">Contains the organization model containing new data.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns an action success result.</returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] GroupModel model, CancellationToken cancellationToken)
        {
            if (model != null && this.ModelState.IsValid)
            {
                model = await this.Service.CreateAsync(model, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(model, "Groups", model.GroupId);
        }

        /// <summary>
        /// Put an updated organization into the database.
        /// </summary>
        /// <param name="id">Contains the organization identity to update.</param>
        /// <param name="model">Contains the organization model containing changes.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns an action success result.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] GroupModel model, CancellationToken cancellationToken)
        {
            if (model != null && this.ModelState.IsValid && id != Guid.Empty)
            {
                model.GroupId = id;
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