// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.GoToDefinition;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal sealed class VSTypeScriptGoToSymbolContext
    {
        internal readonly GoToSymbolContext UnderlyingObject;

        internal VSTypeScriptGoToSymbolContext(GoToSymbolContext underlyingObject)
            => UnderlyingObject = underlyingObject;

        public Document Document => UnderlyingObject.Document;
        public int Position => UnderlyingObject.Position;
        public CancellationToken CancellationToken => UnderlyingObject.CancellationToken;

        public TextSpan Span
        {
            get => UnderlyingObject.Span;
            set => UnderlyingObject.Span = value;
        }

        public void AddItem(string key, VSTypeScriptDefinitionItem item)
            => UnderlyingObject.AddItem(key, item.UnderlyingObject);
    }
}
