using System.Net;

namespace Biwen.AutoClassGen.TestConsole.Entitys
{
    public class Venue
    {
        private ICollection<VenueImage>? _images;

        public string? BusinessId { private get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }

        public ICollection<VenueImage>? Images { get { return _images; } set { _images = value; } }
    }

    public class VenueImage
    {
        public string? VenueId { get; set; }
        public string? Url { get; set; }
        public long? OrderId { get; set; }
    }

}