using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Models
{
    public class AppleStorePodcastModel
    {
        public string             _id                 { get; set; }
        public string             url                 { get; set; }
        public string             name                { get; set; }
        public string             author              { get; set; }
        public string             thumbnail           { get; set; }
        public string             description         { get; set; }
        public string             category            { get; set; }
        public string             language            { get; set; }
        public int                customerRatings     { get; set; }
        public string             podcastWebsite      { get; set; }
        public List<String>       podcastsFromAuthor  { get; set; }
        public List<String>       relatedPodcasts     { get; set; }
        public List<EpisodeModel> episodes            { get; set; }
        public DateTime           lastReleasedContent { get; set; }
    }

    public class EpisodeModel
    {
        public int index            { get; set; }
        public string name          { get; set; }
        public string description   { get; set; }
        public DateTime releaseDate { get; set; }
        public double price         { get; set; }
    }
}
