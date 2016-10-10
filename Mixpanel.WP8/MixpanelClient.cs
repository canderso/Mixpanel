using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if !NETFX
using Windows.Foundation;
using Windows.Storage;
#endif

#if !WINDOWS_PHONE
using System.Net.Http;
#endif

namespace Mixpanel
{
    /// <summary>
    /// Client class for the Mixpanel API.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Mixpanel")]
    public sealed class MixpanelClient
    {
        private const string BaseUrl = "https://api.mixpanel.com/";
#if WINDOWS_PHONE
        private const string DefaultUserAgentFormat = "Mozilla/5.0 (compatible; Phone; Mixpanel Windows Phone API v{0})";
#elif NETFX_CORE
        private const string DefaultUserAgentFormat = "Mozilla/5.0 (compatible; Tablet; Mixpanel WinRT API v{0})";
#elif NETFX_UNIVERSAL
        private const string DefaultUserAgentFormat = "Mozilla/5.0 (compatible; Universal; Mixpanel Universal API v{0})";
#elif NETFX
        private const string DefaultUserAgentFormat = "Mozilla/5.0 (compatible; Desktop; Mixpanel .NET API v{0})";
#endif
        private const string Version = "1.0";

        private static MixpanelClient _current;

        private bool _isGeolocationEnabled = true;
        private bool _isVerboseEnabled = false;
        private int _maxTimeout = 30; // In seconds
        private string _userAgent;
#if !WINDOWS_PHONE
        private HttpClientHandler _handler;
        private HttpClient _httpClient;
#endif

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
        /// Gets or sets the max time, in seconds, of a request before it times out.
        /// </summary>
        /// <value>
        /// The max timeout.
        /// </value>
        public int MaxTimeout
        {
            get
            {
                return _maxTimeout;
            }
            set
            {
                _maxTimeout = value;
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
#if !WINDOWS_PHONE
            _handler = new HttpClientHandler();
            if (_handler.SupportsAutomaticDecompression)
            {
                _handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }
            _httpClient = new HttpClient(_handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(_maxTimeout);
#endif
        }

        private static object _lock = new object();

        /// <summary>
        /// Gets the current Mixpanel client instance.
        /// </summary>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Properties cannot be aync.")]
        public static async Task<MixpanelClient> GetCurrentClient()
        {
            if (_current != null)
                return _current;

            _current = new MixpanelClient();
            await _current.TrySendLocalElements();
            return _current;
        }

        /// <summary>
        /// Resets the current Mixpanel client instance.
        /// </summary>
        public static void ResetClient()
        {
            ResetClient(null);
        }

        /// <summary>
        /// Resets the current Mixpanel client to a specific instance.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <returns></returns>
        public static void ResetClient(MixpanelClient client)
        {
            _current = client;
        }

        /// <summary>
        /// Tries to send locally stored elements if any and if there's a network connection.
        /// This method is called automatically when a new instance of a MixpanelClient is created.
        /// </summary>
        /// <returns></returns>
        public async Task TrySendLocalElements()
        {
            bool storeUpdated = false;
            MixpanelStore store = null;
            await Task.Run(() => store = MixpanelStore.Load());
            if (store.Events.Count > 0)
            {
                IList<TrackingEvent> sentItems = await Track<TrackingEvent>(store.Events);
                foreach (TrackingEvent item in sentItems)
                {
                    store.Events.Remove(item);
                }
                storeUpdated = true;
            }
            if (store.ProfileUpdates.Count > 0)
            {
                IList<ProfileUpdate> sentItems = await Track<ProfileUpdate>(store.ProfileUpdates);
                foreach (ProfileUpdate item in sentItems)
                {
                    store.ProfileUpdates.Remove(item);
                }
                storeUpdated = true;
            }

            if (storeUpdated)
            {
                await Task.Run(() => store.Save());
            }
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

        /// <summary>
        /// Creates an alias (newId) on an original id.
        /// More info: https://mixpanel.com/help/reference/http#distinct-id-alias
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="originalId">The original identifier.</param>
        /// <param name="newId">The new identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">token</exception>
        public async Task CreateAlias(string token, string originalId, string newId)
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
            await Track<TrackingEvent>(evt);
        }

        /// <summary>
        /// Tracks the specified element (event or profile update).
        /// More info: https://mixpanel.com/help/reference/http#tracking-via-http
        /// </summary>
        /// <typeparam name="T">Parameter should be a MixpanelEntity.</typeparam>
        /// <param name="element">The element.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">trackingEvent</exception>
        public async Task Track<T>(T element) where T : MixpanelEntity
        {
            await Track<T>(element, new Dictionary<string, string>());
        }

        /// <summary>
        /// Tracks the specified element (event or profile update).
        /// More info: https://mixpanel.com/help/reference/http#tracking-via-http
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element">The element.</param>
        /// <param name="uriParameters">The parameters. Optional.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">trackingEvent</exception>
        public async Task Track<T>(T element, IDictionary<string, string> uriParameters) where T : MixpanelEntity
        {
            if (element == null)
                throw new ArgumentNullException("element");

            if (uriParameters == null)
                throw new ArgumentNullException("uriParameters");

            Dictionary<string, object> values = new Dictionary<string, object>();
            element.CopyTo(values);

            string data = JsonConvert.SerializeObject(values);
            bool storeLocally = false;
            try
            {
                await Send(element.EndpointName, data, uriParameters);
            }
            catch (WebException) // Timeout
            {
                storeLocally = true;
            }
            catch (Exception ex)
            {
                // fatal error (i.e. wrong parameter), log it and do nothing
                Debug.WriteLine("Mixpanel> Unexpected error: " + ex);
                throw;
            }

            if (!storeLocally)
                return;

            await SaveElement<T>(element);
        }

        /// <summary>
        /// Saves element locally. Event will be sent on next call of "TrySendLocalElements".
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element">The element.</param>
        /// <returns></returns>
        public async Task SaveElement<T>(T element) where T : MixpanelEntity
        {
            // Store uses a mutex, load & save on a background thread to avoid blocking UI.
            await Task.Run(() =>
            {
                MixpanelStore store = MixpanelStore.Load();
                if (!store.Contains(element))
                {
                    store.Add(element);
                }
                store.Save();
            });
        }

        /// <summary>
        /// Sends a batch of elements to track (events or profile updates).
        /// </summary>
        /// <param name="batch">The event list.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">eventList</exception>
        public async Task<IList<T>> Track<T>(IList<T> batch) where T : MixpanelEntity
        {
            return await Track<T>(batch, new Dictionary<string, string>());
        }

        /// <summary>
        /// Sends a batch of elements to track (events or profile updates).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="batch">The event list.</param>
        /// <param name="uriParameters">The URI parameters.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">eventList</exception>
        /// <exception cref="System.ArgumentNullException">uriParameters</exception>
        public async Task<IList<T>> Track<T>(IList<T> batch, IDictionary<string, string> uriParameters) where T : MixpanelEntity
        {
            if (batch == null)
                throw new ArgumentNullException("batch");

            if (uriParameters == null)
                throw new ArgumentNullException("uriParameters");

            List<T> sentItems = new List<T>();
            if (batch.Count == 0)
                return sentItems;

            if (uriParameters == null)
            {
                uriParameters = new Dictionary<string, string>();
            }

            string endpointName = batch[0].EndpointName;
            // Create a list containing all objects ready to be serialized as JSON.
            List<Dictionary<string, object>> rawList = new List<Dictionary<string, object>>();
            for (int i = 0; i < batch.Count; i++)
            {
                Dictionary<string, object> values = new Dictionary<string, object>();
                T element = batch[i];
                element.CopyTo(values);
                rawList.Add(values);
            }

            // Both endpoints accept up to 50 messages in a single batch.
            int maxCount = 50;
            // If less than 50, send list as it is right away.
            if (batch.Count <= maxCount)
            {
                string data = JsonConvert.SerializeObject(rawList);
                try
                {
                    await SendBatch(endpointName, data, uriParameters);
                }
                catch
                {
                    // Batch could not be sent, indicate caller no items were sent.
                    return sentItems;
                }
                sentItems.AddRange(batch);
                return sentItems;
            }

            // Otherwise break it down in smaller lists of 50 elements tops.
            for (int i = 0; i < batch.Count; i += maxCount)
            {
                int upperBound = i + maxCount;
                List<Dictionary<string, object>> chunk = new List<Dictionary<string, object>>(maxCount);
                for (int j = i; j < upperBound && j < batch.Count; j++)
                {
                    chunk.Add(rawList[j]);
                }
                string data = JsonConvert.SerializeObject(chunk);
                try
                {
                    await SendBatch(endpointName, data, uriParameters);
                }
                catch
                {
                    // batch could not be sent, keep on going with other ones.
                    continue;
                }

                // Add sent items that were sent
                for (int j = i; j < upperBound && j < batch.Count; j++)
                {
                    sentItems.Add(batch[j]);
                }
            }
            return sentItems;
        }

        private async Task<bool> Send(string endpointName, string data, IDictionary<string, string> uriParameters)
        {
            if (string.IsNullOrEmpty(endpointName))
                throw new ArgumentException("String cannot be null or empty.", "endpointName");

            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("String cannot be null or empty.", "data");

            if (uriParameters == null)
                throw new ArgumentNullException("uriParameters");

            string encodedData = Utilities.ToBase64(data);

            string requestUri = BaseUrl + endpointName + "/?";
            requestUri += (IsGeolocationEnabled) ? "ip=1" : "ip=0";
            if (IsVerboseEnabled)
            {
                requestUri += "&verbose=1";
            }
            requestUri += "&data=" + encodedData;
            foreach (KeyValuePair<string, string> kvp in uriParameters)
            {
                if (string.IsNullOrEmpty(kvp.Key) || string.IsNullOrEmpty(kvp.Value))
                    continue;

                requestUri += "&" + Utilities.UrlEncode(kvp.Key) + "=" + Utilities.UrlEncode(kvp.Value);
            }

#if WINDOWS_PHONE
            // Ensure built-in cache is bypassed by passing a bogus changing parameter
            requestUri += "&ms-ts=" + (DateTime.UtcNow.Ticks / 10000);
            HttpWebRequest request = HttpWebRequest.CreateHttp(requestUri);
            request.UserAgent = UserAgent;

            MixpanelRequestState state = new MixpanelRequestState();
            state.Request = request;
            // No timeout available on HttpWebRequest object, using a signal instead.
            state.Completed = new ManualResetEvent(false);
            state.Completed.Reset();

            IAsyncResult asyncResult = (IAsyncResult)request.BeginGetResponse(new AsyncCallback(RequestCompleted), state);
            state = (MixpanelRequestState)asyncResult.AsyncState;
            await Task.Run(() =>
            {
                bool completed = false;
                if (state.Completed != null)
                {
                    completed = state.Completed.WaitOne(MaxTimeout * 1000);
                }
                if (!completed || !asyncResult.IsCompleted)
                {
                    // Timeout
                    request.Abort();
                    throw new WebException("Timeout");
                }

                if (state.Error != null)
                    throw state.Error;
            });
            string result = state.Result.ToString();
#else
            TimeSpan timeout = TimeSpan.FromSeconds(MaxTimeout);
            if (_httpClient.Timeout != timeout)
            {
                _httpClient.Timeout = timeout;
            }
            
            string result;
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                request.Headers.Add("User-Agent", UserAgent);
                using (HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead))
                {
                    response.EnsureSuccessStatusCode();
                    if (response.Content == null)
                        return false;

                    result = await response.Content.ReadAsStringAsync();
                }
            }
#endif
            return Utilities.ToBoolean(result, false);
        }

        private async Task<bool> SendBatch(string endpointName, string data, IDictionary<string, string> uriParameters)
        {
            if (string.IsNullOrEmpty(endpointName))
                throw new ArgumentException("String cannot be null or empty.", "endpointName");

            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("String cannot be null or empty.", "data");

            if (uriParameters == null)
                throw new ArgumentNullException("uriParameters");

            string requestUri = BaseUrl + endpointName + "/?ip=1";// +"&verbose=1";
            foreach (KeyValuePair<string, string> kvp in uriParameters)
            {
                if (string.IsNullOrEmpty(kvp.Key) || string.IsNullOrEmpty(kvp.Value))
                    continue;

                requestUri += "&" + Utilities.UrlEncode(kvp.Key) + "=" + Utilities.UrlEncode(kvp.Value);
            }

            // Ensure built-in cache is bypassed by passing a bogus changing parameter
            requestUri += "&ms-ts=" + (DateTime.UtcNow.Ticks / 10000);
            string requestData = "data=" + Utilities.ToBase64(data);
#if WINDOWS_PHONE
            HttpWebRequest request = HttpWebRequest.CreateHttp(requestUri);
            request.UserAgent = UserAgent;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            MixpanelRequestState state = new MixpanelRequestState();
            state.Request = request;
            state.RequestData = requestData;
            IAsyncResult asyncResult = request.BeginGetRequestStream(new AsyncCallback(BuildBatchRequestContent), state);
            state = (MixpanelRequestState)asyncResult.AsyncState;
            await Task.Run(() =>
            {
                bool completed = false;
                if (state.Completed != null)
                {
                    completed = state.Completed.WaitOne(MaxTimeout * 1000);
                }
                if (!completed || !asyncResult.IsCompleted)
                {
                    // Timeout
                    request.Abort();
                    throw new WebException("Timeout");
                }

                if (state.Error != null)
                    throw state.Error;
            });

            string result = null;
            if (state.Result != null)
            {
                result = state.Result.ToString();
            }
#else
            TimeSpan timeout = TimeSpan.FromSeconds(MaxTimeout);
            if (_httpClient.Timeout != timeout)
            {
                _httpClient.Timeout = timeout;
            }

            string result;
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUri))
            {
                request.Headers.Add("User-Agent", UserAgent);
                request.Content = new StringContent(requestData);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
                using (HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead))
                {
                    response.EnsureSuccessStatusCode();
                    if (response.Content == null)
                        return false;

                    result = await response.Content.ReadAsStringAsync();
                }
            }
#endif
            return Utilities.ToBoolean(result, false);
        }

#if WINDOWS_PHONE
        private void BuildBatchRequestContent(IAsyncResult asyncResult)
        {
            MixpanelRequestState state = (MixpanelRequestState)asyncResult.AsyncState;
            HttpWebRequest request = state.Request;

            // No timeout available on HttpWebRequest object, using a signal instead.
            state.Completed = new ManualResetEvent(false);
            try
            {
                Stream stream = request.EndGetRequestStream(asyncResult);
                byte[] data = Encoding.UTF8.GetBytes(state.RequestData);
                // Write to the request stream.
                stream.Write(data, 0, data.Length);
                stream.Flush();
                stream.Close();
            }
            catch (Exception ex)
            {
                state.Error = ex;
                state.Result = "0";
                state.Completed.Set();
                return;
            }

            state.Completed.Reset();
            request.BeginGetResponse(new AsyncCallback(RequestCompleted), state);
        }

        private void RequestCompleted(IAsyncResult asyncResult)
        {
            MixpanelRequestState state = (MixpanelRequestState)asyncResult.AsyncState;
            try
            {
                HttpWebRequest request = state.Request;
                state.Response = (HttpWebResponse)request.EndGetResponse(asyncResult);
                if ((int)state.Response.StatusCode >= 400)
                {
                    state.Error = new WebException("HTTP Error");
                    state.Result = "0";
                }
                else
                {
                    // API sends 0 or 1 as response, it's safe to read stream in a single shot.
                    using (Stream stream = state.Response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(stream, Utilities.DefaultEncoding);
                        state.Result = reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                state.Error = ex;
                state.Result = "0";
            }
            finally
            {
                if (state.Completed != null)
                {
                    state.Completed.Set();
                }
            }
        }
#endif

        [DataContract]
        internal sealed class MixpanelStore
        {
            private const string FileName = "mixpanel.dat";

            /*
             * When two or more threads need to access a shared resource at the same time (e.g. the "mixpanel.dat" file), 
             * the system needs a synchronization mechanism to ensure that only one thread at a time uses the resource. 
            * Mutex is a synchronization primitive that grants exclusive access to the shared resource to only one thread. 
            * If a thread acquires a mutex, the second thread that wants to acquire that mutex is suspended until the first thread releases the mutex.
            * 
            * Mutexes are of two types: local mutexes, which are unnamed, and named system mutexes.
            * A local mutex exists only within your process.
            * Named system mutexes are visible throughout the operating system, and can be used to synchronize the activities of processes.
            * You can create a Mutex object that represents a named system mutex by using a constructor that accepts a name.
            * Source: http://msdn.microsoft.com/en-us/library/system.threading.mutex(v=vs.95).aspx
            */

            private static Mutex _mutex = new Mutex(false, "Mixpanel_Events");

            private List<ProfileUpdate> _profileUpdates;
            private List<TrackingEvent> _events;

            [DataMember]
            public List<ProfileUpdate> ProfileUpdates
            {
                get
                {
                    if (_profileUpdates == null)
                    {
                        _profileUpdates = new List<ProfileUpdate>();
                    }
                    return _profileUpdates;
                }
                set
                {
                    _profileUpdates = value;
                }
            }

            [DataMember]
            public List<TrackingEvent> Events
            {
                get
                {
                    if (_events == null)
                    {
                        _events = new List<TrackingEvent>();
                    }
                    return _events;
                }
                set
                {
                    _events = value;
                }
            }

            public MixpanelStore()
            {
            }

            /// <summary>
            /// Loads playlogs from disk.
            /// </summary>
            /// <returns></returns>
            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "That's precisely what we want to do: have a default value in case of an error.")]
            internal static MixpanelStore Load()
            {
                MixpanelStore store = null;
                try
                {
                    // Wait until it is safe to enter.
                    _mutex.WaitOne();

                    Task<StorageFile> fileTask = Utilities.SafeGetFile(ApplicationData.Current.LocalFolder, FileName);
                    StorageFile file = fileTask.Result;
                    if (file == null)
                        return new MixpanelStore();

                    Task<Stream> streamTask = file.OpenStreamForReadAsync();
                    using (StreamReader reader = new StreamReader(streamTask.Result, Utilities.DefaultEncoding))
                    {
                        string content = reader.ReadToEnd();
                        store = JsonConvert.DeserializeObject<MixpanelStore>(content);
                    }
                }
                catch (Exception ex)
                {
                    // File is invalid: schema might have changed or something like that.
                    // Not a problem, let's return a brand new one.
                    Debug.WriteLine("Mixpanel> Could not load mixpanel events: " + ex.ToString());
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }

                if (store == null)
                {
                    store = new MixpanelStore();
                }
                return store;
            }

            /// <summary>
            /// Saves playlogs to disk.
            /// </summary>
            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            internal void Save()
            {
                // Mutex cannot be used in asynchronous methods.
                try
                {
                    // Wait until it is safe to enter.
                    _mutex.WaitOne();
                    IAsyncOperation<StorageFile> fileOperation = ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
                    StorageFile file = fileOperation.AsTask().Result;

                    Task<Stream> streamTask = file.OpenStreamForWriteAsync();
                    using (StreamWriter writer = new StreamWriter(streamTask.Result, Utilities.DefaultEncoding))
                    {
                        string content = JsonConvert.SerializeObject(this);
                        writer.Write(content);
                    }
                }
                catch (Exception ex)
                {
                    // do nothing, these playlogs will be lost..
                    Debug.WriteLine("Mixpanel> Could not save mixpanel events: " + ex.ToString());
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }
            }

            /// <summary>
            /// Adds the specified entity to the local store.
            /// </summary>
            /// <param name="entity">The entity.</param>
            /// <exception cref="System.ArgumentNullException">entity</exception>
            public void Add(MixpanelEntity entity)
            {
                if (entity == null)
                    throw new ArgumentNullException("entity");

                ProfileUpdate profileUpdate = entity as ProfileUpdate;
                if (profileUpdate != null)
                {
                    Add(profileUpdate);
                    return;
                }

                TrackingEvent trackingEvent = entity as TrackingEvent;
                if (trackingEvent != null)
                {
                    Add(trackingEvent);
                    return;
                }
            }

            /// <summary>
            /// Adds the specified profile update to the local store.
            /// </summary>
            /// <param name="profileUpdate">The profile update.</param>
            /// <exception cref="System.ArgumentNullException">profileUpdate</exception>
            public void Add(ProfileUpdate profileUpdate)
            {
                if (profileUpdate == null)
                    throw new ArgumentNullException("profileUpdate");

                ProfileUpdates.Add(profileUpdate);
            }

            /// <summary>
            /// Adds the specified tracking event to the local store.
            /// </summary>
            /// <param name="trackingEvent">The tracking event.</param>
            /// <exception cref="System.ArgumentNullException">trackingEvent</exception>
            public void Add(TrackingEvent trackingEvent)
            {
                if (trackingEvent == null)
                    throw new ArgumentNullException("trackingEvent");

                Events.Add(trackingEvent);
            }

            /// <summary>
            /// Determines whether the store contains the specified entity.
            /// </summary>
            /// <param name="entity">The entity.</param>
            /// <returns></returns>
            /// <exception cref="System.ArgumentNullException">entity</exception>
            public bool Contains(MixpanelEntity entity)
            {
                if (entity == null)
                    throw new ArgumentNullException("entity");

                ProfileUpdate profileUpdate = entity as ProfileUpdate;
                if (profileUpdate != null)
                    return Contains(profileUpdate);

                TrackingEvent trackingEvent = entity as TrackingEvent;
                if (trackingEvent != null)
                    return Contains(trackingEvent);

                return false;
            }

            /// <summary>
            /// Contains the specified profile update to the local store.
            /// </summary>
            /// <param name="profileUpdate">The profile update.</param>
            /// <exception cref="System.ArgumentNullException">profileUpdate</exception>
            public bool Contains(ProfileUpdate profileUpdate)
            {
                if (profileUpdate == null)
                    throw new ArgumentNullException("profileUpdate");

                return ProfileUpdates.Contains(profileUpdate);
            }

            /// <summary>
            /// Contains the specified tracking event to the local store.
            /// </summary>
            /// <param name="trackingEvent">The tracking event.</param>
            /// <exception cref="System.ArgumentNullException">trackingEvent</exception>
            public bool Contains(TrackingEvent trackingEvent)
            {
                if (trackingEvent == null)
                    throw new ArgumentNullException("trackingEvent");

                return Events.Contains(trackingEvent);
            }

            /// <summary>
            /// Removes the specified entity from the local store.
            /// </summary>
            /// <param name="entity">The entity.</param>
            /// <exception cref="System.ArgumentNullException">entity</exception>
            public void Remove(MixpanelEntity entity)
            {
                if (entity == null)
                    throw new ArgumentNullException("entity");

                ProfileUpdate profileUpdate = entity as ProfileUpdate;
                if (profileUpdate != null)
                {
                    Remove(profileUpdate);
                    return;
                }

                TrackingEvent trackingEvent = entity as TrackingEvent;
                if (trackingEvent != null)
                {
                    Remove(trackingEvent);
                    return;
                }
            }

            /// <summary>
            /// Removes the specified profile update from the local store.
            /// </summary>
            /// <param name="profileUpdate">The profile update.</param>
            /// <exception cref="System.ArgumentNullException">profileUpdate</exception>
            public void Remove(ProfileUpdate profileUpdate)
            {
                if (profileUpdate == null)
                    throw new ArgumentNullException("profileUpdate");

                ProfileUpdates.Remove(profileUpdate);
            }

            /// <summary>
            /// Removes the specified tracking event from the local store.
            /// </summary>
            /// <param name="trackingEvent">The tracking event.</param>
            /// <exception cref="System.ArgumentNullException">trackingEvent</exception>
            public void Remove(TrackingEvent trackingEvent)
            {
                if (trackingEvent == null)
                    throw new ArgumentNullException("trackingEvent");

                Events.Remove(trackingEvent);
            }
        }
    }
}
