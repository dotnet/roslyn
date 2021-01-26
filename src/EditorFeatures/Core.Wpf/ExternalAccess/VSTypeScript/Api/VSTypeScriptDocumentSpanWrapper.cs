// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal sealed class VSTypeScriptDocumentSpanWrapper
    {
        public VSTypeScriptDocumentSpanWrapper(Document document, TextSpan sourceSpan)
            : this(document, sourceSpan, properties: null)
        {
        }

        public VSTypeScriptDocumentSpanWrapper(Document document, TextSpan sourceSpan, ImmutableDictionary<string, object>? properties)
        {
            UnderlyingObject = new DocumentSpan(document, sourceSpan, properties);
        }

        internal DocumentSpan UnderlyingObject { get; }
    }
}
