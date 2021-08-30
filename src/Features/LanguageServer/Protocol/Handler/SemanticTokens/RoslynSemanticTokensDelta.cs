// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    internal sealed class RoslynSemanticTokensDelta : LSP.SemanticTokensDelta
    {
        /// <summary>
        /// True if the token set is complete, meaning it's generated using a full semantic
        /// model rather than a frozen one.
        /// </summary>
        /// <remarks>
        /// Certain clients such as Razor need to know whether we're returning partial
        /// (i.e. possibly inaccurate) results. This may occur if the full compilation
        /// is not yet available.
        /// </remarks>
        [DataMember(Name = "isFinalized")]
        public bool IsFinalized { get; set; }
    }
}
