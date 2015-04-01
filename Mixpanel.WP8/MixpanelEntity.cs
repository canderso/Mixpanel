using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Mixpanel
{
    /// <summary>
    /// Base type for tracking events and profile updates.
    /// </summary>
    [DataContract]
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Mixpanel")]
    public abstract class MixpanelEntity : IComparable, IComparable<MixpanelEntity>
    {
        private Guid _id;
        private string _fileName;

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        [DataMember]
        public Guid Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        /// <summary>
        /// Gets the name of the endpoint.
        /// </summary>
        /// <value>
        /// The name of the endpoint.
        /// </value>
        [IgnoreDataMember]
        public string EndpointName
        {
            get
            {
                return GetEndpointName();
            }
        }

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        /// <value>
        /// The name of the file.
        /// </value>
        [IgnoreDataMember]
        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(_fileName))
                {
                    _fileName = Id.ToString("N") + ".mp";
                }
                return _fileName;
            }
            set
            {
                _fileName = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MixpanelEntity"/> class.
        /// </summary>
        public MixpanelEntity()
        {
            _id = Guid.NewGuid();
        }

        /// <summary>
        /// Gets the name of the endpoint.
        /// </summary>
        /// <returns></returns>
        protected abstract string GetEndpointName();

        /// <summary>
        /// Copies instance to a dictionary.
        /// </summary>
        /// <param name="values">The values.</param>
        public abstract void CopyTo(IDictionary<string, object> values);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            // If parameter is null => false
            if (obj == null)
                return false;

            // If not a deezer entity => false
            MixpanelEntity entity = obj as MixpanelEntity;
            if (entity == null)
                return false;

            // If different deezer entities (e.g. Album / Artist) => false
            if (GetType() != entity.GetType())
                return false;

            // If keys are the same => true
            return Id.Equals(entity.Id);
        }

        /// <summary>
        /// Implementation of the equality operator.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns></returns>
        public static bool operator ==(MixpanelEntity a, MixpanelEntity b)
        {
            // If both are null, or both are same instance, return true.
            if (object.ReferenceEquals(a, b))
                return true;

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
                return false;

            return a.Equals(b);
        }

        /// <summary>
        /// Implementation of the inequality operator.
        /// </summary>
        /// <param name="a">A.</param>
        /// <param name="b">The b.</param>
        /// <returns></returns>
        public static bool operator !=(MixpanelEntity a, MixpanelEntity b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="obj" /> in the sort order. Zero This instance occurs in the same position in the sort order as <paramref name="obj" />. Greater than zero This instance follows <paramref name="obj" /> in the sort order.
        /// </returns>
        public virtual int CompareTo(object obj)
        {
            // If other is not a valid object reference, this instance is greater. 
            if (obj == null)
                return 1;

            MixpanelEntity entity = obj as MixpanelEntity;
            return CompareTo(entity);
        }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other" /> parameter.Zero This object is equal to <paramref name="other" />. Greater than zero This object is greater than <paramref name="other" />.
        /// </returns>
        public virtual int CompareTo(MixpanelEntity other)
        {
            // If other is not a valid object reference, this instance is greater. 
            if (other == null)
                return 1;

            return Id.CompareTo(other.Id);
        }
    }
}
