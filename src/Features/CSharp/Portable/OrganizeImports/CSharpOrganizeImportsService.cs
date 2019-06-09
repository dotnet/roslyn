// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.OrganizeImports;

namespace Microsoft.CodeAnalysis.CSharp.OrganizeImports
{
    [ExportLanguageService(typeof(IOrganizeImportsService), LanguageNames.CSharp), Shared]
    internal partial class CSharpOrganizeImportsService : IOrganizeImportsService
    {
        [ImportingConstructor]
        public CSharpOrganizeImportsService()
        {
        }

        public async Task<Document> OrganizeImportsAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var placeSystemNamespaceFirst = options.GetOption(GenerationOptions.PlaceSystemNamespaceFirst);
            var blankLineBetweenGroups = options.GetOption(GenerationOptions.SeparateImportDirectiveGroups);

            var rewriter = new Rewriter(placeSystemNamespaceFirst, blankLineBetweenGroups);
            var newRoot = rewriter.Visit(root);

            return document.WithSyntaxRoot(newRoot);
        }

        public string SortImportsDisplayStringWithAccelerator =>
            CSharpFeaturesResources.Sort_Usings;

        public string SortAndRemoveUnusedImportsDisplayStringWithAccelerator =>
            CSharpFeaturesResources.Remove_and_Sort_Usings;
    }
}
