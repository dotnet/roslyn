// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        private partial class AssemblyReference : Reference
        {
            private readonly ReferenceAssemblyWithTypeResult _referenceAssemblyWithType;

            public AssemblyReference(
                AbstractAddImportFeatureService<TSimpleNameSyntax> provider,
                SearchResult searchResult,
                ReferenceAssemblyWithTypeResult referenceAssemblyWithType)
                : base(provider, searchResult)
            {
                _referenceAssemblyWithType = referenceAssemblyWithType;
            }

            public override async Task<AddImportFixData> TryGetFixDataAsync(
                Document document, SyntaxNode node, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
            {
                var textChanges = await GetTextChangesAsync(
                    document, node, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);

                var title = $"{provider.GetDescription(SearchResult.NameParts)} ({string.Format(FeaturesResources.from_0, _referenceAssemblyWithType.AssemblyName)})";
                var fullyQualifiedTypeName = string.Join(
                    ".", _referenceAssemblyWithType.ContainingNamespaceNames.Concat(_referenceAssemblyWithType.TypeName));

                return AddImportFixData.CreateForReferenceAssemblySymbol(
                    textChanges, title, _referenceAssemblyWithType.AssemblyName, fullyQualifiedTypeName);
            }

            public override bool Equals(object obj)
            {
                var reference = obj as AssemblyReference;
                return base.Equals(obj) &&
                    _referenceAssemblyWithType.AssemblyName == reference._referenceAssemblyWithType.AssemblyName;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(_referenceAssemblyWithType.AssemblyName, base.GetHashCode());
            }
        }
    }
}
