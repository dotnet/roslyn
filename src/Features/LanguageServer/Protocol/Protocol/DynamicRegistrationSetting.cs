// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents a setting that can be dynamically registered.
    /// </summary>
    [DataContract]
    internal class DynamicRegistrationSetting
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicRegistrationSetting"/> class.
        /// </summary>
        public DynamicRegistrationSetting()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicRegistrationSetting"/> class.
        /// </summary>
        /// <param name="value">Value indicating whether the setting can be dynamically registered.</param>
        public DynamicRegistrationSetting(bool value)
        {
            this.DynamicRegistration = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether setting can be dynamically registered.
        /// </summary>
        [DataMember(Name = "dynamicRegistration")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool DynamicRegistration
        {
            get;
            set;
        }
    }
}
