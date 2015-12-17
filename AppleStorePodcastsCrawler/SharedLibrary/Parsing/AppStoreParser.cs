﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary.Parsing
{
    public class AppStoreParser
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
    }
}
