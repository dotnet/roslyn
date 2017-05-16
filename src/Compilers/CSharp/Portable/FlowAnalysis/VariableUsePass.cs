// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// VariableUsePass marks all referenced variables in the visited node as used (as a read).
    /// This pass is used for things like attribute arguments and default parameter expressions,
    /// which are otherwise not analyzed for use references.
    /// </summary>
    internal sealed class VariableUsePass : BoundTreeWalkerWithStackGuard
    {
        /// <summary>
        /// The current source assembly.
        /// </summary>
        private readonly SourceAssemblySymbol _sourceAssembly;

        /// <summary>
        /// Variables that were used anywhere, in the sense required to suppress warnings about
        /// unused variables.
        /// </summary>
        private readonly HashSet<LocalSymbol> _usedVariablesOpt;

        /// <summary>
        /// Variables that were used anywhere, in the sense required to suppress warnings about
        /// unused variables.
        /// </summary>
        private readonly HashSet<LocalFunctionSymbol> _usedLocalFunctionsOpt;

        /// <summary>
        /// Create a stateless visitor (except for the SourceAssembly).
        /// </summary>
        /// <remarks>
        /// An instance of this class is cached on CSharpCompilation, so be careful adding new state.
        /// (Either avoid state that cannot be shared across the compilation, or do not cache an instance on the compilation)
        /// </remarks>
        public VariableUsePass(SourceAssemblySymbol sourceAssembly)
            : this(sourceAssembly, null, null)
        {
        }

        /// <summary>
        /// Create a visitor for use within a method (e.g. local functions).
        /// Keep track of what local variables are referenced, by placing them in the HashSets.
        /// </summary>
        public VariableUsePass(SourceAssemblySymbol sourceAssembly, HashSet<LocalSymbol> usedVariables, HashSet<LocalFunctionSymbol> usedLocalFunctions)
        {
            _sourceAssembly = sourceAssembly;
            _usedVariablesOpt = usedVariables;
            _usedLocalFunctionsOpt = usedLocalFunctions;
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            _sourceAssembly.NoteFieldAccess(node.FieldSymbol, true, false);
            return base.VisitFieldAccess(node);
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            _usedVariablesOpt?.Add(node.LocalSymbol);
            return base.VisitLocal(node);
        }

        private void VisitLocalFunctionReference(LocalFunctionSymbol symbol)
        {
            _usedLocalFunctionsOpt?.Add(symbol);
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            if (node.Method.MethodKind == MethodKind.LocalFunction)
            {
                var localFunc = (LocalFunctionSymbol)node.Method.OriginalDefinition;
                VisitLocalFunctionReference(localFunc);
            }

            return base.VisitCall(node);
        }

        public override BoundNode VisitConversion(BoundConversion node)
        {
            if (node.ConversionKind == ConversionKind.MethodGroup
                && node.SymbolOpt?.MethodKind == MethodKind.LocalFunction)
            {
                var localFunc = (LocalFunctionSymbol)node.SymbolOpt.OriginalDefinition;
                VisitLocalFunctionReference(localFunc);
            }

            return base.VisitConversion(node);
        }

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            if (node.MethodOpt?.MethodKind == MethodKind.LocalFunction)
            {
                var localFunc = (LocalFunctionSymbol)node.MethodOpt.OriginalDefinition;
                VisitLocalFunctionReference(localFunc);
            }

            return base.VisitDelegateCreationExpression(node);
        }

        public override BoundNode VisitMethodGroup(BoundMethodGroup node)
        {
            foreach (var method in node.Methods)
            {
                if (method.MethodKind == MethodKind.LocalFunction)
                {
                    VisitLocalFunctionReference((LocalFunctionSymbol)method);
                }
            }

            return base.VisitMethodGroup(node);
        }
    }
}
