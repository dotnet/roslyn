// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
{
    private sealed partial class PackageReference(
        AbstractAddImportFeatureService<TSimpleNameSyntax> provider,
        SearchResult searchResult,
        string source,
        string packageName,
        string versionOpt,
        bool isWithinImport) : Reference(provider, searchResult, isWithinImport)
    {
        private readonly string _source = source;
        private readonly string _packageName = packageName;
        private readonly string _versionOpt = versionOpt;

        public override async Task<AddImportFixData> TryGetFixDataAsync(
            Document document, SyntaxNode node, bool cleanupDocument, CodeCleanupOptions options, CancellationToken cancellationToken)
        {
            var textChanges = await GetTextChangesAsync(
                document, node, cleanupDocument, options, cancellationToken).ConfigureAwait(false);

            return AddImportFixData.CreateForPackageSymbol(
                textChanges, _source, _packageName, _versionOpt);
        }

        public override bool Equals(object obj)
        {
            var reference = obj as PackageReference;
            return base.Equals(obj) &&
                _packageName == reference._packageName &&
                _versionOpt == reference._versionOpt;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_versionOpt,
                Hash.Combine(_packageName, base.GetHashCode()));
        }
    }
}
