// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

namespace Telegram.Bot.Host.BotHost
{
    public class BotHostDefaults
    {
        public static readonly string ApplicationKey = "applicationName";

        public static readonly string StartupAssemblyKey = "startupAssembly";

        public static readonly string HostingStartupAssembliesKey = "hostingStartupAssemblies";

        public static readonly string HostingStartupExcludeAssembliesKey = "hostingStartupExcludeAssemblies";

        public static readonly string DetailedErrorsKey = "detailedErrors";

        public static readonly string EnvironmentKey = "environment";

        public static readonly string CaptureStartupErrorsKey = "captureStartupErrors";

        public static readonly string ContentRootKey = "contentRoot";

        public static readonly string PreventHostingStartupKey = "preventHostingStartup";

        public static readonly string SuppressStatusMessagesKey = "suppressStatusMessages";

        public static readonly string ShutdownTimeoutKey = "shutdownTimeoutSeconds";
    }
}