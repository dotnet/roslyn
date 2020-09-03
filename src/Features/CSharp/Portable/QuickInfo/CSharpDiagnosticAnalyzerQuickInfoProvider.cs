// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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

namespace Microsoft.CodeAnalysis.CSharp.QuickInfo
{
    [ExportQuickInfoProvider(QuickInfoProviderNames.DiagnosticAnalyzer, LanguageNames.CSharp), Shared]
    // This provider needs to run before the semantic quick info provider, because of the SuppressMessage attribute handling
    // If it runs after it, BuildQuickInfoAsync is not called. This is not covered by a test.
    [ExtensionOrder(Before = QuickInfoProviderNames.Semantic)]
    internal class CSharpDiagnosticAnalyzerQuickInfoProvider : CommonQuickInfoProvider
    {
        private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpDiagnosticAnalyzerQuickInfoProvider(IDiagnosticAnalyzerService diagnosticAnalyzerService)
        {
            _diagnosticAnalyzerService = diagnosticAnalyzerService;
        }

        protected override async Task<QuickInfoItem?> BuildQuickInfoAsync(
            Document document,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            return GetQuickinfoForPragmaWarning(document, token) ??
                (await GetQuickInfoForSuppressMessageAttributeAsync(document, token, cancellationToken).ConfigureAwait(false));
        }

        private QuickInfoItem? GetQuickinfoForPragmaWarning(Document document, SyntaxToken token)
        {
            var errorCode = token.Parent switch
            {
                PragmaWarningDirectiveTriviaSyntax directive
                    => token.IsKind(SyntaxKind.EndOfDirectiveToken)
                        ? directive.ErrorCodes.LastOrDefault() as IdentifierNameSyntax
                        : directive.ErrorCodes.FirstOrDefault() as IdentifierNameSyntax,
                IdentifierNameSyntax { Parent: PragmaWarningDirectiveTriviaSyntax _ } identifier
                    => identifier,
                _ => null,
            };

            if (errorCode != null)
            {
                return GetQuickInfoFromSupportedDiagnosticsOfProjectAnalyzers(document, errorCode.Identifier.ValueText, errorCode.Span);
            }

            return null;
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
                        } _
                    } _
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
            var infoCache = _diagnosticAnalyzerService.AnalyzerInfoCache;
            var hostAnalyzers = document.Project.Solution.State.Analyzers;
            var groupedDiagnostics = hostAnalyzers.GetDiagnosticDescriptorsPerReference(infoCache, document.Project).Values;
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
}
