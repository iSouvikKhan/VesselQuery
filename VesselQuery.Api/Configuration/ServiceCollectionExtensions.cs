using System.Diagnostics;
using Microsoft.Extensions.Options;
using VesselQuery.Core.Data;
using VesselQuery.Core.Services;

namespace VesselQuery.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVesselQuery(this IServiceCollection services)
    {
        services.AddOptions<VesselDataOptions>()
            .BindConfiguration(VesselDataOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IVesselStore>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<VesselDataOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<InMemoryVesselStore>>();
            var path = Path.GetFullPath(options.FilePath, AppContext.BaseDirectory);

            var stopwatch = Stopwatch.StartNew();
            var store = new InMemoryVesselStore(VesselDataLoader.LoadFromFile(path));
            logger.LogInformation("Loaded {Count} vessels with {FieldCount} fields from {Path} in {Elapsed} ms",
                store.Vessels.Count, store.FieldNames.Count, path, stopwatch.ElapsedMilliseconds);

            return store;
        });

        services.AddSingleton<IVesselQueryService, VesselQueryService>();

        return services;
    }
}
