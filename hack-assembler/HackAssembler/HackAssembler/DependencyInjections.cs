using System.Reflection;
using HackAssembler.Abstractions;
using HackAssembler.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace HackAssembler;

public static class DependencyInjections
{
    public static IServiceCollection AddPipeline(this IServiceCollection services)
    {
        var implementations = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                type.GetInterfaces().Any(interfaceType => interfaceType == typeof(IPipelineStep<string[]>)));

        foreach (var implementation in implementations)
        {
            services.AddScoped(typeof(IPipelineStep<string[]>), implementation);
        }

        services.AddScoped<IPipeline, HackAssemblerPipeline>();

        return services;
    }
}
