using EPiServer.ContentApi.Core.Serialization;
using EPiServer.Personalization.VisitorGroups;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Optimizely.CMS.ODP.Personalization.Serialization;
using System.Buffers;
using Microsoft.Extensions.Options;

namespace Optimizely.CMS.ODP.Personalization
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddODPPersonalization(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ODPSegmentVisitorGroupRepository>()
                .Forward<ODPSegmentVisitorGroupRepository, IVisitorGroupRepository>()
                .Forward<ODPSegmentVisitorGroupRepository, ISegmentRepository>();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IContentApiModelFilter, VariantContentApiModelFilter>());

            services.Configure<ODPOptions>(configuration.GetSection("ODP"));
            services.AddHttpClient<ODPSegmentVisitorGroupRepository>();

            services.AddSingleton<NewtonsoftJsonOutputFormatter>(s =>
            {
                var charPool = s.GetRequiredService<ArrayPool<char>>();
                var mvcOptions = s.GetRequiredService<IOptions<MvcOptions>>();

                var serializerSettings = new JsonSerializerSettings
                {
                    MaxDepth = 32,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.None,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy
                        {
                            ProcessDictionaryKeys = true,
                            ProcessExtensionDataNames = true,
                            OverrideSpecifiedNames = true,
                        }
                    },
                    Converters = new List<JsonConverter>
                    {
                        new StringEnumConverter(new CamelCaseNamingStrategy()),
                        new ProblemDetailsConverter(),
                        new ValidationProblemDetailsConverter(),
                    }
                };

                return new NewtonsoftJsonOutputFormatter(
                    serializerSettings,
                    charPool,
                    mvcOptions.Value,
                    new MvcNewtonsoftJsonOptions());
            });

            return services;
        }
    }
}
