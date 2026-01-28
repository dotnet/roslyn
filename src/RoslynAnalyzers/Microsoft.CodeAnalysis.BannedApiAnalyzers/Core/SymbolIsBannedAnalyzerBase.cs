// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.BannedApiAnalyzers
{
    public abstract class SymbolIsBannedAnalyzerBase<TSyntaxKind> : DiagnosticAnalyzer
        where TSyntaxKind : struct
    {
        protected abstract Dictionary<(string ContainerName, string SymbolName), ImmutableArray<BanFileEntry>>? ReadBannedApis(CompilationStartAnalysisContext compilationContext);

        protected abstract DiagnosticDescriptor SymbolIsBannedRule { get; }

        protected abstract TSyntaxKind XmlCrefSyntaxKind { get; }

        protected abstract SyntaxNode GetReferenceSyntaxNodeFromXmlCref(SyntaxNode syntaxNode);

        protected abstract ImmutableArray<TSyntaxKind> BaseTypeSyntaxKinds { get; }

        protected abstract IEnumerable<SyntaxNode> GetTypeSyntaxNodesFromBaseType(SyntaxNode syntaxNode);

        protected abstract SymbolDisplayFormat SymbolDisplayFormat { get; }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            // Analyzer needs to get callbacks for generated code, and might report diagnostics in generated code.
            // However, this can be disabled via editorconfig option.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static bool ShouldExcludeGeneratedCode(AnalyzerOptions options)
        {
            return options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
                    Analyzer.Utilities.EditorConfigOptionNames.BannedApiExcludeGeneratedCode,
                    out var value) &&
                EditorConfigValueSerializer.GetDefault<bool>(isEditorConfigOption: true).ParseValue(value).GetValueOrDefault();
        }

        private void OnCompilationStart(CompilationStartAnalysisContext compilationContext)
        {
            var bannedApis = ReadBannedApis(compilationContext);
            if (bannedApis == null || bannedApis.Count == 0)
                return;

            var excludeGeneratedCode = ShouldExcludeGeneratedCode(compilationContext.Options);

            if (ShouldAnalyzeAttributes())
            {
                compilationContext.RegisterCompilationEndAction(
                    context =>
                    {
                        VerifyAttributes(context.ReportDiagnostic, compilationContext.Compilation.Assembly.GetAttributes(), context.CancellationToken);
                        VerifyAttributes(context.ReportDiagnostic, compilationContext.Compilation.SourceModule.GetAttributes(), context.CancellationToken);
                    });

                compilationContext.RegisterSymbolAction(
                    context =>
                    {
                        if (excludeGeneratedCode && context.IsGeneratedCode)
                            return;

                        VerifyAttributes(context.ReportDiagnostic, context.Symbol.GetAttributes(), context.CancellationToken);
                    },
                    SymbolKind.NamedType,
                    SymbolKind.Method,
                    SymbolKind.Field,
                    SymbolKind.Property,
                    SymbolKind.Event);
            }

            compilationContext.RegisterOperationAction(
                context =>
                {
                    if (excludeGeneratedCode && context.IsGeneratedCode)
                        return;

                    context.CancellationToken.ThrowIfCancellationRequested();
                    switch (context.Operation)
                    {
                        case IObjectCreationOperation objectCreation:
                            if (objectCreation.Constructor != null)
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
                context =>
                {
                    if (excludeGeneratedCode && context.IsGeneratedCode)
                        return;

                    VerifyDocumentationSyntax(context.ReportDiagnostic, GetReferenceSyntaxNodeFromXmlCref(context.Node), context);
                },
                XmlCrefSyntaxKind);

            compilationContext.RegisterSyntaxNodeAction(
                context =>
                {
                    if (excludeGeneratedCode && context.IsGeneratedCode)
                        return;

                    VerifyBaseTypesSyntax(context.ReportDiagnostic, GetTypeSyntaxNodesFromBaseType(context.Node), context);
                },
                BaseTypeSyntaxKinds);

            return;

            bool IsBannedSymbol([NotNullWhen(true)] ISymbol? symbol, [NotNullWhen(true)] out BanFileEntry? entry)
            {
                if (symbol is { ContainingSymbol.Name: string parentName } &&
                    bannedApis.TryGetValue((parentName, symbol.Name), out var entries))
                {
                    foreach (var bannedFileEntry in entries)
                    {
                        foreach (var bannedSymbol in bannedFileEntry.Symbols)
                        {
                            if (SymbolEqualityComparer.Default.Equals(symbol, bannedSymbol))
                            {
                                entry = bannedFileEntry;
                                return true;
                            }
                        }
                    }
                }

                entry = null;
                return false;
            }

            bool ShouldAnalyzeAttributes()
            {
                // We want to avoid realizing symbols here as that can be very expensive.  So we instead use a simple
                // heuristic which works thanks to .net coding conventions.  Specifically, we look to see if the banned
                // api contains a type that ends in 'Attribute'.  In that case, we do the work to try to get the real symbol.
                foreach (var kvp in bannedApis)
                {
                    if (!kvp.Key.SymbolName.EndsWith("Attribute", StringComparison.InvariantCulture) &&
                        !kvp.Key.ContainerName.EndsWith("Attribute", StringComparison.InvariantCulture))
                    {
                        continue;
                    }

                    foreach (var entry in kvp.Value)
                    {
                        if (entry.Symbols.Any(ContainsAttributeSymbol))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            bool ContainsAttributeSymbol(ISymbol symbol)
            {
                return symbol switch
                {
                    INamedTypeSymbol namedType => namedType.IsAttribute(),
                    IMethodSymbol method => method.ContainingType.IsAttribute() && method.IsConstructor(),
                    _ => false
                };
            }

            void VerifyAttributes(Action<Diagnostic> reportDiagnostic, ImmutableArray<AttributeData> attributes, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var attribute in attributes)
                {
                    if (IsBannedSymbol(attribute.AttributeClass, out var entry))
                    {
                        var node = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken);
                        if (node != null)
                        {
                            reportDiagnostic(
                                node.CreateDiagnostic(
                                    SymbolIsBannedRule,
                                    attribute.AttributeClass.ToDisplayString(),
                                    string.IsNullOrWhiteSpace(entry.Message) ? "" : ": " + entry.Message));
                        }
                    }

                    if (attribute.AttributeConstructor != null)
                    {
                        var syntaxNode = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken);

                        if (syntaxNode != null)
                        {
                            VerifySymbol(reportDiagnostic, attribute.AttributeConstructor, syntaxNode);
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

                    if (IsBannedSymbol(type, out var entry))
                    {
                        reportDiagnostic(
                            syntaxNode.CreateDiagnostic(
                                SymbolIsBannedRule,
                                type.ToDisplayString(SymbolDisplayFormat),
                                string.IsNullOrWhiteSpace(entry.Message) ? "" : ": " + entry.Message));
                        return false;
                    }

                    foreach (var currentNamespace in GetContainingNamespaces(type))
                    {
                        if (IsBannedSymbol(currentNamespace, out entry))
                        {
                            reportDiagnostic(
                                syntaxNode.CreateDiagnostic(
                                    SymbolIsBannedRule,
                                    currentNamespace.ToDisplayString(),
                                    string.IsNullOrWhiteSpace(entry.Message) ? "" : ": " + entry.Message));
                            return false;
                        }
                    }

                    type = type.ContainingType;
                }
                while (!(type is null));

                return true;

                static IEnumerable<INamespaceSymbol> GetContainingNamespaces(ISymbol symbol)
                {
                    INamespaceSymbol? currentNamespace = symbol.ContainingNamespace;

                    while (currentNamespace is { IsGlobalNamespace: false })
                    {
                        foreach (var constituent in currentNamespace.ConstituentNamespaces)
                            yield return constituent;

                        currentNamespace = currentNamespace.ContainingNamespace;
                    }
                }
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
                foreach (var currentSymbol in GetSymbolAndOverridenSymbols(symbol))
                {
                    if (IsBannedSymbol(currentSymbol, out var entry))
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

            void VerifyBaseTypesSyntax(Action<Diagnostic> reportDiagnostic, IEnumerable<SyntaxNode> typeSyntaxNodes, SyntaxNodeAnalysisContext context)
            {
                foreach (var typeSyntaxNode in typeSyntaxNodes)
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(typeSyntaxNode, context.CancellationToken).Symbol;

                    if (symbol is ITypeSymbol typeSymbol)
                    {
                        VerifyType(reportDiagnostic, typeSymbol, typeSyntaxNode);
                    }
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

            private readonly Lazy<ImmutableArray<ISymbol>> _lazySymbols;
            public ImmutableArray<ISymbol> Symbols => _lazySymbols.Value;

            public BanFileEntry(Compilation compilation, string text, TextSpan span, SourceText sourceText, string path)
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

                _lazySymbols = new Lazy<ImmutableArray<ISymbol>>(
                    () => DocumentationCommentId.GetSymbolsForDeclarationId(DeclarationId, compilation)
                        .SelectMany(ExpandConstituentNamespaces).ToImmutableArray());

                static IEnumerable<ISymbol> ExpandConstituentNamespaces(ISymbol symbol)
                {
                    if (symbol is not INamespaceSymbol namespaceSymbol)
                    {
                        yield return symbol;
                        yield break;
                    }

                    foreach (var constituent in namespaceSymbol.ConstituentNamespaces)
                        yield return constituent;
                }
            }

            public Location Location => Location.Create(Path, Span, SourceText.Lines.GetLinePositionSpan(Span));
        }
    }
}
