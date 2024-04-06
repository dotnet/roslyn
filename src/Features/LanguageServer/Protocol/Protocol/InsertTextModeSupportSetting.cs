// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class which represents initialization setting for the tag property on a completion item.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class InsertTextModeSupportSetting
    {
        /// <summary>
        /// Gets or sets a value indicating the client supports the `insertTextMode` property on a completion item to override the whitespace handling mode as defined by the client.
        /// </summary>
        [DataMember(Name = "valueSet", IsRequired = true)]
        public InsertTextMode[] ValueSet
        {
            get;
            set;
        }
    }
}
