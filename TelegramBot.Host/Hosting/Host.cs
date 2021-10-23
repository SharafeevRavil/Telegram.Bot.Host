// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

namespace TelegramBot.Host.Hosting
{
    public static class Host
    {
        public static IHostBuilder CreateDefaultBuilder(string[] args = null)
        {
            var builder = new HostBuilder();

            builder.UseContentRoot(Directory.GetCurrentDirectory());
            builder.ConfigureHostConfiguration(config =>
            {
                config.AddEnvironmentVariables("DOTNET_");
                if (args != null) config.AddCommandLine(args);
            });

            builder.ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;

                    var reloadOnChange =
                        hostingContext.Configuration.GetValue("hostBuilder:reloadConfigOnChange", true);

                    config.AddJsonFile("appsettings.json", true, reloadOnChange)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, reloadOnChange);

                    if (env.IsDevelopment() && !string.IsNullOrEmpty(env.ApplicationName))
                    {
                        var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                        config.AddUserSecrets(appAssembly, true);
                    }

                    config.AddEnvironmentVariables();

                    if (args != null) config.AddCommandLine(args);
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

                    if (isWindows) logging.AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Warning);

                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddEventSourceLogger();

                    if (isWindows) logging.AddEventLog();

                    logging.Configure(options =>
                    {
                        options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId
                                                          | ActivityTrackingOptions.TraceId
                                                          | ActivityTrackingOptions.ParentId;
                    });
                })
                .UseDefaultServiceProvider((context, options) =>
                {
                    var isDevelopment = context.HostingEnvironment.IsDevelopment();
                    options.ValidateScopes = isDevelopment;
                    options.ValidateOnBuild = isDevelopment;
                });

            return builder;
        }
    }
}