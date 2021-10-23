// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TelegramBot.Host.BotHost;

namespace TelegramBot.Host.Hosting
{
    internal static class HostingEnvironmentExtensions
    {
        internal static void Initialize(this IHostEnvironment hostingEnvironment, string contentRootPath,
            BotHostOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(contentRootPath))
                throw new ArgumentException("A valid non-empty content root must be provided.",
                    nameof(contentRootPath));
            if (!Directory.Exists(contentRootPath))
                throw new ArgumentException($"The content root '{contentRootPath}' does not exist.",
                    nameof(contentRootPath));

            hostingEnvironment.ApplicationName = options.ApplicationName;
            hostingEnvironment.ContentRootPath = contentRootPath;
            hostingEnvironment.ContentRootFileProvider = new PhysicalFileProvider(hostingEnvironment.ContentRootPath);

            hostingEnvironment.EnvironmentName =
                options.Environment ??
                hostingEnvironment.EnvironmentName;
        }
    }
}