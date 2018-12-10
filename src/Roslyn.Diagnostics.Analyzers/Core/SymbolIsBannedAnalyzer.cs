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
    internal static class SymbolIsBannedAnalyzer
    {
        public const string BannedSymbolsFileName = "BannedSymbols.txt";

        public static readonly DiagnosticDescriptor SymbolIsBannedRule = new DiagnosticDescriptor(
            id: RoslynDiagnosticIds.SymbolIsBannedRuleId,
            title: RoslynDiagnosticsAnalyzersResources.SymbolIsBannedTitle,
            messageFormat: RoslynDiagnosticsAnalyzersResources.SymbolIsBannedMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: RoslynDiagnosticsAnalyzersResources.SymbolIsBannedDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor DuplicateBannedSymbolRule = new DiagnosticDescriptor(
            id: RoslynDiagnosticIds.DuplicateBannedSymbolRuleId,
            title: RoslynDiagnosticsAnalyzersResources.DuplicateBannedSymbolTitle,
            messageFormat: RoslynDiagnosticsAnalyzersResources.DuplicateBannedSymbolMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: RoslynDiagnosticsAnalyzersResources.DuplicateBannedSymbolDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);
    }

    public abstract class SymbolIsBannedAnalyzer<TSyntaxKind> : DiagnosticAnalyzer
        where TSyntaxKind : struct
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(SymbolIsBannedAnalyzer.SymbolIsBannedRule, SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule);

        protected abstract TSyntaxKind XmlCrefSyntaxKind { get; }

        protected abstract SyntaxNode GetReferenceSyntaxNodeFromXmlCref(SyntaxNode syntaxNode);

        protected abstract SymbolDisplayFormat SymbolDisplayFormat { get; }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            // Analyzer needs to get callbacks for generated code, and might report diagnostics in generated code.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext compilationContext)
        {
            var bannedSymbols = ReadBannedApis();

            if (bannedSymbols.Count == 0)
            {
                return;
            }

            var messageByBannedSymbol = bannedSymbols.ToDictionary(s => s.symbol, s => s.message);

            var bannedAttributes = bannedSymbols
                .Where(s => s.symbol is ITypeSymbol n && n.IsAttribute())
                .ToImmutableDictionary(s => s.symbol, s => s.message);

            List<Diagnostic> diagnostics = null;

            if (bannedAttributes.Count > 0)
            {
                compilationContext.RegisterCompilationEndAction(
                    context =>
                    {
                        VerifyAttributes(compilationContext.Compilation.Assembly.GetAttributes());
                        VerifyAttributes(compilationContext.Compilation.SourceModule.GetAttributes());
                    });

                compilationContext.RegisterSymbolAction(
                    context => VerifyAttributes(context.Symbol.GetAttributes()),
                    SymbolKind.NamedType,
                    SymbolKind.Method,
                    SymbolKind.Field,
                    SymbolKind.Property,
                    SymbolKind.Event);
            }

            compilationContext.RegisterOperationAction(
                context =>
                {
                    switch (context.Operation)
                    {
                        case IObjectCreationOperation objectCreation:
                            VerifySymbol(objectCreation.Constructor, context.Operation.Syntax);
                            VerifyType(objectCreation.Type.OriginalDefinition, context.Operation.Syntax);
                            break;

                        case IInvocationOperation invocation:
                            VerifySymbol(invocation.TargetMethod, context.Operation.Syntax);
                            VerifyType(invocation.TargetMethod.ContainingType.OriginalDefinition, context.Operation.Syntax);
                            break;

                        case IMemberReferenceOperation memberReference:
                            VerifySymbol(memberReference.Member, context.Operation.Syntax);
                            VerifyType(memberReference.Member.ContainingType.OriginalDefinition, context.Operation.Syntax);
                            break;
                    }
                },
                OperationKind.ObjectCreation,
                OperationKind.Invocation,
                OperationKind.EventReference,
                OperationKind.FieldReference,
                OperationKind.MethodReference,
                OperationKind.PropertyReference);

            compilationContext.RegisterSyntaxNodeAction(
                context => VerifyDocumentationSyntax(GetReferenceSyntaxNodeFromXmlCref(context.Node), context),
                XmlCrefSyntaxKind);

            compilationContext.RegisterCompilationEndAction(
                context =>
                {
                    // Flush any diagnostics created during processing
                    if (diagnostics != null)
                    {
                        foreach (var diagnostic in diagnostics)
                        {
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                });

            return;

            ImmutableHashSet<(ISymbol symbol, string message)> ReadBannedApis()
            {
                var query =
                    from additionalFile in compilationContext.Options.AdditionalFiles
                    where StringComparer.Ordinal.Equals(Path.GetFileName(additionalFile.Path), SymbolIsBannedAnalyzer.BannedSymbolsFileName)
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
                    if (lineById.TryGetValue(line.DeclarationId, out var existingLine))
                    {
                        errors.Add(Diagnostic.Create(SymbolIsBannedAnalyzer.DuplicateBannedSymbolRule, line.Location, new[] {existingLine.Location}, line.DeclarationId));
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

            void ReportDiagnostic(Diagnostic diagnostic)
            {
                diagnostics = diagnostics ?? new List<Diagnostic>(4);
                diagnostics.Add(diagnostic);
            }

            void VerifyAttributes(ImmutableArray<AttributeData> attributes)
            {
                foreach (var attribute in attributes)
                {
                    if (bannedAttributes.TryGetValue(attribute.AttributeClass, out var message))
                    {
                        var node = attribute.ApplicationSyntaxReference.GetSyntax();
                        ReportDiagnostic(
                            node.CreateDiagnostic(
                                SymbolIsBannedAnalyzer.SymbolIsBannedRule,
                                attribute.AttributeClass.ToDisplayString(),
                                string.IsNullOrWhiteSpace(message) ? "" : ": " + message));
                    }
                }
            }

            void VerifyType(ITypeSymbol type, SyntaxNode syntaxNode)
            {
                while (!(type is null))
                {
                    if (messageByBannedSymbol.TryGetValue(type, out var message))
                    {
                        ReportDiagnostic(
                            Diagnostic.Create(
                                SymbolIsBannedAnalyzer.SymbolIsBannedRule,
                                syntaxNode.GetLocation(),
                                type.ToDisplayString(SymbolDisplayFormat),
                                string.IsNullOrWhiteSpace(message) ? "" : ": " + message));
                        break;
                    }

                    type = type.ContainingType;
                }
            }

            void VerifySymbol(ISymbol symbol, SyntaxNode syntaxNode)
            {
                if (messageByBannedSymbol.TryGetValue(symbol, out var message))
                {
                    ReportDiagnostic(
                        Diagnostic.Create(
                            SymbolIsBannedAnalyzer.SymbolIsBannedRule,
                            syntaxNode.GetLocation(),
                            symbol.ToDisplayString(SymbolDisplayFormat),
                            string.IsNullOrWhiteSpace(message) ? "" : ": " + message));
                }
            }

            void VerifyDocumentationSyntax(SyntaxNode syntaxNode, SyntaxNodeAnalysisContext context)
            {
                var symbol = syntaxNode.GetDeclaredOrReferencedSymbol(context.SemanticModel);

                if (symbol is ITypeSymbol typeSymbol)
                {
                    VerifyType(typeSymbol, syntaxNode);
                }
                else
                {
                    VerifySymbol(symbol, syntaxNode);
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
