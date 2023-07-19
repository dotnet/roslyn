// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class DiagnosticAnalysisContextHelpers
    {
        internal static void VerifyArguments<TContext>(Action<TContext> action)
        {
            VerifyAction(action);
        }

        internal static void VerifyArguments<TContext>(Action<TContext> action, ImmutableArray<SymbolKind> symbolKinds)
        {
            VerifyAction(action);
            VerifySymbolKinds(symbolKinds);
        }

        internal static void VerifyArguments<TContext, TLanguageKindEnum>(Action<TContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds)
            where TLanguageKindEnum : struct
        {
            VerifyAction(action);
            VerifySyntaxKinds(syntaxKinds);
        }

        internal static void VerifyArguments<TContext>(Action<TContext> action, ImmutableArray<OperationKind> operationKinds)
        {
            VerifyAction(action);
            VerifyOperationKinds(operationKinds);
        }

        internal static void VerifyArguments(Diagnostic diagnostic, Compilation? compilation, Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic, CancellationToken cancellationToken)
        {
            if (diagnostic is DiagnosticWithInfo)
            {
                // Compiler diagnostic, skip validations.
                return;
            }

            if (diagnostic == null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            if (compilation != null)
            {
                VerifyDiagnosticLocationsInCompilation(diagnostic, compilation);
            }

            if (!isSupportedDiagnostic(diagnostic, cancellationToken))
            {
                throw new ArgumentException(string.Format(CodeAnalysisResources.UnsupportedDiagnosticReported, diagnostic.Id), nameof(diagnostic));
            }

            if (!UnicodeCharacterUtilities.IsValidIdentifier(diagnostic.Id))
            {
                // Disallow invalid diagnostic IDs.
                // Note that the parsing logic in Csc/Vbc MSBuild tasks to decode command line compiler output relies on diagnostics having a valid ID.
                // See https://github.com/dotnet/roslyn/issues/4376 for details.
                throw new ArgumentException(string.Format(CodeAnalysisResources.InvalidDiagnosticIdReported, diagnostic.Id), nameof(diagnostic));
            }
        }

        internal static void VerifyDiagnosticLocationsInCompilation(Diagnostic diagnostic, Compilation compilation)
        {
            VerifyDiagnosticLocationInCompilation(diagnostic.Id, diagnostic.Location, compilation);

            if (diagnostic.AdditionalLocations != null)
            {
                foreach (var location in diagnostic.AdditionalLocations)
                {
                    VerifyDiagnosticLocationInCompilation(diagnostic.Id, location, compilation);
                }
            }
        }

        private static void VerifyDiagnosticLocationInCompilation(string id, Location location, Compilation compilation)
        {
            if (!location.IsInSource)
            {
                return;
            }

            Debug.Assert(location.SourceTree != null);
            if (!compilation.ContainsSyntaxTree(location.SourceTree))
            {
                // Disallow diagnostics with source locations outside this compilation.
                throw new ArgumentException(string.Format(CodeAnalysisResources.InvalidDiagnosticLocationReported, id, location.SourceTree.FilePath), "diagnostic");
            }

            if (location.SourceSpan.End > location.SourceTree.Length)
            {
                // Disallow diagnostics with source locations outside this compilation.
                throw new ArgumentException(string.Format(CodeAnalysisResources.InvalidDiagnosticSpanReported, id, location.SourceSpan, location.SourceTree.FilePath), "diagnostic");
            }
        }

        private static void VerifyAction<TContext>(Action<TContext> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
        }

        private static void VerifySymbolKinds(ImmutableArray<SymbolKind> symbolKinds)
        {
            if (symbolKinds.IsDefault)
            {
                throw new ArgumentNullException(nameof(symbolKinds));
            }

            if (symbolKinds.IsEmpty)
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentCannotBeEmpty, nameof(symbolKinds));
            }
        }

        private static void VerifySyntaxKinds<TLanguageKindEnum>(ImmutableArray<TLanguageKindEnum> syntaxKinds)
            where TLanguageKindEnum : struct
        {
            if (syntaxKinds.IsDefault)
            {
                throw new ArgumentNullException(nameof(syntaxKinds));
            }

            if (syntaxKinds.IsEmpty)
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentCannotBeEmpty, nameof(syntaxKinds));
            }
        }

        private static void VerifyOperationKinds(ImmutableArray<OperationKind> operationKinds)
        {
            if (operationKinds.IsDefault)
            {
                throw new ArgumentNullException(nameof(operationKinds));
            }

            if (operationKinds.IsEmpty)
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentCannotBeEmpty, nameof(operationKinds));
            }
        }

        internal static void VerifyArguments<TKey, TValue>(TKey key, AnalysisValueProvider<TKey, TValue> valueProvider)
            where TKey : class
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }
        }

        internal static ControlFlowGraph GetControlFlowGraph(IOperation operation, Func<IOperation, ControlFlowGraph>? getControlFlowGraph, CancellationToken cancellationToken)
        {
            IOperation rootOperation = operation.GetRootOperation();
            return getControlFlowGraph != null ?
                getControlFlowGraph(rootOperation) :
                ControlFlowGraph.CreateCore(rootOperation, nameof(rootOperation), cancellationToken);
        }
    }
}
