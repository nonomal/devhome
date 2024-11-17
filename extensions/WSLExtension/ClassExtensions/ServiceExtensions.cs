﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.DevHome.SDK;
using WSLExtension.Contracts;
using WSLExtension.DevHomeProviders;
using WSLExtension.DistributionDefinitions;
using WSLExtension.Models;
using WSLExtension.Services;

namespace WSLExtension.ClassExtensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddWslExtensionServices(this IServiceCollection services)
    {
        // Services
        services.AddSingleton<IWslServicesMediator, WslServicesMediator>();
        services.AddSingleton<IDistributionDefinitionHelper, DistributionDefinitionHelper>();
        services.AddSingleton<IStringResource, StringResource>();
        services.AddSingleton<IComputeSystemProvider, WslProvider>();
        services.AddSingleton<WslExtension>();
        services.AddSingleton<IProcessCreator, ProcessCreator>();
        services.AddSingleton<IWslManager, WslManager>();

        // Factory delegate to create WslComputeSystems
        services.AddSingleton<WslRegisteredDistributionFactory>(
            serviceProvider => wslDistribution => ActivatorUtilities.CreateInstance<WslComputeSystem>(serviceProvider, wslDistribution));

        return services;
    }
}
