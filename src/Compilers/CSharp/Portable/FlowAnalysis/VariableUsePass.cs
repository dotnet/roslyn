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
    internal sealed class FieldUsePass : BoundTreeWalkerWithStackGuard
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

        /// <summary>
        /// Diagnostics are used when a CancelledByStackGuardException is thrown.
        /// </summary>
        public void Analyze(BoundNode node, DiagnosticBag diagnostics)
        {
            try
            {
                Visit(node);
            }
            catch (CancelledByStackGuardException ex)
            {
                ex.AddAnError(diagnostics);
            }
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
        /// Can throw <see cref="BoundTreeVisitor.CancelledByStackGuardException"/>.
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

        private void VisitMethodReference(MethodSymbol symbol)
        {
            if (symbol?.MethodKind == MethodKind.LocalFunction)
            {
                // Make sure we use the unconstructed local function, if it is generic
                _usedLocalFunctions.Add((LocalFunctionSymbol)symbol.OriginalDefinition);
            }
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            VisitMethodReference(node.Method);
            return base.VisitCall(node);
        }

        public override BoundNode VisitMethodGroup(BoundMethodGroup node)
        {
            // Local functions can only appear in method groups of size 1.
            if (node.Methods.Length == 1)
            {
                VisitMethodReference(node.Methods[0]);
            }
            return base.VisitMethodGroup(node);
        }
    }
}
