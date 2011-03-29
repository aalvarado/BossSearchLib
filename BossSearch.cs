/*
	Filename: BossSearch.cs
	Author: Adan Alvarado
	Email: adan.alvarado7@gmail.com
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Net;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;

namespace BossInterface
{
    /// <summary>
    /// Summary description for BossSearch
    /// Yahoo BOSS is an API that enables a website pull results from the yahoo indexed search
    /// results.
    /// 
    /// This class holds reads the api key from the configuration settings, 
    /// does an http request to the yahoo site, via the HTTPRequest object, and
    /// returns an XML. It then proceeds to load that XML into a dataset.
    /// 
    /// After the DataSet is loaded, a DataSet to HTML function is called. 
    /// 
    /// It currently displays the results in a string which can be embedded into 
    /// a div.InnerHTML. The format of the string is an unordered list (ul)
    /// </summary>
    public class BossSearch
    {

        static String sResultsPage = "searchResults.aspx"; //results page gets called from the outside
        static WebProxy webproxy = new WebProxy(ConfigurationSettings.AppSettings["BossWebProxy"]);
        static String sApiKey = ConfigurationSettings.AppSettings["BossApiKey"];
        static String sSite = ConfigurationSettings.AppSettings["BossSiteSearch"]; //without http://
        bool sIsSearchSuccessful = false;
        String sSearchString = "";

        /// <summary>
        /// address gets build with sQuery, after it gets build an http request is called.
        /// address = new Uri(String.Format(sQuery, sSearchString, sSite, sApiKey, sStart, Convert.ToString(nResultsPerPage)));
        /// </summary>
        String sQuery = "http://boss.yahooapis.com/ysearch/web/v1/{0}%20+site:{1}?appid={2}&format=xml&start={3}&count={4}"; //string used by the http request to the api.
        
        String paginationQuery = sResultsPage + "?term={0}&start={1}";

        int nStart = 0;//indicates the start of the results until results per page is reached
        Uri address; //search Query with interpolated values
        int nResultsPerPage = 10; // how many results per page
        int nTotalResults; //the total amount of results the last call generated
        
        /// <summary>
        /// Yahoo BOSS is an API that enables a website pull results from the yahoo indexed search
        /// results.
        /// 
        /// This class holds reads the api key from the configuration settings, 
        /// does an http request to the yahoo site, via the HTTPRequest object, and
        /// returns an XML. It then proceeds to load that XML into a dataset.
        /// </summary>
        public BossSearch()
        {
         
        }
        
        
        /// <summary>
        /// Gets the address that the HTTPRequest will use to pull the data.
        /// </summary>
        public String Address
        {
            get
            {
                return address.ToString(); ;
            }
        }

        /// <summary>
        /// Gets the start range in which the search is being sent.
        /// </summary>
        public int Start
        {
            get {

                return nStart;
            }

        }

        /// <summary>
        /// Gets the page where it should redirect after a search.
        /// </summary>
        public static String ResultsPage
        {
            get
            {
                return sResultsPage;
            }
        }
        
        
        /// <summary>
        /// Gets the total results after a search
        /// </summary>
        public int TotalResults
        {
            get
            {
                return nTotalResults;
            }
        }
        
        
        /// <summary>
        /// Gets or sets the actual string that the search engine will search for.
        /// </summary>
        public String SearchString
        {
            get
            {
                return sSearchString;
            }
            set
            {
                // search string should be encoded already
                sSearchString = HttpUtility.UrlDecode(value);
				
				// we only allow to search for numbers or letters no +, -, :, yet, sorry :'( trying to prevent getting hacked
                Regex regex = new Regex("[^a-zA-Z0-9 ]");
                sSearchString = regex.Replace(sSearchString, ""); // replace any matches that aren't numbers, letters or spaces
                sSearchString = HttpUtility.UrlEncode(sSearchString);

                if (sSearchString.Trim() == "")
                {
                    throw new System.ArgumentException("SearchString cannot be empty");
                }
            }
        }
        /// <summary>
        /// Returns if the search was successful after doing a Search
        /// </summary>
        public bool IsSearchSuccessfull
        {
            get
            {
                return sIsSearchSuccessful;
            }
        }

        public static string StringSanitize(string s)
        {
            Regex regex = new Regex("[^a-zA-Z0-9 ]");
            s = regex.Replace(s, ""); // replace any matches that aren't numbers, letters or spaces
            return s;
        }
        
        
        /// <summary>
        /// Verifies that the string is in appropiate form and generates an exception if it is.
        /// </summary>
        /// <param name="sSearchString">The string to be checked</param>
        private void CheckString(String sSearchString) // probably deprecated
        {
            if (sSearchString.Trim() == "")
            {
                throw new System.ArgumentException("Parameter cannot be empty", sSearchString);
            }
        }
        
        
        /// <summary>
        /// Searches for SearchString and returns html formated results.
        /// </summary>
        /// <returns>The html that will hold all the results. (set</returns>
        public String Search(int start)
        {
            nStart = start;
            String result = "";
            address = new Uri(String.Format(sQuery, sSearchString, sSite, sApiKey, nStart.ToString(), Convert.ToString(nResultsPerPage)));
            DataSet ds = GetPageAsDataSet(address);
            result = ResultsToHTML(ds);
            return result + GetPagination();
        }


        /// <summary>
        /// Generates the html string to be returned once the ds has been filled.
        /// </summary>
        /// <param name="ds">the dataset that holds the xml info the api returned</param>
        /// <returns>html string formatted results</returns>
        private String ResultsToHTML(DataSet ds)
        {
		
			// we create the html here, maybe there's another easier way to 
			// create the list... will check later
            String html = "<ul class=\"results\">";
            String response = ds.Tables["ysearchresponse"].Rows[0]["responsecode"].ToString();
            String sSuccessfulResponse = "200";     

            if (response == sSuccessfulResponse)
            {
                sIsSearchSuccessful = true;
                nTotalResults = Convert.ToInt32(ds.Tables["resultset_web"].Rows[0]["totalhits"]);

                if (nTotalResults > 0)
                {
                    if (ds.Tables["result"].Rows.Count > 0)
                    {
                        //int index = 0;

                        foreach (DataRow dr in ds.Tables["result"].Rows)
                        {
                            html += String.Format("<li><a href=\"{0}\">{1}</a><span>{2}</span>", dr[1], dr[3], dr[0]);
                        }
                        html += "</ul>";
                    }
                }
                else
                {
                    html = "<div><label>No Results</label></div>";
                }
            }
            return html;
        }
        
        
        /// <summary>
        /// Generates the call to the api using an address
        /// </summary>
        /// <param name="address">Address where the http request will be made</param>
        /// <returns>The results dataset</returns>
        private DataSet GetPageAsDataSet(Uri address)
        {
            DataSet ds = new DataSet();

            // Create the web request  
            HttpWebRequest request = WebRequest.Create(address) as HttpWebRequest;
            
			// if there's a proxy defined, we use it.
			if (webproxy != null)
            {
                request.Proxy = webproxy;
            }
            // Get response  
            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                // Load data into a dataset  
                //DataSet dsWeather = new DataSet();
                ds.ReadXml(response.GetResponseStream());
            }
            return ds;
        }

		/// <summary>
        /// Returns an html list that contains the search pagination
        /// </summary>
        public String GetPagination()
        {
            String pagination="<ul class=\"pagination\">";
            int offsetStart=0;
            int numberOfLinks = nTotalResults / nResultsPerPage;

            if (nTotalResults % nResultsPerPage > 0)
            {
                numberOfLinks += 1;
            }

            for (int i = 1; i <= numberOfLinks; i++)
            {
                pagination += paginationLink(i, offsetStart);
                offsetStart += nResultsPerPage;
            }
            //address = new Uri(String.Format(sQuery, sSearchString, sSite, sApiKey, nStart.ToString(), Convert.ToString(nResultsPerPage)));
            return pagination + "</ul>";
        }

        private String paginationLink(int i, int offsetStart)
        {
            string link = "";
           // if(nStart > offsetStart && nStart < offsetStart + nResultsPerPage){
            if(offsetStart == nStart)
            {
                link = "<li><span>" + i + "</span></li>";
            }
            else{
                link = "<li><a href=\"" + String.Format(paginationQuery, sSearchString, offsetStart) + "\" runat=\"server\">" + i.ToString() + "</a></li>";
            }
            return link;
        }



    }// end of class declaration
} // end of namespace