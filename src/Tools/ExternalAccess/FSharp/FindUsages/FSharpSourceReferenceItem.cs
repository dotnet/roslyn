// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.FindUsages
{
    internal class FSharpSourceReferenceItem
    {
        private readonly Microsoft.CodeAnalysis.FindUsages.SourceReferenceItem _roslynSourceReferenceItem;

#pragma warning disable IDE0051 // Remove unused private members
        private FSharpSourceReferenceItem(Microsoft.CodeAnalysis.FindUsages.SourceReferenceItem roslynDefinitionItem)
#pragma warning restore IDE0051 // Remove unused private members
        {
            _roslynSourceReferenceItem = roslynDefinitionItem;
        }

        public FSharpSourceReferenceItem(FSharpDefinitionItem definition, FSharpDocumentSpan sourceSpan)
        {
            _roslynSourceReferenceItem = new Microsoft.CodeAnalysis.FindUsages.SourceReferenceItem(definition.RoslynDefinitionItem, sourceSpan.ToRoslynDocumentSpan());
        }

        internal Microsoft.CodeAnalysis.FindUsages.SourceReferenceItem RoslynSourceReferenceItem
        {
            get
            {
                return _roslynSourceReferenceItem;
            }
        }
    }
}
