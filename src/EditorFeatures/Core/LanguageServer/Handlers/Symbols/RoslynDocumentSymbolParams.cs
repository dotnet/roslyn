// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal class RoslynDocumentSymbolParams : DocumentSymbolParams
    {
        [DataMember(Name = "useHierarchicalSymbols")]
        public bool UseHierarchicalSymbols { get; set; }
    }
}
