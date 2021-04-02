﻿/*
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

namespace Bastille.Id.Api.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// This class is used as the Error Response Model
    /// </summary>
    public class ErrorResponseModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorResponseModel" /> class.
        /// </summary>
        public ErrorResponseModel()
        {
            this.Messages = new List<ErrorModel>();
            this.HasUnhandledException = false;
        }

        /// <summary>
        /// Gets the error messages.
        /// </summary>
        public List<ErrorModel> Messages { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has unhandled exception.
        /// </summary>
        /// <value><c>true</c> if this instance has unhandled exception; otherwise, <c>false</c>.</value>
        public bool HasUnhandledException { get; set; }
    }
}