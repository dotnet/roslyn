// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class representing reference context information for find reference request parameter.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#referenceContext">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class ReferenceContext
    {
        /// <summary>
        /// Gets or sets a value indicating whether declaration should be included.
        /// </summary>
        [DataMember(Name = "includeDeclaration")]
        public bool IncludeDeclaration
        {
            get;
            set;
        }
    }
}
