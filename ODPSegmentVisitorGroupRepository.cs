using EPiServer.DataAbstraction;
using EPiServer.Framework.Cache;
using EPiServer.Personalization.VisitorGroups;
using Microsoft.Extensions.Options;
using Optimizely.CMS.ODP.Personalization.Model;

namespace Optimizely.CMS.ODP.Personalization
{
    public class ODPSegmentVisitorGroupRepository : IVisitorGroupRepository, ISegmentRepository
    {
        private readonly HttpClient _httpClient;
        private readonly ODPOptions _options;
        private readonly IdentityMappingService _identityMappingService;
        private readonly ISynchronizedObjectInstanceCache _cache;
        private readonly ILogger<ODPSegmentVisitorGroupRepository> _logger;
        private const string _cacheKey = "opd_segments";

        public ODPSegmentVisitorGroupRepository(HttpClient httpClient, IOptions<ODPOptions> options, IdentityMappingService identityMappingService, ISynchronizedObjectInstanceCache cache, ILogger<ODPSegmentVisitorGroupRepository> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _identityMappingService = identityMappingService;
            _cache = cache;
            _logger = logger;
        }

        IEnumerable<Segment> Segments
        {
            get
            {
                var cachedResult = _cache.ReadThrough(_cacheKey, () =>
                {
                    var segments = new List<Segment>();

                    var httpListRequest = new HttpRequestMessage(HttpMethod.Get, $"{_options.BaseUri}/segments");
                    httpListRequest.Headers.Add("x-api-key", _options.ApiKey);
                    httpListRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    var respsone = _httpClient.SendAsync(httpListRequest).Result;
                    var segmentList = respsone.Content.ReadFromJsonAsync<SegmentList>().Result;
                    _logger.LogError($"Segment list {string.Join(',', segmentList.Segments)}");
                    foreach (var segment in segmentList.Segments)
                    {
                        var httpSegmentRequest = new HttpRequestMessage(HttpMethod.Get, $"{_options.BaseUri}/segments/{segment}");
                        _logger.LogError($"Requesting segment named {httpSegmentRequest.RequestUri.ToString()}");
                        httpSegmentRequest.Headers.Add("x-api-key", _options.ApiKey);
                        httpSegmentRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                        respsone = _httpClient.SendAsync(httpSegmentRequest).Result;
                        var externalSegment = respsone.Content.ReadFromJsonAsync<ExternalSegment>().Result;
                        var mappedIdentity = _identityMappingService.Get(httpSegmentRequest.RequestUri, true);
                        segments.Add(new Segment
                        {
                            Id = mappedIdentity.ContentGuid,
                            Name = (string.IsNullOrEmpty(externalSegment.SegmentMetadata.DisplayName) ? segment : externalSegment.SegmentMetadata.DisplayName) ?? mappedIdentity.ContentGuid.ToString()
                        }); ;
                        _logger.LogError($"Loaded segment named {segment} ({segments.Last().Name})");
                    }

                    return segments;
                },
                item => new CacheEvictionPolicy(TimeSpan.FromSeconds(30), CacheTimeoutType.Absolute),
                ReadStrategy.Wait);

                return cachedResult; 
            }
        }

        Segment ISegmentRepository.GetSegment(Guid id) => Segments.FirstOrDefault(s => s.Id == id);

        Segment ISegmentRepository.GetSegment(string name) => Segments.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public IEnumerable<VisitorGroup> List() => Segments.Select(s => new VisitorGroup { Id = s.Id, Name = s.Name });

        public VisitorGroup Copy(VisitorGroup visitorGroup, string copySuffix)
        {
            throw new NotImplementedException();
        }

        public void Delete(Guid visitorGroupsId)
        {
            throw new NotImplementedException();
        }

        public string GetRepositoryName()
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            //throw new NotImplementedException();
        }

        public VisitorGroup Load(Guid visitorGroupsId)
        {
            var segment = Segments.FirstOrDefault(s => s.Id == visitorGroupsId);
            //Alloy has some visitor groups id stored in db, so we fake a response if not matching a segment to not get runtime exception
            return segment is not null ? new VisitorGroup { Id = segment.Id, Name = segment.Name } : new VisitorGroup { Id = visitorGroupsId, Name = visitorGroupsId.ToString("N") };
        }

        public void Save(VisitorGroup visitorGroup)
        {
            throw new NotImplementedException();
        }

        public void Uninitialize()
        {
            //throw new NotImplementedException();
        }
    }
}