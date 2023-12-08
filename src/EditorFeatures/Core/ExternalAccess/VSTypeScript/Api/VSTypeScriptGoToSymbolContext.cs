// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal sealed class VSTypeScriptGoToSymbolContext
    {
        internal DefinitionItem? DefinitionItem;

        internal VSTypeScriptGoToSymbolContext(Document document, int position, CancellationToken cancellationToken)
        {
            Document = document;
            Position = position;
            CancellationToken = cancellationToken;
        }

        public Document Document { get; }
        public int Position { get; }
        public CancellationToken CancellationToken { get; }

        public TextSpan Span { get; set; }

        public void AddItem(string key, VSTypeScriptDefinitionItem item)
        {
            _ = key;
            this.DefinitionItem = item.UnderlyingObject;
        }
    }
}
