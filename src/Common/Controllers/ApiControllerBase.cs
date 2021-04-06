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

namespace Bastille.Id.Api.Common.Controllers
{
    using System;
    using System.Linq;
    using System.Net;
    using Bastille.Id.Server.Controllers.Api.Models;
    using Bastille.Id.Core.Data.Entities;
    using Bastille.Id.Core.Extensions;
    using Bastille.Id.Core.Logging;
    using Bastille.Id.Core.Security;
    using IdentityModel;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Vasont.AspnetCore.RedisClient;
    using Bastille.Id.Core;
    using Bastille.Id.Api.Common.Configuration;
    using Talegen.Common.Messaging.Senders;
    using IdentityServer4.Stores;
    using Talegen.Common.Core.Errors;
    using Talegen.Common.Models.Shared;

    /// <summary>
    /// This class contains enhancements to the controller base for API controllers.
    /// </summary>
    public abstract class ApiControllerBase : ControllerBase
    {
        #region Private Fields

        /// <summary>
        /// Contains a lazy loaded instance of the security service.
        /// </summary>
        private readonly Lazy<SecurityService> securityService;

        /// <summary>
        /// Contains an instance of the security log service.
        /// </summary>
        private readonly Lazy<AuditLogService> auditLogService;

        /// <summary>
        /// Contains the user services.
        /// </summary>
        private readonly Lazy<UserService> userService;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiControllerBase" /> class.
        /// </summary>
        /// <param name="appContext">The application context.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="cache">The distributed cache.</param>
        /// <param name="messageSender">The email sender.</param>
        /// <param name="logger">The logger.</param>
        protected ApiControllerBase(ApplicationContext<ApplicationSettings> appContext, UserManager<User> userManager,
            IAdvancedDistributedCache cache, IMessageSender messageSender, ILogger<ApiControllerBase> logger)
        {
            this.AppContext = appContext;
            this.UserManager = userManager;
            this.Cache = cache;
            this.Messaging = messageSender;
            this.Logger = logger;
            this.auditLogService = new Lazy<AuditLogService>(new AuditLogService(this.AppContext.DataContext));

            // prep the security service
            this.securityService = new Lazy<SecurityService>(new SecurityService(new SecurityServiceContext
            {
                DataContext = appContext.DataContext,
                Cache = cache,
                ErrorManager = appContext.ErrorManager,
                HttpContext = this.HttpContext,
                Principal = this.User,
                UserManager = this.UserManager
            }));

            // prep the user service
            this.userService = new Lazy<UserService>(new UserService(new UserServiceContext
            {
                Cache = cache,
                DataContext = appContext.DataContext,
                ErrorManager = appContext.ErrorManager,
                HttpContext = this.HttpContext,
                Principal = this.User,
                SecurityService = this.Security,
                UserManager = this.UserManager,
                AuditLog = this.AuditLog
            }));
        }

        /// <summary>
        /// Gets the application context.
        /// </summary>
        public ApplicationContext<ApplicationSettings> AppContext { get; }

        /// <summary>
        /// Gets the user manager.
        /// </summary>
        /// <value>The user manager.</value>
        public UserManager<User> UserManager { get; }

        /// <summary>
        /// Gets the cache.
        /// </summary>
        /// <value>The cache.</value>
        public IAdvancedDistributedCache Cache { get; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        public ILogger<ApiControllerBase> Logger { get; }

        /// <summary>
        /// Gets the messaging.
        /// </summary>
        /// <value>The messaging.</value>
        public IMessageSender Messaging { get; }

        /// <summary>
        /// Gets an instance of the security service.
        /// </summary>
        public SecurityService Security => this.securityService.Value;

        /// <summary>
        /// Gets the user service.
        /// </summary>
        /// <value>The user service.</value>
        public UserService UserService => this.userService.Value;

        /// <summary>
        /// Gets a lazy-loaded instance of the security log service.
        /// </summary>
        public AuditLogService AuditLog => this.auditLogService.Value;

        /// <summary>
        /// Overload the existing NotFound with a response that returns an action error type.
        /// </summary>
        /// <returns>Returns an error result containing a 404 message code.</returns>
        protected new IActionResult NotFound()
        {
            return this.CreateErrorResult(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// This method is used to return either an empty success (200) IHttpActionResult if no critical or validation errors are found within the context error
        /// manager class. If errors are found the method returns a bad request (400) response with a <see cref="ErrorResponseModel" /> result that overrides
        /// the success model.
        /// </summary>
        /// <returns>Returns either a empty OK result upon success or returns a bad request with an <see cref="ErrorResponseModel" /> result.</returns>
        protected IActionResult SuccessOrFailResult()
        {
            return this.SuccessOrFailResult<object>(null);
        }

        /// <summary>
        /// Adds the model state errors to the error manager.
        /// </summary>
        protected void AddModelErrors()
        {
            this.ModelState.Where(ms => ms.Value.ValidationState == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid).ToList().ForEach(ms =>
            {
                ms.Value.Errors.ToList().ForEach(err =>
                {
                    this.AppContext.ErrorManager.Validation(ms.Key, err.ErrorMessage, ErrorCategory.General);
                });
            });
        }

        /// <summary>
        /// This method is used to add identity result errors to the model error manager.
        /// </summary>
        /// <param name="result">Contains the identity result that contains errors.</param>
        protected void AddErrors(IdentityResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            foreach (var error in result.Errors)
            {
                this.ModelState.AddModelError(string.Empty, error.Description);
            }

            this.AddModelErrors();
        }

        /// <summary>
        /// This method is used to return either a successful (200) IHttpActionResult if no critical or validation errors are found within the context error
        /// manager class. If errors are found the method returns a bad request (400) response with a <see cref="ErrorResponseModel" /> result that overrides
        /// the success model.
        /// </summary>
        /// <typeparam name="T">Contains the type of the data model to return on success.</typeparam>
        /// <param name="returnValue">Contains the data model to return on success.</param>
        /// <param name="routeName">Contains a a location route when the object was created, requiring a Location header response.</param>
        /// <param name="recordId">Contains an optional record identity value.</param>
        /// <returns>Returns either a successful model upon success or returns a bad request with an <see cref="ErrorResponseModel" /> result.</returns>
        protected IActionResult SuccessOrFailResult<T>(T returnValue, string routeName = "", Guid recordId = default)
        {
            IActionResult result = this.NoContent();

            if (this.AppContext.ErrorManager.HasCriticalErrors || this.AppContext.ErrorManager.HasValidationErrors)
            {
                result = this.CreateErrorResult();
            }
            else if (returnValue != null)
            {
                result = string.IsNullOrWhiteSpace(routeName) ?
                    this.Ok(returnValue) :
                    (IActionResult)this.CreatedAtRoute(routeName, recordId != Guid.Empty ? new { id = recordId } : null, returnValue);
            }

            return result;
        }

        /// <summary>
        /// This method is used to create the error response action result to return to the client.
        /// </summary>
        /// <param name="errorCode">Contains an optional status code to return in the response message. By default 400 Bad Request is returned.</param>
        /// <returns>Returns an action response containing an <see cref="ErrorResponseModel" /> object.</returns>
        private IActionResult CreateErrorResult(HttpStatusCode errorCode = HttpStatusCode.BadRequest)
        {
            ErrorResponseModel responseModel = new ErrorResponseModel();

            // copy error messages into response model error messages list.
            responseModel.Messages.AddRange(this.AppContext.ErrorManager.Messages.Select(m => new ErrorModel(m.Message, m.ErrorType, m.EventDate, m.PropertyName)));

            // determine if there is a suggested errorCode override
            var suggestedReturnCode = this.AppContext.ErrorManager.Messages.OrderByDescending(m => m.SuggestedErrorCode).Select(m => m.SuggestedErrorCode).FirstOrDefault();

            // if a suggestedReturnCode is found and it is greater than the original error code...
            if (suggestedReturnCode > 0 && suggestedReturnCode > (int)errorCode)
            {
                // use the suggested return code.
                errorCode = (HttpStatusCode)suggestedReturnCode;
            }

            // create new response with the error response model in the Content.
            return this.StatusCode((int)errorCode, responseModel);
        }
    }
}