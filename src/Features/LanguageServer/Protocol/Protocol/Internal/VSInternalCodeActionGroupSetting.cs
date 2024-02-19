// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class containing the set of code action default groups that are supported.
    /// </summary>
    [DataContract]
    internal class VSInternalCodeActionGroupSetting
    {
        /// <summary>
        /// Gets or sets the code actions default group names the client supports.
        /// </summary>
        [DataMember(Name = "_vs_valueSet")]
        public string[] ValueSet
        {
            get;
            set;
        }
    }
}
