using EPiServer.Validation.Internal;

namespace Optimizely.CMS.ODP.Personalization.Model
{
    public class PropertyVariant
    {
        public string Property { get; set; }
        public DateTime Generated { get; set; } = DateTime.UtcNow;
        public IList<SegmentVariant> Variants { get; set; }

    }

    public class SegmentVariant
    {
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }   
    }
}
