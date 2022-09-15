using EPiServer;
using EPiServer.ContentApi.Core.Configuration;
using EPiServer.ContentApi.Core.Serialization;
using EPiServer.ContentApi.Core.Serialization.Internal;
using EPiServer.ContentApi.Core.Serialization.Models;
using EPiServer.Core;
using EPiServer.SpecializedProperties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using Optimizely.CMS.ODP.Personalization.Model;
using System.Globalization;

namespace Optimizely.CMS.ODP.Personalization.Controllers
{
    public class SegmentController : Controller
    {
        private readonly IContentLoader _contentLoader;
        private readonly ContentConvertingService _contentConvertingService;
        private readonly ISegmentRepository _segmentRepository;
        private readonly ContentApiOptions _contentApiOptions;
        private readonly NewtonsoftJsonOutputFormatter _outputFormatter;

        public SegmentController(IContentLoader contentLoader, ContentConvertingService contentConvertingService, ISegmentRepository segmentRepository, IOptions<ContentApiOptions> contentApiOptions, NewtonsoftJsonOutputFormatter outputFormatter)
        {
            _contentLoader = contentLoader;
            _contentConvertingService = contentConvertingService;
            _segmentRepository = segmentRepository;
            _contentApiOptions = contentApiOptions.Value;
            _outputFormatter = outputFormatter;
        }

        [Route("/api/episerver/v3.0/content/{contentId}/{property}")]
        public IActionResult GetBySegment(Guid contentId, [FromQuery] string language, string property, [FromQuery]string segment)
        {
            var currentLanguage = !string.IsNullOrEmpty(language) ? CultureInfo.GetCultureInfo(language) : CultureInfo.InvariantCulture;
            var content = _contentLoader.Get<IContent>(contentId, currentLanguage);
            var propertyData = content.Property[property];
            var currentSegment = _segmentRepository.GetSegment(segment).Id.ToString();
            var contentModels = new List<ContentApiModel>();
            switch (propertyData)
            {
                case PropertyContentArea contentArea:
                    {
                        var contentGroups = (contentArea.Value as ContentArea).Items.GroupBy(i => i.ContentGroup);
                        foreach (var group in contentGroups)
                        {
                            var matchingSegment = group.FirstOrDefault(g => g.AllowedRoles is not null && g.AllowedRoles.Contains(currentSegment)) ?? group.FirstOrDefault(g => g.AllowedRoles is null || !g.AllowedRoles.Any());
                            var contentAreaItem = _contentLoader.Get<IContent>(matchingSegment.ContentLink, currentLanguage);
                            contentModels.Add(_contentConvertingService.ConvertToContentApiModel(contentAreaItem,
                                                               new ConverterContext(contentAreaItem.ContentLink, (contentAreaItem as ILocale)?.Language, _contentApiOptions, EPiServer.Web.ContextMode.Default, null, "*", true)));
                        }
                        break;
                    }
                default:
                    break;
            }

            return new OkObjectResult(contentModels) { Formatters = new FormatterCollection<IOutputFormatter>(new List<IOutputFormatter> { _outputFormatter }) };

        }
    }
}
