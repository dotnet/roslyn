// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
{
    private sealed partial class AssemblyReference(
        AbstractAddImportFeatureService<TSimpleNameSyntax> provider,
        SearchResult searchResult,
        ReferenceAssemblyWithTypeResult referenceAssemblyWithType) : Reference(provider, searchResult)
    {
        private readonly ReferenceAssemblyWithTypeResult _referenceAssemblyWithType = referenceAssemblyWithType;

        public override async Task<AddImportFixData> TryGetFixDataAsync(
            Document document, SyntaxNode node, CodeCleanupOptions options, CancellationToken cancellationToken)
        {
            var textChanges = await GetTextChangesAsync(document, node, options, cancellationToken).ConfigureAwait(false);

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
            => Hash.Combine(_referenceAssemblyWithType.AssemblyName, base.GetHashCode());
    }
}
