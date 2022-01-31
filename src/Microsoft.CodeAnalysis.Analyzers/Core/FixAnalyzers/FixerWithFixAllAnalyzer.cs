// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.Analyzers.FixAnalyzers
{
    using static CodeAnalysisDiagnosticsResources;

    /// <summary>
    /// A <see cref="CodeFixProvider"/> that intends to support fix all occurrences must classify the registered code actions into equivalence classes by assigning it an explicit, non-null equivalence key which is unique across all registered code actions by this fixer.
    /// This enables the <see cref="FixAllProvider"/> to fix all diagnostics in the required scope by applying code actions from this fixer that are in the equivalence class of the trigger code action.
    /// This analyzer catches violations of this requirement in the code actions registered by a <see cref="CodeFixProvider"/> that supports <see cref="FixAllProvider"/>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class FixerWithFixAllAnalyzer : DiagnosticAnalyzer
    {
        private const string CodeActionMetadataName = "Microsoft.CodeAnalysis.CodeActions.CodeAction";
        private const string CreateMethodName = "Create";
        private const string EquivalenceKeyPropertyName = "EquivalenceKey";
        private const string EquivalenceKeyParameterName = "equivalenceKey";
        internal const string CodeFixProviderMetadataName = "Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider";
        internal const string GetFixAllProviderMethodName = "GetFixAllProvider";

        private static readonly LocalizableString s_localizableCodeActionNeedsEquivalenceKeyDescription = CreateLocalizableResourceString(nameof(CodeActionNeedsEquivalenceKeyDescription));

        internal static readonly DiagnosticDescriptor CreateCodeActionEquivalenceKeyRule = new(
            DiagnosticIds.CreateCodeActionWithEquivalenceKeyRuleId,
            CreateLocalizableResourceString(nameof(CreateCodeActionWithEquivalenceKeyTitle)),
            CreateLocalizableResourceString(nameof(CreateCodeActionWithEquivalenceKeyMessage)),
            "Correctness",
            DiagnosticSeverity.Warning,
            description: s_localizableCodeActionNeedsEquivalenceKeyDescription,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTagsExtensions.CompilationEndAndTelemetry);

        internal static readonly DiagnosticDescriptor OverrideCodeActionEquivalenceKeyRule = new(
            DiagnosticIds.OverrideCodeActionEquivalenceKeyRuleId,
            CreateLocalizableResourceString(nameof(OverrideCodeActionEquivalenceKeyTitle)),
            CreateLocalizableResourceString(nameof(OverrideCodeActionEquivalenceKeyMessage)),
            "Correctness",
            DiagnosticSeverity.Warning,
            description: s_localizableCodeActionNeedsEquivalenceKeyDescription,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTagsExtensions.CompilationEndAndTelemetry);

        internal static readonly DiagnosticDescriptor OverrideGetFixAllProviderRule = new(
            DiagnosticIds.OverrideGetFixAllProviderRuleId,
            CreateLocalizableResourceString(nameof(OverrideGetFixAllProviderTitle)),
            CreateLocalizableResourceString(nameof(OverrideGetFixAllProviderMessage)),
            "Correctness",
            DiagnosticSeverity.Warning,
            description: CreateLocalizableResourceString(nameof(OverrideGetFixAllProviderDescription)),
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTagsExtensions.CompilationEndAndTelemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(CreateCodeActionEquivalenceKeyRule, OverrideCodeActionEquivalenceKeyRule, OverrideGetFixAllProviderRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            // We need to analyze generated code, but don't intend to report diagnostics on generated code.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);

            context.RegisterCompilationStartAction(CreateAnalyzerWithinCompilation);
        }

        private void CreateAnalyzerWithinCompilation(CompilationStartAnalysisContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            INamedTypeSymbol? codeFixProviderSymbol = context.Compilation.GetOrCreateTypeByMetadataName(CodeFixProviderMetadataName);
            if (codeFixProviderSymbol == null)
            {
                return;
            }

            IMethodSymbol? getFixAllProviderMethod = codeFixProviderSymbol.GetMembers(GetFixAllProviderMethodName).OfType<IMethodSymbol>().FirstOrDefault();
            if (getFixAllProviderMethod == null)
            {
                return;
            }

            INamedTypeSymbol? codeActionSymbol = context.Compilation.GetOrCreateTypeByMetadataName(CodeActionMetadataName);
            if (codeActionSymbol == null)
            {
                return;
            }

            IPropertySymbol? equivalenceKeyProperty = codeActionSymbol.GetMembers(EquivalenceKeyPropertyName).OfType<IPropertySymbol>().FirstOrDefault();
            if (equivalenceKeyProperty == null)
            {
                return;
            }

            var createMethods = codeActionSymbol.GetMembers(CreateMethodName).OfType<IMethodSymbol>().ToImmutableHashSet();

            CompilationAnalyzer compilationAnalyzer = new CompilationAnalyzer(codeFixProviderSymbol, codeActionSymbol, context.Compilation.Assembly, createMethods);

            context.RegisterSymbolAction(compilationAnalyzer.AnalyzeNamedTypeSymbol, SymbolKind.NamedType);
            context.RegisterOperationBlockStartAction(compilationAnalyzer.OperationBlockStart);
            context.RegisterCompilationEndAction(compilationAnalyzer.CompilationEnd);
        }

        private sealed class CompilationAnalyzer
        {
            private readonly INamedTypeSymbol _codeFixProviderSymbol;
            private readonly INamedTypeSymbol _codeActionSymbol;
            private readonly ImmutableHashSet<IMethodSymbol> _createMethods;
            private readonly IAssemblySymbol _sourceAssembly;

            /// <summary>
            /// Set of all non-abstract sub-types of <see cref="CodeFixProvider"/> in this compilation.
            /// </summary>
            private readonly HashSet<INamedTypeSymbol> _codeFixProviders = new();

            /// <summary>
            /// Set of all non-abstract sub-types of <see cref="CodeAction"/> which override <see cref="CodeAction.EquivalenceKey"/> in this compilation.
            /// </summary>
            private readonly HashSet<INamedTypeSymbol> _codeActionsWithEquivalenceKey = new();

            /// <summary>
            /// Map of invocations from code fix providers to invocations that create a code action using the static "Create" methods on <see cref="CodeAction"/>.
            /// </summary>
            private readonly Dictionary<INamedTypeSymbol, HashSet<IInvocationOperation>> _codeActionCreateInvocations = new();

            /// <summary>
            /// Map of invocations from code fix providers to object creations that create a code action using sub-types of <see cref="CodeAction"/>.
            /// </summary>
            private readonly Dictionary<INamedTypeSymbol, HashSet<IObjectCreationOperation>> _codeActionObjectCreations = new();

            public CompilationAnalyzer(
                INamedTypeSymbol codeFixProviderSymbol,
                INamedTypeSymbol codeActionSymbol,
                IAssemblySymbol sourceAssembly,
                ImmutableHashSet<IMethodSymbol> createMethods)
            {
                _codeFixProviderSymbol = codeFixProviderSymbol;
                _codeActionSymbol = codeActionSymbol;
                _sourceAssembly = sourceAssembly;
                _createMethods = createMethods;
            }

            internal void AnalyzeNamedTypeSymbol(SymbolAnalysisContext context)
            {
                var namedType = (INamedTypeSymbol)context.Symbol;
                if (namedType.DerivesFrom(_codeFixProviderSymbol))
                {
                    lock (_codeFixProviders)
                    {
                        _codeFixProviders.Add(namedType);
                    }
                }
                else if (IsCodeActionWithOverriddenEquivlanceKeyCore(namedType))
                {
                    lock (_codeActionsWithEquivalenceKey)
                    {
                        _codeActionsWithEquivalenceKey.Add(namedType);
                    }
                }
            }

            internal void OperationBlockStart(OperationBlockStartAnalysisContext context)
            {
                if (context.OwningSymbol is not IMethodSymbol method)
                {
                    return;
                }

                INamedTypeSymbol namedType = method.ContainingType;
                if (!namedType.DerivesFrom(_codeFixProviderSymbol))
                {
                    return;
                }

                context.RegisterOperationAction(operationContext =>
                {
                    var invocation = (IInvocationOperation)operationContext.Operation;
                    if (invocation.TargetMethod is IMethodSymbol invocationSym && _createMethods.Contains(invocationSym))
                    {
                        AddOperation(namedType, invocation, _codeActionCreateInvocations);
                    }
                },
                OperationKind.Invocation);

                context.RegisterOperationAction(operationContext =>
                {
                    var objectCreation = (IObjectCreationOperation)operationContext.Operation;
                    IMethodSymbol constructor = objectCreation.Constructor;
                    if (constructor != null && constructor.ContainingType.DerivesFrom(_codeActionSymbol))
                    {
                        AddOperation(namedType, objectCreation, _codeActionObjectCreations);
                    }
                },
                OperationKind.ObjectCreation);
            }

            private static void AddOperation<T>(INamedTypeSymbol namedType, T operation, Dictionary<INamedTypeSymbol, HashSet<T>> map)
                where T : IOperation
            {
                lock (map)
                {
                    if (!map.TryGetValue(namedType, out HashSet<T> value))
                    {
                        value = new HashSet<T>();
                        map[namedType] = value;
                    }

                    value.Add(operation);
                }
            }

            internal void CompilationEnd(CompilationAnalysisContext context)
            {
                if (_codeFixProviders == null)
                {
                    // No fixers.
                    return;
                }

                if (_codeActionCreateInvocations == null && _codeActionObjectCreations == null)
                {
                    // No registered fixes.
                    return;
                }

                // Analyze all fixers that have FixAll support.
                // Otherwise, report RS1016 (OverrideGetFixAllProviderRule) to recommend adding FixAll support.
                foreach (INamedTypeSymbol fixer in _codeFixProviders)
                {
                    if (OverridesGetFixAllProvider(fixer))
                    {
                        AnalyzeFixerWithFixAll(fixer, context);
                    }
                    else if (SymbolEqualityComparer.Default.Equals(fixer.BaseType, _codeFixProviderSymbol))
                    {
                        Diagnostic diagnostic = fixer.CreateDiagnostic(OverrideGetFixAllProviderRule, fixer.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }

                return;

                // Local functions
                bool OverridesGetFixAllProvider(INamedTypeSymbol fixer)
                {
                    foreach (INamedTypeSymbol type in fixer.GetBaseTypesAndThis())
                    {
                        if (!SymbolEqualityComparer.Default.Equals(type, _codeFixProviderSymbol))
                        {
                            IMethodSymbol getFixAllProviderMethod = type.GetMembers(GetFixAllProviderMethodName).OfType<IMethodSymbol>().FirstOrDefault();
                            if (getFixAllProviderMethod != null && getFixAllProviderMethod.IsOverride)
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                }

                static bool IsViolatingCodeActionCreateInvocation(IInvocationOperation invocation)
                {
                    IParameterSymbol param = invocation.TargetMethod.Parameters.FirstOrDefault(p => p.Name == EquivalenceKeyParameterName);
                    if (param == null)
                    {
                        // User is calling an overload without the equivalenceKey parameter
                        return false;
                    }

                    foreach (var argument in invocation.Arguments)
                    {
                        if (SymbolEqualityComparer.Default.Equals(argument.Parameter, param))
                        {
                            return argument.Value.ConstantValue.HasValue && argument.Value.ConstantValue.Value == null;
                        }
                    }

                    return true;
                }

                void AnalyzeFixerWithFixAll(INamedTypeSymbol fixer, CompilationAnalysisContext context)
                {
                    if (_codeActionCreateInvocations != null
                        && _codeActionCreateInvocations.TryGetValue(fixer, out HashSet<IInvocationOperation> invocations))
                    {
                        foreach (IInvocationOperation invocation in invocations)
                        {
                            if (IsViolatingCodeActionCreateInvocation(invocation))
                            {
                                Diagnostic diagnostic = invocation.CreateDiagnostic(CreateCodeActionEquivalenceKeyRule, EquivalenceKeyParameterName);
                                context.ReportDiagnostic(diagnostic);
                            }
                        }
                    }

                    if (_codeActionObjectCreations != null
                        && _codeActionObjectCreations.TryGetValue(fixer, out HashSet<IObjectCreationOperation> objectCreations))
                    {
                        foreach (IObjectCreationOperation objectCreation in objectCreations)
                        {
                            if (IsViolatingCodeActionObjectCreation(objectCreation))
                            {
                                Diagnostic diagnostic = objectCreation.CreateDiagnostic(OverrideCodeActionEquivalenceKeyRule, objectCreation.Constructor.ContainingType, EquivalenceKeyPropertyName);
                                context.ReportDiagnostic(diagnostic);
                            }
                        }
                    }
                }

                bool IsViolatingCodeActionObjectCreation(IObjectCreationOperation objectCreation)
                {
                    return objectCreation.Constructor.ContainingType.GetBaseTypesAndThis().All(namedType => !IsCodeActionWithOverriddenEquivalenceKey(namedType));

                    // Local functions
                    bool IsCodeActionWithOverriddenEquivalenceKey(INamedTypeSymbol namedType)
                    {
                        if (SymbolEqualityComparer.Default.Equals(namedType, _codeActionSymbol))
                        {
                            return false;
                        }

                        // We are already tracking CodeActions with equivalence key in this compilation.
                        if (SymbolEqualityComparer.Default.Equals(namedType.ContainingAssembly, _sourceAssembly))
                        {
                            return _codeActionsWithEquivalenceKey != null && _codeActionsWithEquivalenceKey.Contains(namedType);
                        }

                        // For types in different compilation, perform the check.
                        return IsCodeActionWithOverriddenEquivlanceKeyCore(namedType);
                    }
                }
            }

            private bool IsCodeActionWithOverriddenEquivlanceKeyCore(INamedTypeSymbol namedType)
            {
                if (!namedType.DerivesFrom(_codeActionSymbol))
                {
                    // Not a CodeAction.
                    return false;
                }

                IPropertySymbol equivalenceKeyProperty = namedType.GetMembers(EquivalenceKeyPropertyName).OfType<IPropertySymbol>().FirstOrDefault();
                return equivalenceKeyProperty != null && equivalenceKeyProperty.IsOverride;
            }
        }
    }
}
