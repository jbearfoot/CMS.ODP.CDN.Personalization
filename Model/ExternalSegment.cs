using System.Text.Json.Serialization;

namespace Optimizely.CMS.ODP.Personalization.Model
{
    public class SegmentList
    {
        [JsonPropertyName("segment_ids")]
        public IList<string> Segments { get; set; }
    }

    public class SegmentMetadata
    {
        [JsonPropertyName("com.optimizely/displayName")]
        public string DisplayName { get; set; }
    }
    public class ExternalSegment
    {
        [JsonPropertyName("metadata")]
        public SegmentMetadata SegmentMetadata { get; set; }
    }
}
