// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// FieldUsePass marks all referenced fields in the visited node as used (as a read).
    /// This pass is used for things like attribute arguments and default parameter expressions,
    /// which are otherwise not analyzed for use references.
    /// </summary>
    internal class FieldUsePass : BoundTreeWalkerWithStackGuard
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
        public FieldUsePass(SourceAssemblySymbol sourceAssembly)
        {
            _sourceAssembly = sourceAssembly;
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            _sourceAssembly.NoteFieldAccess(node.FieldSymbol, true, false);
            return base.VisitFieldAccess(node);
        }
    }

    internal sealed class LocalVariableUsePass : BoundTreeWalkerWithStackGuard
    {
        /// <summary>
        /// Variables that were used anywhere, in the sense required to suppress warnings about
        /// unused variables.
        /// </summary>
        private readonly HashSet<LocalSymbol> _usedVariables;

        /// <summary>
        /// Local functions that were used anywhere, in the sense required to suppress warnings
        /// about unused local functions.
        /// </summary>
        private readonly HashSet<LocalFunctionSymbol> _usedLocalFunctions;

        /// <summary>
        /// Create a visitor for use within a method (e.g. local functions).
        /// Keep track of what local variables are referenced, by placing them in the HashSets.
        /// </summary>
        public LocalVariableUsePass(HashSet<LocalSymbol> usedVariables, HashSet<LocalFunctionSymbol> usedLocalFunctions)
        {
            _usedVariables = usedVariables;
            _usedLocalFunctions = usedLocalFunctions;
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            _usedVariables.Add(node.LocalSymbol);
            return base.VisitLocal(node);
        }

        public override BoundNode VisitNameOfOperator(BoundNameOfOperator node)
        {
            if (node.Argument is BoundMethodGroup methodGroup &&
                methodGroup.Methods.Length == 1 &&
                methodGroup.Methods[0].MethodKind == MethodKind.LocalFunction)
            {
                // Generic local functions don't matter: the only way they can appear in a constant pattern
                // is if they are in nameof, and nameof does not allow constructed generic local functions.
                _usedLocalFunctions.Add((LocalFunctionSymbol)methodGroup.Methods[0]);
            }
            return base.VisitNameOfOperator(node);
        }
    }
}
