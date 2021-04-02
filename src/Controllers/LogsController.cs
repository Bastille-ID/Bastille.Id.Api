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
    using System.Threading;
    using System.Threading.Tasks;
    using Bastille.Id.Api.Common.Configuration;
    using Bastille.Id.Api.Common.Controllers;
    using Bastille.Id.Core;
    using Bastille.Id.Core.Data.Entities;
    using Bastille.Id.Models.Logging;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Talegen.Common.Messaging.Senders;
    using Vasont.AspnetCore.RedisClient;

    /// <summary>
    /// This controller class handles all client related calls.
    /// </summary>
    [Route("[controller]")]
    public class LogsController : ApiControllerBase
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
        public LogsController(ApplicationContext<ApplicationSettings> appContext,
            UserManager<User> userManager, IAdvancedDistributedCache distributedCache, IMessageSender emailSender, ILogger<LogsController> logger)
            : base(appContext, userManager, distributedCache, emailSender, logger)
        {
        }

        /// <summary>
        /// Gets all available clients
        /// </summary>
        /// <param name="model">Contains the query model.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns the action result for the API request.</returns>
        [HttpGet]
        public async Task<IActionResult> Get(AuditLogQueryFilterModel model, CancellationToken cancellationToken = default)
        {
            return this.SuccessOrFailResult(await this.AuditLog.ReadAuditLogsAsync(model, cancellationToken));
        }
    }
}