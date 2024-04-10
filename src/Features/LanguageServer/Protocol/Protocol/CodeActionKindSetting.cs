// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class containing the set of code action kinds that are supported.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeActionClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class CodeActionKindSetting
    {
        /// <summary>
        /// Gets or sets the code actions kinds the client supports.
        /// </summary>
        [DataMember(Name = "valueSet")]
        public CodeActionKind[] ValueSet
        {
            get;
            set;
        }
    }
}
