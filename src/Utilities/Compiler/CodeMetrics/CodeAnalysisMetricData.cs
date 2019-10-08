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
    /// <summary>
    /// Code analysis metrics data.
    /// See https://docs.microsoft.com/visualstudio/code-quality/code-metrics-values for more details
    /// </summary>
    public abstract partial class CodeAnalysisMetricData
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

            var executableLines = !computationalComplexityMetrics.IsDefault ?
                computationalComplexityMetrics.ExecutableLines :
                children.Sum(c => c.ExecutableLines);

            Symbol = symbol;
            MaintainabilityIndex = maintainabilityIndex;
            ComputationalComplexityMetrics = computationalComplexityMetrics;
            CoupledNamedTypes = coupledNamedTypes;
            SourceLines = linesOfCode;
            ExecutableLines = executableLines;
            CyclomaticComplexity = cyclomaticComplexity;
            DepthOfInheritance = depthOfInheritance;
            Children = children;
        }

        /// <summary>
        /// Symbol corresponding to the metric data.
        /// </summary>
        public ISymbol Symbol { get; }

        internal ComputationalComplexityMetrics ComputationalComplexityMetrics { get; }

        /// <summary>
        /// Indicates an index value between 0 and 100 that represents the relative ease of maintaining the code.
        /// A high value means better maintainability.
        /// </summary>
        public int MaintainabilityIndex { get; }

        /// <summary>
        /// Indicates the coupling to unique named types through parameters, local variables, return types, method calls,
        /// generic or template instantiations, base classes, interface implementations, fields defined on external types, and attribute decoration.
        /// Good software design dictates that types and methods should have high cohesion and low coupling.
        /// High coupling indicates a design that is difficult to reuse and maintain because of its many interdependencies on other types.
        /// </summary>
        public ImmutableHashSet<INamedTypeSymbol> CoupledNamedTypes { get; }

        /// <summary>
        /// Indicates the exact number of lines in source code file.
        /// </summary>
        public long SourceLines { get; }

        /// <summary>
        /// Indicates the approximate number of executable statements/lines in code.
        /// The count is based on the executable <see cref="IOperation"/>s in code and is therefore not the exact number of lines in the source code file.
        /// A high count might indicate that a type or method is trying to do too much work and should be split up.
        /// It might also indicate that the type or method might be hard to maintain.
        /// </summary>
        public long ExecutableLines { get; }

        /// <summary>
        /// Measures the structural complexity of the code.
        /// It is created by calculating the number of different code paths in the flow of the program.
        /// A program that has complex control flow requires more tests to achieve good code coverage and is less maintainable.
        /// </summary>
        public int CyclomaticComplexity { get; }

        /// <summary>
        /// Indicates the number of different classes that inherit from one another, all the way back to the base class.
        /// Depth of Inheritance is similar to class coupling in that a change in a base class can affect any of its inherited classes.
        /// The higher this number, the deeper the inheritance and the higher the potential for base class modifications to result in a breaking change.
        /// For Depth of Inheritance, a low value is good and a high value is bad.
        /// </summary>
        public int? DepthOfInheritance { get; }

        /// <summary>
        /// Array of code metrics data for symbolic children of <see cref="Symbol"/>, if any.
        /// </summary>
        public ImmutableArray<CodeAnalysisMetricData> Children { get; }

        /// <summary>
        /// Computes string representation of metrics data.
        /// </summary>
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

            builder.Append($"{symbolName}: (Lines: {SourceLines}, ExecutableLines: {ExecutableLines}, MntIndex: {MaintainabilityIndex}, CycCxty: {CyclomaticComplexity}");
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

        /// <summary>
        /// Computes <see cref="CodeAnalysisMetricData"/> for the given <paramref name="compilation"/>.
        /// </summary>
        public static Task<CodeAnalysisMetricData> ComputeAsync(Compilation compilation, CancellationToken cancellationToken)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            return ComputeAsync(compilation.Assembly, compilation, cancellationToken);
        }

        /// <summary>
        /// Computes <see cref="CodeAnalysisMetricData"/> for the given <paramref name="symbol"/> from the given <paramref name="compilation"/>.
        /// </summary>
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
            return symbol.Kind switch
            {
                SymbolKind.Assembly => await AssemblyMetricData.ComputeAsync((IAssemblySymbol)symbol, semanticModelProvider, cancellationToken).ConfigureAwait(false),

                SymbolKind.Namespace => await NamespaceMetricData.ComputeAsync((INamespaceSymbol)symbol, semanticModelProvider, cancellationToken).ConfigureAwait(false),

                SymbolKind.NamedType => await NamedTypeMetricData.ComputeAsync((INamedTypeSymbol)symbol, semanticModelProvider, cancellationToken).ConfigureAwait(false),

                SymbolKind.Method => await MethodMetricData.ComputeAsync((IMethodSymbol)symbol, semanticModelProvider, cancellationToken).ConfigureAwait(false),

                SymbolKind.Property => await PropertyMetricData.ComputeAsync((IPropertySymbol)symbol, semanticModelProvider, cancellationToken).ConfigureAwait(false),

                SymbolKind.Field => await FieldMetricData.ComputeAsync((IFieldSymbol)symbol, semanticModelProvider, cancellationToken).ConfigureAwait(false),

                SymbolKind.Event => await EventMetricData.ComputeAsync((IEventSymbol)symbol, semanticModelProvider, cancellationToken).ConfigureAwait(false),

                _ => throw new NotSupportedException(),
            };
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
