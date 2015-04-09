// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.RemoveUnnecessaryImports
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpRemoveUnnecessaryImportsDiagnosticAnalyzer : RemoveUnnecessaryImportsDiagnosticAnalyzerBase
    {
        private static readonly LocalizableString s_TitleAndMessageFormat =
            new LocalizableResourceString(nameof(CSharpFeaturesResources.RemoveUnnecessaryUsingsDiagnosticTitle), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));

        protected override LocalizableString GetTitleAndMessageFormatForClassificationIdDescriptor()
        {
            return s_TitleAndMessageFormat;
        }

        protected override IEnumerable<SyntaxNode> GetUnnecessaryImports(SemanticModel semanticModel, SyntaxNode root, CancellationToken cancellationToken = default(CancellationToken))
        {
            return CSharpRemoveUnnecessaryImportsService.GetUnnecessaryImports(semanticModel, root, cancellationToken);
        }

        protected override IEnumerable<TextSpan> GetFixableDiagnosticSpans(IEnumerable<SyntaxNode> nodes, SyntaxTree tree, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfFalse(nodes.Any());

            var nodesContainingUnnecessaryUsings = new HashSet<SyntaxNode>();
            foreach (var node in nodes)
            {
                var nodeContainingUnnecessaryUsings = node.GetAncestors().First(n => n is NamespaceDeclarationSyntax || n is CompilationUnitSyntax);
                if (!nodesContainingUnnecessaryUsings.Add(nodeContainingUnnecessaryUsings))
                {
                    continue;
                }

                yield return nodeContainingUnnecessaryUsings is NamespaceDeclarationSyntax ?
                    ((NamespaceDeclarationSyntax)nodeContainingUnnecessaryUsings).Usings.GetContainedSpan() :
                    ((CompilationUnitSyntax)nodeContainingUnnecessaryUsings).Usings.GetContainedSpan();
            }
        }
    }
}
