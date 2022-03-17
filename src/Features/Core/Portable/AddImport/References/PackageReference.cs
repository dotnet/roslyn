// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        private partial class PackageReference : Reference
        {
            private readonly string _source;
            private readonly string _packageName;
            private readonly string _versionOpt;

            public PackageReference(
                AbstractAddImportFeatureService<TSimpleNameSyntax> provider,
                SearchResult searchResult,
                string source,
                string packageName,
                string versionOpt)
                : base(provider, searchResult)
            {
                _source = source;
                _packageName = packageName;
                _versionOpt = versionOpt;
            }

            public override async Task<AddImportFixData> TryGetFixDataAsync(
                Document document, SyntaxNode node, AddImportPlacementOptions options, CancellationToken cancellationToken)
            {
                var textChanges = await GetTextChangesAsync(
                    document, node, options, cancellationToken).ConfigureAwait(false);

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
}
