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
    using Bastille.Id.Core.Security;
    using Bastille.Id.Models.Security;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Talegen.Common.Messaging.Senders;
    using Talegen.Common.Models.Security.Queries;
    using Vasont.AspnetCore.RedisClient;

    /// <summary>
    /// This controller class handles all Identity Server client management related calls.
    /// </summary>
    [Authorize(Roles = SecurityDefaults.AdministratorRoleName)]
    [Route("[controller]")]
    public class UsersController : ApiControllerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UsersController" /> class.
        /// </summary>
        /// <param name="appContext">The application context.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="distributedCache">The distributed cache.</param>
        /// <param name="clientStore">Contains an instance of the client store.</param>
        /// <param name="resourceStore">Contains an instance of the resource store.</param>
        /// <param name="emailSender">The email sender.</param>
        /// <param name="logger">The logger.</param>
        public UsersController(ApplicationContext<ApplicationSettings> appContext,
            UserManager<User> userManager, IAdvancedDistributedCache distributedCache, IMessageSender emailSender,
            ILogger<UsersController> logger)
            : base(appContext, userManager, distributedCache, emailSender, logger)
        {
        }

        /// <summary>
        /// Gets all available users
        /// </summary>
        /// <param name="model">Contains the query filter model.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns the action result for the API request.</returns>
        [HttpGet]
        public async Task<IActionResult> Get(UserQueryFilterModel model, CancellationToken cancellationToken)
        {
            return this.SuccessOrFailResult(await this.UserService.BrowseAsync(model, cancellationToken));
        }

        /// <summary>
        /// Gets a specific user
        /// </summary>
        /// <param name="id">Contains the user identity.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns an action success result.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
        {
            return this.SuccessOrFailResult(await this.UserService.ReadUserAsync(id, cancellationToken));
        }

        /// <summary>
        /// Post a new user to the database.
        /// </summary>
        /// <param name="model">Contains the user model containing new data.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns an action success result.</returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] BastilleUserModel model, CancellationToken cancellationToken)
        {
            IdentityResult result = null;

            if (model != null && this.ModelState.IsValid)
            {
                await this.UserService.CreateNewUserAsync(model, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(result);
        }

        /// <summary>
        /// Put an updated user into the database.
        /// </summary>
        /// <param name="id">Contains the user identity to update.</param>
        /// <param name="model">Contains the user model containing changes.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns an action success result.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] BastilleUserModel model, CancellationToken cancellationToken)
        {
            if (model != null && this.ModelState.IsValid)
            {
                // Force set the Id of the model to update for consistency
                model.UserId = id;
                model = await this.UserService.UpdateUserAsync(model, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(model);
        }

        /// <summary>
        /// Deletes a specific user.
        /// </summary>
        /// <param name="id">Contains the user identity.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns an action success result.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await this.UserService.DeleteUserAsync(id, cancellationToken);
            return this.SuccessOrFailResult();
        }
    }
}