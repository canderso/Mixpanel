using System;

namespace Mixpanel
{
    /// <summary>
    /// Mixpanel People analytics supports the following operations on profiles
    /// Source: https://mixpanel.com/help/reference/http#people-analytics-updates
    /// </summary>
    public enum ProfileUpdateOperation
    {
        /// <summary>
        /// Takes a JSON object containing names and values of profile properties. If the profile does not exist, it creates it with these properties. If it does exist, it sets the properties to these values, overwriting existing values.
        /// </summary>
        Set = 0,
        
        /// <summary>
        /// Works just like "$set", except it will not overwrite existing property values. This is useful for properties like "First login date".
        /// </summary>
        SetOnce,

        /// <summary>
        /// Takes a JSON object containing keys and numerical values. When processed, the property values are added to the existing values of the properties on the profile. 
        /// If the property is not present on the profile, the value will be added to 0. It is possible to decrement by calling "$add" with negative values. This is useful for maintaining the values of properties like "Number of Logins" or "Files Uploaded".
        /// </summary>
        Add,

        /// <summary>
        /// Takes a JSON object containing keys and values, and appends each to a list associated with the corresponding property name. $appending to a property that doesn't exist will result in assigning a list with one element to that property.
        /// </summary>
        Append,

        /// <summary>
        /// Takes a JSON object containing keys and list values. The list values in the request are merged with the existing list on the user profile, ignoring duplicate list values.
        /// </summary>
        Union,

        /// <summary>
        /// Takes a JSON list of string property names, and permanently removes the properties and their values from a profile.
        /// </summary>
        Unset,

        /// <summary>
        /// Permanently delete the profile from Mixpanel, along with all of its properties. The value is ignored - the profile is determined by the $distinct_id from the request itself.
        /// </summary>
        Delete
    }
}
