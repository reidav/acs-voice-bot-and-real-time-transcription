// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

namespace Api.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackendServices(this IServiceCollection services)
    {
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<ICallAutomationService, CallAutomationService>();
        services.AddSingleton<IIdentityService, IdentityService>();
        services.AddSingleton<ITranscriptReceiver, TranscriptReceiver>();
        return services;
    }
}