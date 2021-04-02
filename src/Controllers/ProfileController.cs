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
    using Bastille.Id.Models.Security;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Talegen.Common.Messaging.Senders;
    using Vasont.AspnetCore.RedisClient;

    /// <summary>
    /// This controller class handles all profile related calls.
    /// </summary>
    [Route("[controller]")]
    public class ProfileController : ApiControllerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileController" /> class.
        /// </summary>
        /// <param name="appContext">The application context.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="distributedCache">The distributed cache.</param>
        /// <param name="emailSender">The email sender.</param>
        /// <param name="logger">The logger.</param>
        public ProfileController(ApplicationContext<ApplicationSettings> appContext,
            UserManager<User> userManager, IAdvancedDistributedCache distributedCache, IMessageSender emailSender,
            ILogger<ProfileController> logger)
            : base(appContext, userManager, distributedCache, emailSender, logger)
        {
        }

        /// <summary>
        /// Gets a specific user
        /// </summary>
        /// <param name="id">Contains the user identity.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns an action success result.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        {
            return this.SuccessOrFailResult(await this.UserService.FindUserProfileModelAsync(id, cancellationToken));
        }

        /// <summary>
        /// Put an updated user into the database.
        /// </summary>
        /// <param name="id">Contains the user identity to update.</param>
        /// <param name="model">Contains the user model containing changes.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns an action success result.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] ProfileModel model, CancellationToken cancellationToken)
        {
            if (model != null && this.ModelState.IsValid)
            {
                // Force set the Id of the model to update for consistency
                model.UserId = id;
                await this.UserService.UpdateProfileAsync(model, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(model);
        }

        /// <summary>
        /// Puts the change password.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="model">The model.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        [HttpPut("{id}/Password")]
        public async Task<IActionResult> PutChangePassword(Guid id, [FromBody] ChangePasswordModel model, CancellationToken cancellationToken)
        {
            if (model != null && this.ModelState.IsValid)
            {
                await this.UserService.ChangeProfilePasswordAsync(id, model, cancellationToken);
            }
            else
            {
                this.AddModelErrors();
            }

            return this.SuccessOrFailResult(model);
        }
    }
}