// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS3001 // Some types from Roslyn are not CLS-Compliant
#pragma warning disable CS3003 // Some types from Roslyn are not CLS-Compliant

namespace Microsoft.CodeAnalysis.CodeMetrics
{
    internal abstract partial class CodeAnalysisMetricData
    {
        internal CodeAnalysisMetricData(
            ISymbol symbol,
            int maintainabilityIndex,
            ComputationalComplexityMetrics computationalComplexityMetrics,
            ImmutableHashSet<INamedTypeSymbol> coupledNamedTypes,
            long linesOfCode,
            int cyclomaticComplexity,
            int? depthOfInheritance,
            ImmutableArray<CodeAnalysisMetricData> children)
        {
            Debug.Assert(symbol != null);
            Debug.Assert(
                symbol.Kind == SymbolKind.Assembly ||
                symbol.Kind == SymbolKind.Namespace ||
                symbol.Kind == SymbolKind.NamedType ||
                symbol.Kind == SymbolKind.Method ||
                symbol.Kind == SymbolKind.Field ||
                symbol.Kind == SymbolKind.Event ||
                symbol.Kind == SymbolKind.Property);
            Debug.Assert(depthOfInheritance.HasValue == (symbol.Kind == SymbolKind.Assembly || symbol.Kind == SymbolKind.Namespace || symbol.Kind == SymbolKind.NamedType));

#if LEGACY_CODE_METRICS_MODE
            linesOfCode = !computationalComplexityMetrics.IsDefault ?
                computationalComplexityMetrics.EffectiveLinesOfCode :
                children.Sum(c => c.LinesOfCode);
#endif

            Symbol = symbol;
            MaintainabilityIndex = maintainabilityIndex;
            ComputationalComplexityMetrics = computationalComplexityMetrics;
            CoupledNamedTypes = coupledNamedTypes;
            LinesOfCode = linesOfCode;
            CyclomaticComplexity = cyclomaticComplexity;
            DepthOfInheritance = depthOfInheritance;
            Children = children;
        }

        public ISymbol Symbol { get; }

        internal ComputationalComplexityMetrics ComputationalComplexityMetrics { get; }

        public int MaintainabilityIndex { get; }

        public ImmutableHashSet<INamedTypeSymbol> CoupledNamedTypes { get; }

        public long LinesOfCode { get; }

        public int CyclomaticComplexity { get; }

        public int? DepthOfInheritance { get; }

        public ImmutableArray<CodeAnalysisMetricData> Children { get; }

        public sealed override string ToString()
        {
            var builder = new StringBuilder();
            string symbolName;
            switch (Symbol.Kind)
            {
                case SymbolKind.Assembly:
                    symbolName = "Assembly";
                    break;

                case SymbolKind.Namespace:
                    // Skip explicit display for global namespace.
                    if (((INamespaceSymbol)Symbol).IsGlobalNamespace)
                    {
                        appendChildren(indent: string.Empty);
                        return builder.ToString();
                    }

                    symbolName = Symbol.Name;
                    break;

                case SymbolKind.NamedType:
                    symbolName = Symbol.ToDisplayString();
                    var index = symbolName.LastIndexOf(".", StringComparison.OrdinalIgnoreCase);
                    if (index >= 0 && index < symbolName.Length)
                    {
                        symbolName = symbolName.Substring(index + 1);
                    }

                    break;

                default:
                    symbolName = Symbol.ToDisplayString();
                    break;
            }

            builder.Append($"{symbolName}: (Lines: {LinesOfCode}, MntIndex: {MaintainabilityIndex}, CycCxty: {CyclomaticComplexity}");
            if (CoupledNamedTypes.Count > 0)
            {
                var coupledNamedTypesStr = string.Join(", ", CoupledNamedTypes.Select(t => t.ToDisplayString()).OrderBy(n => n));
                builder.Append($", CoupledTypes: {{{coupledNamedTypesStr}}}");
            }

            if (DepthOfInheritance.HasValue)
            {
                builder.Append($", DepthInherit: {DepthOfInheritance}");
            }

            builder.Append($")");
            appendChildren(indent: "   ");
            return builder.ToString();

            void appendChildren(string indent)
            {
                foreach (var child in Children)
                {
                    foreach (var line in child.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        builder.AppendLine();
                        builder.Append($"{indent}{line}");
                    }
                }
            }
        }

        public static Task<CodeAnalysisMetricData> ComputeAsync(Compilation compilation, CancellationToken cancellationToken)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            return ComputeAsync(compilation.Assembly, compilation, cancellationToken);
        }

        public static Task<CodeAnalysisMetricData> ComputeAsync(ISymbol symbol, Compilation compilation, CancellationToken cancellationToken)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            var semanticModelProvider = new SemanticModelProvider(compilation);
            return ComputeAsync(symbol, semanticModelProvider, cancellationToken);
        }

        internal async static Task<CodeAnalysisMetricData> ComputeAsync(ISymbol symbol, SemanticModelProvider semanticModelProvider, CancellationToken cancellationToken)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Assembly:
                    return await AssemblyMetricData.ComputeAsync((IAssemblySymbol)symbol, semanticModelProvider, cancellationToken).ConfigureAwait(false);

                case SymbolKind.Namespace:
                    return await NamespaceMetricData.ComputeAsync((INamespaceSymbol)symbol, semanticModelProvider, cancellationToken).ConfigureAwait(false);

                case SymbolKind.NamedType:
                    return await NamedTypeMetricData.ComputeAsync((INamedTypeSymbol)symbol, semanticModelProvider, cancellationToken).ConfigureAwait(false);

                case SymbolKind.Method:
                    return await MethodMetricData.ComputeAsync((IMethodSymbol)symbol, semanticModelProvider, cancellationToken).ConfigureAwait(false);

                case SymbolKind.Property:
                    return await PropertyMetricData.ComputeAsync((IPropertySymbol)symbol, semanticModelProvider, cancellationToken).ConfigureAwait(false);

                case SymbolKind.Field:
                    return await FieldMetricData.ComputeAsync((IFieldSymbol)symbol, semanticModelProvider, cancellationToken).ConfigureAwait(false);

                case SymbolKind.Event:
                    return await EventMetricData.ComputeAsync((IEventSymbol)symbol, semanticModelProvider, cancellationToken).ConfigureAwait(false);

                default:
                    throw new NotSupportedException();
            }
        }

        internal static async Task<ImmutableArray<CodeAnalysisMetricData>> ComputeAsync(IEnumerable<ISymbol> children, SemanticModelProvider semanticModelProvider, CancellationToken cancellationToken)
            => (await Task.WhenAll(
                from child in children
#if !LEGACY_CODE_METRICS_MODE // Skip implicitly declared symbols, such as default constructor, for non-legacy mode.
                where !child.IsImplicitlyDeclared || (child as INamespaceSymbol)?.IsGlobalNamespace == true
#endif
                select Task.Run(() => ComputeAsync(child, semanticModelProvider, cancellationToken))).ConfigureAwait(false)).ToImmutableArray();
    }
}

#endif
