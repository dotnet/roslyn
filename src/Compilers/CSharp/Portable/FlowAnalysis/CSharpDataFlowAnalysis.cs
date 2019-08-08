// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This class implements the region data flow analysis operations.  Region data flow analysis
    /// provides information how data flows into and out of a region.  The analysis is done lazily.
    /// When created, it performs no analysis, but simply caches the arguments. Then, the first time
    /// one of the analysis results is used it computes that one result and caches it. Each result
    /// is computed using a custom algorithm.
    /// </summary>
    internal class CSharpDataFlowAnalysis : DataFlowAnalysis
    {
        private readonly RegionAnalysisContext _context;

        private ImmutableArray<ISymbol> _variablesDeclared;
        private HashSet<Symbol> _unassignedVariables;
        private ImmutableArray<ISymbol> _dataFlowsIn;
        private ImmutableArray<ISymbol> _dataFlowsOut;
        private ImmutableArray<ISymbol> _definitelyAssignedOnEntry;
        private ImmutableArray<ISymbol> _definitelyAssignedOnExit;
        private ImmutableArray<ISymbol> _alwaysAssigned;
        private ImmutableArray<ISymbol> _readInside;
        private ImmutableArray<ISymbol> _writtenInside;
        private ImmutableArray<ISymbol> _readOutside;
        private ImmutableArray<ISymbol> _writtenOutside;
        private ImmutableArray<ISymbol> _captured;
        private ImmutableArray<ISymbol> _capturedInside;
        private ImmutableArray<ISymbol> _capturedOutside;
        private ImmutableArray<ISymbol> _unsafeAddressTaken;
        private HashSet<PrefixUnaryExpressionSyntax> _unassignedVariableAddressOfSyntaxes;
        private bool? _succeeded;

        internal CSharpDataFlowAnalysis(RegionAnalysisContext context)
        {
            _context = context;
        }

        /// <summary>
        /// A collection of the local variables that are declared within the region. Note that the region must be
        /// bounded by a method's body or a field's initializer, so method parameter symbols are never included
        /// in the result, but lambda parameters might appear in the result.
        /// </summary>
        public override ImmutableArray<ISymbol> VariablesDeclared
        {
            // Variables declared in the region is computed by a simple scan.
            // ISSUE: are these only variables declared at the top level in the region,
            // or are we to include variables declared in deeper scopes within the region?
            get
            {
                if (_variablesDeclared.IsDefault)
                {
                    var result = Succeeded
                        ? Normalize(VariablesDeclaredWalker.Analyze(_context.Compilation, _context.Member, _context.BoundNode, _context.FirstInRegion, _context.LastInRegion))
                        : ImmutableArray<ISymbol>.Empty;
                    ImmutableInterlocked.InterlockedInitialize(ref _variablesDeclared, result);
                }

                return _variablesDeclared;
            }
        }

        private HashSet<Symbol> UnassignedVariables
        {
            get
            {
                if (_unassignedVariables == null)
                {
                    var result = Succeeded
                        ? UnassignedVariablesWalker.Analyze(_context.Compilation, _context.Member, _context.BoundNode)
                        : new HashSet<Symbol>();
                    Interlocked.CompareExchange(ref _unassignedVariables, result, null);
                }

                return _unassignedVariables;
            }
        }

        /// <summary>
        /// A collection of the local variables for which a value assigned outside the region may be used inside the region.
        /// </summary>
        public override ImmutableArray<ISymbol> DataFlowsIn
        {
            get
            {
                if (_dataFlowsIn.IsDefault)
                {
                    _succeeded = !_context.Failed;
                    var result = _context.Failed ? ImmutableArray<ISymbol>.Empty :
                        Normalize(DataFlowsInWalker.Analyze(_context.Compilation, _context.Member, _context.BoundNode, _context.FirstInRegion, _context.LastInRegion, UnassignedVariables, UnassignedVariableAddressOfSyntaxes, out _succeeded));
                    ImmutableInterlocked.InterlockedInitialize(ref _dataFlowsIn, result);
                }

                return _dataFlowsIn;
            }
        }

        /// <summary>
        /// The set of local variables which are definitely assigned a value when a region is
        /// entered.
        /// </summary>
        public override ImmutableArray<ISymbol> DefinitelyAssignedOnEntry
            => ComputeDefinitelyAssignedValues().onEntry;

        /// <summary>
        /// The set of local variables which are definitely assigned a value when a region is
        /// exited.
        /// </summary>
        public override ImmutableArray<ISymbol> DefinitelyAssignedOnExit
            => ComputeDefinitelyAssignedValues().onExit;

        private (ImmutableArray<ISymbol> onEntry, ImmutableArray<ISymbol> onExit) ComputeDefinitelyAssignedValues()
        {
            if (_definitelyAssignedOnEntry.IsDefault ||
                _definitelyAssignedOnExit.IsDefault)
            {
                var entryResult = ImmutableArray<ISymbol>.Empty;
                var exitResult = ImmutableArray<ISymbol>.Empty;
                if (Succeeded)
                {
                    var (entry, exit) = DefinitelyAssignedWalker.Analyze(_context.Compilation, _context.Member, _context.BoundNode, _context.FirstInRegion, _context.LastInRegion);
                    entryResult = Normalize(entry);
                    exitResult = Normalize(exit);
                }

                ImmutableInterlocked.InterlockedInitialize(ref _definitelyAssignedOnEntry, entryResult);
                ImmutableInterlocked.InterlockedInitialize(ref _definitelyAssignedOnExit, exitResult);
            }

            return (_definitelyAssignedOnEntry, _definitelyAssignedOnExit);
        }

        /// <summary>
        /// A collection of the local variables for which a value assigned inside the region may be used outside the region.
        /// Note that every reachable assignment to a ref or out variable will be included in the results.
        /// </summary>
        public override ImmutableArray<ISymbol> DataFlowsOut
        {
            get
            {
                var discarded = DataFlowsIn; // force DataFlowsIn to be computed
                if (_dataFlowsOut.IsDefault)
                {
                    var result = Succeeded
                        ? Normalize(DataFlowsOutWalker.Analyze(_context.Compilation, _context.Member, _context.BoundNode, _context.FirstInRegion, _context.LastInRegion, UnassignedVariables, _dataFlowsIn))
                        : ImmutableArray<ISymbol>.Empty;
                    ImmutableInterlocked.InterlockedInitialize(ref _dataFlowsOut, result);
                }

                return _dataFlowsOut;
            }
        }

        /// <summary>
        /// A collection of the local variables for which a value is always assigned inside the region.
        /// </summary>
        public override ImmutableArray<ISymbol> AlwaysAssigned
        {
            get
            {
                if (_alwaysAssigned.IsDefault)
                {
                    var result = Succeeded
                        ? Normalize(AlwaysAssignedWalker.Analyze(_context.Compilation, _context.Member, _context.BoundNode, _context.FirstInRegion, _context.LastInRegion))
                        : ImmutableArray<ISymbol>.Empty;
                    ImmutableInterlocked.InterlockedInitialize(ref _alwaysAssigned, result);
                }

                return _alwaysAssigned;
            }
        }

        /// <summary>
        /// A collection of the local variables that are read inside the region.
        /// </summary>
        public override ImmutableArray<ISymbol> ReadInside
        {
            get
            {
                if (_readInside.IsDefault)
                {
                    AnalyzeReadWrite();
                }

                return _readInside;
            }
        }

        /// <summary>
        /// A collection of local variables that are written inside the region.
        /// </summary>
        public override ImmutableArray<ISymbol> WrittenInside
        {
            get
            {
                if (_writtenInside.IsDefault)
                {
                    AnalyzeReadWrite();
                }

                return _writtenInside;
            }
        }

        /// <summary>
        /// A collection of the local variables that are read outside the region.
        /// </summary>
        public override ImmutableArray<ISymbol> ReadOutside
        {
            get
            {
                if (_readOutside.IsDefault)
                {
                    AnalyzeReadWrite();
                }

                return _readOutside;
            }
        }

        /// <summary>
        /// A collection of local variables that are written outside the region.
        /// </summary>
        public override ImmutableArray<ISymbol> WrittenOutside
        {
            get
            {
                if (_writtenOutside.IsDefault)
                {
                    AnalyzeReadWrite();
                }

                return _writtenOutside;
            }
        }

        private void AnalyzeReadWrite()
        {
            IEnumerable<Symbol> readInside, writtenInside, readOutside, writtenOutside, captured, unsafeAddressTaken, capturedInside, capturedOutside;
            if (Succeeded)
            {
                ReadWriteWalker.Analyze(_context.Compilation, _context.Member, _context.BoundNode, _context.FirstInRegion, _context.LastInRegion, UnassignedVariableAddressOfSyntaxes,
                    readInside: out readInside, writtenInside: out writtenInside,
                    readOutside: out readOutside, writtenOutside: out writtenOutside,
                    captured: out captured, unsafeAddressTaken: out unsafeAddressTaken,
                    capturedInside: out capturedInside, capturedOutside: out capturedOutside);
            }
            else
            {
                readInside = writtenInside = readOutside = writtenOutside = captured = unsafeAddressTaken = capturedInside = capturedOutside = Enumerable.Empty<Symbol>();
            }

            ImmutableInterlocked.InterlockedInitialize(ref _readInside, Normalize(readInside));
            ImmutableInterlocked.InterlockedInitialize(ref _writtenInside, Normalize(writtenInside));
            ImmutableInterlocked.InterlockedInitialize(ref _readOutside, Normalize(readOutside));
            ImmutableInterlocked.InterlockedInitialize(ref _writtenOutside, Normalize(writtenOutside));
            ImmutableInterlocked.InterlockedInitialize(ref _captured, Normalize(captured));
            ImmutableInterlocked.InterlockedInitialize(ref _capturedInside, Normalize(capturedInside));
            ImmutableInterlocked.InterlockedInitialize(ref _capturedOutside, Normalize(capturedOutside));
            ImmutableInterlocked.InterlockedInitialize(ref _unsafeAddressTaken, Normalize(unsafeAddressTaken));
        }

        /// <summary>
        /// A collection of the non-constant local variables and parameters that have been referenced in anonymous functions
        /// and therefore must be moved to a field of a frame class.
        /// </summary>
        public override ImmutableArray<ISymbol> Captured
        {
            get
            {
                if (_captured.IsDefault)
                {
                    AnalyzeReadWrite();
                }

                return _captured;
            }
        }

        public override ImmutableArray<ISymbol> CapturedInside
        {
            get
            {
                if (_capturedInside.IsDefault)
                {
                    AnalyzeReadWrite();
                }

                return _capturedInside;
            }
        }

        public override ImmutableArray<ISymbol> CapturedOutside
        {
            get
            {
                if (_capturedOutside.IsDefault)
                {
                    AnalyzeReadWrite();
                }

                return _capturedOutside;
            }
        }

        /// <summary>
        /// A collection of the non-constant local variables and parameters that have had their address (or the address of one
        /// of their fields) taken using the '&amp;' operator.
        /// </summary>
        /// <remarks>
        /// If there are any of these in the region, then a method should not be extracted.
        /// </remarks>
        public override ImmutableArray<ISymbol> UnsafeAddressTaken
        {
            get
            {
                if (_unsafeAddressTaken.IsDefault)
                {
                    AnalyzeReadWrite();
                }

                return _unsafeAddressTaken;
            }
        }

        private HashSet<PrefixUnaryExpressionSyntax> UnassignedVariableAddressOfSyntaxes
        {
            get
            {
                if (_unassignedVariableAddressOfSyntaxes == null)
                {
                    var result = Succeeded
                        ? UnassignedAddressTakenVariablesWalker.Analyze(_context.Compilation, _context.Member, _context.BoundNode)
                        : new HashSet<PrefixUnaryExpressionSyntax>();
                    Interlocked.CompareExchange(ref _unassignedVariableAddressOfSyntaxes, result, null);
                }

                return _unassignedVariableAddressOfSyntaxes;
            }
        }

        /// <summary>
        /// Returns true iff analysis was successful.  Analysis can fail if the region does not properly span a single expression,
        /// a single statement, or a contiguous series of statements within the enclosing block.
        /// </summary>
        public sealed override bool Succeeded
        {
            get
            {
                if (_succeeded == null)
                {
                    var discarded = DataFlowsIn;
                }

                return _succeeded.Value;
            }
        }

        private static ImmutableArray<ISymbol> Normalize(IEnumerable<Symbol> data)
        {
            var builder = ArrayBuilder<Symbol>.GetInstance();
            builder.AddRange(data.Where(s => s.CanBeReferencedByName));
            builder.Sort(LexicalOrderSymbolComparer.Instance);
            return builder.ToImmutableAndFree().As<ISymbol>();
        }
    }
}
