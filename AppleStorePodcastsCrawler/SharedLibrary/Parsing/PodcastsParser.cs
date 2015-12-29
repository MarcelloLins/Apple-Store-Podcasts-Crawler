using HtmlAgilityPack;
using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SharedLibrary.Parsing
{
    public class PodcastsParser
    {
        public IEnumerable<String> ParseCategoryUrls (string rootHtmlPage)
        {
            // Creating Html Map, and loading root page html on it
            HtmlDocument map = new HtmlDocument ();
            map.LoadHtml (rootHtmlPage);

            // Reaching Nodes of Interest
            foreach (var htmlNode in map.DocumentNode.SelectNodes (Consts.XPATH_CATEGORIES_URLS))
            {
                // Checking for the Href Attribute
                HtmlAttribute href = htmlNode.Attributes["href"];

                // Sanity Check
                if (href != null)
                {
                    yield return href.Value;
                }
            }
        }

        public IEnumerable<String> ParseCharacterUrls (string htmlResponse)
        {
            // Creating HTML Map based on the html response
            HtmlDocument map = new HtmlDocument ();
            map.LoadHtml (htmlResponse);

            // Reaching nodes of interest
            foreach (HtmlNode characterNode in map.DocumentNode.SelectNodes (Consts.XPATH_CHARACTERS_URLS))
            {
                // Checking for Href Attribute within the node
                HtmlAttribute href = characterNode.Attributes["href"];

                // Sanity Check
                if (href != null)
                {
                    yield return href.Value;
                }
            }
        }

        public IEnumerable<String> ParseNumericUrls (string htmlResponse)
        {
            // Creating HTML Map based on the html response
            HtmlDocument map = new HtmlDocument ();
            map.LoadHtml (htmlResponse);

            // Reaching nodes of interest
            foreach (HtmlNode characterNode in map.DocumentNode.SelectNodes (Consts.XPATH_NUMERIC_URLS))
            {
                // Checking for Href Attribute within the node
                HtmlAttribute href = characterNode.Attributes["href"];

                // Sanity Check
                if (href != null)
                {
                    yield return href.Value;
                }
            }
        }

        public IEnumerable<String> ParsePodcastUrls (string htmlResponse)
        {
            // Creating HTML Map based on the html response
            HtmlDocument map = new HtmlDocument ();
            map.LoadHtml (htmlResponse);

            // Reaching nodes of interest
            foreach (HtmlNode characterNode in map.DocumentNode.SelectNodes (Consts.XPATH_APPS_URLS))
            {
                // Checking for Href Attribute within the node
                HtmlAttribute href = characterNode.Attributes["href"];

                // Sanity Check
                if (href != null)
                {
                    yield return href.Value;
                }
            }
        }

        public bool HasPageIndexes (string htmlResponse)
        {
            HtmlDocument map = new HtmlDocument ();
            map.LoadHtml (htmlResponse);

            var nodes = map.DocumentNode.SelectNodes (Consts.XPATH_NUMERIC_URLS);
            return nodes != null;
        }

        public AppleStorePodcastModel ParsePodcastPage (string htmlResponse)
        {
            AppleStorePodcastModel parsedPodcast = new AppleStorePodcastModel ();

            // Loading map of HTML Nodes
            HtmlDocument map = new HtmlDocument ();
            map.LoadHtml (htmlResponse);

            // Parsing Name
            parsedPodcast.name = GetNodeValue (map, Consts.XPATH_TITLE);

            // Parsing Author Name
            string tmpValue     = GetNodeValue (map, Consts.XPATH_AUTHOR);

            if (tmpValue.StartsWith ("By ", StringComparison.InvariantCultureIgnoreCase))
            {
                tmpValue = tmpValue.Substring (tmpValue.IndexOf (' ')).Trim();
            }

            parsedPodcast.author = tmpValue.Trim();

            // Parsing Description
            parsedPodcast.description = GetNodeValue (map, Consts.XPATH_DESCRIPTION);
            
            // Parsing Thumbnail
            var tmpNode = map.DocumentNode.SelectSingleNode (Consts.XPATH_THUMBNAIL);

            if (tmpNode != null)
            {
                parsedPodcast.thumbnail = tmpNode.Attributes["src"].Value;
            }

            // Parsing Category
            parsedPodcast.category = GetNodeValue (map, Consts.XPATH_CATEGORY);

            // Parsing Language
            parsedPodcast.language = GetNodeValue (map, Consts.XPATH_LANGUAGE).Replace("Language:", "").Trim();
            
            // Customer Rating
            tmpValue = GetNodeValue (map, Consts.XPATH_RATINGS);
            
            if (String.IsNullOrEmpty(tmpValue))
            {
                parsedPodcast.customerRatings = 0;
            }
            else
            {
                // Splitting the string by SPACE and reaching the first element, which is the rating itself
                parsedPodcast.customerRatings = Int32.Parse (tmpValue.Split (' ')[0]);
            }

            // Podcast Website
            tmpNode = map.DocumentNode.SelectSingleNode (Consts.XPATH_WEBSITE);

            if (tmpNode != null)
            {
                parsedPodcast.podcastWebsite = tmpNode.Attributes["href"].Value;
            }

            // More podcasts from author
            HtmlNodeCollection moreFromAuthorNodes = map.DocumentNode.SelectNodes (Consts.XPATH_MORE_FROM_AUTHOR);

            if (moreFromAuthorNodes != null && moreFromAuthorNodes.Count != 0)
            {
                parsedPodcast.podcastsFromAuthor = moreFromAuthorNodes.Select (t => t.Attributes["href"].Value).ToList ();
            }

            // Related Podcasts
            HtmlNodeCollection relatedPodcasts = map.DocumentNode.SelectNodes (Consts.XPATH_RELATED_PODCASTS);

            if (relatedPodcasts != null && relatedPodcasts.Count != 0)
            {
                parsedPodcast.relatedPodcasts = relatedPodcasts.Select (t => t.Attributes["href"].Value).ToList ();
            }

            // Individual Episodes
            HtmlNodeCollection episodes = map.DocumentNode.SelectNodes (Consts.XPATH_EPISODES);

            if (episodes != null && episodes.Count != 0)
            {
                parsedPodcast.episodes = new List<EpisodeModel> ();
                DateTime dtReleased;
                string dateReleased;

                // Parsing Episodes Content
                foreach(var episode in episodes)
                {
                    // Picking nodes of interest
                    List<HtmlNode> tableRows = episode.ChildNodes.Where (t => t.Name == "td").ToList();

                    // Episode Index
                    int index           = Int32.Parse(tableRows[0].Attributes["sort-value"].Value);
                    string name         = tableRows[1].Attributes["sort-value"].Value.Trim();
                    string description  = tableRows[2].Attributes["sort-value"].Value.Trim();

                    if (tableRows[3].Attributes["sort-value"] != null)
                    {
                        dateReleased = tableRows[3].Attributes["sort-value"].Value.Trim ();
                    }
                    else
                    {
                        dateReleased = "01/01/01";
                    }

                    // Normalizing Date
                    if(!DateTime.TryParse(dateReleased, out dtReleased))
                    {
                        dtReleased = new DateTime (1, 1, 1);
                    }

                    // Adding Parsed data to the list
                    parsedPodcast.episodes.Add (new EpisodeModel ()
                        {
                            index       = index,
                            name        = name,
                            description = description,
                            releaseDate = dtReleased
                        });
                }

                // Updating 'Last Released Content' Attribute
                parsedPodcast.lastReleasedContent = parsedPodcast.episodes.Max (t => t.releaseDate);
            }

            return parsedPodcast;
        }

        private string GetNodeValue (HtmlDocument map, string xPath)
        {
            var node    = map.DocumentNode.SelectSingleNode (xPath);

            return node == null ? String.Empty : HttpUtility.HtmlDecode (node.InnerText);
        }
    }
}
