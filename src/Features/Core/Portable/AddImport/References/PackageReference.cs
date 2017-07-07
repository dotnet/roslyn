// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
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
                Document document, SyntaxNode node, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
            {
                var textChanges = await GetTextChangesAsync(
                    document, node, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);

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
