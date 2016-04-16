// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class ITypeSymbolExtensions
    {
        public static ExpressionSyntax GenerateExpressionSyntax(
            this ITypeSymbol typeSymbol)
        {
            return typeSymbol.Accept(ExpressionSyntaxGeneratorVisitor.Instance).WithAdditionalAnnotations(Simplifier.Annotation);
        }

        public static TypeSyntax GenerateTypeSyntax(
            this ITypeSymbol typeSymbol)
        {
            return typeSymbol.Accept(TypeSyntaxGeneratorVisitor.Instance).WithAdditionalAnnotations(Simplifier.Annotation);
        }

        public static bool ContainingTypesOrSelfHasUnsafeKeyword(this ITypeSymbol containingType)
        {
            do
            {
                foreach (var reference in containingType.DeclaringSyntaxReferences)
                {
                    if (reference.GetSyntax().ChildTokens().Any(t => t.IsKind(SyntaxKind.UnsafeKeyword)))
                    {
                        return true;
                    }
                }

                containingType = containingType.ContainingType;
            }
            while (containingType != null);
            return false;
        }

        public static async Task<ISymbol> FindApplicableAlias(this ITypeSymbol type, int position, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            try
            {
                if (semanticModel.IsSpeculativeSemanticModel)
                {
                    position = semanticModel.OriginalPositionForSpeculation;
                    semanticModel = semanticModel.ParentModel;
                }

                var root = await semanticModel.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

                IEnumerable<UsingDirectiveSyntax> applicableUsings = GetApplicableUsings(position, root as CompilationUnitSyntax);
                foreach (var applicableUsing in applicableUsings)
                {
                    var alias = semanticModel.GetOriginalSemanticModel().GetDeclaredSymbol(applicableUsing, cancellationToken);
                    if (alias != null && alias.Target == type)
                    {
                        return alias;
                    }
                }

                return null;
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static IEnumerable<UsingDirectiveSyntax> GetApplicableUsings(int position, SyntaxNode root)
        {
            var namespaceUsings = root.FindToken(position).Parent.GetAncestors<NamespaceDeclarationSyntax>().SelectMany(n => n.Usings);
            var allUsings = root is CompilationUnitSyntax
                ? ((CompilationUnitSyntax)root).Usings.Concat(namespaceUsings)
                : namespaceUsings;
            return allUsings.Where(u => u.Alias != null);
        }
    }
}
