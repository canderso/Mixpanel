using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Threading;

namespace Mixpanel
{
    /// <summary>
    /// Client class for the Mixpanel API.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Mixpanel")]
    public sealed class MixpanelClient
    {
        private const string BaseUrlFormat = "http://api.mixpanel.com/{0}/?";
        private const string DefaultUserAgentFormat = "Mozilla/5.0 (compatible; Desktop; Mixpanel .NET API v{0})";

        private const string Version = "1.0";
        private static MixpanelClient _current;

        private bool _isGeolocationEnabled = true;
        private bool _isVerboseEnabled = false;

        private string _userAgent;

        /// <summary>
        /// Gets or sets the user agent.
        /// </summary>
        /// <value>
        /// The user agent.
        /// </value>
        public string UserAgent
        {
            get
            {
                if (_userAgent == null)
                {
                    _userAgent = string.Format(CultureInfo.InvariantCulture, DefaultUserAgentFormat, Version);
                }
                return _userAgent;
            }
            set
            {
                _userAgent = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether geolocation is enabled.
        /// Default is true.
        /// </summary>
        /// <value>
        /// <c>true</c> if geolocation is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsGeolocationEnabled
        {
            get
            {
                return _isGeolocationEnabled;
            }
            set
            {
                _isGeolocationEnabled = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether verbose mode is enabled. 
        /// False by default.
        /// </summary>
        /// <value>
        /// <c>true</c> if verbose is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsVerboseEnabled
        {
            get
            {
                return _isVerboseEnabled;
            }
            set
            {
                _isVerboseEnabled = value;
            }
        }

        private MixpanelClient()
        {
        }

        /// <summary>
        /// Gets the current Mixpanel client instance.
        /// </summary>
        /// <returns></returns>
        public static MixpanelClient GetCurrentClient()
        {
            if (_current != null)
                return _current;

            _current = new MixpanelClient();
            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate
                {
                    _current.TrySendLocalElements();
                }));
            return _current;
        }

        /// <summary>
        /// Tracks the specified element (event or profile update).
        /// More info: https://mixpanel.com/help/reference/http#tracking-via-http
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">element</exception>
        public void Track(MixpanelEntity element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            WriteToFile(element);
            SendFile(element.EndpointName, element.FileName);
        }

        /// <summary>
        /// Creates an alias (newId) on an original id.
        /// More info: https://mixpanel.com/help/reference/http#distinct-id-alias
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="originalId">The original identifier.</param>
        /// <param name="newId">The new identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">token</exception>
        public void CreateAlias(string token, string originalId, string newId)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException("token");

            if (string.IsNullOrEmpty(originalId))
                throw new ArgumentNullException("originalId");

            if (string.IsNullOrEmpty(newId))
                throw new ArgumentNullException("newId");

            TrackingEventProperties properties = new TrackingEventProperties(token);
            properties.All["distinct_id"] = originalId;
            properties.All["alias"] = newId;
            TrackingEvent evt = new TrackingEvent("$create_alias", properties);
            Track(evt);
        }

        /// <summary>
        /// Properties in updates can be any of the data types valid in JSON: strings, numbers, boolean, null, arrays or objects.
        /// In addition, Mixpanel will interpret strings of a particular format as dates.
        /// This format is: YYYY-MM-DDThh:mm:ss
        /// Source: https://mixpanel.com/help/reference/http#people-analytics-updates
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Mixpanel")]
        public static string ConvertToMixpanelDate(DateTime dateTime)
        {
            return dateTime.ToString("s", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a unix based number of seconds into a .NET DateTime value (UTC).
        /// </summary>
        /// <param name="unixDateTicks">The number of seconds since January 1st, 1970.</param>
        /// <returns></returns>
        public static DateTime ConvertToDateTime(long unixDateTicks)
        {
            return Utilities.ToDateTime(unixDateTicks);
        }

        /// <summary>
        /// Converts a .NET DateTime into a unix "Epoch Time" based number of seconds.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns></returns>
        public static long ToEpochTime(DateTime date)
        {
            return Utilities.ToEpochTime(date);
        }

        private string GetUrlFormat()
        {
            string urlFormat = BaseUrlFormat;
            urlFormat += (IsGeolocationEnabled) ? "ip=1" : "ip=0";
            if (IsVerboseEnabled)
            {
                urlFormat += "&verbose=1";
            }
            urlFormat += "&data={1}";
            return urlFormat;
        }

        private bool SendFile(string endpointName, string fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");

            string data = ReadFromFile(endpointName, fileName);
            using (WebClient client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                string urlFormat = GetUrlFormat();
                string url = string.Format(CultureInfo.InvariantCulture, urlFormat, endpointName, data);
                try
                {
                    client.DownloadString(url);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MixpanelClient.Track failed: could not send event (endpoint=" + endpointName +  ", fileName=" + fileName + ", data=" + data + "). Error: " + ex);
                    return false;
                }
            }

            DeleteFile(endpointName, fileName);
            return true;
        }

        /// <summary>
        /// Tries to send locally stored elements if any and if there's a network connection.
        /// This method is called automatically when a new instance of a MixpanelClient is created.
        /// </summary>
        /// <returns></returns>
        public void TrySendLocalElements()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                string[] endpointNames = store.GetDirectoryNames();
                foreach (string endpointName in endpointNames)
                {
                    string[] files = store.GetFileNames("/" + endpointName + "/*");
                    foreach (string fileName in files)
                    {
                        SendFile(endpointName, fileName);
                    }
                }
            }
        }

        /// <summary>
        /// Saves element locally. Event will be sent on next call of "TrySendLocalElements".
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns></returns>
        public void SaveElement(MixpanelEntity element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            WriteToFile(element);
        }

        private static void WriteToFile(MixpanelEntity element)
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            element.CopyTo(values);
            string json = JsonConvert.SerializeObject(values);
            string b64 = Utilities.ToBase64(json);

            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                if (!store.DirectoryExists(element.EndpointName))
                {
                    store.CreateDirectory(element.EndpointName);
                }
                
                string path = element.EndpointName + "\\" + element.FileName;
                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(path, FileMode.Create, store))
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.Write(b64);
                    }
                }
            }
        }

        private static string ReadFromFile(string endpointName, string fileName)
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                if (!store.DirectoryExists(endpointName))
                    return null;

                string path = endpointName + "\\" + fileName;
                if (!store.FileExists(path))
                    return null;

                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(path, FileMode.Open, store))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        private static void DeleteFile(string endpointName, string fileName)
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                if (!store.DirectoryExists(endpointName))
                    return;

                string path = endpointName + "\\" + fileName;
                if (!store.FileExists(path))
                    return;

                Utilities.WrapSharingViolations(() => store.DeleteFile(path));
            }
        }
    }
}
