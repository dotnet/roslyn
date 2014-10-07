// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal partial class UsingDirectivesAdder
    {
        private class Rewriter : CSharpSyntaxRewriter
        {
            private readonly Document document;
            private readonly IDictionary<SyntaxNode, IList<INamespaceSymbol>> namespacesToImport;
            private readonly CancellationToken cancellationToken;
            private readonly bool placeSystemNamespaceFirst;

            public Rewriter(
                Document document,
                IDictionary<SyntaxNode, IList<INamespaceSymbol>> namespacesToImport,
                bool placeSystemNamespaceFirst,
                CancellationToken cancellationToken)
            {
                this.document = document;
                this.namespacesToImport = namespacesToImport;
                this.placeSystemNamespaceFirst = placeSystemNamespaceFirst;
                this.cancellationToken = cancellationToken;
            }

            private IList<UsingDirectiveSyntax> CreateDirectives(IList<INamespaceSymbol> namespaces)
            {
                var usingDirectives =
                    from n in namespaces
                    let displayString = n.ToDisplayString(SymbolDisplayFormats.NameFormat)
                    let name = SyntaxFactory.ParseName(displayString)
                    select SyntaxFactory.UsingDirective(name);

                return usingDirectives.ToList();
            }

            public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                var result = (NamespaceDeclarationSyntax)base.VisitNamespaceDeclaration(node);

                IList<INamespaceSymbol> namespaces;
                if (!namespacesToImport.TryGetValue(node, out namespaces))
                {
                    return result;
                }

                if (!result.CanAddUsingDirectives(cancellationToken))
                {
                    return result;
                }

                var directives = CreateDirectives(namespaces);
                return result.AddUsingDirectives(directives, placeSystemNamespaceFirst, Formatter.Annotation);
            }

            public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
            {
                var result = (CompilationUnitSyntax)base.VisitCompilationUnit(node);

                IList<INamespaceSymbol> namespaces;
                if (!namespacesToImport.TryGetValue(node, out namespaces))
                {
                    return result;
                }

                if (!result.CanAddUsingDirectives(cancellationToken))
                {
                    return result;
                }

                var directives = CreateDirectives(namespaces);
                return result.AddUsingDirectives(directives, placeSystemNamespaceFirst, Formatter.Annotation);
            }
        }
    }
}