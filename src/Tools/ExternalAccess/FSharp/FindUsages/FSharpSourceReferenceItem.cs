// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.FindUsages
{
    internal class FSharpSourceReferenceItem
    {
        private readonly Microsoft.CodeAnalysis.FindUsages.SourceReferenceItem _roslynSourceReferenceItem;

        private FSharpSourceReferenceItem(Microsoft.CodeAnalysis.FindUsages.SourceReferenceItem roslynDefinitionItem)
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
