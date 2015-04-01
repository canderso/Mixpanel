using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Mixpanel
{
    /// <summary>
    /// People analytics updates describe a fact you've learned about one of your customers. For example, when a customer enters their first name or their birthday into your sign-in form, or signs up for a new level of service, you may send a profile update to record what you've learned.
    /// Profile updates are recorded at endpoint http://api.mixpanel.com/engage/.
    /// Source: https://mixpanel.com/help/reference/http#people-analytics-updates
    /// </summary>
    [DataContract]
    [DebuggerDisplay("Id={Id}, Operation={Operation}")]
    public class ProfileUpdate : MixpanelEntity
    {
        private const string _endpointName = "engage";

        private IDictionary<string, object> _operationValues;
        private IList<string> _unsetValueList;

        /// <summary>
        /// Gets or sets the Mixpanel token associated with your project.
        /// You can find your Mixpanel token in the project settings dialog in the Mixpanel app.
        /// </summary>
        /// <value>
        /// The token.
        /// </value>
        [DataMember]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets a string that identifies the profile you would like to update.
        /// Updates with the same $distinct_id refer to the same profile.
        /// If this $distinct_id matches a distinct_id you use in your events, those events will show up in the activity feed associated with the profile you've updated.
        /// </summary>
        /// <value>
        /// The distinct id.
        /// </value>
        [DataMember]
        public string DistinctId { get; set; }

        /// <summary>
        /// Gets or sets the $ip associated with a given profile.
        /// If $ip isn't provided, Mixpanel will use the IP address of the request. 
        /// Mixpanel uses IP to guess at the geographic location of users. 
        /// If $ip is absent or set to "0", Mixpanel will ignore IP information.
        /// </summary>
        /// <value>
        /// The ip.
        /// </value>
        [DataMember]
        public string IP { get; set; }

        /// <summary>
        /// Gets or sets milliseconds since midnight, January 1st, UTC.
        /// Updates are applied in $time order, so setting this value can lead to unexpected results unless care is taken.
        /// If $time is not included in a request, Mixpanel will use the time the update arrives at the Mixpanel server.
        /// </summary>
        /// <value>
        /// The time.
        /// </value>
        [DataMember]
        public long Time { get; set; }

        /// <summary>
        /// If the $ignore_time property is present and true in your update request, Mixpanel will not automatically update the "Last Seen" property of the profile.
        /// Otherwise, Mixpanel will add a "Last Seen" property associated with the current time for all $set, $append, and $add operations.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [ignore time]; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool IgnoreTime { get; set; }

        /// <summary>
        /// Gets or sets the desired operation.
        /// In addition to the attributes common to all updates, every update should also have a key and value associated with a particular update operation.
        /// Every call to the profile update API should have a single associated operation - you can't do two operations at once.
        /// Each operation has its own key name and format for appropriate values. 
        /// </summary>
        /// <value>
        /// The name of the operation.
        /// </value>
        [DataMember]
        public ProfileUpdateOperation Operation { get; set; }

        /// <summary>
        /// Gets or sets the value of the operation.
        /// In addition to the attributes common to all updates, every update should also have a key and value associated with a particular update operation.
        /// Every call to the profile update API should have a single associated operation - you can't do two operations at once. 
        /// Each operation has its own key name and format for appropriate values. 
        /// </summary>
        /// <value>
        /// The operation.
        /// </value>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Required for serialization purposes.")]
        [DataMember]
        public IDictionary<string, object> OperationValues 
        {
            get
            {
                if (_operationValues == null)
                {
                    _operationValues = new Dictionary<string, object>();
                }
                return _operationValues;
            }
            set
            {
                _operationValues = value;
            }
        }

        /// <summary>
        /// Gets or sets the operation value list. 
        /// Can be used for operations requiring a list and not a dictionary such as Unset.
        /// </summary>
        /// <value>
        /// The operation value list.
        /// </value>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Required for serialization purposes.")]
        [DataMember]
        public IList<string> UnsetValueList
        {
            get
            {
                if (_unsetValueList == null)
                {
                    _unsetValueList = new List<string>();
                }
                return _unsetValueList;
            }
            set
            {
                _unsetValueList = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileUpdate"/> class.
        /// </summary>
        public ProfileUpdate()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileUpdate"/> class.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="distinctId">The distinct id.</param>
        /// <param name="operation">The operation.</param>
        /// <exception cref="System.ArgumentException">
        /// String cannot be null or empty.;token
        /// or
        /// String cannot be null or empty.;distinctId
        /// </exception>
        public ProfileUpdate(string token, string distinctId, ProfileUpdateOperation operation)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentException("String cannot be null or empty.", "token");

            if (string.IsNullOrEmpty(distinctId))
                throw new ArgumentException("String cannot be null or empty.", "distinctId");

            Token = token;
            DistinctId = distinctId;
            Operation = operation;
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
        /// Gets the name of the operation.
        /// Source: https://mixpanel.com/help/reference/http#people-analytics-updates
        /// </summary>
        /// <param name="operation">The operation.</param>
        /// <returns></returns>
        public static string GetOperationName(ProfileUpdateOperation operation)
        {
            switch (operation)
            {
                case ProfileUpdateOperation.Set:
                    return "$set";
                case ProfileUpdateOperation.SetOnce:
                    return "$set_once";
                case ProfileUpdateOperation.Add:
                    return "$add";
                case ProfileUpdateOperation.Append:
                    return "$append";
                case ProfileUpdateOperation.Union:
                    return "$union";
                case ProfileUpdateOperation.Unset:
                    return "$unset";
                case ProfileUpdateOperation.Delete:
                    return "$delete";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Copies values of current instance to a dictionary.
        /// </summary>
        /// <param name="values">The values.</param>
        public override void CopyTo(IDictionary<string, object> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");

            values["$token"] = Token;
            values["$distinct_id"] = DistinctId;
            if (!string.IsNullOrEmpty(IP))
            {
                values["$ip"] = IP;
            }
            if (Time > 0)
            {
                values["$time"] = Time;
            }
            if (IgnoreTime)
            {
                values["$ignore_time"] = IgnoreTime;
            }

            string operationName = GetOperationName(Operation);
            if (Operation == ProfileUpdateOperation.Delete)
            {
                values[operationName] = string.Empty;
            }
            else if (Operation == ProfileUpdateOperation.Unset)
            {
                values[operationName] = UnsetValueList;
            }
            else
            {
                values[operationName] = OperationValues;
            }
        }
    }
}
