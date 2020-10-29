// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.DocumentSymbols
{
    [ExportLanguageService(typeof(IDocumentSymbolsService), LanguageNames.CSharp), Shared]
    internal class CSharpDocumentSymbolsService : AbstractDocumentSymbolsService
    {
        private static readonly SymbolDisplayFormat s_typeFormat =
            SymbolDisplayFormat.CSharpErrorMessageFormat.AddGenericsOptions(SymbolDisplayGenericsOptions.IncludeVariance);

        private static readonly SymbolDisplayFormat s_memberFormat =
            new(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters |
                               SymbolDisplayMemberOptions.IncludeExplicitInterface,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType |
                                  SymbolDisplayParameterOptions.IncludeName |
                                  SymbolDisplayParameterOptions.IncludeDefaultValue |
                                  SymbolDisplayParameterOptions.IncludeParamsRefOut,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                                      SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral |
                                      SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        private static readonly ImmutableArray<string> s_constantTag = ImmutableArray.Create(WellKnownTags.Constant);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpDocumentSymbolsService()
        {
        }

        protected override ISymbol? GetSymbol(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
            => node switch
            {
                NamespaceDeclarationSyntax or GlobalStatementSyntax => null,
                MemberDeclarationSyntax member => semanticModel.GetDeclaredSymbol(member, cancellationToken),
                LabeledStatementSyntax labeledStatement => semanticModel.GetDeclaredSymbol(labeledStatement, cancellationToken),
                SingleVariableDesignationSyntax singleVariable => semanticModel.GetDeclaredSymbol(singleVariable, cancellationToken),
                VariableDeclaratorSyntax variableDeclarator => semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken),
                LocalFunctionStatementSyntax localFunction => semanticModel.GetDeclaredSymbol(localFunction, cancellationToken),
                TypeParameterSyntax typeParameter => semanticModel.GetDeclaredSymbol(typeParameter, cancellationToken),
                _ => null,
            };

        protected override bool ShouldSkipSyntaxChildren(SyntaxNode node, DocumentSymbolsOptions options)
        {
            // If we're not looking for the full hierarchy, we don't care about things nested inside type members
            if (options == DocumentSymbolsOptions.TypesAndMembersOnly)
            {
                return node is BaseMethodDeclarationSyntax or BasePropertyDeclarationSyntax
                            or BaseFieldDeclarationSyntax or StatementSyntax or ExpressionSyntax;
            }

            // We don't return info about things nested inside lambdas for simplicity
            return node is LambdaExpressionSyntax or CastExpressionSyntax
                        or AttributeListSyntax or TypeParameterConstraintClauseSyntax;
        }

        protected override DocumentSymbolInfo GetMemberInfoForType(INamedTypeSymbol type, SyntaxTree tree, ISymbolDeclarationService declarationService, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<DocumentSymbolInfo>.GetInstance(out var membersBuilder);

            foreach (var member in type.GetMembers())
            {
                switch (member)
                {
                    case { IsImplicitlyDeclared: true } or { Kind: SymbolKind.NamedType } or
                         IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet }:
                        continue;

                    case IMethodSymbol { PartialImplementationPart: { } partialPart }:
                        membersBuilder.Add(CreateInfo(member, tree, declarationService, ImmutableArray<DocumentSymbolInfo>.Empty, cancellationToken));
                        membersBuilder.Add(CreateInfo(partialPart, tree, declarationService, ImmutableArray<DocumentSymbolInfo>.Empty, cancellationToken));
                        break;

                    default:
                        membersBuilder.Add(CreateInfo(member, tree, declarationService, ImmutableArray<DocumentSymbolInfo>.Empty, cancellationToken));
                        break;
                }
            }

            membersBuilder.Sort((d1, d2) => d1.Text.CompareTo(d2.Text));
            return CreateInfo(type, tree, declarationService, membersBuilder.ToImmutable(), cancellationToken);
        }

        protected override DocumentSymbolInfo CreateInfo(
            ISymbol symbol,
            SyntaxTree tree,
            ISymbolDeclarationService declarationService,
            ImmutableArray<DocumentSymbolInfo> childrenSymbols,
            CancellationToken cancellationToken)
        {
            var enclosingSpans = GetEnclosingSpansForSymbol(symbol, tree, declarationService);
            var declaringSpans = GetDeclaringSpans(symbol, tree);
            var obsolete = symbol.GetAttributes().Any(attr => attr.AttributeClass?.MetadataName == "ObsoleteAttribute");

            var isConstant = symbol is IFieldSymbol { IsConst: true } or ILocalSymbol { IsConst: true };

            return new(
                symbol.ToDisplayString(symbol is ITypeSymbol ? s_typeFormat : s_memberFormat),
                symbol.Name,
                symbol.GetGlyph(),
                obsolete,
                isConstant ? s_constantTag : ImmutableArray<string>.Empty,
                DocumentSymbolInfoExtensions.GetDocumentSymbolInfoPropertiesForSymbol(symbol, cancellationToken),
                enclosingSpans,
                declaringSpans,
                childrenSymbols);
        }

        protected override bool ConsiderNestedNodesChildren(ISymbol node) => node is not (ILabelSymbol or ILocalSymbol);

        private static ImmutableArray<TextSpan> GetEnclosingSpansForSymbol(ISymbol symbol, SyntaxTree tree, ISymbolDeclarationService declarationService)
        {
            using var _ = ArrayBuilder<TextSpan>.GetInstance(out var spans);

            if (symbol.Kind is SymbolKind.Field or SymbolKind.Local)
            {
                if (symbol.ContainingType.TypeKind == TypeKind.Enum)
                {
                    AddEnumMemberSpan(symbol, tree, spans);
                }
                else
                {
                    AddFieldOrLocalSpan(symbol, tree, spans);
                }
            }
            else
            {
                foreach (var reference in declarationService.GetDeclarations(symbol))
                {
                    if (reference.SyntaxTree.Equals(tree))
                    {
                        var span = reference.Span;

                        spans.Add(span);
                    }
                }
            }

            return spans.ToImmutable();
        }

        /// <summary>
        /// Computes a span for a given field symbol, expanding to the outer 
        /// </summary>
        private static void AddFieldOrLocalSpan(ISymbol symbol, SyntaxTree tree, ArrayBuilder<TextSpan> spans)
        {
            var reference = symbol.DeclaringSyntaxReferences.FirstOrDefault(r => r.SyntaxTree == tree);
            if (reference == null)
            {
                return;
            }

            var declaringNode = reference.GetSyntax();

            var spanStart = declaringNode.SpanStart;
            var spanEnd = declaringNode.Span.End;

            var fieldDeclaration = declaringNode.GetAncestor<FieldDeclarationSyntax>();
            if (fieldDeclaration != null)
            {
                var variables = fieldDeclaration.Declaration.Variables;

                if (variables.FirstOrDefault() == declaringNode)
                {
                    spanStart = fieldDeclaration.SpanStart;
                }

                if (variables.LastOrDefault() == declaringNode)
                {
                    spanEnd = fieldDeclaration.Span.End;
                }
            }
            else
            {
                var localDeclaration = declaringNode.GetAncestor<LocalDeclarationStatementSyntax>();

                if (localDeclaration != null)
                {
                    var variables = localDeclaration.Declaration.Variables;

                    if (variables.FirstOrDefault() == declaringNode)
                    {
                        spanStart = localDeclaration.SpanStart;
                    }

                    if (variables.LastOrDefault() == declaringNode)
                    {
                        spanEnd = localDeclaration.Span.End;
                    }
                }
            }

            spans.Add(TextSpan.FromBounds(spanStart, spanEnd));
        }

        private static void AddEnumMemberSpan(ISymbol symbol, SyntaxTree tree, ArrayBuilder<TextSpan> spans)
        {
            // Ideally we want the span of this to include the trailing comma, so let's find
            // the declaration
            var reference = symbol.DeclaringSyntaxReferences.FirstOrDefault(r => r.SyntaxTree == tree);
            if (reference == null)
            {
                return;
            }

            var declaringNode = reference.GetSyntax();
            if (declaringNode is EnumMemberDeclarationSyntax enumMember)
            {
                var enumDeclaration = enumMember.GetAncestor<EnumDeclarationSyntax>();

                if (enumDeclaration != null)
                {
                    var index = enumDeclaration.Members.IndexOf(enumMember);
                    if (index != -1 && index < enumDeclaration.Members.SeparatorCount)
                    {
                        // Cool, we have a comma, so do it
                        var start = enumMember.SpanStart;
                        var end = enumDeclaration.Members.GetSeparator(index).Span.End;

                        spans.Add(TextSpan.FromBounds(start, end));
                        return;
                    }
                }
            }

            spans.Add(declaringNode.Span);
        }
    }
}
