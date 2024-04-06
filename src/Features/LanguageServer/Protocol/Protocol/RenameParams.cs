// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Class representing the rename parameters for the textDocument/rename request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#renameParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class RenameParams : TextDocumentPositionParams
    {
        /// <summary>
        /// Gets or sets the new name of the renamed symbol.
        /// </summary>
        [DataMember(Name = "newName")]
        public string NewName
        {
            get;
            set;
        }
    }
}
