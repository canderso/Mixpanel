using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Mixpanel
{
    /// <summary>
    /// Events describe things that happen in your application, usually as the result of user interaction; 
    /// for example, when a customer reads an article, uploads content, or signs up for your service, you can send an event to record the incident. 
    /// Events are tracked at endpoint http://api.mixpanel.com/track/
    /// Source: https://mixpanel.com/help/reference/http#tracking-events
    /// </summary>
    [DataContract]
    [DebuggerDisplay("Id={Id}, EventName={EventName}")]
    public class TrackingEvent : MixpanelEntity
    {
        private const string _endpointName = "track";

        /// <summary>
        /// Gets or sets the name of the event.
        /// </summary>
        /// <value>
        /// The name of the event.
        /// </value>
        [DataMember(Name="event")]
        public string EventName { get; set; }

        /// <summary>
        /// Gets or sets the properties.
        /// </summary>
        /// <value>
        /// The properties.
        /// </value>
        [DataMember(Name="properties")]
        public TrackingEventProperties Properties { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackingEvent"/> class.
        /// </summary>
        public TrackingEvent() : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackingEvent"/> class.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        public TrackingEvent(string eventName) : this(eventName, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackingEvent"/> class.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="properties">The properties.</param>
        public TrackingEvent(string eventName, TrackingEventProperties properties)
        {
            EventName = eventName;
            Properties = properties;
        }

        /// <summary>
        /// Gets the name of the endpoint.
        /// </summary>
        /// <returns></returns>
        protected override string GetEndpointName()
        {
            return _endpointName;
        }

        /// <summary>
        /// Copies values of current instance to a dictionary.
        /// </summary>
        /// <param name="values">The values.</param>
        public override void CopyTo(IDictionary<string, object> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            values["event"] = EventName;
            if (Properties != null)
            {
                Dictionary<string, object> propertyValues = new Dictionary<string,object>();
                Properties.CopyTo(propertyValues);
                values["properties"] = propertyValues;
            }
        }
    }
}
