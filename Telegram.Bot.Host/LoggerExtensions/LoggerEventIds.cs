// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using Microsoft.Extensions.Logging;

namespace Telegram.Bot.Host.LoggerExtensions
{
    internal static class LoggerEventIds
    {
        public static readonly EventId RequestStarting = new(1, "RequestStarting");
        public static readonly EventId RequestFinished = new(2, "RequestFinished");
        public static readonly EventId Starting = new(3, "Starting");
        public static readonly EventId Started = new(4, "Started");
        public static readonly EventId Shutdown = new(5, "Shutdown");
        public static readonly EventId ApplicationStartupException = new(6, "ApplicationStartupException");
        public static readonly EventId ApplicationStoppingException = new(7, "ApplicationStoppingException");
        public static readonly EventId ApplicationStoppedException = new(8, "ApplicationStoppedException");
        public static readonly EventId HostedServiceStartException = new(9, "HostedServiceStartException");
        public static readonly EventId HostedServiceStopException = new(10, "HostedServiceStopException");
        public static readonly EventId HostingStartupAssemblyException = new(11, "HostingStartupAssemblyException");
        public static readonly EventId ServerShutdownException = new(12, "ServerShutdownException");
    }
}