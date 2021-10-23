// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotHost.BotServer;
using TelegramBotHost.GenericBotHost;

namespace TelegramBotHost.BotHost
{
    public static class HostingAbstractionsBotHostBuilderExtensions
    {
        public static IBotHostBuilder UseConfiguration(this IBotHostBuilder hostBuilder, IConfiguration configuration)
        {
            foreach (var (key, value) in configuration.AsEnumerable(true)) hostBuilder.UseSetting(key, value);

            return hostBuilder;
        }

        public static IBotHostBuilder CaptureStartupErrors(this IBotHostBuilder hostBuilder, bool captureStartupErrors)
        {
            return hostBuilder.UseSetting(BotHostDefaults.CaptureStartupErrorsKey,
                captureStartupErrors ? "true" : "false");
        }

        [RequiresUnreferencedCode("Types and members the loaded assembly depends on might be removed.")]
        public static IBotHostBuilder UseStartup(this IBotHostBuilder hostBuilder, string startupAssemblyName)
        {
            if (startupAssemblyName == null) throw new ArgumentNullException(nameof(startupAssemblyName));

            return hostBuilder
                .UseSetting(BotHostDefaults.ApplicationKey, startupAssemblyName)
                .UseSetting(BotHostDefaults.StartupAssemblyKey, startupAssemblyName);
        }

        public static IBotHostBuilder UseServer(this IBotHostBuilder hostBuilder, IServer server)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));

            return hostBuilder.ConfigureServices(services => { services.AddSingleton(server); });
        }

        public static IBotHostBuilder UseEnvironment(this IBotHostBuilder hostBuilder, string environment)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));

            return hostBuilder.UseSetting(BotHostDefaults.EnvironmentKey, environment);
        }

        public static IBotHostBuilder UseContentRoot(this IBotHostBuilder hostBuilder, string contentRoot)
        {
            if (contentRoot == null) throw new ArgumentNullException(nameof(contentRoot));

            return hostBuilder.UseSetting(BotHostDefaults.ContentRootKey, contentRoot);
        }

        public static IBotHostBuilder SuppressStatusMessages(this IBotHostBuilder hostBuilder,
            bool suppressStatusMessages)
        {
            return hostBuilder.UseSetting(BotHostDefaults.SuppressStatusMessagesKey,
                suppressStatusMessages ? "true" : "false");
        }

        public static IBotHostBuilder UseShutdownTimeout(this IBotHostBuilder hostBuilder, TimeSpan timeout)
        {
            return hostBuilder.UseSetting(BotHostDefaults.ShutdownTimeoutKey,
                ((int)timeout.TotalSeconds).ToString(CultureInfo.InvariantCulture));
        }

        public static IBotHost Start(this IBotHostBuilder hostBuilder, params string[] urls)
        {
            var host = hostBuilder.Build();
            host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            return host;
        }
    }
}