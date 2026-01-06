// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Shared.Lightup;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal static partial class ITypeSymbolExtensions
{
    /// <paramref name="nameSyntax"><see langword="true"/> if only normal name-syntax nodes should be returned.
    /// <see langword="false"/> if special nodes (like predefined types) can be used.</paramref>
    public static ExpressionSyntax GenerateExpressionSyntax(this ITypeSymbol typeSymbol, bool nameSyntax = false)
        => typeSymbol.Accept(ExpressionSyntaxGeneratorVisitor.Create(nameSyntax))!.WithAdditionalAnnotations(Simplifier.Annotation);

    public static NameSyntax GenerateNameSyntax(this INamespaceOrTypeSymbol symbol, bool allowVar = true)
        => (NameSyntax)GenerateTypeSyntax(symbol, nameSyntax: true, allowVar: allowVar);

    public static TypeSyntax GenerateTypeSyntax(this INamespaceOrTypeSymbol symbol, bool allowVar = true)
        => GenerateTypeSyntax(symbol, nameSyntax: false, allowVar: allowVar);

    private static TypeSyntax GenerateTypeSyntax(
        INamespaceOrTypeSymbol symbol, bool nameSyntax, bool allowVar = true)
    {
        var type = symbol as ITypeSymbol;
        var containsAnonymousType = type != null && type.ContainsAnonymousType();

        // something with an anonymous type can only be represented with 'var', regardless
        // of what the user's preferences might be.
        if (containsAnonymousType && allowVar)
            return IdentifierName("var");

        var syntax = containsAnonymousType
            ? TypeSyntaxGeneratorVisitor.CreateSystemObject()
            : symbol.Accept(TypeSyntaxGeneratorVisitor.Create(nameSyntax))!.WithAdditionalAnnotations(Simplifier.Annotation);

        if (!allowVar)
            syntax = syntax.WithAdditionalAnnotations(DoNotAllowVarAnnotation.Annotation);

        if (type != null && type.IsReferenceType)
        {
            var additionalAnnotation = type.NullableAnnotation switch
            {
                NullableAnnotation.None => NullableSyntaxAnnotationEx.Oblivious,
                NullableAnnotation.Annotated => NullableSyntaxAnnotationEx.AnnotatedOrNotAnnotated,
                NullableAnnotation.NotAnnotated => NullableSyntaxAnnotationEx.AnnotatedOrNotAnnotated,
                _ => throw ExceptionUtilities.UnexpectedValue(type.NullableAnnotation),
            };

            if (additionalAnnotation is not null)
                syntax = syntax.WithAdditionalAnnotations(additionalAnnotation);
        }

        return syntax;
    }

    public static TypeSyntax GenerateRefTypeSyntax(
        this INamespaceOrTypeSymbol symbol, bool allowVar = true)
    {
        var underlyingType = GenerateTypeSyntax(symbol, allowVar)
            .WithPrependedLeadingTrivia(ElasticMarker)
            .WithAdditionalAnnotations(Simplifier.Annotation);
        var refKeyword = RefKeyword;
        return RefType(refKeyword, underlyingType);
    }

    public static TypeSyntax GenerateRefReadOnlyTypeSyntax(
        this INamespaceOrTypeSymbol symbol, bool allowVar = true)
    {
        var underlyingType = GenerateTypeSyntax(symbol, allowVar)
            .WithPrependedLeadingTrivia(ElasticMarker)
            .WithAdditionalAnnotations(Simplifier.Annotation);
        var refKeyword = RefKeyword;
        var readOnlyKeyword = ReadOnlyKeyword;
        return RefType(refKeyword, readOnlyKeyword, underlyingType);
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

    public static async Task<ISymbol?> FindApplicableAliasAsync(this ITypeSymbol type, int position, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        try
        {
            if (semanticModel.IsSpeculativeSemanticModel)
            {
                position = semanticModel.OriginalPositionForSpeculation;
                semanticModel = semanticModel.ParentModel;
            }

            var root = await semanticModel.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            var applicableUsings = GetApplicableUsings(position, (CompilationUnitSyntax)root);
            foreach (var applicableUsing in applicableUsings)
            {
                var alias = semanticModel.GetOriginalSemanticModel().GetDeclaredSymbol(applicableUsing, cancellationToken);
                if (alias != null && Equals(alias.Target, type))
                {
                    return alias;
                }
            }

            return null;
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.General))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    private static IEnumerable<UsingDirectiveSyntax> GetApplicableUsings(int position, SyntaxNode root)
    {
        var namespaceUsings = root.FindToken(position).Parent!.GetAncestors<BaseNamespaceDeclarationSyntax>().SelectMany(n => n.Usings);
        var allUsings = root is CompilationUnitSyntax compilationUnit
            ? compilationUnit.Usings.Concat(namespaceUsings)
            : namespaceUsings;
        return allUsings.Where(u => u.Alias != null);
    }
}
