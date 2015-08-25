// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.OrganizeImports;

namespace Microsoft.CodeAnalysis.CSharp.OrganizeImports
{
    [ExportLanguageService(typeof(IOrganizeImportsService), LanguageNames.CSharp), Shared]
    internal partial class CSharpOrganizeImportsService : IOrganizeImportsService
    {
        public async Task<Document> OrganizeImportsAsync(Document document, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var rewriter = new Rewriter(placeSystemNamespaceFirst);
            var newRoot = rewriter.Visit(root);

            return document.WithSyntaxRoot(newRoot);
        }

        public string OrganizeImportsDisplayStringWithAccelerator
        {
            get
            {
                return CSharpFeaturesResources.OrganizeUsingsWithAccelerator;
            }
        }

        public string SortImportsDisplayStringWithAccelerator
        {
            get
            {
                return CSharpFeaturesResources.SortUsingsWithAccelerator;
            }
        }

        public string RemoveUnusedImportsDisplayStringWithAccelerator
        {
            get
            {
                return CSharpFeaturesResources.RemoveUnnecessaryUsingsWithAccelerator;
            }
        }

        public string SortAndRemoveUnusedImportsDisplayStringWithAccelerator
        {
            get
            {
                return CSharpFeaturesResources.RemoveAndSortUsingsWithAccelerator;
            }
        }
    }
}
