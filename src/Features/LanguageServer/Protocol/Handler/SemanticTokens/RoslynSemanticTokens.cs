// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Runtime.Serialization;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    internal sealed class RoslynSemanticTokens : LSP.SemanticTokens
    {
        /// <summary>
        /// True if the token set may be incomplete.
        /// </summary>
        /// <remarks>
        /// Certain clients such as Razor need to know whether we're returning partial results.
        /// This may occur if the full compilation is not yet available.
        /// </remarks>
        [DataMember(Name = "isPartial")]
        public bool IsPartial { get; set; }
    }
}
