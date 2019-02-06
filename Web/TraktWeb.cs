﻿namespace TraktRater.Web
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;

    using global::TraktRater.Settings;

    public class ExtendedWebClient : WebClient
    {
        public int Timeout { private get; set; }
        private WebHeaderCollection headerContainer = new WebHeaderCollection();
        private CookieContainer cookieContainer = new CookieContainer();

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            HttpWebRequest webRequest = request as HttpWebRequest;
            if (request != null)
            {
                request.Timeout = Timeout;
                headerContainer = webRequest.Headers;
                webRequest.CookieContainer = cookieContainer;
            }
            return request;
        }

        public ExtendedWebClient()
        {
            Timeout = 100000; // the standard HTTP Request Timeout default
        }

        public WebHeaderCollection GetHeaderContainer()
        {
            return headerContainer;
        }

        public CookieContainer GetCookieContainer()
        {
            return cookieContainer;
        }
    }

    public static class TraktWeb
    {
        public static readonly Dictionary<string, string> CustomRequestHeaders = new Dictionary<string, string>();

        #region Events
        internal delegate void OnDataSendDelegate(string url, string postData);
        internal delegate void OnDataReceivedDelegate(string response);
        internal delegate void OnDataErrorReceivedDelegate(string error);

        internal static event OnDataSendDelegate OnDataSend;
        internal static event OnDataReceivedDelegate OnDataReceived;
        internal static event OnDataErrorReceivedDelegate OnDataErrorReceived;
        #endregion

        /// <summary>
        /// Communicates to and from a Server
        /// </summary>
        /// <param name="address">The URI to use</param>
        /// <param name="data">The Data to send</param>
        /// <param name="logResponse">Shall we log the response?</param>
        /// <returns>The response from Server</returns>
        public static string Transmit(string address, string data, bool logResponse = true, WebHeaderCollection headers = null)
        {
            if (OnDataSend != null)
                OnDataSend(address, data);

            try
            {
                ServicePointManager.Expect100Continue = false;
                var client = new ExtendedWebClient { Timeout = 120000, Encoding = Encoding.UTF8 };
                if (headers != null) { client.Headers = headers; }
                client.Headers.Add("user-agent", AppSettings.UserAgent);

                var response = string.Empty;

                if (string.IsNullOrEmpty(data))
                    response = client.DownloadString(address);
                else
                    response = client.UploadString(address, data);

                if (logResponse && OnDataReceived != null)
                    OnDataReceived(response);

                return response;
            }
            catch (WebException we)
            {
                string ret = null;

                // something bad happened
                var response = we.Response as HttpWebResponse;
                try
                {
                    using (var stream = response.GetResponseStream())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            ret = reader.ReadToEnd();

                            if (OnDataErrorReceived != null)
                                OnDataErrorReceived(ret);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (OnDataErrorReceived != null)
                        OnDataErrorReceived(e.Message);
                }
                return ret;
            }
         }

        public static string TransmitExtended(string address, WebHeaderCollection headers = null)
        {
            if (OnDataSend != null)
                OnDataSend(address, null);

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
                if (headers != null) { request.Headers = headers; }
                request.UserAgent = AppSettings.UserAgent;
                request.Accept = "application/json";                

                WebResponse response = (HttpWebResponse)request.GetResponse();
                if (response == null) return null;

                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string strResponse = reader.ReadToEnd();

                if (OnDataReceived != null)
                    OnDataReceived(strResponse);

                stream.Close();
                reader.Close();
                response.Close();

                return strResponse;
            }
            catch (Exception e)
            {
                if (OnDataErrorReceived != null)
                    OnDataErrorReceived(e.Message);

                return null;
            }
        }

        public static bool DeleteFromTrakt(string address)
        {
            var response = GetFromTrakt(address, "DELETE");
            return response != null;
        }

        public static string GetFromTrakt(string address, string method = "GET")
        {
            if (OnDataSend != null)
                OnDataSend(address, null);

            var request = WebRequest.Create(address) as HttpWebRequest;

            request.KeepAlive = true;
            request.Method = method;
            request.ContentLength = 0;
            request.Timeout = 120000;
            request.ContentType = "application/json";
            request.UserAgent = AppSettings.UserAgent;
            foreach (var header in CustomRequestHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            try
            {
                WebResponse response = (HttpWebResponse)request.GetResponse();
                if (response == null) return null;

                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string strResponse = reader.ReadToEnd();

                if (OnDataReceived != null)
                    OnDataReceived(strResponse);

                stream.Close();
                reader.Close();
                response.Close();

                return strResponse;
            }
            catch (WebException e)
            {
                if (OnDataErrorReceived != null)
                    OnDataErrorReceived(e.Message);

                return null;
            }
        }

        public static string PostToTrakt(string address, string postData, bool logRequest = true)
        {
            if (OnDataSend != null && logRequest)
                OnDataSend(address, postData);

            byte[] data = new UTF8Encoding().GetBytes(postData);

            var request = WebRequest.Create(address) as HttpWebRequest;
            request.KeepAlive = true;

            request.Method = "POST";
            request.ContentLength = data.Length;
            request.Timeout = 120000;
            request.ContentType = "application/json";
            request.UserAgent = AppSettings.UserAgent;
            foreach (var header in CustomRequestHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            try
            {
                // post to trakt
                Stream postStream = request.GetRequestStream();
                postStream.Write(data, 0, data.Length);
                
                // get the response
                var response = (HttpWebResponse)request.GetResponse();
                if (response == null) return null;

                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                string strResponse = reader.ReadToEnd();

                if (OnDataReceived != null)
                    OnDataReceived(strResponse);

                // cleanup
                postStream.Close();
                responseStream.Close();
                reader.Close();
                response.Close();

                return strResponse;
            }
            catch (WebException e)
            {
                if (OnDataErrorReceived != null)
                    OnDataErrorReceived(e.Message);

                return null;
            }
        }
    }
}
