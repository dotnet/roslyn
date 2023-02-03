// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;

using DiagnosticIds = Roslyn.Diagnostics.Analyzers.RoslynDiagnosticIds;

namespace Microsoft.CodeAnalysis.BannedApiAnalyzers
{
    using static BannedApiAnalyzerResources;

    internal static class SymbolIsBannedAnalyzer
    {
        public static readonly DiagnosticDescriptor SymbolIsBannedRule = new(
            id: DiagnosticIds.SymbolIsBannedRuleId,
            title: CreateLocalizableResourceString(nameof(SymbolIsBannedTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(SymbolIsBannedMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(SymbolIsBannedDescription)),
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor DuplicateBannedSymbolRule = new(
            id: DiagnosticIds.DuplicateBannedSymbolRuleId,
            title: CreateLocalizableResourceString(nameof(DuplicateBannedSymbolTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(DuplicateBannedSymbolMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DuplicateBannedSymbolDescription)),
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.CompilationEndAndTelemetry);
    }

    public abstract class SymbolIsBannedAnalyzer<TSyntaxKind> : SymbolIsBannedAnalyzerBase<TSyntaxKind>
        where TSyntaxKind : struct
    {
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(SymbolIsBannedAnalyzer.SymbolIsBannedRule, SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule);

        protected sealed override DiagnosticDescriptor SymbolIsBannedRule => SymbolIsBannedAnalyzer.SymbolIsBannedRule;

#pragma warning disable RS1013 // 'compilationContext' does not register any analyzer actions, except for a 'CompilationEndAction'. Consider replacing this start/end action pair with a 'RegisterCompilationAction' or moving actions registered in 'Initialize' that depend on this start action to 'compilationContext'.
        protected sealed override Dictionary<ISymbol, BanFileEntry>? ReadBannedApis(CompilationStartAnalysisContext compilationContext)
        {
            var query =
                from additionalFile in compilationContext.Options.AdditionalFiles
                let fileName = Path.GetFileName(additionalFile.Path)
                where fileName != null && fileName.StartsWith("BannedSymbols.", StringComparison.Ordinal) && fileName.EndsWith(".txt", StringComparison.Ordinal)
                orderby additionalFile.Path // Additional files are sorted by DocumentId (which is a GUID), make the file order deterministic
                let sourceText = additionalFile.GetText(compilationContext.CancellationToken)
                where sourceText != null
                from line in sourceText.Lines
                let text = line.ToString()
                let commentIndex = text.IndexOf("//", StringComparison.Ordinal)
                let textWithoutComment = commentIndex == -1 ? text : text[..commentIndex]
                where !string.IsNullOrWhiteSpace(textWithoutComment)
                let trimmedTextWithoutComment = textWithoutComment.TrimEnd()
                let span = commentIndex == -1 ? line.Span : new Text.TextSpan(line.Span.Start, trimmedTextWithoutComment.Length)
                select new BanFileEntry(trimmedTextWithoutComment, span, sourceText, additionalFile.Path);

            var entries = query.ToList();

            if (entries.Count == 0)
            {
                return null;
            }

            var errors = new List<Diagnostic>();

            var result = new Dictionary<ISymbol, BanFileEntry>(SymbolEqualityComparer.Default);

            foreach (var line in entries)
            {
                var symbols = DocumentationCommentId.GetSymbolsForDeclarationId(line.DeclarationId, compilationContext.Compilation);

                if (!symbols.IsDefaultOrEmpty)
                {
                    foreach (var symbol in symbols)
                    {
                        if (result.TryGetValue(symbol, out var existingLine))
                        {
                            errors.Add(Diagnostic.Create(SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule, line.Location, new[] { existingLine.Location }, symbol.ToDisplayString()));
                        }
                        else
                        {
                            result.Add(symbol, line);
                        }
                    }
                }
            }

            if (errors.Count != 0)
            {
                compilationContext.RegisterCompilationEndAction(
                    endContext =>
                    {
                        foreach (var error in errors)
                        {
                            endContext.ReportDiagnostic(error);
                        }
                    });
            }

            return result;
        }
    }
}
