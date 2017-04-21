// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal partial class UsingDirectivesAdder
    {
        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly Document _document;
            private readonly IDictionary<SyntaxNode, IList<INamespaceSymbol>> _namespacesToImport;
            private readonly CancellationToken _cancellationToken;
            private readonly bool _placeSystemNamespaceFirst;

            public Rewriter(
                Document document,
                IDictionary<SyntaxNode, IList<INamespaceSymbol>> namespacesToImport,
                bool placeSystemNamespaceFirst,
                CancellationToken cancellationToken)
            {
                _document = document;
                _namespacesToImport = namespacesToImport;
                _placeSystemNamespaceFirst = placeSystemNamespaceFirst;
                _cancellationToken = cancellationToken;
            }

            private IList<UsingDirectiveSyntax> CreateDirectives(IList<INamespaceSymbol> namespaces)
            {
                var usingDirectives =
                    from n in namespaces
                    let displayString = n.ToDisplayString(SymbolDisplayFormats.NameFormat)
                    let name = SyntaxFactory.ParseName(displayString).WithAdditionalAnnotations(Simplifier.Annotation)
                    select SyntaxFactory.UsingDirective(name);

                return usingDirectives.ToList();
            }

            public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                var result = (NamespaceDeclarationSyntax)base.VisitNamespaceDeclaration(node);
                if (!_namespacesToImport.TryGetValue(node, out var namespaces))
                {
                    return result;
                }

                if (!result.CanAddUsingDirectives(_cancellationToken))
                {
                    return result;
                }

                var directives = CreateDirectives(namespaces);
                return result.AddUsingDirectives(directives, _placeSystemNamespaceFirst, Formatter.Annotation);
            }

            public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
            {
                var result = (CompilationUnitSyntax)base.VisitCompilationUnit(node);
                if (!_namespacesToImport.TryGetValue(node, out var namespaces))
                {
                    return result;
                }

                if (!result.CanAddUsingDirectives(_cancellationToken))
                {
                    return result;
                }

                var directives = CreateDirectives(namespaces);
                return result.AddUsingDirectives(directives, _placeSystemNamespaceFirst, Formatter.Annotation);
            }
        }
    }
}
