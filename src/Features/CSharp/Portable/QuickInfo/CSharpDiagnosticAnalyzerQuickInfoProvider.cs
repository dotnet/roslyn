// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo;

[ExportQuickInfoProvider(QuickInfoProviderNames.DiagnosticAnalyzer, LanguageNames.CSharp), Shared]
// This provider needs to run before the semantic quick info provider, because of the SuppressMessage attribute handling
// If it runs after it, BuildQuickInfoAsync is not called. This is not covered by a test.
[ExtensionOrder(Before = QuickInfoProviderNames.Semantic)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal class CSharpDiagnosticAnalyzerQuickInfoProvider(DiagnosticAnalyzerInfoCache.SharedGlobalCache globalCache) : CommonQuickInfoProvider
{
    private readonly DiagnosticAnalyzerInfoCache _diagnosticAnalyzerInfoCache = globalCache.AnalyzerInfoCache;

    protected override async Task<QuickInfoItem?> BuildQuickInfoAsync(
        QuickInfoContext context,
        SyntaxToken token)
    {
        var document = context.Document;
        return GetQuickinfoForPragmaWarning(document, token) ??
            (await GetQuickInfoForSuppressMessageAttributeAsync(document, token, context.CancellationToken).ConfigureAwait(false));
    }

    protected override Task<QuickInfoItem?> BuildQuickInfoAsync(
        CommonQuickInfoContext context,
        SyntaxToken token)
    {
        // TODO: This provider currently needs access to Document/Project to compute applicable analyzers
        //       and provide quick info, which is not available in CommonQuickInfoContext.
        return Task.FromResult<QuickInfoItem?>(null);
    }

    private QuickInfoItem? GetQuickinfoForPragmaWarning(Document document, SyntaxToken token)
    {
        var errorCodeNode = token.Parent switch
        {
            PragmaWarningDirectiveTriviaSyntax directive
                => token.IsKind(SyntaxKind.EndOfDirectiveToken)
                    ? directive.ErrorCodes.LastOrDefault()
                    : directive.ErrorCodes.FirstOrDefault(),
            { Parent: PragmaWarningDirectiveTriviaSyntax } node => node,
            _ => null,
        };
        if (errorCodeNode is null)
        {
            return null;
        }

        // https://docs.microsoft.com/en-US/dotnet/csharp/language-reference/preprocessor-directives/preprocessor-pragma-warning
        // warning-list: A comma-separated list of warning numbers. The "CS" prefix is optional.
        // errorCodeNode is single error code from the comma separated list
        var errorCode = errorCodeNode switch
        {
            // case CS0219 or SA0012:
            IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
            // case 0219 or 219:
            // Take the number and add the "CS" prefix.
            LiteralExpressionSyntax(SyntaxKind.NumericLiteralExpression) literal
                => int.TryParse(literal.Token.ValueText, out var errorCodeNumber)
                    ? $"CS{errorCodeNumber:0000}"
                    : literal.Token.ValueText,
            _ => null,
        };
        if (errorCode is null)
        {
            return null;
        }

        return GetQuickInfoFromSupportedDiagnosticsOfProjectAnalyzers(document, errorCode, errorCodeNode.Span);
    }

    private async Task<QuickInfoItem?> GetQuickInfoForSuppressMessageAttributeAsync(
        Document document,
        SyntaxToken token,
        CancellationToken cancellationToken)
    {
        // SuppressMessageAttribute docs 
        // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.suppressmessageattribute
        var suppressMessageCheckIdArgument = token.GetAncestor<AttributeArgumentSyntax>() switch
        {
            AttributeArgumentSyntax
            {
                Parent: AttributeArgumentListSyntax
                {
                    Arguments: var arguments,
                    Parent: AttributeSyntax
                    {
                        Name: var attributeName
                    }
                }
            } argument when
                attributeName.IsSuppressMessageAttribute() &&
                (argument.NameColon is null
                    ? arguments.IndexOf(argument) == 1 // Positional argument "checkId"
                    : argument.NameColon.Name.Identifier.ValueText == "checkId") // Named argument "checkId"
                => argument,
            _ => null,
        };

        if (suppressMessageCheckIdArgument != null)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var checkIdObject = semanticModel.GetConstantValue(suppressMessageCheckIdArgument.Expression, cancellationToken);
            if (checkIdObject.HasValue && checkIdObject.Value is string checkId)
            {
                var errorCode = checkId.ExtractErrorCodeFromCheckId();
                return GetQuickInfoFromSupportedDiagnosticsOfProjectAnalyzers(document, errorCode, suppressMessageCheckIdArgument.Span);
            }
        }

        return null;
    }

    private QuickInfoItem? GetQuickInfoFromSupportedDiagnosticsOfProjectAnalyzers(Document document,
        string errorCode, TextSpan location)
    {
        var hostAnalyzers = document.Project.Solution.SolutionState.Analyzers;
        var groupedDiagnostics = hostAnalyzers.GetDiagnosticDescriptorsPerReference(_diagnosticAnalyzerInfoCache, document.Project).Values;
        var supportedDiagnostics = groupedDiagnostics.SelectMany(d => d);
        var diagnosticDescriptor = supportedDiagnostics.FirstOrDefault(d => d.Id == errorCode);
        if (diagnosticDescriptor != null)
        {
            return CreateQuickInfo(location, diagnosticDescriptor);
        }

        return null;
    }

    private static QuickInfoItem CreateQuickInfo(TextSpan location, DiagnosticDescriptor descriptor,
        params TextSpan[] relatedSpans)
    {
        var description =
            descriptor.Title.ToStringOrNull() ??
            descriptor.Description.ToStringOrNull() ??
            descriptor.MessageFormat.ToStringOrNull() ??
            descriptor.Id;
        var idTag = !string.IsNullOrWhiteSpace(descriptor.HelpLinkUri)
            ? new TaggedText(TextTags.Text, descriptor.Id, TaggedTextStyle.None, descriptor.HelpLinkUri, descriptor.HelpLinkUri)
            : new TaggedText(TextTags.Text, descriptor.Id);
        return QuickInfoItem.Create(location, sections: new[]
            {
                QuickInfoSection.Create(QuickInfoSectionKinds.Description, new[]
                {
                    idTag,
                    new TaggedText(TextTags.Punctuation, ":"),
                    new TaggedText(TextTags.Space, " "),
                    new TaggedText(TextTags.Text, description)
                }.ToImmutableArray())
            }.ToImmutableArray(), relatedSpans: relatedSpans.ToImmutableArray());
    }
}
