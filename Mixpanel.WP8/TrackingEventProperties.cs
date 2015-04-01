using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Mixpanel
{
    /// <summary>
    /// Represent tracking event properties.
    /// </summary>
    [DataContract]
    public class TrackingEventProperties : MixpanelEntity
    {
#if WINDOWS_PHONE
        private const string Library = "WP/CarlAnderson";
        private const string OperatingSystem = "Windows Phone";
#elif NETFX_CORE
        private const string Library = "WinRT/CarlAnderson";
        private const string OperatingSystem = "Windows";
#elif NETFX_UNIVERSAL
        private const string Library = "WinRT-Universal/CarlAnderson";
        private const string OperatingSystem = "Windows/WindowsPhone";
#elif NETFX
        private const string Library = ".NET/CarlAnderson";
        private const string OperatingSystem = "Windows";
#endif

        private IDictionary<string, object> _all;

        /// <summary>
        /// Gets all values.
        /// </summary>
        /// <value>
        /// The values.
        /// </value>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Required by serializer.")]
        [DataMember]
        public IDictionary<string, object> All
        {
            get
            {
                if (_all == null)
                {
                    _all = new Dictionary<string, object>();
                }
                return _all;
            }
            set
            {
                _all = value;
            }
        }

        /// <summary>
        /// Gets or sets the Mixpanel token associated with your project.
        /// You can find your Mixpanel token in the project settings dialog in the Mixpanel app.
        /// Events without a valid token will be ignored.
        /// </summary>
        /// <value>
        /// The token.
        /// </value>
        [DataMember(Name = "token")]
        public string Token 
        {
            get
            {
                return (string)GetProperty("token");
            }
            set
            {
                All["token"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the value of distinct_id will be treated as a string, and used to uniquely identify a user associated with your event.
        /// If you provide a distinct_id property with your events, you can track a given user through funnels and distinguish unique users for retention analyses.
        /// You should always send the same distinct_id when an event is triggered by the same user.
        /// </summary>
        /// <value>
        /// The distinct id.
        /// </value>
        [DataMember(Name = "distinct_id")]
        public string DistinctId 
        {
            get
            {
                return (string)GetProperty("distinct_id");
            }
            set
            {
                All["distinct_id"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the time an event occurred. 
        /// If present, the value should be a unix timestamp ( seconds since midnight, January 1st, 1970, GMT). 
        /// If this property is not included in your request, Mixpanel will use the time of the event arrives at the server.
        /// </summary>
        /// <value>
        /// The time.
        /// </value>
        [DataMember(Name = "time")]
        public long Time 
        {
            get
            {
                object value = GetProperty("time");
                if (value == null)
                    return 0;
                return (long)value;
            }
            set
            {
                All["time"] = value;
            }
        }

        /// <summary>
        /// Gets or sets an ip address string (e.g. "127.0.0.1") associated with the event.
        /// This is used for adding geolocation data to events, and should only be required if you are making requests from your backend.
        /// Requests received directly from the client will use the IP from the request.
        /// </summary>
        /// <value>
        /// The ip.
        /// </value>
        [DataMember(Name = "ip")]
        public string IP 
        {
            get
            {
                return (string)GetProperty("ip");
            }
            set
            {
                All["ip"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a string. This value will appear in Mixpanel's streams report.
        /// </summary>
        /// <value>
        /// The tag.
        /// </value>
        [DataMember(Name = "mp_name_tag")]
        public string Tag 
        {
            get
            {
                return (string)GetProperty("mp_name_tag");
            }
            set
            {
                All["mp_name_tag"] = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackingEventProperties"/> class.
        /// </summary>
        public TrackingEventProperties()
        {
            All["mp_lib"] = Library;
            All["$os"] = OperatingSystem;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackingEventProperties"/> class.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <exception cref="System.ArgumentException">String cannot be null or empty.;token</exception>
        public TrackingEventProperties(string token) : this()
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("String cannot be null or empty.", "token");

            Token = token;
        }

        /// <summary>
        /// Gets the name of the endpoint.
        /// </summary>
        /// <returns></returns>
        protected override string GetEndpointName()
        {
            return null;
        }

        private object GetProperty(string key)
        {
            object value;
            All.TryGetValue(key, out value);
            return value;
        }

        /// <summary>
        /// Copies values of current instance to a dictionary.
        /// </summary>
        /// <param name="values">The values.</param>
        public override void CopyTo(IDictionary<string, object> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            foreach (KeyValuePair<string, object> kvp in All)
            {
                values[kvp.Key] = kvp.Value;
            }
        }
    }
}
