// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Telegram.Bot.Host.BotHost
{
    public class BotHostOptions
    {
        public BotHostOptions()
        {
        }

        public BotHostOptions(IConfiguration configuration)
            : this(configuration, string.Empty)
        {
        }

        public BotHostOptions(IConfiguration configuration, string applicationNameFallback)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            ApplicationName = configuration[BotHostDefaults.ApplicationKey] ?? applicationNameFallback;
            StartupAssembly = configuration[BotHostDefaults.StartupAssemblyKey];
            BotHostUtilities.ParseBool(configuration, BotHostDefaults.DetailedErrorsKey);
            CaptureStartupErrors = BotHostUtilities.ParseBool(configuration, BotHostDefaults.CaptureStartupErrorsKey);
            Environment = configuration[BotHostDefaults.EnvironmentKey];
            ContentRootPath = configuration[BotHostDefaults.ContentRootKey];
            PreventHostingStartup = BotHostUtilities.ParseBool(configuration, BotHostDefaults.PreventHostingStartupKey);
            SuppressStatusMessages =
                BotHostUtilities.ParseBool(configuration, BotHostDefaults.SuppressStatusMessagesKey);

            // Search the primary assembly and configured assemblies.
            HostingStartupAssemblies =
                Split($"{ApplicationName};{configuration[BotHostDefaults.HostingStartupAssembliesKey]}");
            HostingStartupExcludeAssemblies = Split(configuration[BotHostDefaults.HostingStartupExcludeAssembliesKey]);

            var timeout = configuration[BotHostDefaults.ShutdownTimeoutKey];
            if (!string.IsNullOrEmpty(timeout)
                && int.TryParse(timeout, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
                ShutdownTimeout = TimeSpan.FromSeconds(seconds);
        }

        public string ApplicationName { get; set; }

        public bool PreventHostingStartup { get; set; }

        public bool SuppressStatusMessages { get; set; }

        public IReadOnlyList<string> HostingStartupAssemblies { get; set; }

        public IReadOnlyList<string> HostingStartupExcludeAssemblies { get; set; }

        public bool CaptureStartupErrors { get; set; }

        public string Environment { get; set; }

        public string StartupAssembly { get; set; }

        public string ContentRootPath { get; set; }

        public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public IEnumerable<string> GetFinalHostingStartupAssemblies()
        {
            return HostingStartupAssemblies.Except(HostingStartupExcludeAssemblies, StringComparer.OrdinalIgnoreCase);
        }

        private static IReadOnlyList<string> Split(string value)
        {
            return value?.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                   ?? Array.Empty<string>();
        }
    }
}