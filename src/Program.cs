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

namespace Bastille.Id.Api
{
    using System;
    using System.IO;
    using Bastille.Id.Api.Properties;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Serilog;

    /// <summary>
    /// This class is the main entry startup point of the API application.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Gets or sets an instance of the application configuration.
        /// </summary>
        public static IConfiguration Configuration { get; set; }

        /// <summary>
        /// The main entry point of the application.
        /// </summary>
        /// <param name="args">Contains an array of command line arguments.</param>
        public static void Main(string[] args)
        {
            Console.Title = Resources.ApplicationName;

            // Configure the default use of a default Xml resolver if one is not found. This is default behavior in .NET Framework that was removed/broken in
            // .NET Core
            AppContext.SetSwitch("Switch.System.Xml.AllowDefaultResolver", true);

            // get the connection string information early
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables();

            // set the configuration instance.
            Configuration = builder.Build();

            // setup logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .ReadFrom.Configuration(Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            try
            {
                Log.Information(Resources.StartupMessageText, Resources.ApplicationName);

                // run the web host
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, Resources.HostTerminatedErrorText);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// This method is used to build a new web host instance.
        /// </summary>
        /// <param name="args">Contains an array of command line arguments.</param>
        /// <returns>Returns a new web host instance.</returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddSerilog();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.CaptureStartupErrors(true);
                    webBuilder.UseSerilog();
                });
    }
}