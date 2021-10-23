// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Modifications copyright (c) 2021 Sharafeev Ravil

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramBot.Host.Middleware;

namespace TelegramBot.Host.ApplicationBuilder
{
    public class ApplicationBuilder : IApplicationBuilder
    {
        private const string ApplicationServicesKey = "application.Services";

        private readonly IList<Func<BotUpdateDelegate, BotUpdateDelegate>> _components =
            new List<Func<BotUpdateDelegate, BotUpdateDelegate>>();

        public ApplicationBuilder(IServiceProvider serviceProvider)
        {
            Properties = new Dictionary<string, object>(StringComparer.Ordinal);
            ApplicationServices = serviceProvider;
        }

        private ApplicationBuilder()
        {
        }

        private IDictionary<string, object> Properties { get; }

        public IServiceProvider ApplicationServices
        {
            get => GetProperty<IServiceProvider>(ApplicationServicesKey)!;
            set => SetProperty(ApplicationServicesKey, value);
        }

        public IApplicationBuilder Use(Func<BotUpdateDelegate, BotUpdateDelegate> middleware)
        {
            _components.Add(middleware);
            return this;
        }

        public IApplicationBuilder New()
        {
            return new ApplicationBuilder();
        }

        public BotUpdateDelegate Build()
        {
            BotUpdateDelegate app = _ => Task.CompletedTask;

            return _components.Reverse().Aggregate(app, (current, component) => component(current));
        }

        private T GetProperty<T>(string key)
        {
            return Properties.TryGetValue(key, out var value) ? (T)value : default;
        }

        private void SetProperty<T>(string key, T value)
        {
            Properties[key] = value;
        }
    }
}