// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MethodImplementation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

internal sealed partial class CSharpCopilotNotImplementedMethodFixProvider
{
    private static class DocumentAnalyzer
    {
        private static readonly MethodImplementationOptions Options = MethodImplementationOptions.Default;

        public static async Task<MethodImplementationProposal?> AnalyzeDocumentAsync(Document document, SyntaxNode throwNode, CancellationToken cancellationToken)
        {
            // Find the containing member declaration
            var memberDeclaration = throwNode.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            if (memberDeclaration == null || !(memberDeclaration is BasePropertyDeclarationSyntax || memberDeclaration is BaseMethodDeclarationSyntax))
            {
                return null;
            }

            // Get the semantic model and syntax root
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbol = semanticModel.GetDeclaredSymbol(memberDeclaration, cancellationToken);

            // Get symbol information
            var memberSymbol = semanticModel.GetRequiredDeclaredSymbol(memberDeclaration, cancellationToken);

            // Find references
            var references = await FindReferencesAsync(document, memberSymbol, cancellationToken).ConfigureAwait(false);
            var referenceCount = references.Sum(r => r.Locations.Count());

            // Get top 2 surrounding code snippets
            var groupedReferences = references
                .SelectMany(r => r.Locations)
                .GroupBy(l => l.Document)
                .Select(async g =>
                {
                    var refDocument = g.Key;
                    var refRoot = await refDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    if (refRoot == null)
                        return [];
                    var refText = await refDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    var memberReferences = g
                        .Select(l => refRoot.FindNode(l.Location.SourceSpan).FirstAncestorOrSelf<MemberDeclarationSyntax>())
                        .WhereNotNull()
                        .Distinct()
                        .Take(Options.MaxSurroundingCodeSnippets);

                    return memberReferences.SelectAsArray(m =>
                    {
                        var contextSpan = GetContextSpan(refText, m.Span, m);
                        return new ReferenceContext
                        {
                            FileName = refDocument.Name,
                            SurroundingCode = refText.ToString(contextSpan)
                        };
                    });
                });

            var referenceContexts = (await Task.WhenAll(groupedReferences).ConfigureAwait(false))
                .SelectMany(x => x)
                .ToList();

            // Get previous and next tokens for context
            var previousToken = memberDeclaration.GetFirstToken();
            var nextToken = memberDeclaration.GetLastToken();

            // Get C# language version
            var parseOptions = document.Project.ParseOptions as CSharpParseOptions;
            var effectiveLanguageVersion = LanguageVersionFacts.MapSpecifiedToEffectiveVersion(parseOptions!.LanguageVersion);
            var languageVersion = LanguageVersionFacts.ToDisplayString(effectiveLanguageVersion);

            // Create the proposal
            var proposal = new MethodImplementationProposal
            {
                MethodName = GetMethodName(memberDeclaration),
                MethodBody = GetMethodBody(memberDeclaration),
                ExpressionBody = GetExpressionBody(memberDeclaration),
                ReturnType = GetReturnType(memberSymbol),
                Parameters = GetParameters(memberSymbol, cancellationToken),
                ReferenceCount = referenceCount,
                TopReferences = [.. referenceContexts.WhereNotNull()],
                ContainingType = TruncateString(memberDeclaration.Parent?.ToString()),
                Accessibility = memberSymbol.DeclaredAccessibility.ToString().ToLower(),
                Modifiers = memberDeclaration.Modifiers.SelectAsArray(m => m.Text),
                PreviousTokenText = previousToken.Text,
                NextTokenText = nextToken.Text,
                LanguageVersion = languageVersion
            };

            return proposal;
        }

        private static string GetMethodName(MemberDeclarationSyntax memberDeclaration) => memberDeclaration switch
        {
            BasePropertyDeclarationSyntax baseProperty => baseProperty switch
            {
                PropertyDeclarationSyntax { Identifier.Text: var propertyName } => propertyName,
                IndexerDeclarationSyntax => "this[]",
                _ => string.Empty
            },
            BaseMethodDeclarationSyntax baseMethod => baseMethod switch
            {
                MethodDeclarationSyntax { Identifier.Text: var methodName } => methodName,
                ConstructorDeclarationSyntax { Identifier.Text: var constructorName } => constructorName,
                DestructorDeclarationSyntax { Identifier.Text: var destructorName } => destructorName,
                OperatorDeclarationSyntax { OperatorToken.Text: var operatorName } => operatorName,
                ConversionOperatorDeclarationSyntax { Type: var conversionType } => conversionType.ToString(),
                _ => string.Empty
            },
            _ => string.Empty
        };

        private static string? GetMethodBody(MemberDeclarationSyntax memberDeclaration) => memberDeclaration switch
        {
            BaseMethodDeclarationSyntax { Body: var methodBody } => methodBody?.ToString(),
            BasePropertyDeclarationSyntax baseProperty => baseProperty switch
            {
                PropertyDeclarationSyntax { AccessorList: var accessorList } => accessorList?.ToString(),
                IndexerDeclarationSyntax { AccessorList: var accessorList } => accessorList?.ToString(),
                _ => null
            },
            _ => null
        };

        private static string? GetExpressionBody(MemberDeclarationSyntax memberDeclaration) => memberDeclaration switch
        {
            BaseMethodDeclarationSyntax { ExpressionBody: var methodExprBody } => methodExprBody?.ToString(),
            BasePropertyDeclarationSyntax baseProperty => baseProperty switch
            {
                PropertyDeclarationSyntax { ExpressionBody: var propertyExprBody } => propertyExprBody?.ToString(),
                IndexerDeclarationSyntax { ExpressionBody: var indexerExprBody } => indexerExprBody?.ToString(),
                _ => null
            },
            _ => null
        };

        private static string GetReturnType(ISymbol memberSymbol) => memberSymbol switch
        {
            IMethodSymbol { ReturnType: var returnType } => returnType.ToDisplayString(),
            IPropertySymbol { Type: var propertyType } => propertyType.ToDisplayString(),
            _ => string.Empty
        };

        private static ImmutableArray<ParameterContext> GetParameters(ISymbol memberSymbol, CancellationToken cancellationToken) => memberSymbol switch
        {
            IMethodSymbol { Parameters: var methodParams } => methodParams.SelectAsArray(CreateParameterContext, cancellationToken),
            IPropertySymbol { Parameters: var propertyParams } => propertyParams.SelectAsArray(CreateParameterContext, cancellationToken),
            _ => ImmutableArray<ParameterContext>.Empty
        };

        private static ParameterContext CreateParameterContext(IParameterSymbol p, CancellationToken cancellationToken)
        {
            return new ParameterContext
            {
                Name = p.Name,
                Type = p.Type.ToDisplayString(),
                Modifiers = p.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) is ParameterSyntax { Modifiers: var parameterModifiers }
                    ? [.. parameterModifiers.Select(static m => m.Text)] : []
            };
        }

        private static string TruncateString(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Length > Options.MaxContainingTypeLength ? value.Substring(0, Options.MaxContainingTypeLength) : value;
        }

        private static async Task<ImmutableArray<ReferencedSymbol>> FindReferencesAsync(Document document, ISymbol symbol, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var progress = new StreamingProgressCollector();
            await SymbolFinder.FindReferencesAsync(
                symbol, document.Project.Solution, progress, documents: null,
                FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);

            return progress.GetReferencedSymbols();
        }

        private static TextSpan GetContextSpan(SourceText text, TextSpan referenceSpan, MemberDeclarationSyntax? containingMethod)
        {
            // If we have a reasonably-sized containing method, use its full span
            if (containingMethod != null && containingMethod.Span.Length <= Options.MaxMethodLength)
            {
                return containingMethod.Span;
            }

            // Otherwise just get context around the reference
            var startLine = text.Lines.GetLineFromPosition(referenceSpan.Start).LineNumber;
            var endLine = text.Lines.GetLineFromPosition(referenceSpan.End).LineNumber;
            var expandedStart = text.Lines[Math.Max(0, startLine - Options.ContextLineCount)].Start;
            var expandedEnd = text.Lines[Math.Min(text.Lines.Count - 1, endLine + Options.ContextLineCount)].End;
            return TextSpan.FromBounds(expandedStart, expandedEnd);
        }
    }
}
