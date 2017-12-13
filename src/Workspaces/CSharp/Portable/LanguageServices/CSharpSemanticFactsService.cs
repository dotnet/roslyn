// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class CSharpSemanticFactsService : ISemanticFactsService
    {
        internal static readonly CSharpSemanticFactsService Instance = new CSharpSemanticFactsService();

        private CSharpSemanticFactsService()
        {
        }

        public bool SupportsImplicitInterfaceImplementation => true;

        public bool ExposesAnonymousFunctionParameterNames => false;

        public bool IsExpressionContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.SyntaxTree.IsExpressionContext(
                position,
                semanticModel.SyntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken),
                attributes: true, cancellationToken: cancellationToken, semanticModelOpt: semanticModel);
        }

        public bool IsInExpressionTree(SemanticModel semanticModel, SyntaxNode node, INamedTypeSymbol expressionTypeOpt, CancellationToken cancellationToken)
            => node.IsInExpressionTree(semanticModel, expressionTypeOpt, cancellationToken);

        public bool IsStatementContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.SyntaxTree.IsStatementContext(
                position, semanticModel.SyntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken), cancellationToken);
        }

        public bool IsTypeContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.SyntaxTree.IsTypeContext(position, cancellationToken, semanticModel);
        }

        public bool IsNamespaceContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.SyntaxTree.IsNamespaceContext(position, cancellationToken, semanticModel);
        }

        public bool IsNamespaceDeclarationNameContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.SyntaxTree.IsNamespaceDeclarationNameContext(position, cancellationToken);
        }

        public bool IsTypeDeclarationContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.SyntaxTree.IsTypeDeclarationContext(
                position, semanticModel.SyntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken), cancellationToken);
        }

        public bool IsMemberDeclarationContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.SyntaxTree.IsMemberDeclarationContext(
                position, semanticModel.SyntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken), cancellationToken);
        }

        public bool IsPreProcessorDirectiveContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.SyntaxTree.IsPreProcessorDirectiveContext(
                position, semanticModel.SyntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives: true), cancellationToken);
        }

        public bool IsGlobalStatementContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.SyntaxTree.IsGlobalStatementContext(position, cancellationToken);
        }

        public bool IsLabelContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.SyntaxTree.IsLabelContext(position, cancellationToken);
        }

        public bool IsAttributeNameContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.SyntaxTree.IsAttributeNameContext(position, cancellationToken);
        }

        public bool IsWrittenTo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
            => (node as ExpressionSyntax).IsWrittenTo();

        public bool IsOnlyWrittenTo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
            => (node as ExpressionSyntax).IsOnlyWrittenTo();

        public bool IsInOutContext(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
            => (node as ExpressionSyntax).IsInOutContext();

        public bool IsInRefContext(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
            => (node as ExpressionSyntax).IsInRefContext();

        public bool IsInInContext(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
            => (node as ExpressionSyntax).IsInInContext();

        public bool CanReplaceWithRValue(SemanticModel semanticModel, SyntaxNode expression, CancellationToken cancellationToken)
        {
            return (expression as ExpressionSyntax).CanReplaceWithRValue(semanticModel, cancellationToken);
        }

        public string GenerateNameForExpression(SemanticModel semanticModel, SyntaxNode expression, bool capitalize, CancellationToken cancellationToken)
            => semanticModel.GenerateNameForExpression((ExpressionSyntax)expression, capitalize, cancellationToken);

        public ISymbol GetDeclaredSymbol(SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
        {
            var location = token.GetLocation();
            var q = from node in token.GetAncestors<SyntaxNode>()
                    let symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken)
                    where symbol != null && symbol.Locations.Contains(location)
                    select symbol;

            return q.FirstOrDefault();
        }

        public bool LastEnumValueHasInitializer(INamedTypeSymbol namedTypeSymbol)
        {
            var enumDecl = namedTypeSymbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<EnumDeclarationSyntax>().FirstOrDefault();
            if (enumDecl != null)
            {
                var lastMember = enumDecl.Members.LastOrDefault();
                if (lastMember != null)
                {
                    return lastMember.EqualsValue != null;
                }
            }

            return false;
        }

        public bool SupportsParameterizedProperties
        {
            get
            {
                return false;
            }
        }

        public bool TryGetSpeculativeSemanticModel(SemanticModel oldSemanticModel, SyntaxNode oldNode, SyntaxNode newNode, out SemanticModel speculativeModel)
        {
            Contract.Requires(oldNode.Kind() == newNode.Kind());

            var model = oldSemanticModel;

            // currently we only support method. field support will be added later.
            var oldMethod = oldNode as BaseMethodDeclarationSyntax;
            var newMethod = newNode as BaseMethodDeclarationSyntax;
            if (oldMethod == null || newMethod == null || oldMethod.Body == null)
            {
                speculativeModel = null;
                return false;
            }

            var success = model.TryGetSpeculativeSemanticModelForMethodBody(oldMethod.Body.OpenBraceToken.Span.End, newMethod, out var csharpModel);
            speculativeModel = csharpModel;
            return success;
        }

        public ImmutableHashSet<string> GetAliasNameSet(SemanticModel model, CancellationToken cancellationToken)
        {
            var original = model.GetOriginalSemanticModel();
            if (!original.SyntaxTree.HasCompilationUnitRoot)
            {
                return ImmutableHashSet.Create<string>();
            }

            var root = original.SyntaxTree.GetCompilationUnitRoot(cancellationToken);
            var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);

            AppendAliasNames(root.Usings, builder);
            AppendAliasNames(root.Members.OfType<NamespaceDeclarationSyntax>(), builder, cancellationToken);

            return builder.ToImmutable();
        }

        private static void AppendAliasNames(SyntaxList<UsingDirectiveSyntax> usings, ImmutableHashSet<string>.Builder builder)
        {
            foreach (var @using in usings)
            {
                if (@using.Alias == null || @using.Alias.Name == null)
                {
                    continue;
                }

                @using.Alias.Name.Identifier.ValueText.AppendToAliasNameSet(builder);
            }
        }

        private void AppendAliasNames(IEnumerable<NamespaceDeclarationSyntax> namespaces, ImmutableHashSet<string>.Builder builder, CancellationToken cancellationToken)
        {
            foreach (var @namespace in namespaces)
            {
                cancellationToken.ThrowIfCancellationRequested();

                AppendAliasNames(@namespace.Usings, builder);
                AppendAliasNames(@namespace.Members.OfType<NamespaceDeclarationSyntax>(), builder, cancellationToken);
            }
        }

        public ForEachSymbols GetForEachSymbols(SemanticModel semanticModel, SyntaxNode forEachStatement)
        {
            if (forEachStatement is CommonForEachStatementSyntax csforEachStatement)
            {
                var info = semanticModel.GetForEachStatementInfo(csforEachStatement);
                return new ForEachSymbols(
                    info.GetEnumeratorMethod,
                    info.MoveNextMethod,
                    info.CurrentProperty,
                    info.DisposeMethod,
                    info.ElementType);
            }
            else
            {
                return default;
            }
        }

        public ImmutableArray<IMethodSymbol> GetDeconstructionAssignmentMethods(SemanticModel semanticModel, SyntaxNode node)
        {
            if (node is AssignmentExpressionSyntax assignment && assignment.IsDeconstruction())
            {
                var builder = ArrayBuilder<IMethodSymbol>.GetInstance();
                FlattenDeconstructionMethods(semanticModel.GetDeconstructionInfo(assignment), builder);
                return builder.ToImmutableAndFree();
            }

            return ImmutableArray<IMethodSymbol>.Empty;
        }

        public ImmutableArray<IMethodSymbol> GetDeconstructionForEachMethods(SemanticModel semanticModel, SyntaxNode node)
        {
            if (node is ForEachVariableStatementSyntax @foreach)
            {
                var builder = ArrayBuilder<IMethodSymbol>.GetInstance();
                FlattenDeconstructionMethods(semanticModel.GetDeconstructionInfo(@foreach), builder);
                return builder.ToImmutableAndFree();
            }

            return ImmutableArray<IMethodSymbol>.Empty;
        }

        private static void FlattenDeconstructionMethods(DeconstructionInfo deconstruction, ArrayBuilder<IMethodSymbol> builder)
        {
            var method = deconstruction.Method;
            if (method != null)
            {
                builder.Add(method);
            }

            foreach (var nested in deconstruction.Nested)
            {
                FlattenDeconstructionMethods(nested, builder);
            }
        }

        public bool IsAssignableTo(ITypeSymbol fromSymbol, ITypeSymbol toSymbol, Compilation compilation)
        {
            return fromSymbol != null &&
                toSymbol != null &&
                ((CSharpCompilation)compilation).ClassifyConversion(fromSymbol, toSymbol).IsImplicit;
        }

        public bool IsNameOfContext(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return semanticModel.SyntaxTree.IsNameOfContext(position, semanticModel, cancellationToken);
        }

        public bool IsPartial(ITypeSymbol typeSymbol, CancellationToken cancellationToken)
        {
            var syntaxRefs = typeSymbol.DeclaringSyntaxReferences;
            return syntaxRefs.Any(n => ((BaseTypeDeclarationSyntax)n.GetSyntax(cancellationToken)).Modifiers.Any(SyntaxKind.PartialKeyword));
        }

        public IEnumerable<ISymbol> GetDeclaredSymbols(
            SemanticModel semanticModel, SyntaxNode memberDeclaration, CancellationToken cancellationToken)
        {
            if (memberDeclaration is FieldDeclarationSyntax field)
            {
                return field.Declaration.Variables.Select(
                    v => semanticModel.GetDeclaredSymbol(v, cancellationToken));
            }

            return SpecializedCollections.SingletonEnumerable(
                semanticModel.GetDeclaredSymbol(memberDeclaration, cancellationToken));
        }
    }
}
