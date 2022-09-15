using EPiServer.Core.Routing.Pipeline;

namespace Optimizely.CMS.ODP.Personalization
{
    public class Segment
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public interface ISegmentRepository
    {
        public Segment GetSegment(Guid id);
        public Segment GetSegment(string name);
    }
}
