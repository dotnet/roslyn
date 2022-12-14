// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Operations;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities;

namespace Microsoft.CodeAnalysis.BannedApiAnalyzers
{
    public abstract class SymbolIsBannedAnalyzerBase<TSyntaxKind> : DiagnosticAnalyzer
        where TSyntaxKind : struct
    {
        protected abstract Dictionary<ISymbol, BanFileEntry>? ReadBannedApis(CompilationStartAnalysisContext compilationContext);

        protected abstract DiagnosticDescriptor SymbolIsBannedRule { get; }

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
            var entryBySymbol = ReadBannedApis(compilationContext);

            if (entryBySymbol == null || entryBySymbol.Count == 0)
            {
                return;
            }

            Dictionary<ISymbol, BanFileEntry> entryByAttributeSymbol = entryBySymbol
                .Where(pair => pair.Key is ITypeSymbol n && n.IsAttribute())
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            if (entryByAttributeSymbol.Count > 0)
            {
                compilationContext.RegisterCompilationEndAction(
                    context =>
                    {
                        VerifyAttributes(context.ReportDiagnostic, compilationContext.Compilation.Assembly.GetAttributes(), context.CancellationToken);
                        VerifyAttributes(context.ReportDiagnostic, compilationContext.Compilation.SourceModule.GetAttributes(), context.CancellationToken);
                    });

                compilationContext.RegisterSymbolAction(
                    context => VerifyAttributes(context.ReportDiagnostic, context.Symbol.GetAttributes(), context.CancellationToken),
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
                        case ITypeOfOperation typeOfOperation:
                            VerifyType(context.ReportDiagnostic, typeOfOperation.TypeOperand, context.Operation.Syntax);
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
                OperationKind.Decrement,
                OperationKind.TypeOf);

            compilationContext.RegisterSyntaxNodeAction(
                context => VerifyDocumentationSyntax(context.ReportDiagnostic, GetReferenceSyntaxNodeFromXmlCref(context.Node), context),
                XmlCrefSyntaxKind);

            return;

            void VerifyAttributes(Action<Diagnostic> reportDiagnostic, ImmutableArray<AttributeData> attributes, CancellationToken cancellationToken)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute.AttributeClass is { } attributeClass && entryByAttributeSymbol.TryGetValue(attributeClass, out var entry))
                    {
                        var node = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken);
                        if (node != null)
                        {
                            reportDiagnostic(
                                node.CreateDiagnostic(
                                    SymbolIsBannedRule,
                                    attributeClass.ToDisplayString(),
                                    string.IsNullOrWhiteSpace(entry.Message) ? "" : ": " + entry.Message));
                        }
                    }
                }
            }

            bool VerifyType(Action<Diagnostic> reportDiagnostic, ITypeSymbol? type, SyntaxNode syntaxNode)
            {
                RoslynDebug.Assert(entryBySymbol != null);

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

                    if (entryBySymbol.TryGetValue(type, out var entry))
                    {
                        reportDiagnostic(
                            syntaxNode.CreateDiagnostic(
                                SymbolIsBannedRule,
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
                RoslynDebug.Assert(entryBySymbol != null);

                foreach (var currentSymbol in GetSymbolAndOverridenSymbols(symbol))
                {
                    if (entryBySymbol.TryGetValue(currentSymbol, out var entry))
                    {
                        reportDiagnostic(
                            syntaxNode.CreateDiagnostic(
                                SymbolIsBannedRule,
                                currentSymbol.ToDisplayString(SymbolDisplayFormat),
                                string.IsNullOrWhiteSpace(entry.Message) ? "" : ": " + entry.Message));
                        return;
                    }
                }

                static IEnumerable<ISymbol> GetSymbolAndOverridenSymbols(ISymbol symbol)
                {
                    ISymbol? currentSymbol = symbol.OriginalDefinition;

                    while (currentSymbol != null)
                    {
                        yield return currentSymbol;

                        // It's possible to have `IsOverride` true and yet have `GetOverriddeMember` returning null when the code is invalid
                        // (e.g. base symbol is not marked as `virtual` or `abstract` and current symbol has the `overrides` modifier).
                        currentSymbol = currentSymbol.IsOverride
                            ? currentSymbol.GetOverriddenMember()?.OriginalDefinition
                            : null;
                    }
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

        protected sealed class BanFileEntry
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
                    DeclarationId = text[0..^1].Trim();
                    Message = "";
                }
                else
                {
                    DeclarationId = text[..index].Trim();
                    Message = text[(index + 1)..].Trim();
                }

                Span = span;
                SourceText = sourceText;
                Path = path;
            }

            public Location Location => Location.Create(Path, Span, SourceText.Lines.GetLinePositionSpan(Span));
        }
    }
}