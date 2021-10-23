// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotHost.ApplicationBuilder;

namespace TelegramBotHost.Startup
{
    internal class ConventionBasedStartup : IStartup
    {
        private readonly StartupMethods _methods;

        public ConventionBasedStartup(StartupMethods methods)
        {
            _methods = methods;
        }

        public void Configure(IApplicationBuilder app)
        {
            try
            {
                _methods.ConfigureDelegate(app);
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException)
                    ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();

                throw;
            }
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            throw new Exception();
            /* try
             {
                 return _methods.ConfigureServicesDelegate(services);
             }
             catch (Exception ex)
             {
                 if (ex is TargetInvocationException) 
                     ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
 
                 throw;
             }*/
        }
    }
}