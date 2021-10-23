// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using TelegramBotHost.Middleware;

namespace TelegramBotHost.LoggerExtensions
{
    internal static class HostingLoggerExtensions
    {
        public static IDisposable RequestScope(this ILogger logger, BotUpdateContext botUpdateContext)
        {
            return logger.BeginScope(new HostingLogScope(botUpdateContext));
        }

        public static void ApplicationError(this ILogger logger, Exception exception)
        {
            logger.ApplicationError(LoggerEventIds.ApplicationStartupException, "Application startup exception",
                exception);
        }

        public static void HostingStartupAssemblyError(this ILogger logger, Exception exception)
        {
            logger.ApplicationError(LoggerEventIds.HostingStartupAssemblyException,
                "Hosting startup assembly exception", exception);
        }

        public static void ApplicationError(
            this ILogger logger,
            EventId eventId,
            string message,
            Exception exception)
        {
            if (exception is ReflectionTypeLoadException typeLoadException)
                message = typeLoadException.LoaderExceptions
                    .Aggregate(message,
                        (current, loaderException) => current + Environment.NewLine + loaderException!.Message);

            var str = message;
            var objArray = Array.Empty<object>();
            logger.LogCritical(eventId, exception, str, objArray);
        }

        public static void Starting(this ILogger logger)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
                return;
            logger.LogDebug(LoggerEventIds.Starting, "Hosting starting");
        }

        public static void Started(this ILogger logger)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
                return;
            logger.LogDebug(LoggerEventIds.Started, "Hosting started");
        }

        public static void Shutdown(this ILogger logger)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
                return;
            logger.LogDebug(LoggerEventIds.Shutdown, "Hosting shutdown");
        }

        public static void ServerShutdownException(this ILogger logger, Exception ex)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
                return;
            logger.LogDebug(LoggerEventIds.ServerShutdownException, ex, "Server shutdown exception");
        }

        private class HostingLogScope :
            IReadOnlyList<KeyValuePair<string, object>>
        {
            private readonly string _path;
            private readonly string _traceIdentifier;
            private string _cachedToString;

            public HostingLogScope(BotUpdateContext botUpdateContext)
            {
                _traceIdentifier = botUpdateContext.Update.Id.ToString();
                _path = botUpdateContext.Update.Message?.Text ?? "not text message";
            }

            public int Count => 2;

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    return index switch
                    {
                        0 => new KeyValuePair<string, object>("RequestId", _traceIdentifier),
                        1 => new KeyValuePair<string, object>("RequestPath", _path),
                        _ => throw new ArgumentOutOfRangeException(nameof(index))
                    };
                }
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                for (var i = 0; i < Count; ++i)
                    yield return this[i];
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public override string ToString()
            {
                return _cachedToString ??= string.Format(CultureInfo.InvariantCulture,
                    "RequestPath:{0} RequestId:{1}", _path, _traceIdentifier);
            }
        }
    }
}