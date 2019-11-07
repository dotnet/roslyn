// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

using DiagnosticIds = Roslyn.Diagnostics.Analyzers.RoslynDiagnosticIds;

namespace Microsoft.CodeAnalysis.BannedApiAnalyzers
{
    internal static class SymbolIsBannedAnalyzer
    {
        public const string BannedSymbolsFileName = "BannedSymbols.txt";

        public static readonly DiagnosticDescriptor SymbolIsBannedRule = new DiagnosticDescriptor(
            id: DiagnosticIds.SymbolIsBannedRuleId,
            title: BannedApiAnalyzerResources.SymbolIsBannedTitle,
            messageFormat: BannedApiAnalyzerResources.SymbolIsBannedMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: BannedApiAnalyzerResources.SymbolIsBannedDescription,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md",
            customTags: WellKnownDiagnosticTags.Telemetry);

        public static readonly DiagnosticDescriptor DuplicateBannedSymbolRule = new DiagnosticDescriptor(
            id: DiagnosticIds.DuplicateBannedSymbolRuleId,
            title: BannedApiAnalyzerResources.DuplicateBannedSymbolTitle,
            messageFormat: BannedApiAnalyzerResources.DuplicateBannedSymbolMessage,
            category: "ApiDesign",
            defaultSeverity: DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: BannedApiAnalyzerResources.DuplicateBannedSymbolDescription,
            helpLinkUri: "https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md",
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
            var entryBySymbol = ReadBannedApis();

            if (entryBySymbol == null || entryBySymbol.Count == 0)
            {
                return;
            }

            var entryByAttributeSymbol = entryBySymbol
                .Where(pair => pair.Key is ITypeSymbol n && n.IsAttribute())
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            if (entryByAttributeSymbol.Count > 0)
            {
                compilationContext.RegisterCompilationEndAction(
                    context =>
                    {
                        VerifyAttributes(context.ReportDiagnostic, compilationContext.Compilation.Assembly.GetAttributes());
                        VerifyAttributes(context.ReportDiagnostic, compilationContext.Compilation.SourceModule.GetAttributes());
                    });

                compilationContext.RegisterSymbolAction(
                    context => VerifyAttributes(context.ReportDiagnostic, context.Symbol.GetAttributes()),
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
                            VerifySymbol(context.ReportDiagnostic, objectCreation.Constructor, context.Operation.Syntax);
                            VerifyType(context.ReportDiagnostic, objectCreation.Type, context.Operation.Syntax);
                            break;

                        case IInvocationOperation invocation:
                            VerifySymbol(context.ReportDiagnostic, invocation.TargetMethod, context.Operation.Syntax);
                            VerifyType(context.ReportDiagnostic, invocation.TargetMethod.ContainingType, context.Operation.Syntax);
                            break;

                        case IMemberReferenceOperation memberReference:
                            VerifySymbol(context.ReportDiagnostic, memberReference.Member, context.Operation.Syntax);
                            VerifyType(context.ReportDiagnostic, memberReference.Member.ContainingType, context.Operation.Syntax);
                            break;

                        case IArrayCreationOperation arrayCreation:
                            VerifyType(context.ReportDiagnostic, arrayCreation.Type, context.Operation.Syntax);
                            break;

                        case IAddressOfOperation addressOf:
                            VerifyType(context.ReportDiagnostic, addressOf.Type, context.Operation.Syntax);
                            break;

                        case IConversionOperation conversion:
                            if (conversion.OperatorMethod != null)
                            {
                                VerifySymbol(context.ReportDiagnostic, conversion.OperatorMethod, context.Operation.Syntax);
                                VerifyType(context.ReportDiagnostic, conversion.OperatorMethod.ContainingType, context.Operation.Syntax);
                            }
                            break;

                        case IUnaryOperation unary:
                            if (unary.OperatorMethod != null)
                            {
                                VerifySymbol(context.ReportDiagnostic, unary.OperatorMethod, context.Operation.Syntax);
                                VerifyType(context.ReportDiagnostic, unary.OperatorMethod.ContainingType, context.Operation.Syntax);
                            }
                            break;

                        case IBinaryOperation binary:
                            if (binary.OperatorMethod != null)
                            {
                                VerifySymbol(context.ReportDiagnostic, binary.OperatorMethod, context.Operation.Syntax);
                                VerifyType(context.ReportDiagnostic, binary.OperatorMethod.ContainingType, context.Operation.Syntax);
                            }
                            break;

                        case IIncrementOrDecrementOperation incrementOrDecrement:
                            if (incrementOrDecrement.OperatorMethod != null)
                            {
                                VerifySymbol(context.ReportDiagnostic, incrementOrDecrement.OperatorMethod, context.Operation.Syntax);
                                VerifyType(context.ReportDiagnostic, incrementOrDecrement.OperatorMethod.ContainingType, context.Operation.Syntax);
                            }
                            break;
                    }
                },
                OperationKind.ObjectCreation,
                OperationKind.Invocation,
                OperationKind.EventReference,
                OperationKind.FieldReference,
                OperationKind.MethodReference,
                OperationKind.PropertyReference,
                OperationKind.ArrayCreation,
                OperationKind.AddressOf,
                OperationKind.Conversion,
                OperationKind.UnaryOperator,
                OperationKind.BinaryOperator,
                OperationKind.Increment,
                OperationKind.Decrement);

            compilationContext.RegisterSyntaxNodeAction(
                context => VerifyDocumentationSyntax(context.ReportDiagnostic, GetReferenceSyntaxNodeFromXmlCref(context.Node), context),
                XmlCrefSyntaxKind);

            return;

            Dictionary<ISymbol, BanFileEntry>? ReadBannedApis()
            {
                var query =
                    from additionalFile in compilationContext.Options.AdditionalFiles
                    where StringComparer.Ordinal.Equals(Path.GetFileName(additionalFile.Path), SymbolIsBannedAnalyzer.BannedSymbolsFileName)
                    let sourceText = additionalFile.GetText(compilationContext.CancellationToken)
                    where sourceText != null
                    from line in sourceText.Lines
                    let text = line.ToString()
                    where !string.IsNullOrWhiteSpace(text)
                    select new BanFileEntry(text, line.Span, sourceText, additionalFile.Path);

                var entries = query.ToList();

                if (entries.Count == 0)
                {
                    return null;
                }

                var errors = new List<Diagnostic>();

                var result = new Dictionary<ISymbol, BanFileEntry>();

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

            void VerifyAttributes(Action<Diagnostic> reportDiagnostic, ImmutableArray<AttributeData> attributes)
            {
                foreach (var attribute in attributes)
                {
                    if (entryByAttributeSymbol.TryGetValue(attribute.AttributeClass, out var entry))
                    {
                        var node = attribute.ApplicationSyntaxReference?.GetSyntax();
                        if (node != null)
                        {
                            reportDiagnostic(
                                node.CreateDiagnostic(
                                    SymbolIsBannedAnalyzer.SymbolIsBannedRule,
                                    attribute.AttributeClass.ToDisplayString(),
                                    string.IsNullOrWhiteSpace(entry.Message) ? "" : ": " + entry.Message));
                        }
                    }
                }
            }

            bool VerifyType(Action<Diagnostic> reportDiagnostic, ITypeSymbol? type, SyntaxNode syntaxNode)
            {
                do
                {
                    if (!VerifyTypeArguments(reportDiagnostic, type, syntaxNode, out type))
                    {
                        return false;
                    }

                    if (type == null)
                    {
                        // Type will be null for arrays and pointers.
                        return true;
                    }

                    if (entryBySymbol!.TryGetValue(type, out var entry))
                    {
                        reportDiagnostic(
                            Diagnostic.Create(
                                SymbolIsBannedAnalyzer.SymbolIsBannedRule,
                                syntaxNode.GetLocation(),
                                type.ToDisplayString(SymbolDisplayFormat),
                                string.IsNullOrWhiteSpace(entry.Message) ? "" : ": " + entry.Message));
                        return false;
                    }

                    type = type.ContainingType;
                }
                while (!(type is null));

                return true;
            }

            bool VerifyTypeArguments(Action<Diagnostic> reportDiagnostic, ITypeSymbol? type, SyntaxNode syntaxNode, out ITypeSymbol? originalDefinition)
            {
                switch (type)
                {
                    case INamedTypeSymbol namedTypeSymbol:
                        originalDefinition = namedTypeSymbol.ConstructedFrom;
                        foreach (var typeArgument in namedTypeSymbol.TypeArguments)
                        {
                            if (typeArgument.TypeKind != TypeKind.TypeParameter &&
                                typeArgument.TypeKind != TypeKind.Error &&
                                !VerifyType(reportDiagnostic, typeArgument, syntaxNode))
                            {
                                return false;
                            }
                        }
                        break;

                    case IArrayTypeSymbol arrayTypeSymbol:
                        originalDefinition = null;
                        return VerifyType(reportDiagnostic, arrayTypeSymbol.ElementType, syntaxNode);

                    case IPointerTypeSymbol pointerTypeSymbol:
                        originalDefinition = null;
                        return VerifyType(reportDiagnostic, pointerTypeSymbol.PointedAtType, syntaxNode);

                    default:
                        originalDefinition = type?.OriginalDefinition;
                        break;

                }

                return true;
            }

            void VerifySymbol(Action<Diagnostic> reportDiagnostic, ISymbol symbol, SyntaxNode syntaxNode)
            {
                symbol = symbol.OriginalDefinition;

                if (entryBySymbol!.TryGetValue(symbol, out var entry))
                {
                    reportDiagnostic(
                        Diagnostic.Create(
                            SymbolIsBannedAnalyzer.SymbolIsBannedRule,
                            syntaxNode.GetLocation(),
                            symbol.ToDisplayString(SymbolDisplayFormat),
                            string.IsNullOrWhiteSpace(entry.Message) ? "" : ": " + entry.Message));
                }
            }

            void VerifyDocumentationSyntax(Action<Diagnostic> reportDiagnostic, SyntaxNode syntaxNode, SyntaxNodeAnalysisContext context)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(syntaxNode, context.CancellationToken).Symbol;

                if (symbol is ITypeSymbol typeSymbol)
                {
                    VerifyType(reportDiagnostic, typeSymbol, syntaxNode);
                }
                else if (symbol != null)
                {
                    VerifySymbol(reportDiagnostic, symbol, syntaxNode);
                }
            }
        }

        private sealed class BanFileEntry
        {
            public TextSpan Span { get; }
            public SourceText SourceText { get; }
            public string Path { get; }
            public string DeclarationId { get; }
            public string Message { get; }

            public BanFileEntry(string text, TextSpan span, SourceText sourceText, string path)
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
