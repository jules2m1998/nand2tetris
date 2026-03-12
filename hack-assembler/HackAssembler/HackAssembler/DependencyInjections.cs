using HackAssembler.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
namespace HackAssembler;

public static class DependencyInjections
{
    extension(IServiceCollection services){
        public IServiceCollection AddPipeLine()
        {
            var implementations = Assembly.GetExecutingAssembly().GetTypes().Where(t => t is {IsClass: true, IsAbstract: false} && t.GetInterfaces().Any(i => i == typeof(IPipelineStep<string[]>)));
            foreach (var implementation in implementations)
            {
                services.AddScoped(typeof(IPipelineStep<string[]>), implementation);
            }
            
            return services;
        }
    }
}
