using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary
{
    public class Consts
    {
        // Urls
        public const string ROOT_STORE_URL        = "https://itunes.apple.com/us/genre/podcasts/id26?mt=2";
                                                  
        // Http Headers                           
        public const string USER_AGENT            = "Crawling for a college work - more at https://github.com/MarcelloLins/AppStoreCrawler";
        
        // XPaths - Root Page 
        public const string XPATH_CATEGORIES_URLS = "//a[contains(@class,'top-level-genre')]";
        public const string XPATH_CHARACTERS_URLS = "//div[@id='selectedgenre']/ul[@class='list alpha']/li/a";
        public const string XPATH_NUMERIC_URLS    = "//ul[@class='list paginate']/li/a";
        public const string XPATH_NEXT_PAGE       = "//ul[@class='list paginate'][1]/li/a[@class='paginate-more']";
        public const string XPATH_LAST_PAGE       = "//ul[@class='list paginate'][1]/li/a[not(@class)]";
        public const string XPATH_APPS_URLS       = "//div[contains(@class,'column') and not(@id)]/ul/li/a";

        // XPaths - App Page
        public const string XPATH_TITLE            = "//div[@id='title']/div[@class='left']/h1";
        public const string XPATH_AUTHOR           = "//div[@id='title']/div[@class='left']/h2";
        public const string XPATH_DESCRIPTION      = "//div[@metrics-loc='Titledbox_Description' and @class='product-review']/p";
        public const string XPATH_THUMBNAIL        = "//div[@class='lockup product podcast']/a/div/img";
        public const string XPATH_CATEGORY         = "//li[@class='genre']/a/span";
        public const string XPATH_LANGUAGE         = "//li[@class='language']";
        public const string XPATH_RATINGS          = "//span[@class='rating-count' and @itemprop='reviewCount']";
        public const string XPATH_WEBSITE          = "//ul/li/a[text() = 'Podcast Website']";
        public const string XPATH_MORE_FROM_AUTHOR = "//div[@metrics-loc and @class='extra-list more-by']/ul/li/div/a";
        public const string XPATH_RELATED_PODCASTS = "//div[@metrics-loc='Swoosh_']//div[@class='lockup small podcast audio']/a[@class='artwork-link']";
        public const string XPATH_EPISODES         = "//table[@role='presentation']//tr[@kind]";

        // Culture Info and Globalization
        public const string CURRENT_CULTURE_INFO = "en-US";
        public const string DATE_FORMAT          = "MMM dd, yyyy";
    }
}