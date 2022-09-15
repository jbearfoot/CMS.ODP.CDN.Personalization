using EPiServer;
using EPiServer.ContentApi.Core.Serialization;
using EPiServer.ContentApi.Core.Serialization.Internal;
using EPiServer.ContentApi.Core.Serialization.Models;
using EPiServer.ContentApi.Core.Tracking;
using EPiServer.Core;
using EPiServer.SpecializedProperties;
using EPiServer.Web;
using Optimizely.CMS.ODP.Personalization.Model;
using System.Globalization;
using System.Web;

namespace Optimizely.CMS.ODP.Personalization.Serialization
{
    internal class VariantContentApiModelFilter : ContentApiModelFilter<ContentApiModel>
    {
        private readonly IContentApiTrackingContextAccessor _contentApiTrackingContextAccessor;
        private readonly ISegmentRepository _segmentRepository;
        private readonly IContentLoader _contentLoader;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VariantContentApiModelFilter(IContentApiTrackingContextAccessor contentApiTrackingContextAccesor, ISegmentRepository segmentRepository, IContentLoader contentLoader, IHttpContextAccessor httpContextAccessor)
        {
            _contentApiTrackingContextAccessor = contentApiTrackingContextAccesor;
            _segmentRepository = segmentRepository;
            _contentLoader = contentLoader;
            _httpContextAccessor = httpContextAccessor;
        }

        public override void Filter(ContentApiModel contentApiModel, ConverterContext converterContext)
        {
            var currentReference = new LanguageContentReference(new ContentReference(contentApiModel.ContentLink.Id.Value, contentApiModel.ContentLink.WorkId.GetValueOrDefault(), contentApiModel.ContentLink.ProviderName), CultureInfo.GetCultureInfo(contentApiModel.Language.Name));
            if (_contentApiTrackingContextAccessor.Current.ReferencedContent.TryGetValue(currentReference, out var metadata))
            {
                if (metadata.PersonalizedProperties.Any())
                {
                    var content = _contentLoader.Get<IContent>(currentReference.ContentLink, currentReference.Language);
                    var variants = new List<PropertyVariant>();
                    foreach (var personalizedProperty in metadata.PersonalizedProperties)
                    {
                        if (converterContext.SelectedProperties.Any() && !converterContext.SelectedProperties.Contains(personalizedProperty))
                        {
                            continue;
                        }

                        var propertyVariant = new PropertyVariant { Property = personalizedProperty, Variants = new List<SegmentVariant>() };
                        var property = content.Property[personalizedProperty];
                     
                        switch (property)
                        {
                            case PropertyContentArea contentArea:
                            {
                                    var contentGroups = (contentArea.Value as ContentArea).Items.GroupBy(i => i.ContentGroup);
                                    foreach (var group in contentGroups.Where(k => !string.IsNullOrEmpty(k.Key)))
                                    {
                                        var usedSegments = group.Where(g => g.AllowedRoles is not null).SelectMany(i => i.AllowedRoles).ToHashSet();
                                        foreach (var segment in usedSegments)
                                        {
                                            var externalSegment = _segmentRepository.GetSegment(Guid.Parse(segment));
                                            propertyVariant.Variants.Add(new SegmentVariant
                                            {
                                                Name = externalSegment.Name,
                                                Link = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}/api/episerver/v3.0/content/{content.ContentGuid.ToString()}/{personalizedProperty}?language={currentReference.Language.Name}&segment={HttpUtility.UrlEncode(externalSegment.Name)}"
                                            });
                                        }
                                    }
                                    break;
                            }
                           default:
                                break;
                        }

                        if (propertyVariant.Variants.Any())
                        {
                            variants.Add(propertyVariant);
                        }
                    }

                    if (variants.Any())
                    {
                        if (converterContext.SelectedProperties.Any())
                        {
                            (converterContext.SelectedProperties as HashSet<string>).Add("PersonalizedVariants");
                        }
                        //Clear personalized properties so ContentDeliveryAPi does not prevent CDN caching
                        metadata.PersonalizedProperties.Clear();
                        contentApiModel.Properties.Add("PersonalizedVariants", variants);
                    }
                }
            }
        }
    }
}
