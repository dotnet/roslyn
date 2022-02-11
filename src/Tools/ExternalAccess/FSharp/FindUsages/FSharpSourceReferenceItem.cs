// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.FindUsages
{
    internal class FSharpSourceReferenceItem
    {
        private readonly SourceReferenceItem _roslynSourceReferenceItem;

        private FSharpSourceReferenceItem(SourceReferenceItem roslynDefinitionItem)
        {
            _roslynSourceReferenceItem = roslynDefinitionItem;
        }

        public FSharpSourceReferenceItem(FSharpDefinitionItem definition, FSharpDocumentSpan sourceSpan)
        {
            _roslynSourceReferenceItem = new SourceReferenceItem(definition.RoslynDefinitionItem, sourceSpan.ToRoslynDocumentSpan(), SymbolUsageInfo.None);
        }

        internal SourceReferenceItem RoslynSourceReferenceItem
            => _roslynSourceReferenceItem;
    }
}
