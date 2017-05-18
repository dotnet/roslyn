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
    internal class VariableUsePass : BoundTreeWalkerWithStackGuard
    {
        /// <summary>
        /// The current source assembly.
        /// </summary>
        private readonly SourceAssemblySymbol _sourceAssembly;

        /// <summary>
        /// Create a field-only visitor.
        /// </summary>
        /// <remarks>
        /// Even though this class looks stateless, it cannot be shared/cached on the CSharpCompilation.
        /// Specifically, BoundTreeWalkerWithStackGuard has an int _recursionDepth that breaks in multithreaded scenarios.
        /// </remarks>
        public VariableUsePass(SourceAssemblySymbol sourceAssembly)
        {
            _sourceAssembly = sourceAssembly;
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            _sourceAssembly.NoteFieldAccess(node.FieldSymbol.OriginalDefinition, true, false);
            return base.VisitFieldAccess(node);
        }
    }

    internal sealed class LocalVariableUsePass : VariableUsePass
    {
        /// <summary>
        /// Variables that were used anywhere, in the sense required to suppress warnings about
        /// unused variables.
        /// </summary>
        private readonly HashSet<LocalSymbol> _usedVariables;

        /// <summary>
        /// Variables that were used anywhere, in the sense required to suppress warnings about
        /// unused variables.
        /// </summary>
        private readonly HashSet<LocalFunctionSymbol> _usedLocalFunctions;

        /// <summary>
        /// Create a visitor for use within a method (e.g. local functions).
        /// Keep track of what local variables are referenced, by placing them in the HashSets.
        /// Also track field references, by marking them on the SourceAssembly.
        /// </summary>
        public LocalVariableUsePass(SourceAssemblySymbol sourceAssembly, HashSet<LocalSymbol> usedVariables, HashSet<LocalFunctionSymbol> usedLocalFunctions)
            : base(sourceAssembly)
        {
            _usedVariables = usedVariables;
            _usedLocalFunctions = usedLocalFunctions;
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            _usedVariables.Add(node.LocalSymbol);
            return base.VisitLocal(node);
        }

        private void VisitMethodReference(MethodSymbol symbol)
        {
            if (symbol == null)
            {
                return;
            }
            if (symbol.MethodKind == MethodKind.LocalFunction)
            {
                // Generic local functions don't matter: the only way they can appear in a constant pattern
                // is if they are in nameof, and nameof does not allow constructed generic local functions.
                _usedLocalFunctions.Add((LocalFunctionSymbol)symbol);
            }
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            VisitMethodReference(node.Method);
            return base.VisitCall(node);
        }

        public override BoundNode VisitConversion(BoundConversion node)
        {
            if (node.ConversionKind == ConversionKind.MethodGroup)
            {
                VisitMethodReference(node.SymbolOpt);
            }

            return base.VisitConversion(node);
        }

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            VisitMethodReference(node.MethodOpt);

            return base.VisitDelegateCreationExpression(node);
        }

        public override BoundNode VisitMethodGroup(BoundMethodGroup node)
        {
            foreach (var method in node.Methods)
            {
                VisitMethodReference(method);
            }

            return base.VisitMethodGroup(node);
        }
    }
}
