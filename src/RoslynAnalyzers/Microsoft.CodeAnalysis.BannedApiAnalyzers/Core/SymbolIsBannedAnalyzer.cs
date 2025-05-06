// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public static readonly DiagnosticDescriptor DuplicateBannedSymbolRule = new(
            id: DiagnosticIds.DuplicateBannedSymbolRuleId,
            title: CreateLocalizableResourceString(nameof(DuplicateBannedSymbolTitle)),
            messageFormat: CreateLocalizableResourceString(nameof(DuplicateBannedSymbolMessage)),
            category: "ApiDesign",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DuplicateBannedSymbolDescription)),
            helpLinkUri: "https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTagsExtensions.CompilationEndAndTelemetry);
    }

    public abstract class SymbolIsBannedAnalyzer<TSyntaxKind> : SymbolIsBannedAnalyzerBase<TSyntaxKind>
        where TSyntaxKind : struct
    {
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(SymbolIsBannedAnalyzer.SymbolIsBannedRule, SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule);

        protected sealed override DiagnosticDescriptor SymbolIsBannedRule => SymbolIsBannedAnalyzer.SymbolIsBannedRule;

#pragma warning disable RS1013 // 'compilationContext' does not register any analyzer actions, except for a 'CompilationEndAction'. Consider replacing this start/end action pair with a 'RegisterCompilationAction' or moving actions registered in 'Initialize' that depend on this start action to 'compilationContext'.
        protected sealed override Dictionary<(string ContainerName, string SymbolName), ImmutableArray<BanFileEntry>>? ReadBannedApis(
            CompilationStartAnalysisContext compilationContext)
        {
            var compilation = compilationContext.Compilation;

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
                let entry = new BanFileEntry(compilation, trimmedTextWithoutComment, span, sourceText, additionalFile.Path)
                where !string.IsNullOrWhiteSpace(entry.DeclarationId)
                select entry;

            var entries = query.ToList();

            if (entries.Count == 0)
                return null;

            var errors = new List<Diagnostic>();

            // Report any duplicates.
            var groups = entries.GroupBy(e => TrimForErrorReporting(e.DeclarationId));
            foreach (var group in groups)
            {
                if (group.Count() >= 2)
                {
                    var groupList = group.ToList();
                    var firstEntry = groupList[0];
                    for (int i = 1; i < groupList.Count; i++)
                    {
                        var nextEntry = groupList[i];
                        errors.Add(Diagnostic.Create(
                            SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule,
                            nextEntry.Location, new[] { firstEntry.Location },
                            firstEntry.Symbols.FirstOrDefault()?.ToDisplayString() ?? ""));
                    }
                }
            }

            if (errors.Count != 0)
            {
                compilationContext.RegisterCompilationEndAction(
                    endContext =>
                    {
                        foreach (var error in errors)
                            endContext.ReportDiagnostic(error);
                    });
            }

            var result = new Dictionary<(string ContainerName, string SymbolName), List<BanFileEntry>>();

            foreach (var entry in entries)
            {
                var parsed = DocumentationCommentIdParser.ParseDeclaredSymbolId(entry.DeclarationId);
                if (parsed is null)
                    continue;

                if (!result.TryGetValue(parsed.Value, out var existing))
                {
                    existing = [];
                    result.Add(parsed.Value, existing);
                }

                existing.Add(entry);
            }

            return result.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableArray());

            static string TrimForErrorReporting(string declarationId)
            {
#pragma warning disable format
                return declarationId switch
                {
                    // Remove the prefix and colon if there.
                    [_, ':', .. var rest] => rest,
                    // Colon is technically optional.  So remove just the first character if not there.
                    [_, .. var rest] => rest,
                    _ => declarationId,
                };
#pragma warning restore format
            }
        }
    }
}
