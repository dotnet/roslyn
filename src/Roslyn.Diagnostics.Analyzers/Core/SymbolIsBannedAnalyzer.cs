// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Diagnostics.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class SymbolIsBannedAnalyzer : DiagnosticAnalyzer
    {
        internal const string BannedSymbolsFileName = "BannedSymbols.txt";

        internal static readonly DiagnosticDescriptor SymbolIsBannedRule = new DiagnosticDescriptor(
            id: RoslynDiagnosticIds.SymbolIsBannedRuleId,
            title: RoslynDiagnosticsAnalyzersResources.SymbolIsBannedTitle,
            messageFormat: RoslynDiagnosticsAnalyzersResources.SymbolIsBannedMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: RoslynDiagnosticsAnalyzersResources.SymbolIsBannedDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor DuplicateBannedSymbolRule = new DiagnosticDescriptor(
            id: RoslynDiagnosticIds.DuplicateBannedSymbolRuleId,
            title: RoslynDiagnosticsAnalyzersResources.DuplicateBannedSymbolTitle,
            messageFormat: RoslynDiagnosticsAnalyzersResources.DuplicateBannedSymbolMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: RoslynDiagnosticsAnalyzersResources.DuplicateBannedSymbolDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(SymbolIsBannedRule, DuplicateBannedSymbolRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            // Analyzer needs to get callbacks for generated code, and might report diagnostics in generated code.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext compilationContext)
        {
            var bannedSymbols = ReadBannedApis();

            if (bannedSymbols.Count == 0)
            {
                return;
            }

            var symbolDisplayFormat = compilationContext.Compilation.Language == LanguageNames.CSharp
                ? SymbolDisplayFormat.CSharpShortErrorMessageFormat
                : SymbolDisplayFormat.VisualBasicShortErrorMessageFormat;

            var bannedAttributes = bannedSymbols
                .Where(s => s.symbol is ITypeSymbol n && n.IsAttribute())
                .ToImmutableDictionary(s => s.symbol, s => s.message);

            if (bannedAttributes.Count > 0)
            {
                compilationContext.RegisterCompilationEndAction(
                    context =>
                    {
                        VerifyAttributes(context.ReportDiagnostic, compilationContext.Compilation.Assembly.GetAttributes());
                        VerifyAttributes(context.ReportDiagnostic, compilationContext.Compilation.SourceModule.GetAttributes());
                    });

                compilationContext.RegisterSymbolAction(
                    sac => VerifyAttributes(sac.ReportDiagnostic, sac.Symbol.GetAttributes()),
                    SymbolKind.NamedType,
                    SymbolKind.Method,
                    SymbolKind.Field,
                    SymbolKind.Property,
                    SymbolKind.Event);
            }

            var messageByBannedSymbol = bannedSymbols.ToDictionary(s => s.symbol, s => s.message);

            compilationContext.RegisterOperationAction(
                oac => AnalyzeOperation(oac, messageByBannedSymbol, symbolDisplayFormat),
                OperationKind.ObjectCreation,
                OperationKind.Invocation,
                OperationKind.EventReference,
                OperationKind.FieldReference,
                OperationKind.MethodReference,
                OperationKind.PropertyReference);

            void VerifyAttributes(Action<Diagnostic> reportDiagnostic, ImmutableArray<AttributeData> attributes)
            {
                foreach (AttributeData attribute in attributes)
                {
                    if (bannedAttributes.TryGetValue(attribute.AttributeClass, out var message))
                    {
                        SyntaxNode node = attribute.ApplicationSyntaxReference.GetSyntax();
                        reportDiagnostic(
                            node.CreateDiagnostic(
                                SymbolIsBannedRule,
                                attribute.AttributeClass.ToDisplayString(),
                                string.IsNullOrWhiteSpace(message) ? "" : ": " + message));
                    }
                }
            }

            ImmutableHashSet<(ISymbol symbol, string message)> ReadBannedApis()
            {
                var query =
                    from additionalFile in compilationContext.Options.AdditionalFiles
                    where StringComparer.Ordinal.Equals(Path.GetFileName(additionalFile.Path), BannedSymbolsFileName)
                    let sourceText = additionalFile.GetText(compilationContext.CancellationToken)
                    where sourceText != null
                    from line in sourceText.Lines
                    let text = line.ToString()
                    where !string.IsNullOrWhiteSpace(text)
                    select new ApiLine(text, line.Span, sourceText, additionalFile.Path);

                var apiLines = query.ToList();

                if (apiLines.Count == 0)
                {
                    return ImmutableHashSet<(ISymbol, string)>.Empty;
                }

                var lineById = new Dictionary<string, ApiLine>(StringComparer.Ordinal);
                var errors = new List<Diagnostic>();
                var builder = ImmutableHashSet.CreateBuilder<(ISymbol symbol, string message)>();

                foreach (var line in apiLines)
                {
                    if (lineById.TryGetValue(line.DeclarationId, out ApiLine existingLine))
                    {
                        errors.Add(Diagnostic.Create(DuplicateBannedSymbolRule, line.Location, new[] { existingLine.Location }, line.DeclarationId));
                        continue;
                    }

                    lineById.Add(line.DeclarationId, line);

                    var symbols = DocumentationCommentId.GetSymbolsForDeclarationId(line.DeclarationId, compilationContext.Compilation);
                    if (!symbols.IsDefaultOrEmpty)
                    {
                        foreach (var symbol in symbols)
                        {
                            builder.Add((symbol, line.Message));
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

                return builder.ToImmutable();
            }
        }

        private static void AnalyzeOperation(OperationAnalysisContext oac, Dictionary<ISymbol, string> messageByBannedSymbol, SymbolDisplayFormat symbolDisplayFormat)
        {
            switch (oac.Operation)
            {
                case IObjectCreationOperation objectCreation:
                    ValidateSymbol(objectCreation.Constructor);
                    ValidateType(objectCreation.Type.OriginalDefinition);
                    break;

                case IInvocationOperation invocation:
                    ValidateSymbol(invocation.TargetMethod);
                    ValidateType(invocation.TargetMethod.ContainingType.OriginalDefinition);
                    break;

                case IMemberReferenceOperation memberReference:
                    ValidateSymbol(memberReference.Member);
                    ValidateType(memberReference.Member.ContainingType.OriginalDefinition);
                    break;
            }

            void ValidateType(ITypeSymbol type)
            {
                while (!(type is null))
                {
                    if (messageByBannedSymbol.TryGetValue(type, out var message))
                    {
                        oac.ReportDiagnostic(
                            Diagnostic.Create(
                                SymbolIsBannedRule,
                                oac.Operation.Syntax.GetLocation(),
                                type.ToDisplayString(symbolDisplayFormat),
                                string.IsNullOrWhiteSpace(message) ? "" : ": " + message));
                        break;
                    }

                    type = type.ContainingType;
                }
            }

            void ValidateSymbol(ISymbol symbol)
            {
                if (messageByBannedSymbol.TryGetValue(symbol, out var message))
                {
                    oac.ReportDiagnostic(
                        Diagnostic.Create(
                            SymbolIsBannedRule,
                            oac.Operation.Syntax.GetLocation(),
                            symbol.ToDisplayString(symbolDisplayFormat),
                            string.IsNullOrWhiteSpace(message) ? "" : ": " + message));
                }
            }
        }

        private sealed class ApiLine
        {
            public TextSpan Span { get; }
            public SourceText SourceText { get; }
            public string Path { get; }
            public string DeclarationId { get; }
            public string Message { get; }

            public ApiLine(string text, TextSpan span, SourceText sourceText, string path)
            {
                // Split the text on semicolon into declaration ID and message
                var index = text.IndexOf(';');

                if (index == -1)
                {
                    DeclarationId = text.Trim();
                    Message = "";
                }
                else if (index == text.Length - 1)
                {
                    DeclarationId = text.Substring(0, text.Length - 1).Trim();
                    Message = "";
                }
                else
                {
                    DeclarationId = text.Substring(0, index).Trim();
                    Message = text.Substring(index + 1).Trim();
                }

                Span = span;
                SourceText = sourceText;
                Path = path;
            }

            public Location Location => Location.Create(Path, Span, SourceText.Lines.GetLinePositionSpan(Span));
        }
    }
}
