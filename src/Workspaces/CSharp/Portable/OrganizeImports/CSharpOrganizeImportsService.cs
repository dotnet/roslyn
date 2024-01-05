// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.OrganizeImports;

namespace Microsoft.CodeAnalysis.CSharp.OrganizeImports
{
    [ExportLanguageService(typeof(IOrganizeImportsService), LanguageNames.CSharp), Shared]
    internal partial class CSharpOrganizeImportsService : IOrganizeImportsService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpOrganizeImportsService()
        {
        }

        public async Task<Document> OrganizeImportsAsync(Document document, OrganizeImportsOptions options, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var rewriter = new Rewriter(options);
            var newRoot = rewriter.Visit(root);

            return document.WithSyntaxRoot(newRoot);
        }

        public string SortImportsDisplayStringWithAccelerator
            => CSharpWorkspaceResources.Sort_Usings;

        public string SortAndRemoveUnusedImportsDisplayStringWithAccelerator
            => CSharpWorkspaceResources.Remove_and_Sort_Usings;
    }
}
