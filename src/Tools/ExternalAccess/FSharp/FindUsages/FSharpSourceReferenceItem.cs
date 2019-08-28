// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

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
            _roslynSourceReferenceItem = new Microsoft.CodeAnalysis.FindUsages.SourceReferenceItem(definition.RoslynDefinitionItem, sourceSpan.ToRoslynDocumentSpan(), ImmutableDictionary<string, ImmutableArray<string>>.Empty);
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
