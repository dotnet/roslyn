// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class DataFlowPass : AbstractFlowPass<DataFlowPass.LocalState>
    {
        /// <summary>
        /// Some variables that should be considered initially assigned.  Used for region analysis.
        /// </summary>
        protected readonly HashSet<Symbol> initiallyAssignedVariables;

        /// <summary>
        /// Variables that were used anywhere, in the sense required to suppress warnings about
        /// unused variables.
        /// </summary>
        private readonly PooledHashSet<LocalSymbol> _usedVariables = PooledHashSet<LocalSymbol>.GetInstance();

        /// <summary>
        /// Variables that were used anywhere, in the sense required to suppress warnings about
        /// unused variables.
        /// </summary>
        private readonly PooledHashSet<LocalFunctionSymbol> _usedLocalFunctions = PooledHashSet<LocalFunctionSymbol>.GetInstance();

        /// <summary>
        /// Variables that were initialized or written anywhere.
        /// </summary>
        private readonly PooledHashSet<Symbol> _writtenVariables = PooledHashSet<Symbol>.GetInstance();

        /// <summary>
        /// Map from variables that had their addresses taken, to the location of the first corresponding
        /// address-of expression.
        /// </summary>
        /// <remarks>
        /// Doesn't include fixed statement address-of operands.
        /// </remarks>
        private readonly PooledDictionary<Symbol, Location> _unsafeAddressTakenVariables = PooledDictionary<Symbol, Location>.GetInstance();

        /// <summary>
        /// Variables that were captured by anonymous functions.
        /// </summary>
        private readonly PooledHashSet<Symbol> _capturedVariables = PooledHashSet<Symbol>.GetInstance();

        /// <summary>
        /// The current source assembly.
        /// </summary>
        private readonly SourceAssemblySymbol _sourceAssembly;

        /// <summary>
        /// A mapping from local variables to the index of their slot in a flow analysis local state.
        /// </summary>
        private readonly PooledDictionary<VariableIdentifier, int> _variableSlot = PooledDictionary<VariableIdentifier, int>.GetInstance();

        /// <summary>
        /// A set of address-of expressions for which the operand is not definitely assigned.
        /// </summary>
        private readonly HashSet<PrefixUnaryExpressionSyntax> _unassignedVariableAddressOfSyntaxes;

        /// <summary>
        /// A mapping from the local variable slot to the symbol for the local variable itself.  This
        /// is used in the implementation of region analysis (support for extract method) to compute
        /// the set of variables "always assigned" in a region of code.
        /// </summary>
        protected VariableIdentifier[] variableBySlot = new VariableIdentifier[1];

        /// <summary>
        /// Variable slots are allocated to local variables sequentially and never reused.  This is
        /// the index of the next slot number to use.
        /// </summary>
        protected int nextVariableSlot = 1;

        /// <summary>
        /// Tracks variables for which we have already reported a definite assignment error.  This
        /// allows us to report at most one such error per variable.
        /// </summary>
        private BitVector _alreadyReported;

        /// <summary>
        /// Reflects the enclosing method or lambda at the current location (in the bound tree).
        /// </summary>
        protected MethodSymbol currentMethodOrLambda { get; private set; }

        /// <summary>
        /// A cache for remember which structs are empty.
        /// </summary>
        private readonly EmptyStructTypeCache _emptyStructTypeCache;

        /// <summary>
        /// true if we should check to ensure that out parameters are assigned on every exit point.
        /// </summary>
        private readonly bool _requireOutParamsAssigned;

        /// <summary>
        /// The topmost method of this analysis.
        /// </summary>
        protected MethodSymbol topLevelMethod;

        protected bool _convertInsufficientExecutionStackExceptionToCancelledByStackGuardException = false; // By default, just let the original exception to bubble up.

        private bool _performStaticNullChecks;
        private PooledDictionary<BoundExpression, ObjectCreationPlaceholderLocal> _placeholderLocals;
        private LocalSymbol _implicitReceiver;

        protected override void Free()
        {
            _usedVariables.Free();
            _usedLocalFunctions.Free();
            _writtenVariables.Free();
            _capturedVariables.Free();
            _unsafeAddressTakenVariables.Free();
            _variableSlot.Free();

            if (_placeholderLocals != null)
            {
                _placeholderLocals.Free();
            }

            base.Free();
        }

        internal DataFlowPass(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            bool trackUnassignments = false,
            HashSet<PrefixUnaryExpressionSyntax> unassignedVariableAddressOfSyntaxes = null,
            bool requireOutParamsAssigned = true)
            : base(compilation, member, node, trackUnassignments: trackUnassignments)
        {
            this.initiallyAssignedVariables = null;
            _sourceAssembly = ((object)member == null) ? null : (SourceAssemblySymbol)member.ContainingAssembly;
            this.currentMethodOrLambda = member as MethodSymbol;
            _unassignedVariableAddressOfSyntaxes = unassignedVariableAddressOfSyntaxes;
            bool strict = compilation.FeatureStrictEnabled; // Compiler flag /features:strict removes the relaxed DA checking we have for backward compatibility
            _emptyStructTypeCache = new EmptyStructTypeCache(compilation, !strict);
            _requireOutParamsAssigned = requireOutParamsAssigned;
            this.topLevelMethod = member as MethodSymbol;
        }

        internal DataFlowPass(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            EmptyStructTypeCache emptyStructs,
            bool trackUnassignments = false,
            HashSet<Symbol> initiallyAssignedVariables = null)
            : base(compilation, member, node, trackUnassignments: trackUnassignments)
        {
            this.initiallyAssignedVariables = initiallyAssignedVariables;
            _sourceAssembly = ((object)member == null) ? null : (SourceAssemblySymbol)member.ContainingAssembly;
            this.currentMethodOrLambda = member as MethodSymbol;
            _unassignedVariableAddressOfSyntaxes = null;
            bool strict = compilation.FeatureStrictEnabled; // Compiler flag /features:strict removes the relaxed DA checking we have for backward compatibility
            _emptyStructTypeCache = emptyStructs ?? new EmptyStructTypeCache(compilation, !strict);
            _requireOutParamsAssigned = true;
            this.topLevelMethod = member as MethodSymbol;
        }

        /// <summary>
        /// Constructor to be used for region analysis, for which a struct type should never be considered empty.
        /// </summary>
        internal DataFlowPass(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            BoundNode firstInRegion,
            BoundNode lastInRegion,
            HashSet<Symbol> initiallyAssignedVariables = null,
            HashSet<PrefixUnaryExpressionSyntax> unassignedVariableAddressOfSyntaxes = null,
            bool trackUnassignments = false)
            : base(compilation, member, node, firstInRegion, lastInRegion, trackUnassignments: trackUnassignments)
        {
            this.initiallyAssignedVariables = initiallyAssignedVariables;
            _sourceAssembly = null;
            this.currentMethodOrLambda = member as MethodSymbol;
            _unassignedVariableAddressOfSyntaxes = unassignedVariableAddressOfSyntaxes;
            _emptyStructTypeCache = new NeverEmptyStructTypeCache();
        }

        protected override bool ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()
        {
            return _convertInsufficientExecutionStackExceptionToCancelledByStackGuardException;
        }

        protected override ImmutableArray<PendingBranch> Scan(ref bool badRegion)
        {
            this.Diagnostics.Clear();
            ImmutableArray<ParameterSymbol> methodParameters = MethodParameters;
            ParameterSymbol methodThisParameter = MethodThisParameter;
            _alreadyReported = BitVector.Empty;           // no variables yet reported unassigned
            this.State = ReachableState();                   // entry point is reachable
            this.regionPlace = RegionPlace.Before;
            EnterParameters(methodParameters);               // with parameters assigned
            if ((object)methodThisParameter != null)
            {
                EnterParameter(methodThisParameter);
                if (methodThisParameter.Type.SpecialType != SpecialType.None)
                {
                    int slot = GetOrCreateSlot(methodThisParameter);
                    SetSlotState(slot, true);
                }
            }

            this.backwardBranchChanged = false;              // prepare to detect backward goto statements

            ImmutableArray<PendingBranch> pendingReturns = base.Scan(ref badRegion);

            // check that each out parameter is definitely assigned at the end of the method.  If
            // there's more than one location, then the method is partial and we prefer to report an
            // out parameter in partial method error.
            Location location;
            if (ShouldAnalyzeOutParameters(out location))
            {
                LeaveParameters(methodParameters, null, location);
                if ((object)methodThisParameter != null) LeaveParameter(methodThisParameter, null, location);

                var savedState = this.State;
                foreach (PendingBranch returnBranch in pendingReturns)
                {
                    this.State = returnBranch.State;
                    LeaveParameters(methodParameters, returnBranch.Branch.Syntax, null);
                    if ((object)methodThisParameter != null) LeaveParameter(methodThisParameter, returnBranch.Branch.Syntax, null);
                    IntersectWith(ref savedState, ref this.State);
                }

                this.State = savedState;
            }

            return pendingReturns;
        }

        protected override ImmutableArray<PendingBranch> RemoveReturns()
        {
            var result = base.RemoveReturns();

            if ((object)currentMethodOrLambda != null &&
                currentMethodOrLambda.IsAsync &&
                !currentMethodOrLambda.IsImplicitlyDeclared)
            {
                var foundAwait = result.Any(pending => pending.Branch != null && pending.Branch.Kind == BoundKind.AwaitExpression);
                if (!foundAwait)
                {
                    Diagnostics.Add(ErrorCode.WRN_AsyncLacksAwaits, currentMethodOrLambda.Locations[0]);
                }
            }

            return result;
        }

        protected virtual void ReportUnassignedOutParameter(ParameterSymbol parameter, CSharpSyntaxNode node, Location location)
        {
            if (!_requireOutParamsAssigned && topLevelMethod == currentMethodOrLambda) return;
            if (Diagnostics != null && this.State.Reachable)
            {
                if (location == null)
                {
                    location = new SourceLocation(node);
                }

                bool reported = false;
                if (parameter.IsThis)
                {
                    // if it is a "this" parameter in a struct constructor, we use a different diagnostic reflecting which pieces are not assigned
                    int thisSlot = VariableSlot(parameter);
                    Debug.Assert(thisSlot > 0);
                    if (!this.State.IsAssigned(thisSlot))
                    {
                        foreach (var field in _emptyStructTypeCache.GetStructInstanceFields(parameter.Type.TypeSymbol))
                        {
                            if (_emptyStructTypeCache.IsEmptyStructType(field.Type.TypeSymbol)) continue;

                            var sourceField = field as SourceMemberFieldSymbol;
                            if ((object)sourceField != null && sourceField.HasInitializer) continue;

                            var backingField = field as SynthesizedBackingFieldSymbol;
                            if ((object)backingField != null && backingField.HasInitializer) continue;

                            int fieldSlot = VariableSlot(field, thisSlot);
                            if (fieldSlot == -1 || !this.State.IsAssigned(fieldSlot))
                            {
                                Symbol associatedPropertyOrEvent = field.AssociatedSymbol;
                                if ((object)associatedPropertyOrEvent != null && associatedPropertyOrEvent.Kind == SymbolKind.Property)
                                {
                                    Diagnostics.Add(ErrorCode.ERR_UnassignedThisAutoProperty, location, associatedPropertyOrEvent);
                                }
                                else
                                {
                                    Diagnostics.Add(ErrorCode.ERR_UnassignedThis, location, field);
                                }
                            }
                        }
                        reported = true;
                    }
                }

                if (!reported)
                {
                    Debug.Assert(!parameter.IsThis);
                    Diagnostics.Add(ErrorCode.ERR_ParamUnassigned, location, parameter.Name);
                }
            }
        }

        /// <summary>
        /// Perform data flow analysis, reporting all necessary diagnostics.
        /// </summary>
        public static void Analyze(CSharpCompilation compilation, Symbol member, BoundNode node, DiagnosticBag diagnostics, bool requireOutParamsAssigned = true)
        {
            var walker = new DataFlowPass(compilation, member, node, requireOutParamsAssigned: requireOutParamsAssigned);

            if (diagnostics != null)
            {
                walker._convertInsufficientExecutionStackExceptionToCancelledByStackGuardException = true;

                if ((node.SyntaxTree.Options as CSharpParseOptions)?.IsFeatureEnabled(MessageID.IDS_FeatureStaticNullChecking) == true &&
                    !member.NullableOptOut)
                {
                    walker._performStaticNullChecks = true;
                }
            }

            try
            {
                bool badRegion = false;
                walker.Analyze(ref badRegion, diagnostics);
                Debug.Assert(!badRegion);
            }
            catch (BoundTreeVisitor.CancelledByStackGuardException ex) when (diagnostics != null)
            {
                ex.AddAnError(diagnostics);
            }
            finally
            {
                walker.Free();
            }
        }

        /// <summary>
        /// Analyze the body, reporting all necessary diagnostics.
        /// </summary>
        protected void Analyze(ref bool badRegion, DiagnosticBag diagnostics)
        {
            ImmutableArray<PendingBranch> returns = Analyze(ref badRegion);
            if (diagnostics != null)
            {
                foreach (Symbol captured in _capturedVariables)
                {
                    Location location;
                    if (_unsafeAddressTakenVariables.TryGetValue(captured, out location))
                    {
                        Debug.Assert(captured.Kind == SymbolKind.Parameter || captured.Kind == SymbolKind.Local || captured.Kind == SymbolKind.RangeVariable);
                        diagnostics.Add(ErrorCode.ERR_LocalCantBeFixedAndHoisted, location, captured.Name);
                    }
                }

                diagnostics.AddRange(this.Diagnostics);
            }
        }

        /// <summary>
        /// Check if the variable is captured and, if so, add it to this._capturedVariables.
        /// </summary>
        /// <param name="variable">The variable to be checked</param>
        /// <param name="rangeVariableUnderlyingParameter">If variable.Kind is RangeVariable, its underlying lambda parameter. Else null.</param>
        private void CheckCaptured(Symbol variable, ParameterSymbol rangeVariableUnderlyingParameter = null)
        {
            switch (variable.Kind)
            {
                case SymbolKind.Local:
                    if (((LocalSymbol)variable).IsConst) break;
                    goto case SymbolKind.Parameter;
                case SymbolKind.Parameter:
                    if (currentMethodOrLambda != variable.ContainingSymbol)
                    {
                        NoteCaptured(variable);
                    }
                    break;
                case SymbolKind.RangeVariable:
                    if (rangeVariableUnderlyingParameter != null && currentMethodOrLambda != rangeVariableUnderlyingParameter.ContainingSymbol)
                    {
                        NoteCaptured(variable);
                    }
                    break;
            }
        }

        /// <summary>
        /// Add the variable to the captured set. For range variables we only add it if inside the region.
        /// </summary>
        /// <param name="variable"></param>
        private void NoteCaptured(Symbol variable)
        {
            if (variable.Kind != SymbolKind.RangeVariable || this.regionPlace == PreciseAbstractFlowPass<LocalState>.RegionPlace.Inside)
            {
                _capturedVariables.Add(variable);
            }
        }

        protected IEnumerable<Symbol> GetCaptured()
        {
            // do not expose poolable capturedVariables outside of this class
            return _capturedVariables.ToArray();
        }

        protected IEnumerable<Symbol> GetUnsafeAddressTaken()
        {
            // do not expose poolable unsafeAddressTakenVariables outside of this class
            return _unsafeAddressTakenVariables.Keys.ToArray();
        }

        #region Tracking reads/writes of variables for warnings

        protected virtual void NoteRead(Symbol variable, ParameterSymbol rangeVariableUnderlyingParameter = null)
        {
            var local = variable as LocalSymbol;
            if ((object)local != null)
            {
                _usedVariables.Add(local);
            }
            var localFunction = variable as LocalFunctionSymbol;
            if ((object)localFunction != null)
            {
                _usedLocalFunctions.Add(localFunction);
            }

            if ((object)variable != null)
            {
                if ((object)_sourceAssembly != null && variable.Kind == SymbolKind.Field)
                {
                    _sourceAssembly.NoteFieldAccess((FieldSymbol)variable.OriginalDefinition, read: true, write: false);
                }

                CheckCaptured(variable, rangeVariableUnderlyingParameter);
            }
        }

        private void NoteRead(BoundNode fieldOrEventAccess)
        {
            Debug.Assert(fieldOrEventAccess.Kind == BoundKind.FieldAccess || fieldOrEventAccess.Kind == BoundKind.EventAccess);
            BoundNode n = fieldOrEventAccess;
            while (n != null)
            {
                switch (n.Kind)
                {
                    case BoundKind.FieldAccess:
                        {
                            var fieldAccess = (BoundFieldAccess)n;
                            NoteRead(fieldAccess.FieldSymbol);
                            if (MayRequireTracking(fieldAccess.ReceiverOpt, fieldAccess.FieldSymbol))
                            {
                                n = fieldAccess.ReceiverOpt;
                                continue;
                            }
                            else
                            {
                                return;
                            }
                        }

                    case BoundKind.EventAccess:
                        {
                            var eventAccess = (BoundEventAccess)n;
                            FieldSymbol associatedField = eventAccess.EventSymbol.AssociatedField;
                            if ((object)associatedField != null)
                            {
                                NoteRead(associatedField);
                                if (MayRequireTracking(eventAccess.ReceiverOpt, associatedField))
                                {
                                    n = eventAccess.ReceiverOpt;
                                    continue;
                                }
                            }
                            return;
                        }

                    case BoundKind.ThisReference:
                        NoteRead(MethodThisParameter);
                        return;

                    case BoundKind.Local:
                        NoteRead(((BoundLocal)n).LocalSymbol);
                        return;

                    case BoundKind.Parameter:
                        NoteRead(((BoundParameter)n).ParameterSymbol);
                        return;

                    default:
                        return;
                }
            }
        }

        protected virtual void NoteWrite(Symbol variable, BoundExpression value, bool read)
        {
            if ((object)variable != null)
            {
                _writtenVariables.Add(variable);
                if ((object)_sourceAssembly != null && variable.Kind == SymbolKind.Field)
                {
                    var field = (FieldSymbol)variable.OriginalDefinition;
                    _sourceAssembly.NoteFieldAccess(field, read: read && WriteConsideredUse(field.Type.TypeSymbol, value), write: true);
                }

                var local = variable as LocalSymbol;
                if ((object)local != null && read && WriteConsideredUse(local.Type.TypeSymbol, value))
                {
                    // A local variable that is written to is considered to also be read,
                    // unless the written value is always a constant. The reasons for this
                    // unusual behavior are:
                    //
                    // * The debugger does not make it easy to see the returned value of 
                    //   a method. Often a call whose returned value would normally be
                    //   discarded is written into a local variable so that it can be
                    //   easily inspected in the debugger.
                    // 
                    // * An otherwise unread local variable that contains a reference to an
                    //   object can keep the object alive longer, particularly if the jitter
                    //   is not optimizing the lifetimes of locals. (Because, for example,
                    //   the debugger is running.) Again, this can be useful when debugging
                    //   because an otherwise unused object might be finalized later, allowing
                    //   the developer to more easily examine its state.
                    //
                    // * A developer who wishes to deliberately discard a value returned by
                    //   a method can do so in a self-documenting manner via 
                    //   "var unread = M();"
                    //
                    // We suppress the "written but not read" message on locals unless what is
                    // written is a constant, a null, a default(T) expression, a default constructor
                    // of a value type, or a built-in conversion operating on a constant, etc.

                    _usedVariables.Add(local);
                }

                CheckCaptured(variable);
            }
        }

        /// <summary>
        /// This reflects the Dev10 compiler's rules for when a variable initialization is considered a "use"
        /// for the purpose of suppressing the warning about unused variables.
        /// </summary>
        internal static bool WriteConsideredUse(TypeSymbol type, BoundExpression value)
        {
            if (value == null || value.HasAnyErrors) return true;
            if ((object)type != null && type.IsReferenceType && type.SpecialType != SpecialType.System_String)
            {
                return value.ConstantValue != ConstantValue.Null;
            }

            if ((object)type != null && type.IsPointerType())
            {
                // We always suppress the warning for pointer types. 
                return true;
            }

            if (value.ConstantValue != null) return false;
            switch (value.Kind)
            {
                case BoundKind.Conversion:
                    {
                        BoundConversion boundConversion = (BoundConversion)value;
                        // The native compiler suppresses the warning for all user defined
                        // conversions. A cast from int to IntPtr is also treated as an explicit
                        // user-defined conversion. Therefore the IntPtr ConversionKind is included
                        // here.
                        if (boundConversion.ConversionKind.IsUserDefinedConversion() ||
                            boundConversion.ConversionKind == ConversionKind.IntPtr)
                        {
                            return true;
                        }
                        return WriteConsideredUse(null, boundConversion.Operand);
                    }
                case BoundKind.DefaultOperator:
                    return false;
                case BoundKind.ObjectCreationExpression:
                    var init = (BoundObjectCreationExpression)value;
                    return !init.Constructor.IsImplicitlyDeclared || init.InitializerExpressionOpt != null;
                default:
                    return true;
            }
        }

        private void NoteWrite(BoundExpression n, BoundExpression value, bool read)
        {
            while (n != null)
            {
                switch (n.Kind)
                {
                    case BoundKind.FieldAccess:
                        {
                            var fieldAccess = (BoundFieldAccess)n;
                            if ((object)_sourceAssembly != null)
                            {
                                var field = fieldAccess.FieldSymbol.OriginalDefinition;
                                _sourceAssembly.NoteFieldAccess(field, read: value == null || WriteConsideredUse(fieldAccess.FieldSymbol.Type.TypeSymbol, value), write: true);
                            }

                            if (MayRequireTracking(fieldAccess.ReceiverOpt, fieldAccess.FieldSymbol))
                            {
                                n = fieldAccess.ReceiverOpt;
                                if (n.Kind == BoundKind.Local)
                                {
                                    _usedVariables.Add(((BoundLocal)n).LocalSymbol);
                                }
                                continue;
                            }
                            else
                            {
                                return;
                            }
                        }

                    case BoundKind.EventAccess:
                        {
                            var eventAccess = (BoundEventAccess)n;
                            FieldSymbol associatedField = eventAccess.EventSymbol.AssociatedField;
                            if ((object)associatedField != null)
                            {
                                if ((object)_sourceAssembly != null)
                                {
                                    var field = associatedField.OriginalDefinition;
                                    _sourceAssembly.NoteFieldAccess(field, read: value == null || WriteConsideredUse(associatedField.Type.TypeSymbol, value), write: true);
                                }

                                if (MayRequireTracking(eventAccess.ReceiverOpt, associatedField))
                                {
                                    n = eventAccess.ReceiverOpt;
                                    continue;
                                }
                            }
                            return;
                        }

                    case BoundKind.ThisReference:
                        NoteWrite(MethodThisParameter, value, read);
                        return;

                    case BoundKind.Local:
                        NoteWrite(((BoundLocal)n).LocalSymbol, value, read);
                        return;

                    case BoundKind.Parameter:
                        NoteWrite(((BoundParameter)n).ParameterSymbol, value, read);
                        return;

                    case BoundKind.RangeVariable:
                        NoteWrite(((BoundRangeVariable)n).Value, value, read);
                        return;

                    default:
                        return;
                }
            }
        }

        #endregion Tracking reads/writes of variables for warnings

        /// <summary>
        /// Locals are given slots when their declarations are encountered.  We only need give slots
        /// to local variables, out parameters, and the "this" variable of a struct constructs.
        /// Other variables are not given slots, and are therefore not tracked by the analysis.  This
        /// returns -1 for a variable that is not tracked, for fields of structs that have the same
        /// assigned status as the container, and for structs that (recursively) contain no data members.
        /// We do not need to track references to
        /// variables that occur before the variable is declared, as those are reported in an
        /// earlier phase as "use before declaration". That allows us to avoid giving slots to local
        /// variables before processing their declarations.
        /// </summary>
        protected int VariableSlot(Symbol symbol, int containingSlot = 0)
        {
            int slot;
            return (_variableSlot.TryGetValue(new VariableIdentifier(symbol, containingSlot), out slot)) ? slot : -1;
        }

        /// <summary>
        /// Force a variable to have a slot.  Returns -1 if the variable has an empty struct type.
        /// </summary>
        protected int GetOrCreateSlot(Symbol symbol, int containingSlot = 0)
        {
            Debug.Assert(!IsConditionalState);

            if (symbol is RangeVariableSymbol) return -1;
            VariableIdentifier identifier = new VariableIdentifier(symbol, containingSlot);
            int slot;

            // Since analysis may proceed in multiple passes, it is possible the slot is already assigned.
            if (!_variableSlot.TryGetValue(identifier, out slot))
            {
                TypeSymbol variableType = VariableType(symbol);
                if (_emptyStructTypeCache.IsEmptyStructType(variableType))
                {
                    return -1;
                }

                slot = nextVariableSlot++;
                _variableSlot.Add(identifier, slot);
                if (slot >= variableBySlot.Length)
                {
                    Array.Resize(ref this.variableBySlot, slot * 2);
                }

                variableBySlot[slot] = identifier;
            }

            NormalizeAssigned(ref this.State);

            if (_performStaticNullChecks && this.State.Reachable)
            {
                NormalizeNullable(ref this.State);
            }

            return slot;
        }

        private void NormalizeAssigned(ref LocalState state)
        {
            int oldNext = state.Assigned.Capacity;
            state.Assigned.EnsureCapacity(nextVariableSlot);
            for (int i = oldNext; i < nextVariableSlot; i++)
            {
                var id = variableBySlot[i];
                state.Assigned[i] = (id.ContainingSlot > 0) && state.Assigned[id.ContainingSlot];
            }
        }

        private void NormalizeNullable(ref LocalState state)
        {
            state.KnownNullState.EnsureCapacity(nextVariableSlot);
            state.NotNull.EnsureCapacity(nextVariableSlot);
        }

        /// <summary>
        /// Return the slot for a variable, or -1 if it is not tracked (because, for example, it is an empty struct).
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected int MakeSlot(BoundExpression node)
        {
            switch (node.Kind)
            {
                case BoundKind.ThisReference:
                    return (object)MethodThisParameter != null ? GetOrCreateSlot(MethodThisParameter) : -1;
                case BoundKind.BaseReference:
                    return GetOrCreateSlot(MethodThisParameter);
                case BoundKind.Local:
                    return GetOrCreateSlot(((BoundLocal)node).LocalSymbol);
                case BoundKind.Parameter:
                    return GetOrCreateSlot(((BoundParameter)node).ParameterSymbol);
                case BoundKind.RangeVariable:
                    return MakeSlot(((BoundRangeVariable)node).Value);
                case BoundKind.FieldAccess:
                    return MakeSlot((BoundFieldAccess)node);
                case BoundKind.EventAccess:
                    {
                        var eventAccess = (BoundEventAccess)node;
                        var eventSymbol = eventAccess.EventSymbol;
                        var receiverOpt = eventAccess.ReceiverOpt;
                        if (eventSymbol.IsStatic || receiverOpt == null || receiverOpt.Kind == BoundKind.TypeExpression) return -1; // access of static event
                        if ((object)receiverOpt.Type == null || receiverOpt.Type.TypeKind != TypeKind.Struct) return -1; // event of non-struct
                        if (!eventSymbol.HasAssociatedField) return -1;
                        int containingSlot = MakeSlot(receiverOpt);
                        return (containingSlot == -1) ? -1 : GetOrCreateSlot(eventSymbol.AssociatedField, containingSlot);
                    }
                case BoundKind.PropertyAccess:
                    {
                        var propAccess = (BoundPropertyAccess)node;
                        var propSymbol = propAccess.PropertySymbol;

                        if (Binder.AccessingAutopropertyFromConstructor(propAccess, this.currentMethodOrLambda))
                        {
                            var backingField = (propSymbol as SourcePropertySymbol)?.BackingField;
                            if (backingField != null)
                            {
                                var receiverOpt = propAccess.ReceiverOpt;
                                if (propSymbol.IsStatic || receiverOpt == null || receiverOpt.Kind == BoundKind.TypeExpression) return -1; // access of static property
                                if ((object)receiverOpt.Type == null || receiverOpt.Type.TypeKind != TypeKind.Struct) return -1; // property of non-struct
                                int containingSlot = MakeSlot(receiverOpt);
                                return (containingSlot == -1) ? -1 : GetOrCreateSlot(backingField, containingSlot);
                            }
                        }
                        else if (IsTrackableAnonymousTypeProperty(propSymbol))
                        {
                            Debug.Assert(!propSymbol.Type.IsReferenceType || propSymbol.Type.IsNullable == true);
                            var receiverOpt = propAccess.ReceiverOpt;
                            if (receiverOpt == null || receiverOpt.Kind == BoundKind.TypeExpression) return -1;
                            int containingSlot = MakeSlot(receiverOpt);
                            return (containingSlot == -1) ? -1 : GetOrCreateSlot(propSymbol, containingSlot);
                        }

                        goto default;
                    }
                case BoundKind.ObjectCreationExpression:
                case BoundKind.AnonymousObjectCreationExpression:
                    {
                        if (_performStaticNullChecks)
                        {
                            ObjectCreationPlaceholderLocal placeholder;
                            if (_placeholderLocals != null && _placeholderLocals.TryGetValue(node, out placeholder))
                            {
                                return GetOrCreateSlot(placeholder);
                            }
                        }

                        goto default;
                    }
                default:
                    return -1;
            }
        }

        private bool IsTrackableAnonymousTypeProperty(PropertySymbol propSymbol)
        {
            return _performStaticNullChecks && 
                   !propSymbol.IsStatic && 
                   propSymbol.IsReadOnly &&
                   propSymbol.ContainingType.IsAnonymousType &&
                   (propSymbol.Type.IsReferenceType || EmptyStructTypeCache.IsTrackableStructType(propSymbol.Type.TypeSymbol));
        }

        protected int MakeSlot(BoundFieldAccess fieldAccess)
        {
            var fieldSymbol = fieldAccess.FieldSymbol;
            var receiverOpt = fieldAccess.ReceiverOpt;

            if (!MayRequireTracking(receiverOpt, fieldSymbol))
            {
                return -1;
            }

            int containingSlot = MakeSlot(receiverOpt);
            return (containingSlot == -1) ? -1 : GetOrCreateSlot(fieldSymbol, containingSlot);
        }

        /// <summary>
        /// Check that the given variable is definitely assigned.  If not, produce an error.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="node"></param>
        protected void CheckAssigned(Symbol symbol, CSharpSyntaxNode node)
        {
            if ((object)symbol != null)
            {
                if (this.State.Reachable)
                {
                    int slot = VariableSlot(symbol);
                    if (slot >= this.State.Assigned.Capacity) NormalizeAssigned(ref this.State);
                    if (slot > 0 && !this.State.IsAssigned(slot))
                    {
                        ReportUnassigned(symbol, node);
                    }
                }

                NoteRead(symbol);
            }
        }

        /// <summary>
        /// Report a given variable as not definitely assigned.  Once a variable has been so
        /// reported, we suppress further reports of that variable.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="node"></param>
        protected virtual void ReportUnassigned(Symbol symbol, CSharpSyntaxNode node)
        {
            int slot = VariableSlot(symbol);
            if (slot <= 0) return;
            if (slot >= _alreadyReported.Capacity) _alreadyReported.EnsureCapacity(nextVariableSlot);
            if (symbol.Kind == SymbolKind.Local && (symbol.Locations.Length == 0 || node.Span.End < symbol.Locations[0].SourceSpan.Start))
            {
                // We've already reported the use of a local before its declaration.  No need to emit
                // another diagnostic for the same issue.
            }
            else if (!_alreadyReported[slot])
            {
                // CONSIDER: could suppress this diagnostic in cases where the local was declared in a using
                // or fixed statement because there's a special error code for not initializing those.

                ErrorCode errorCode =
                    (symbol.Kind == SymbolKind.Parameter && ((ParameterSymbol)symbol).RefKind == RefKind.Out) ?
                    (((ParameterSymbol)symbol).IsThis) ? ErrorCode.ERR_UseDefViolationThis : ErrorCode.ERR_UseDefViolationOut :
                        ErrorCode.ERR_UseDefViolation;
                Diagnostics.Add(errorCode, new SourceLocation(node), symbol.Name);
            }

            _alreadyReported[slot] = true; // mark the variable's slot so that we don't complain about the variable again
        }

        protected virtual void CheckAssigned(BoundExpression expr, FieldSymbol fieldSymbol, CSharpSyntaxNode node)
        {
            int unassignedSlot;
            if (this.State.Reachable && !IsAssigned(expr, out unassignedSlot))
            {
                ReportUnassigned(fieldSymbol, unassignedSlot, node);
            }

            NoteRead(expr);
        }

        private bool IsAssigned(BoundExpression node, out int unassignedSlot)
        {
            unassignedSlot = -1;
            if (_emptyStructTypeCache.IsEmptyStructType(node.Type)) return true;
            switch (node.Kind)
            {
                case BoundKind.ThisReference:
                    {
                        var self = MethodThisParameter;
                        if ((object)self == null)
                        {
                            unassignedSlot = -1;
                            return true;
                        }

                        unassignedSlot = GetOrCreateSlot(MethodThisParameter);
                        break;
                    }

                case BoundKind.Local:
                    {
                        unassignedSlot = GetOrCreateSlot(((BoundLocal)node).LocalSymbol);
                        break;
                    }

                case BoundKind.FieldAccess:
                    {
                        var fieldAccess = (BoundFieldAccess)node;
                        if (!MayRequireTracking(fieldAccess.ReceiverOpt, fieldAccess.FieldSymbol) || IsAssigned(fieldAccess.ReceiverOpt, out unassignedSlot))
                        {
                            return true;
                        }

                        unassignedSlot = GetOrCreateSlot(fieldAccess.FieldSymbol, unassignedSlot);
                        break;
                    }

                case BoundKind.EventAccess:
                    {
                        var eventAccess = (BoundEventAccess)node;
                        if (!MayRequireTracking(eventAccess.ReceiverOpt, eventAccess.EventSymbol.AssociatedField) || IsAssigned(eventAccess.ReceiverOpt, out unassignedSlot))
                        {
                            return true;
                        }

                        unassignedSlot = GetOrCreateSlot(eventAccess.EventSymbol.AssociatedField, unassignedSlot);
                        break;
                    }

                case BoundKind.PropertyAccess:
                    {
                        var propertyAccess = (BoundPropertyAccess)node;
                        if (Binder.AccessingAutopropertyFromConstructor(propertyAccess, this.currentMethodOrLambda))
                        {
                            var property = propertyAccess.PropertySymbol;
                            var backingField = (property as SourcePropertySymbol)?.BackingField;
                            if (backingField != null)
                            {
                                if (!MayRequireTracking(propertyAccess.ReceiverOpt, backingField) || IsAssigned(propertyAccess.ReceiverOpt, out unassignedSlot))
                                {
                                    return true;
                                }

                                unassignedSlot = GetOrCreateSlot(backingField, unassignedSlot);
                                break;
                            }
                        }

                        goto default;
                    }

                case BoundKind.Parameter:
                    {
                        var parameter = ((BoundParameter)node);
                        unassignedSlot = GetOrCreateSlot(parameter.ParameterSymbol);
                        break;
                    }

                case BoundKind.RangeVariable:
                // range variables are always assigned
                default:
                    {
                        // The value is a method call return value or something else we can assume is assigned.
                        unassignedSlot = -1;
                        return true;
                    }
            }

            Debug.Assert(unassignedSlot > 0);
            return this.State.IsAssigned(unassignedSlot);
        }

        protected virtual void ReportUnassigned(FieldSymbol fieldSymbol, int unassignedSlot, CSharpSyntaxNode node)
        {
            _alreadyReported.EnsureCapacity(unassignedSlot + 1);
            if (!_alreadyReported[unassignedSlot])
            {
                var associatedSymbol = fieldSymbol.AssociatedSymbol;
                if (associatedSymbol?.Kind == SymbolKind.Property)
                {
                    Diagnostics.Add(ErrorCode.ERR_UseDefViolationProperty, new SourceLocation(node), associatedSymbol.Name);
                }
                else
                {
                    Diagnostics.Add(ErrorCode.ERR_UseDefViolationField, new SourceLocation(node), fieldSymbol.Name);
                }

                _alreadyReported[unassignedSlot] = true; // mark the variable's slot so that we don't complain about the variable again
            }
        }

        protected Symbol GetNonMemberSymbol(int slot)
        {
            VariableIdentifier variableId = variableBySlot[slot];
            while (variableId.ContainingSlot > 0)
            {
                Debug.Assert(variableId.Symbol.Kind == SymbolKind.Field || (_performStaticNullChecks && variableId.Symbol.Kind == SymbolKind.Property));
                variableId = variableBySlot[variableId.ContainingSlot];
            }
            return variableId.Symbol;
        }

        private Symbol UseNonFieldSymbolUnsafely(BoundExpression expression)
        {
            while (expression != null)
            {
                switch (expression.Kind)
                {
                    case BoundKind.FieldAccess:
                        {
                            var fieldAccess = (BoundFieldAccess)expression;
                            var fieldSymbol = fieldAccess.FieldSymbol;
                            if ((object)_sourceAssembly != null) _sourceAssembly.NoteFieldAccess(fieldSymbol, true, true);
                            if (fieldSymbol.ContainingType.IsReferenceType || fieldSymbol.IsStatic) return null;
                            expression = fieldAccess.ReceiverOpt;
                            continue;
                        }
                    case BoundKind.Local:
                        var result = ((BoundLocal)expression).LocalSymbol;
                        _usedVariables.Add(result);
                        return result;
                    case BoundKind.RangeVariable:
                        return ((BoundRangeVariable)expression).RangeVariableSymbol;
                    case BoundKind.Parameter:
                        return ((BoundParameter)expression).ParameterSymbol;
                    case BoundKind.ThisReference:
                        return this.MethodThisParameter;
                    case BoundKind.BaseReference:
                        return this.MethodThisParameter;
                    default:
                        return null;
                }
            }

            return null;
        }

        protected void Assign(BoundNode node, BoundExpression value, bool? valueIsNotNull, RefKind refKind = RefKind.None, bool read = true)
        {
            AssignImpl(node, value, valueIsNotNull, written: true, refKind: refKind, read: read);
        }

        /// <summary>
        /// Mark a variable as assigned (or unassigned).
        /// </summary>
        /// <param name="node">Node being assigned to.</param>
        /// <param name="value">The value being assigned.</param>
        /// <param name="valueIsNotNull"/>
        /// <param name="written">True if target location is considered written to.</param>
        /// <param name="refKind">Target kind (by-ref or not).</param>
        /// <param name="read">True if target location is considered read from.</param>
        protected virtual void AssignImpl(BoundNode node, BoundExpression value, bool? valueIsNotNull, RefKind refKind, bool written, bool read)
        {
            Debug.Assert(!IsConditionalState);

            switch (node.Kind)
            {
                case BoundKind.LocalDeclaration:
                    {
                        var local = (BoundLocalDeclaration)node;
                        Debug.Assert(local.InitializerOpt == value || value == null);
                        LocalSymbol symbol = local.LocalSymbol;
                        int slot = GetOrCreateSlot(symbol);
                        SetSlotState(slot, assigned: written || !this.State.Reachable);
                        if (written)
                        {
                            NoteWrite(symbol, value, read);
                            TrackNullableStateForAssignment(node, symbol, slot, value, valueIsNotNull);
                        }

                        break;
                    }

                case BoundKind.Local:
                    {
                        var local = (BoundLocal)node;
                        if (local.LocalSymbol.RefKind != refKind)
                        {
                            // Writing through the (reference) value of a reference local
                            // requires us to read the reference itself.
                            if (written) VisitRvalue(local);

                            // TODO: StaticNullChecking?
                        }
                        else
                        {
                            int slot = MakeSlot(local);
                            SetSlotState(slot, written);
                            if (written)
                            {
                                NoteWrite(local, value, read);
                                TrackNullableStateForAssignment(node, local.LocalSymbol, slot, value, valueIsNotNull);
                            }
                        }
                        break;
                    }

                case BoundKind.Parameter:
                    {
                        var parameter = (BoundParameter)node;
                        int slot = GetOrCreateSlot(parameter.ParameterSymbol); 
                        SetSlotState(slot, written);
                        if (written)
                        {
                            NoteWrite(parameter, value, read);
                            TrackNullableStateForAssignment(node, parameter.ParameterSymbol, slot, value, valueIsNotNull);
                        }
                        break;
                    }

                case BoundKind.FieldAccess:
                    {
                        var fieldAccess = (BoundFieldAccess)node;
                        int slot = MakeSlot(fieldAccess);
                        SetSlotState(slot, written);
                        if (written)
                        {
                            NoteWrite(fieldAccess, value, read);
                            TrackNullableStateForAssignment(node, fieldAccess.FieldSymbol, slot, value, valueIsNotNull);
                        }
                        break;
                    }

                case BoundKind.EventAccess:
                    {
                        var eventAccess = (BoundEventAccess)node;
                        int slot = MakeSlot(eventAccess);
                        SetSlotState(slot, written);
                        if (written)
                        {
                            NoteWrite(eventAccess, value, read);
                            TrackNullableStateForAssignment(node, eventAccess.EventSymbol, slot, value, valueIsNotNull);
                        }
                        break;
                    }

                case BoundKind.PropertyAccess:
                    {
                        var propertyAccesss = (BoundPropertyAccess)node;
                        int slot = MakeSlot(propertyAccesss);
                        SetSlotState(slot, written);
                        if (written)
                        {
                            NoteWrite(propertyAccesss, value, read);
                            TrackNullableStateForAssignment(node, propertyAccesss.PropertySymbol, slot, value, valueIsNotNull);
                        }
                        break;
                    }

                case BoundKind.IndexerAccess:
                    {
                        if (written && _performStaticNullChecks && this.State.Reachable)
                        {
                            var indexerAccesss = (BoundIndexerAccess)node;
                            TrackNullableStateForAssignment(node, indexerAccesss.Indexer, -1, value, valueIsNotNull);
                        }
                        break;
                    }

                case BoundKind.ArrayAccess:
                    {
                        if (written && _performStaticNullChecks && this.State.Reachable)
                        {
                            var arrayAccess = (BoundArrayAccess)node;
                            TypeSymbolWithAnnotations elementType = (arrayAccess.Expression.Type as ArrayTypeSymbol)?.ElementType;

                            if ((object)elementType != null)
                            {
                                // Pass array type symbol as the target for the assignment. 
                                TrackNullableStateForAssignment(node, arrayAccess.Expression.Type, -1, value, valueIsNotNull);
                            }
                        }
                        break;
                    }

                case BoundKind.ObjectInitializerMember:
                    if (written && _performStaticNullChecks && this.State.Reachable)
                    {
                        var initializerMember = (BoundObjectInitializerMember)node;
                        Symbol memberSymbol = initializerMember.MemberSymbol;

                        if ((object)memberSymbol != null)
                        {
                            int slot = -1;

                            if ((object)_implicitReceiver != null && !memberSymbol.IsStatic)
                            {
                                // TODO: Do we need to handle events?
                                if (memberSymbol.Kind == SymbolKind.Field)
                                {
                                    slot = GetOrCreateSlot(memberSymbol, GetOrCreateSlot(_implicitReceiver));
                                    if (slot > 0)
                                    {
                                        SetSlotState(slot, written);
                                    }
                                }
                            }

                            TrackNullableStateForAssignment(node, memberSymbol, slot, value, valueIsNotNull);
                        }
                    }
                    break;

                case BoundKind.ThisReference:
                    {
                        var expression = (BoundThisReference)node;
                        int slot = MakeSlot(expression);
                        SetSlotState(slot, written);
                        if (written)
                        {
                            NoteWrite(expression, value, read);
                            ParameterSymbol thisParameter = MethodThisParameter;

                            if ((object)thisParameter != null)
                            {
                                TrackNullableStateForAssignment(node, thisParameter, slot, value, valueIsNotNull);
                            }
                        }
                        break;
                    }

                case BoundKind.RangeVariable:
                    // TODO: StaticNullChecking?
                    AssignImpl(((BoundRangeVariable)node).Value, value, valueIsNotNull, refKind, written, read);
                    break;

                case BoundKind.ForEachStatement:
                    {
                        var iterationVariable = ((BoundForEachStatement)node).IterationVariable;
                        Debug.Assert((object)iterationVariable != null);
                        int slot = GetOrCreateSlot(iterationVariable);
                        if (slot > 0) SetSlotState(slot, written);
                        if (written) NoteWrite(iterationVariable, value, read);
                        break;
                    }

                case BoundKind.LocalFunctionStatement:
                    {
                        int slot = GetOrCreateSlot(((BoundLocalFunctionStatement)node).Symbol);
                        SetSlotState(slot, written);
                        break;
                    }

                case BoundKind.BadExpression:
                    {
                        // Sometimes a bad node is not so bad that we cannot analyze it at all.
                        var bad = (BoundBadExpression)node;
                        if (!bad.ChildBoundNodes.IsDefault && bad.ChildBoundNodes.Length == 1)
                        {
                            AssignImpl(bad.ChildBoundNodes[0], value, valueIsNotNull, refKind, written, read);
                        }
                        break;
                    }

                default:
                    // Other kinds of left-hand-sides either represent things not tracked (e.g. array elements)
                    // or errors that have been reported earlier (e.g. assignment to a unary increment)
                    break;
            }
        }

        private void TrackNullableStateForAssignment(BoundNode node, Symbol assignmentTarget, int slot, BoundExpression value, bool? valueIsNotNull)
        {
            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                // Specially handle array types as assignment targets, the assignment is happening to an array element.
                TypeSymbolWithAnnotations targetType = assignmentTarget.Kind == SymbolKind.ArrayType ?
                                                           ((ArrayTypeSymbol)assignmentTarget).ElementType :
                                                           GetTypeOrReturnTypeWithAdjustedNullableAnnotations(assignmentTarget);

                if (targetType.IsReferenceType)
                {
                    bool isByRefTarget = IsByRefTarget(slot);

                    if (targetType.IsNullable == false)
                    {
                        if (valueIsNotNull == false)
                        {
                            ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceAssignment, (value ?? node).Syntax);
                        }
                    }
                    else if (slot > 0)
                    {
                        if (slot >= this.State.KnownNullState.Capacity) NormalizeNullable(ref this.State);

                        if (isByRefTarget)
                        {
                            // Since reference can point to the heap, we cannot assume the value is not null after this assignment,
                            // regardless of what value is being assigned. 
                            this.State.KnownNullState[slot] = (targetType.IsNullable == true);
                            this.State.NotNull[slot] = false;
                        }
                        else if (valueIsNotNull.HasValue)
                        {
                            this.State.KnownNullState[slot] = true;
                            this.State.NotNull[slot] = valueIsNotNull.GetValueOrDefault();
                        }
                        else
                        {
                            this.State.KnownNullState[slot] = false;
                            this.State.NotNull[slot] = false;
                        }
                    }

                    if (slot > 0 && targetType.TypeSymbol.IsAnonymousType && targetType.TypeSymbol.IsClassType() &&
                        (value == null || targetType.TypeSymbol == value.Type))
                    {
                        InheritNullableStateOfAnonymousTypeInstance(targetType.TypeSymbol, slot, GetValueSlotForAssignment(value), isByRefTarget);
                    }
                }
                else if (slot > 0 && EmptyStructTypeCache.IsTrackableStructType(targetType.TypeSymbol) &&
                        (value == null || targetType.TypeSymbol == value.Type))
                {
                    InheritNullableStateOfTrackableStruct(targetType.TypeSymbol, slot, GetValueSlotForAssignment(value), IsByRefTarget(slot));
                }

                if (value != null && (object)value.Type != null &&
                    targetType.TypeSymbol.Equals(value.Type, TypeSymbolEqualityOptions.SameType) &&
                    !targetType.TypeSymbol.Equals(value.Type, 
                                       TypeSymbolEqualityOptions.CompareNullableModifiersForReferenceTypes | TypeSymbolEqualityOptions.UnknownNullableModifierMatchesAny))
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInAssignment, value.Syntax, value.Type, targetType.TypeSymbol);
                }
            }
        }

        private bool IsByRefTarget(int slot)
        {
            if (slot > 0)
            {
                Symbol associatedNonMemberSymbol = GetNonMemberSymbol(slot);

                switch (associatedNonMemberSymbol.Kind)
                {
                    case SymbolKind.Local:
                        return ((LocalSymbol)associatedNonMemberSymbol).RefKind != RefKind.None;
                    case SymbolKind.Parameter:
                        var parameter = (ParameterSymbol)associatedNonMemberSymbol;
                        return !parameter.IsThis && parameter.RefKind != RefKind.None;
                }
            }

            return false;
        }

        private int GetValueSlotForAssignment(BoundExpression value)
        {
            if (value != null)
            {
                return MakeSlot(value);
            }

            return -1;
        }

        private void ReportStaticNullCheckingDiagnostics(ErrorCode errorCode, SyntaxNode syntaxNode, params object[] arguments)
        {
            Diagnostics.Add(errorCode, syntaxNode.GetLocation(), arguments);
        }

        private void InheritNullableStateOfTrackableStruct(TypeSymbol targetType, int targetSlot, int valueSlot, bool isByRefTarget)
        {
            Debug.Assert(targetSlot > 0);
            Debug.Assert(EmptyStructTypeCache.IsTrackableStructType(targetType));

            foreach (var field in _emptyStructTypeCache.GetStructInstanceFields(targetType))
            {
                InheritNullableStateOfFieldOrProperty(targetSlot, valueSlot, field, isByRefTarget);
            }
        }

        private void InheritNullableStateOfFieldOrProperty(int targetContainerSlot, int valueContainerSlot, Symbol fieldOrProperty, bool isByRefTarget)
        {
            TypeSymbolWithAnnotations fieldOrPropertyType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(fieldOrProperty);

            if (fieldOrPropertyType.IsReferenceType)
            {
                // If statically declared as not-nullable, no need to adjust the tracking info. 
                // Declaration information takes priority.
                if (fieldOrPropertyType.IsNullable != false)
                {
                    int targetMemberSlot = GetOrCreateSlot(fieldOrProperty, targetContainerSlot);
                    if (targetMemberSlot >= this.State.KnownNullState.Capacity) NormalizeNullable(ref this.State);

                    if (isByRefTarget)
                    {
                        // This is a property/field acesses through a by ref entity and it isn't considered declared as not-nullable. 
                        // Since reference can point to the heap, we cannot assume the property/field doesn't have null value after this assignment,
                        // regardless of what value is being assigned. 
                        this.State.KnownNullState[targetMemberSlot] = (fieldOrPropertyType.IsNullable == true);
                        this.State.NotNull[targetMemberSlot] = false;
                    }
                    else if (valueContainerSlot > 0)
                    {
                        int valueMemberSlot = VariableSlot(fieldOrProperty, valueContainerSlot);
                        this.State.KnownNullState[targetMemberSlot] = valueMemberSlot > 0 && valueMemberSlot < this.State.KnownNullState.Capacity && this.State.KnownNullState[valueMemberSlot];
                        this.State.NotNull[targetMemberSlot] = valueMemberSlot > 0 && valueMemberSlot < this.State.NotNull.Capacity && this.State.NotNull[valueMemberSlot];
                    }
                    else
                    {
                        // No tracking information for the value. We need to fill tracking state for the target
                        // with information inferred from the declaration. 
                        Debug.Assert(fieldOrPropertyType.IsNullable != false);

                        this.State.KnownNullState[targetMemberSlot] = (fieldOrPropertyType.IsNullable == true);
                        this.State.NotNull[targetMemberSlot] = false;
                    }
                }

                if (fieldOrPropertyType.TypeSymbol.IsAnonymousType && fieldOrPropertyType.TypeSymbol.IsClassType())
                {
                    InheritNullableStateOfAnonymousTypeInstance(fieldOrPropertyType.TypeSymbol, 
                                                                GetOrCreateSlot(fieldOrProperty, targetContainerSlot),
                                                                valueContainerSlot > 0 ? GetOrCreateSlot(fieldOrProperty, valueContainerSlot) : -1, isByRefTarget);
                }
            }
            else if (EmptyStructTypeCache.IsTrackableStructType(fieldOrPropertyType.TypeSymbol))
            {
                InheritNullableStateOfTrackableStruct(fieldOrPropertyType.TypeSymbol, 
                                                      GetOrCreateSlot(fieldOrProperty, targetContainerSlot),
                                                      valueContainerSlot > 0 ? GetOrCreateSlot(fieldOrProperty, valueContainerSlot) : -1, isByRefTarget);
            }
        }

        private void InheritNullableStateOfAnonymousTypeInstance(TypeSymbol targetType, int targetSlot, int valueSlot, bool isByRefTarget)
        {
            Debug.Assert(targetSlot > 0);
            Debug.Assert(targetType.IsAnonymousType && targetType.IsClassType());

            foreach (var member in targetType.GetMembersUnordered())
            {
                if (member.Kind != SymbolKind.Property)
                {
                    continue;
                }

                var propertySymbol = (PropertySymbol)member;

                if (!IsTrackableAnonymousTypeProperty(propertySymbol))
                {
                    continue;
                }

                InheritNullableStateOfFieldOrProperty(targetSlot, valueSlot, propertySymbol, isByRefTarget);
            }
        }

        /// <summary>
        /// Does the struct variable at the given slot have all of its instance fields assigned?
        /// </summary>
        private bool FieldsAllSet(int containingSlot, LocalState state)
        {
            Debug.Assert(containingSlot != -1);
            Debug.Assert(!state.IsAssigned(containingSlot));
            VariableIdentifier variable = variableBySlot[containingSlot];
            NamedTypeSymbol structType = VariableType(variable.Symbol) as NamedTypeSymbol;
            foreach (var field in _emptyStructTypeCache.GetStructInstanceFields(structType))
            {
                if (_emptyStructTypeCache.IsEmptyStructType(field.Type.TypeSymbol)) continue;
                int slot = VariableSlot(field, containingSlot);
                if (slot == -1 || !state.IsAssigned(slot)) return false;
            }

            return true;
        }

        private static TypeSymbol VariableType(Symbol s)
        {
            switch (s.Kind)
            {
                case SymbolKind.Local:
                    return ((LocalSymbol)s).Type.TypeSymbol;
                case SymbolKind.Field:
                    return ((FieldSymbol)s).Type.TypeSymbol;
                case SymbolKind.Parameter:
                    return ((ParameterSymbol)s).Type.TypeSymbol;
                case SymbolKind.Method:
                    Debug.Assert(((MethodSymbol)s).MethodKind == MethodKind.LocalFunction);
                    return null;
                case SymbolKind.Property:
                    Debug.Assert(s.ContainingType.IsAnonymousType);
                    return ((PropertySymbol)s).Type.TypeSymbol;
                default:
                    throw ExceptionUtilities.UnexpectedValue(s.Kind);
            }
        }

        protected void SetSlotState(int slot, bool assigned)
        {
            if (slot <= 0) return;
            if (assigned)
            {
                SetSlotAssigned(slot);
            }
            else
            {
                SetSlotUnassigned(slot);
            }
        }

        private void SetSlotAssigned(int slot, ref LocalState state)
        {
            if (slot < 0) return;
            VariableIdentifier id = variableBySlot[slot];
            TypeSymbol type = VariableType(id.Symbol);
            Debug.Assert(!_emptyStructTypeCache.IsEmptyStructType(type));
            if (slot >= state.Assigned.Capacity) NormalizeAssigned(ref state);
            if (state.IsAssigned(slot)) return; // was already fully assigned.
            state.Assign(slot);
            bool fieldsTracked = EmptyStructTypeCache.IsTrackableStructType(type);

            // if a struct, child fields are assigned
            if (fieldsTracked)
            {
                foreach (var field in _emptyStructTypeCache.GetStructInstanceFields(type))
                {
                    int s2 = VariableSlot(field, slot);
                    if (s2 > 0) SetSlotAssigned(s2, ref state);
                }
            }

            // if a struct member, and now all fields of enclosing are assigned, then enclosing is assigned
            while (id.ContainingSlot > 0)
            {
                slot = id.ContainingSlot;
                if (state.IsAssigned(slot) || !FieldsAllSet(slot, state)) break;
                state.Assign(slot);
                id = variableBySlot[slot];
            }
        }

        private void SetSlotAssigned(int slot)
        {
            SetSlotAssigned(slot, ref this.State);
        }

        private void SetSlotUnassigned(int slot, ref LocalState state)
        {
            if (slot < 0) return;
            VariableIdentifier id = variableBySlot[slot];
            TypeSymbol type = VariableType(id.Symbol);
            Debug.Assert(!_emptyStructTypeCache.IsEmptyStructType(type));
            if (!state.IsAssigned(slot)) return; // was already unassigned
            state.Unassign(slot);
            bool fieldsTracked = EmptyStructTypeCache.IsTrackableStructType(type);

            // if a struct, child fields are unassigned
            if (fieldsTracked)
            {
                foreach (var field in _emptyStructTypeCache.GetStructInstanceFields(type))
                {
                    int s2 = VariableSlot(field, slot);
                    if (s2 > 0) SetSlotUnassigned(s2, ref state);
                }
            }

            // if a struct member, then the parent is unassigned
            while (id.ContainingSlot > 0)
            {
                slot = id.ContainingSlot;
                state.Unassign(slot);
                id = variableBySlot[slot];
            }
        }

        private void SetSlotUnassigned(int slot)
        {
            if (_tryState != null)
            {
                var state = _tryState.Value;
                SetSlotUnassigned(slot, ref state);
                _tryState = state;
            }

            SetSlotUnassigned(slot, ref this.State);
        }

        protected override LocalState ReachableState()
        {
            return new LocalState(BitVector.Empty, BitVector.Empty, BitVector.Empty);
        }

        protected override LocalState AllBitsSet()
        {
            LocalState result;

            if (_performStaticNullChecks)
            {
                result = new LocalState(BitVector.AllSet(nextVariableSlot), BitVector.Create(nextVariableSlot), BitVector.Create(nextVariableSlot));
            }
            else
            {
                result = new LocalState(BitVector.AllSet(nextVariableSlot), BitVector.Empty, BitVector.Empty);
            }

            result.Assigned[0] = false;
            return result;
        }

        private void EnterParameters(ImmutableArray<ParameterSymbol> parameters)
        {
            // label out parameters as not assigned.
            foreach (var parameter in parameters)
            {
                EnterParameter(parameter);
            }
        }

        protected virtual void EnterParameter(ParameterSymbol parameter)
        {
            if (parameter.RefKind == RefKind.Out && !this.currentMethodOrLambda.IsAsync) // out parameters not allowed in async
            {
                int slot = GetOrCreateSlot(parameter);
                if (slot > 0) SetSlotState(slot, initiallyAssignedVariables != null && initiallyAssignedVariables.Contains(parameter));
            }
            else
            {
                // this code has no effect except in region analysis APIs such as DataFlowsOut where we unassign things
                int slot = GetOrCreateSlot(parameter);
                if (slot > 0) SetSlotState(slot, true);
                NoteWrite(parameter, value: null, read: true);

                Debug.Assert(!IsConditionalState);
                if (_performStaticNullChecks && slot > 0 && parameter.RefKind != RefKind.Out)
                {
                    TypeSymbolWithAnnotations paramType = parameter.Type;

                    if (paramType.IsReferenceType)
                    {
                        if (paramType.IsNullable != false)
                        {
                            if (slot >= this.State.KnownNullState.Capacity) NormalizeNullable(ref this.State);

                            this.State.KnownNullState[slot] = (paramType.IsNullable == true);
                            this.State.NotNull[slot] = false;
                        }

                        if (paramType.TypeSymbol.IsAnonymousType && paramType.TypeSymbol.IsClassType())
                        {
                            InheritNullableStateOfAnonymousTypeInstance(paramType.TypeSymbol, slot, -1, parameter.RefKind != RefKind.None);
                        }
                    }
                    else if (EmptyStructTypeCache.IsTrackableStructType(paramType.TypeSymbol))
                    {
                        InheritNullableStateOfTrackableStruct(paramType.TypeSymbol, slot, -1, parameter.RefKind != RefKind.None);
                    }
                }
            }
        }

        private void LeaveParameters(ImmutableArray<ParameterSymbol> parameters, CSharpSyntaxNode syntax, Location location)
        {
            if (!this.State.Reachable)
            {
                // if the code is not reachable, then it doesn't matter if out parameters are assigned.
                return;
            }

            foreach (ParameterSymbol parameter in parameters)
            {
                LeaveParameter(parameter, syntax, location);
            }
        }

        private void LeaveParameter(ParameterSymbol parameter, CSharpSyntaxNode syntax, Location location)
        {
            if (parameter.RefKind != RefKind.None)
            {
                var slot = VariableSlot(parameter);
                if (slot > 0 && !this.State.IsAssigned(slot))
                {
                    ReportUnassignedOutParameter(parameter, syntax, location);
                }

                NoteRead(parameter);
            }
        }

        protected override LocalState UnreachableState()
        {
            LocalState result = this.State.Clone();
            result.Assigned.EnsureCapacity(1);
            result.Assign(0);
            return result;
        }

        #region Visitors

        public override BoundNode VisitBlock(BoundBlock node)
        {
            DeclareVariables(node.Locals);
            var result = base.VisitBlock(node);
            ReportUnusedVariables(node.Locals);
            ReportUnusedVariables(node.LocalFunctions);
            return result;
        }

        public override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            var result = VisitRvalue(node.ExpressionOpt);

            Debug.Assert(!IsConditionalState);
            if (node.ExpressionOpt != null && _performStaticNullChecks && this.State.Reachable)
            {
                TypeSymbolWithAnnotations returnType = this.currentMethodOrLambda?.ReturnType;

                if (this.State.ResultIsNotNull == false)
                {
                    if ((object)returnType != null && returnType.IsReferenceType && returnType.IsNullable == false)
                    {
                        ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceReturn, node.ExpressionOpt.Syntax);
                    }
                }

                if ((object)node.ExpressionOpt.Type != null &&
                    returnType.TypeSymbol.Equals(node.ExpressionOpt.Type, TypeSymbolEqualityOptions.SameType) &&
                    !returnType.TypeSymbol.Equals(node.ExpressionOpt.Type,
                                       TypeSymbolEqualityOptions.CompareNullableModifiersForReferenceTypes | TypeSymbolEqualityOptions.UnknownNullableModifierMatchesAny))
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInAssignment, node.ExpressionOpt.Syntax, node.ExpressionOpt.Type, returnType.TypeSymbol);
                }
            }

            AdjustStateAfterReturnStatement(node);
            return result;
        }

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            DeclareVariables(node.InnerLocals);
            var result = base.VisitSwitchStatement(node);
            ReportUnusedVariables(node.InnerLocals);
            ReportUnusedVariables(node.InnerLocalFunctions);
            return result;
        }

        public override BoundNode VisitForStatement(BoundForStatement node)
        {
            DeclareVariables(node.OuterLocals);
            var result = base.VisitForStatement(node);
            ReportUnusedVariables(node.OuterLocals);
            return result;
        }

        public override BoundNode VisitDoStatement(BoundDoStatement node)
        {
            var result = base.VisitDoStatement(node);
            return result;
        }

        public override BoundNode VisitWhileStatement(BoundWhileStatement node)
        {
            var result = base.VisitWhileStatement(node);
            return result;
        }

        public override BoundNode VisitForEachStatement(BoundForEachStatement node)
        {
            var result = base.VisitForEachStatement(node);
            return result;
        }

        public override BoundNode VisitIfStatement(BoundIfStatement node)
        {
            var result = base.VisitIfStatement(node);
            return result;
        }

        public override BoundNode VisitLockStatement(BoundLockStatement node)
        {
            var result = base.VisitLockStatement(node);
            return result;
        }

        /// <remarks>
        /// Variables declared in a using statement are always considered used, so this is just an assert.
        /// </remarks>
        public override BoundNode VisitUsingStatement(BoundUsingStatement node)
        {
            ImmutableArray<LocalSymbol> localsOpt = node.Locals;

            if (localsOpt.IsDefaultOrEmpty)
            {
                return base.VisitUsingStatement(node);
            }

            foreach (LocalSymbol local in localsOpt)
            {
                if (local.DeclarationKind == LocalDeclarationKind.RegularVariable)
                {
                    DeclareVariable(local);
                }
                else
                {
                    Debug.Assert(local.DeclarationKind == LocalDeclarationKind.UsingVariable);
                    int slot = GetOrCreateSlot(local);
                    if (slot >= 0)
                    {
                        SetSlotAssigned(slot);
                        NoteWrite(local, value: null, read: true);
                    }
                    else
                    {
                        Debug.Assert(_emptyStructTypeCache.IsEmptyStructType(local.Type.TypeSymbol));
                    }
                }
            }

            var result = base.VisitUsingStatement(node);

            foreach (LocalSymbol local in localsOpt)
            {
                if (local.DeclarationKind == LocalDeclarationKind.RegularVariable)
                {
                    ReportIfUnused(local, assigned: true);
                }
                else
                {
                    NoteRead(local); // At the end of the statement, there's an implied read when the local is disposed
                }
            }
            Debug.Assert(localsOpt.All(_usedVariables.Contains));

            return result;
        }

        public override BoundNode VisitFixedStatement(BoundFixedStatement node)
        {
            foreach (LocalSymbol local in node.Locals)
            {
                if (local.DeclarationKind == LocalDeclarationKind.RegularVariable)
                {
                    DeclareVariable(local);
                }
                else
                {
                    Debug.Assert(local.DeclarationKind == LocalDeclarationKind.FixedVariable);
                    // TODO: should something be done about this local?
                }
            }

            var result = base.VisitFixedStatement(node);

            foreach (LocalSymbol local in node.Locals)
            {
                if (local.DeclarationKind == LocalDeclarationKind.RegularVariable)
                {
                    ReportIfUnused(local, assigned: true);
                }
            }

            return result;
        }

        public override BoundNode VisitSequence(BoundSequence node)
        {
            DeclareVariables(node.Locals);
            var result = base.VisitSequence(node);
            ReportUnusedVariables(node.Locals);
            
            if (node.Value == null)
            {
                SetUnknownResultNullability();
            }

            return result;
        }

        private void DeclareVariables(ImmutableArray<LocalSymbol> locals)
        {
            foreach (var symbol in locals)
            {
                DeclareVariable(symbol);
            }
        }

        private void DeclareVariable(LocalSymbol symbol)
        {
            var initiallyAssigned =
                symbol.IsConst ||
                // When data flow analysis determines that the variable is sometimes used without being assigned
                // first, we want to treat that variable, during region analysis, as assigned where it is introduced.
                initiallyAssignedVariables != null && initiallyAssignedVariables.Contains(symbol);
            SetSlotState(GetOrCreateSlot(symbol), initiallyAssigned);
        }

        private void ReportUnusedVariables(ImmutableArray<LocalSymbol> locals)
        {
            foreach (var symbol in locals)
            {
                ReportIfUnused(symbol, assigned: true);
            }
        }

        private void ReportIfUnused(LocalSymbol symbol, bool assigned)
        {
            if (!_usedVariables.Contains(symbol))
            {
                if (!string.IsNullOrEmpty(symbol.Name)) // avoid diagnostics for parser-inserted names
                {
                    Diagnostics.Add(assigned && _writtenVariables.Contains(symbol) ? ErrorCode.WRN_UnreferencedVarAssg : ErrorCode.WRN_UnreferencedVar, symbol.Locations[0], symbol.Name);
                }
            }
        }

        private void ReportUnusedVariables(ImmutableArray<LocalFunctionSymbol> locals)
        {
            foreach (var symbol in locals)
            {
                ReportIfUnused(symbol);
            }
        }

        private void ReportIfUnused(LocalFunctionSymbol symbol)
        {
            if (!_usedLocalFunctions.Contains(symbol))
            {
                if (!string.IsNullOrEmpty(symbol.Name)) // avoid diagnostics for parser-inserted names
                {
                    Diagnostics.Add(ErrorCode.WRN_UnreferencedVar, symbol.Locations[0], symbol.Name);
                }
            }
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            // Note: the caller should avoid allowing this to be called for the left-hand-side of
            // an assignment (if a simple variable or this-qualified) or an out parameter.  That's
            // because this code assumes the variable is being read, not written.
            LocalSymbol localSymbol = node.LocalSymbol;
            CheckAssigned(localSymbol, node.Syntax);
            if (localSymbol.IsFixed &&
                (this.currentMethodOrLambda.MethodKind == MethodKind.AnonymousFunction || this.currentMethodOrLambda.MethodKind == MethodKind.LocalFunction) &&
                _capturedVariables.Contains(localSymbol))
            {
                Diagnostics.Add(ErrorCode.ERR_FixedLocalInLambda, new SourceLocation(node.Syntax), localSymbol);
            }

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                this.State.ResultIsNotNull = IsResultNotNull(node, localSymbol);
            }

            return null;
        }

        public override BoundNode VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            int slot = GetOrCreateSlot(node.LocalSymbol); // not initially assigned
            if (initiallyAssignedVariables != null && initiallyAssignedVariables.Contains(node.LocalSymbol))
            {
                // When data flow analysis determines that the variable is sometimes
                // used without being assigned first, we want to treat that variable, during region analysis,
                // as assigned at its point of declaration.
                Assign(node, value: null, valueIsNotNull: null);
            }

            if (node.InitializerOpt != null)
            {
                VisitRvalue(node.InitializerOpt); // analyze the expression
                Assign(node, node.InitializerOpt, this.State.ResultIsNotNull);
            }

            return null;
        }

        protected override BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            Debug.Assert(!IsConditionalState);
            this.State.ResultIsNotNull = null;
            var result = base.VisitExpressionWithoutStackGuard(node);

            Debug.Assert(!IsConditionalState || node.ConstantValue == null || node.Type?.IsReferenceType != true);
            if (_performStaticNullChecks && !IsConditionalState && this.State.Reachable)
            {
                var constant = node.ConstantValue;

                if (constant != null && node.Type?.IsReferenceType == true)
                {
                    this.State.ResultIsNotNull = !constant.IsNull;
                } 
            }

            return result;
        }

        public override BoundNode VisitObjectCreationExpression(BoundObjectCreationExpression node)
        {
            LocalSymbol saveImplicitReceiver = _implicitReceiver;
            _implicitReceiver = null;

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable &&
                EmptyStructTypeCache.IsTrackableStructType(node.Type))
            {
                _implicitReceiver = GetOrCreateObjectCreationPlaceholder(node);
                InheritNullableStateOfTrackableStruct(node.Type, MakeSlot(node), -1, false);
            }

            var result = base.VisitObjectCreationExpression(node);

            SetResultIsNotNull(node);

            _implicitReceiver = saveImplicitReceiver;
            return result;
        }

        private void SetResultIsNotNull(BoundExpression node)
        {
            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                if (node.Type?.IsReferenceType == true)
                {
                    this.State.ResultIsNotNull = true;
                }
                else
                {
                    this.State.ResultIsNotNull = null;
                }
            }
        }

        private ObjectCreationPlaceholderLocal GetOrCreateObjectCreationPlaceholder(BoundExpression node)
        {
            ObjectCreationPlaceholderLocal placeholder;
            if (_placeholderLocals == null)
            {
                _placeholderLocals = PooledDictionary<BoundExpression, ObjectCreationPlaceholderLocal>.GetInstance();
                placeholder = null;
            }
            else
            {
                _placeholderLocals.TryGetValue(node, out placeholder);
            }

            if ((object)placeholder == null)
            {
                placeholder = new ObjectCreationPlaceholderLocal(_member, node);
                _placeholderLocals.Add(node, placeholder);
            }

            return placeholder;
        }

        public override BoundNode VisitAnonymousObjectCreationExpression(BoundAnonymousObjectCreationExpression node)
        {
            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                ObjectCreationPlaceholderLocal implicitReceiver = GetOrCreateObjectCreationPlaceholder(node);
                int receiverSlot = -1;

                //  visit arguments as r-values
                var arguments = node.Arguments;
                var constructor = node.Constructor;
                for (int i = 0; i < arguments.Length; i++)
                {
                    VisitArgumentAsRvalue(arguments[i], constructor.Parameters[i], expanded: false);

                    PropertySymbol property = node.Declarations[i].Property;

                    if (IsTrackableAnonymousTypeProperty(property))
                    {
                        if (receiverSlot <= 0)
                        {
                            receiverSlot = GetOrCreateSlot(implicitReceiver);
                        }

                        TrackNullableStateForAssignment(arguments[i], property, GetOrCreateSlot(property, receiverSlot), arguments[i], this.State.ResultIsNotNull);
                    }
                }

                if (_trackExceptions) NotePossibleException(node);

                this.State.ResultIsNotNull = true;
                return null;
            }
            else
            {
                return base.VisitAnonymousObjectCreationExpression(node);
            }
        }

        public override BoundNode VisitArrayCreation(BoundArrayCreation node)
        {
            var result = base.VisitArrayCreation(node);
            SetResultIsNotNull(node);
            return result;
        }

        protected override BoundNode VisitArrayElementInitializer(BoundArrayCreation arrayCreation, BoundExpression elementInitializer)
        {
            VisitRvalue(elementInitializer);

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                TypeSymbolWithAnnotations elementType = (arrayCreation.Type as ArrayTypeSymbol)?.ElementType;

                if (elementType?.IsReferenceType == true)
                {
                    // Pass array type symbol as the target for the assignment. 
                    TrackNullableStateForAssignment(elementInitializer, arrayCreation.Type, -1, elementInitializer, this.State.ResultIsNotNull);
                }
            }

            return null;
        }

        public override BoundNode VisitArrayAccess(BoundArrayAccess node)
        {
            var result = base.VisitArrayAccess(node);

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                bool? resultIsNotNull = null;
                TypeSymbolWithAnnotations elementType = (node.Expression.Type as ArrayTypeSymbol)?.ElementType;

                if (elementType?.IsReferenceType == true)
                {
                    resultIsNotNull = !elementType.IsNullable;
                }

                this.State.ResultIsNotNull = resultIsNotNull;
            }

            return result;
        }

        protected override void VisitArrayAccessTargetAsRvalue(BoundArrayAccess node)
        {
            base.VisitArrayAccessTargetAsRvalue(node);

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable && this.State.ResultIsNotNull == false)
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceReceiver, node.Expression.Syntax);
            }
        }

        private bool? InferResultNullability(BoundBinaryOperator node, bool? leftIsNotNull, bool? rightIsNotNull)
        {
            return InferResultNullability(node.OperatorKind, node.MethodOpt, node.Type, leftIsNotNull, rightIsNotNull);
        }

        private bool? InferResultNullability(BinaryOperatorKind operatorKind, MethodSymbol methodOpt, TypeSymbol resultType, bool? leftIsNotNull, bool? rightIsNotNull)
        {
            if (operatorKind.IsUserDefined())
            {
                if ((object)methodOpt != null && methodOpt.ParameterCount == 2)
                {
                    return IsResultNotNull(methodOpt);
                }
                else
                {
                    return null;
                }
            }
            else if (operatorKind.IsDynamic())
            {
                return null;
            }
            else if (resultType.IsReferenceType == true)
            {
                switch (operatorKind.Operator() | operatorKind.OperandTypes())
                {
                    case BinaryOperatorKind.DelegateCombination:
                        if (leftIsNotNull == true || rightIsNotNull == true)
                        {
                            return true;
                        }
                        else if (leftIsNotNull == false && rightIsNotNull == false)
                        {
                            return false;
                        }
                        else
                        {
                            Debug.Assert(leftIsNotNull == null || rightIsNotNull == null);
                            return null;
                        }

                    case BinaryOperatorKind.DelegateRemoval:
                        return false; // Delegate removal can produce null.
                }

                return true;
            }
            else
            {
                return null;
            }
        }

        protected override void AfterLeftChildHasBeenVisited(BoundBinaryOperator binary)
        {
            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                bool? leftIsNotNull = this.State.ResultIsNotNull;
                bool warnOnNullReferenceArgument = (binary.OperatorKind.IsUserDefined() && (object)binary.MethodOpt != null && binary.MethodOpt.ParameterCount == 2);

                if (warnOnNullReferenceArgument)
                {
                    WarnOnNullReferenceArgument(binary.Left, leftIsNotNull, binary.MethodOpt.Parameters[0], expanded: false);
                }

                VisitRvalue(binary.Right);
                Debug.Assert(!IsConditionalState);
                Debug.Assert(this.State.Reachable);
                bool? rightIsNotNull = this.State.ResultIsNotNull;

                if (warnOnNullReferenceArgument)
                {
                    WarnOnNullReferenceArgument(binary.Right, rightIsNotNull, binary.MethodOpt.Parameters[1], expanded: false);
                }

                AfterRightChildHasBeenVisited(binary);

                Debug.Assert(!IsConditionalState);
                Debug.Assert(this.State.Reachable);
                this.State.ResultIsNotNull = InferResultNullability(binary, leftIsNotNull, rightIsNotNull);

                BinaryOperatorKind op = binary.OperatorKind.Operator();
                if (op == BinaryOperatorKind.Equal || op == BinaryOperatorKind.NotEqual)
                {
                    BoundExpression operandComparedToNull = null;
                    bool? operandComparedToNullIsNotNull = null;

                    if (binary.Right.ConstantValue?.IsNull == true)
                    {
                        operandComparedToNull = binary.Left;
                        operandComparedToNullIsNotNull = leftIsNotNull;
                    }
                    else if (binary.Left.ConstantValue?.IsNull == true)
                    {
                        operandComparedToNull = binary.Right;
                        operandComparedToNullIsNotNull = rightIsNotNull;
                    }

                    if (operandComparedToNull != null)
                    {
                        if (operandComparedToNullIsNotNull == true)
                        {
                            ReportStaticNullCheckingDiagnostics(op == BinaryOperatorKind.Equal ?
                                                                    ErrorCode.HDN_NullCheckIsProbablyAlwaysFalse :
                                                                    ErrorCode.HDN_NullCheckIsProbablyAlwaysTrue,
                                                                binary.Syntax);
                        }

                        // Skip reference conversions
                        operandComparedToNull = SkipReferenceConversions(operandComparedToNull);

                        if (operandComparedToNull.Type?.IsReferenceType == true)
                        {
                            int slot = MakeSlot(operandComparedToNull);

                            if (slot > 0)
                            {
                                if (slot >= this.State.KnownNullState.Capacity) NormalizeNullable(ref this.State);

                                Split();

                                if (op == BinaryOperatorKind.Equal)
                                {
                                    this.StateWhenFalse.KnownNullState[slot] = true;
                                    this.StateWhenFalse.NotNull[slot] = true;
                                }
                                else
                                {
                                    this.StateWhenTrue.KnownNullState[slot] = true;
                                    this.StateWhenTrue.NotNull[slot] = true;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                base.AfterLeftChildHasBeenVisited(binary);
                Debug.Assert(!IsConditionalState);
            }
        }

        private static BoundExpression SkipReferenceConversions(BoundExpression possiblyConversion)
        {
            while (possiblyConversion.Kind == BoundKind.Conversion)
            {
                var conversion = (BoundConversion)possiblyConversion;
                switch (conversion.ConversionKind)
                {
                    case ConversionKind.ImplicitReference:
                    case ConversionKind.ExplicitReference:
                        possiblyConversion = conversion.Operand;
                        break;

                    default:
                        return possiblyConversion;
                }
            }

            return possiblyConversion;
        }

        public override BoundNode VisitNullCoalescingOperator(BoundNullCoalescingOperator node)
        {
            Debug.Assert(!IsConditionalState);

            if (!(_performStaticNullChecks && this.State.Reachable) || node.LeftOperand.ConstantValue != null || node.LeftOperand.Type?.IsReferenceType != true)
            {
                return base.VisitNullCoalescingOperator(node);
            }

            VisitRvalue(node.LeftOperand);
            var savedState = this.State.Clone();

            BoundExpression operandComparedToNull = node.LeftOperand;

            if (savedState.ResultIsNotNull == true)
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.HDN_ExpressionIsProbablyNeverNull, node.LeftOperand.Syntax);
            }

            operandComparedToNull = SkipReferenceConversions(operandComparedToNull);
            int slot = MakeSlot(operandComparedToNull);

            VisitRvalue(node.RightOperand);
            bool? rightOperandIsNotNull = this.State.ResultIsNotNull;
            IntersectWith(ref this.State, ref savedState);
            Debug.Assert(!IsConditionalState);
            this.State.ResultIsNotNull = rightOperandIsNotNull | savedState.ResultIsNotNull;
            return null;
        }

        public override BoundNode VisitConditionalAccess(BoundConditionalAccess node)
        {
            Debug.Assert(!IsConditionalState);

            if (!(_performStaticNullChecks && this.State.Reachable) || node.Receiver.ConstantValue != null || node.Receiver.Type?.IsReferenceType != true)
            {
                return base.VisitConditionalAccess(node);
            }

            VisitRvalue(node.Receiver);
            var savedState = this.State.Clone();

            BoundExpression operandComparedToNull = node.Receiver;

            if (savedState.ResultIsNotNull == true)
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.HDN_ExpressionIsProbablyNeverNull, node.Receiver.Syntax);
            }

            operandComparedToNull = SkipReferenceConversions(operandComparedToNull);
            int slot = MakeSlot(operandComparedToNull);

            if (slot > 0)
            {
                if (slot >= this.State.KnownNullState.Capacity) NormalizeNullable(ref this.State);

                this.State.KnownNullState[slot] = true;
                this.State.NotNull[slot] = true;
            }

            VisitRvalue(node.AccessExpression);
            IntersectWith(ref this.State, ref savedState);
            return null;
        }

        public override BoundNode VisitLoweredConditionalAccess(BoundLoweredConditionalAccess node)
        {
            Debug.Assert(!_performStaticNullChecks);
            return base.VisitLoweredConditionalAccess(node);
        }

        public override BoundNode VisitConditionalReceiver(BoundConditionalReceiver node)
        {
            var result = base.VisitConditionalReceiver(node);
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitComplexConditionalReceiver(BoundComplexConditionalReceiver node)
        {
            Debug.Assert(!_performStaticNullChecks);
            return base.VisitComplexConditionalReceiver(node);
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            if (node.Method.MethodKind == MethodKind.LocalFunction)
            {
                CheckAssigned(node.Method.OriginalDefinition, node.Syntax);
            }

            var result = base.VisitCall(node);
            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                this.State.ResultIsNotNull = IsResultNotNull(node.Method);
            }

            return result;
        }

        private bool? IsResultNotNull(Symbol resultSymbol)
        {
            TypeSymbolWithAnnotations resultType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(resultSymbol);

            if (!resultType.IsVoid && resultType.IsReferenceType)
            {
                return !resultType.IsNullable;
            }
            else
            {
                return null;
            }
        }

        private bool? IsResultNotNull(BoundExpression node, Symbol resultSymbol)
        {
            TypeSymbolWithAnnotations resultType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(resultSymbol);

            if ((object)resultType != null && !resultType.IsVoid && resultType.IsReferenceType)
            {
                if (resultType.IsNullable == false)
                {
                    // Statically declared as not-nullable. This takes priority.
                    return true;
                }

                int slot = MakeSlot(node);

                if (slot > 0)
                {
                    // We are supposed to track information for the node. Use whatever we managed to
                    // accumulate so far.
                    if (slot < this.State.KnownNullState.Capacity && this.State.KnownNullState[slot])
                    {
                        return slot < this.State.NotNull.Capacity && this.State.NotNull[slot];
                    }
                    else
                    {
                        return null;
                    }
                }

                // The node is not trackable, use information from the declaration.
                Debug.Assert(resultType.IsNullable != false);
                return !resultType.IsNullable;
            }
            else
            {
                return null;
            }
        }

        protected override void VisitReceiverBeforeCall(BoundExpression receiverOpt, MethodSymbol method)
        {
            base.VisitReceiverBeforeCall(receiverOpt, method);

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable && 
                receiverOpt != null && (object)receiverOpt.Type != null && receiverOpt.Type.IsReferenceType && this.State.ResultIsNotNull == false &&
                (object)method != null && !method.IsStatic && method.MethodKind != MethodKind.Constructor)
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceReceiver, receiverOpt.Syntax);
            }
        }

        protected override void VisitFieldReceiverAsRvalue(BoundExpression receiverOpt, FieldSymbol fieldSymbol)
        {
            base.VisitFieldReceiverAsRvalue(receiverOpt, fieldSymbol);

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable &&
                receiverOpt != null && (object)receiverOpt.Type != null && receiverOpt.Type.IsReferenceType && this.State.ResultIsNotNull == false &&
                (object)fieldSymbol != null && !fieldSymbol.IsStatic)
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceReceiver, receiverOpt.Syntax);
            }
        }

        public override BoundNode VisitConversion(BoundConversion node)
        {
            if (node.ConversionKind == ConversionKind.MethodGroup && node.SymbolOpt?.MethodKind == MethodKind.LocalFunction)
            {
                CheckAssigned(node.SymbolOpt.OriginalDefinition, node.Syntax);
            }

            var result = base.VisitConversion(node);

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                switch (node.ConversionKind)
                {
                    case ConversionKind.ExplicitUserDefined:
                    case ConversionKind.ImplicitUserDefined:
                        if ((object)node.SymbolOpt != null && node.SymbolOpt.ParameterCount == 1)
                        {
                            WarnOnNullReferenceArgument(node.Operand, this.State.ResultIsNotNull, node.SymbolOpt.Parameters[0], expanded: false);
                        }
                        break;

                    case ConversionKind.AnonymousFunction:
                        if (!node.ExplicitCastInCode && node.Operand.Kind == BoundKind.Lambda)
                        {
                            var lambda = (BoundLambda)node.Operand;
                            ReportNullabilityMismatchWithTargetDelegate(node.Operand.Syntax, node.Type.GetDelegateType(), lambda.Symbol);
                        }
                        break;

                    case ConversionKind.MethodGroup:
                        if (!node.ExplicitCastInCode)
                        {
                            ReportNullabilityMismatchWithTargetDelegate(node.Operand.Syntax, node.Type.GetDelegateType(), node.SymbolOpt);
                        }
                        break;
                }

                this.State.ResultIsNotNull = InferResultNullability(node.ConversionKind, node.Operand.Type, node.Type, node.SymbolOpt, this.State.ResultIsNotNull);
            }

            return result;
        }

        private void ReportNullabilityMismatchWithTargetDelegate(CSharpSyntaxNode syntax, NamedTypeSymbol delegateType, MethodSymbol method)
        {
            if ((object)delegateType == null || (object)method == null)
            {
                return;
            }

            MethodSymbol invoke = delegateType.DelegateInvokeMethod;

            if ((object)invoke == null)
            {
                return;
            }

            if (!invoke.ReturnType.Equals(method.ReturnType, TypeSymbolEqualityOptions.CompareNullableModifiersForReferenceTypes | TypeSymbolEqualityOptions.UnknownNullableModifierMatchesAny) &&
                invoke.ReturnType.Equals(method.ReturnType, TypeSymbolEqualityOptions.SameType))
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, syntax,
                    new FormattedSymbol(method, SymbolDisplayFormat.MinimallyQualifiedFormat), 
                    delegateType);
            }

            int count = Math.Min(invoke.ParameterCount, method.ParameterCount);

            for (int i = 0; i < count; i++)
            {
                if (!invoke.Parameters[i].Type.Equals(method.Parameters[i].Type, TypeSymbolEqualityOptions.CompareNullableModifiersForReferenceTypes | TypeSymbolEqualityOptions.UnknownNullableModifierMatchesAny) &&
                    invoke.Parameters[i].Type.Equals(method.Parameters[i].Type, TypeSymbolEqualityOptions.SameType))
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, syntax,
                        new FormattedSymbol(method.Parameters[i], SymbolDisplayFormat.ShortFormat),
                        new FormattedSymbol(method, SymbolDisplayFormat.MinimallyQualifiedFormat), 
                        delegateType);
                }
            }
        }

        private bool? InferResultNullability(ConversionKind conversionKind, TypeSymbol sourceTypeOpt, TypeSymbol targetType, MethodSymbol methodOpt, bool? operandIsNotNull)
        {
            if (targetType.IsReferenceType)
            {
                switch (conversionKind)
                {
                    case ConversionKind.MethodGroup:
                    case ConversionKind.AnonymousFunction:
                    case ConversionKind.InterpolatedString:
                        return true;

                    case ConversionKind.ExplicitUserDefined:
                    case ConversionKind.ImplicitUserDefined:
                        if ((object)methodOpt != null && methodOpt.ParameterCount == 1)
                        {
                            return IsResultNotNull(methodOpt);
                        }
                        else
                        {
                            return null;
                        }

                    case ConversionKind.ExplicitDynamic:
                    case ConversionKind.ImplicitDynamic:
                    case ConversionKind.NoConversion:
                        return null;

                    case ConversionKind.NullLiteral:
                        return false;

                    case ConversionKind.Boxing:
                        if (sourceTypeOpt?.IsValueType == true)
                        {
                            if (sourceTypeOpt.IsNullableType())
                            {
                                // TODO: Should we worry about a pathological case of boxing nullable value known to be not null?
                                //       For example, new int?(0)
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else
                        {
                            Debug.Assert(sourceTypeOpt?.IsReferenceType != true);
                            return null;
                        }

                    case ConversionKind.Identity:
                    case ConversionKind.ImplicitReference:
                    case ConversionKind.ExplicitReference:
                        // Inherit state from the operand
                        return operandIsNotNull;

                    default:
                        Debug.Assert(false);
                        return null;
                }
            }
            else
            {
                return null;
            }
        }

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            if (node.MethodOpt?.MethodKind == MethodKind.LocalFunction)
            {
                CheckAssigned(node.MethodOpt.OriginalDefinition, node.Syntax);
            }

            SetResultIsNotNull(node);

            return base.VisitDelegateCreationExpression(node);
        }

        public override BoundNode VisitMethodGroup(BoundMethodGroup node)
        {
            foreach (var method in node.Methods)
            {
                if (method.MethodKind == MethodKind.LocalFunction)
                {
                    CheckAssigned(method, node.Syntax);
                }
            }

            Debug.Assert(!IsConditionalState);

            BoundExpression receiverOpt = node.ReceiverOpt;
            if (receiverOpt != null)
            {
                // An explicit or implicit receiver, for example in an expression such as (x.Foo is Action, or Foo is Action), is considered to be read.
                VisitRvalue(receiverOpt);

                if (_performStaticNullChecks && this.State.Reachable &&
                    (object)receiverOpt.Type != null && receiverOpt.Type.IsReferenceType && this.State.ResultIsNotNull == false)
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceReceiver, receiverOpt.Syntax);
                }
            }

            if (_performStaticNullChecks && this.State.Reachable)
            {
                this.State.ResultIsNotNull = null;
            }

            return null;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            var result = VisitLambdaOrLocalFunction(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitUnboundLambda(UnboundLambda node)
        {
            var result = base.VisitUnboundLambda(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            Assign(node, value: null, valueIsNotNull: null);
            return VisitLambdaOrLocalFunction(node);
        }

        private BoundNode VisitLambdaOrLocalFunction(IBoundLambdaOrFunction node)
        {
            var oldMethodOrLambda = this.currentMethodOrLambda;
            this.currentMethodOrLambda = node.Symbol;

            var oldPending = SavePending(); // we do not support branches into a lambda
            LocalState finalState = this.State;
            this.State = this.State.Reachable ? this.State.Clone() : AllBitsSet();
            if (!node.WasCompilerGenerated) EnterParameters(node.Symbol.Parameters);
            var oldPending2 = SavePending();
            VisitAlways(node.Body);
            RestorePending(oldPending2); // process any forward branches within the lambda body
            ImmutableArray<PendingBranch> pendingReturns = RemoveReturns();
            RestorePending(oldPending);
            LeaveParameters(node.Symbol.Parameters, node.Syntax, null);
            IntersectWith(ref finalState, ref this.State); // a no-op except in region analysis
            foreach (PendingBranch pending in pendingReturns)
            {
                this.State = pending.State;
                if (pending.Branch.Kind == BoundKind.ReturnStatement)
                {
                    // ensure out parameters are definitely assigned at each return
                    LeaveParameters(node.Symbol.Parameters, pending.Branch.Syntax, null);
                }
                else
                {
                    // other ways of branching out of a lambda are errors, previously reported in control-flow analysis
                }

                IntersectWith(ref finalState, ref this.State); // a no-op except in region analysis
            }

            this.State = finalState;

            this.currentMethodOrLambda = oldMethodOrLambda;
            return null;
        }

        public override BoundNode VisitThisReference(BoundThisReference node)
        {
            // TODO: in a struct constructor, "this" is not initially assigned.
            CheckAssigned(MethodThisParameter, node.Syntax);
            SetResultIsNotNull(node);
            return null;
        }

        public override BoundNode VisitParameter(BoundParameter node)
        {
            if (!node.WasCompilerGenerated)
            {
                CheckAssigned(node.ParameterSymbol, node.Syntax);
            }

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                this.State.ResultIsNotNull = IsResultNotNull(node, node.ParameterSymbol);
            }

            return null;
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            base.VisitAssignmentOperator(node);
            Debug.Assert(!IsConditionalState);
            bool? valueIsNotNull = this.State.ResultIsNotNull;
            Assign(node.Left, node.Right, valueIsNotNull, refKind: node.RefKind);

            if (_performStaticNullChecks && this.State.Reachable)
            {
                this.State.ResultIsNotNull = valueIsNotNull;
            }

            return null;
        }

        public override BoundNode VisitIncrementOperator(BoundIncrementOperator node)
        {
            base.VisitIncrementOperator(node);

            bool? resultOfIncrementIsNotNull;

            Debug.Assert(!IsConditionalState);

            if (_performStaticNullChecks && this.State.Reachable)
            {
                bool? operandIsNotNull;
                if (RegularPropertyAccess(node.Operand))
                {
                    PropertySymbol property = ((BoundPropertyAccess)node.Operand).PropertySymbol;
                    operandIsNotNull = IsResultNotNull(node, property);
                }
                else
                {
                    operandIsNotNull = this.State.ResultIsNotNull;
                }

                MethodSymbol incrementOperator = (node.OperatorKind.IsUserDefined() && (object)node.MethodOpt != null && node.MethodOpt.ParameterCount == 1) ? node.MethodOpt : null;
                TypeSymbol targetTypeOfOperandConversion;

                if (node.OperandConversion.IsUserDefined && (object)node.OperandConversion.Method != null && node.OperandConversion.Method.ParameterCount == 1)
                {
                    WarnOnNullReferenceArgument(node.Operand, operandIsNotNull, node.OperandConversion.Method.Parameters[0], expanded: false);
                    targetTypeOfOperandConversion = node.OperandConversion.Method.ReturnType.TypeSymbol;
                }
                else if ((object)incrementOperator != null)
                {
                    targetTypeOfOperandConversion = incrementOperator.Parameters[0].Type.TypeSymbol;
                }
                else
                {
                    // Either a built-in increment, or an error case.
                    targetTypeOfOperandConversion = null;
                }

                bool? resultOfOperandConversionIsNotNull;

                if ((object)targetTypeOfOperandConversion != null)
                {
                    // TODO: Should something special be done for targetTypeOfOperandConversion for lifted case?
                    resultOfOperandConversionIsNotNull = InferResultNullability(node.OperandConversion.Kind,
                                                                                node.Operand.Type,
                                                                                targetTypeOfOperandConversion,
                                                                                node.OperandConversion.Method,
                                                                                operandIsNotNull);
                }
                else
                {
                    resultOfOperandConversionIsNotNull = null;
                }

                if ((object)incrementOperator == null)
                {
                    resultOfIncrementIsNotNull = null;
                }
                else 
                {
                    WarnOnNullReferenceArgument(node.Operand, 
                                                resultOfOperandConversionIsNotNull,
                                                incrementOperator.Parameters[0], expanded: false);

                    resultOfIncrementIsNotNull = IsResultNotNull(incrementOperator);
                }

                if (node.ResultConversion.IsUserDefined && (object)node.ResultConversion.Method != null && node.ResultConversion.Method.ParameterCount == 1)
                {
                    WarnOnNullReferenceArgument(node, resultOfIncrementIsNotNull, node.ResultConversion.Method.Parameters[0], expanded: false);
                }

                resultOfIncrementIsNotNull = InferResultNullability(node.ResultConversion.Kind,
                                                                    incrementOperator?.ReturnType.TypeSymbol,
                                                                    node.Type,
                                                                    node.ResultConversion.Method,
                                                                    resultOfIncrementIsNotNull);

                var op = node.OperatorKind.Operator();
                if (op == UnaryOperatorKind.PrefixIncrement || op == UnaryOperatorKind.PrefixDecrement)
                {
                    this.State.ResultIsNotNull = resultOfIncrementIsNotNull;
                }
                else
                {
                    this.State.ResultIsNotNull = operandIsNotNull;
                }
            }
            else
            {
                resultOfIncrementIsNotNull = null;
            }

            Assign(node.Operand, value: node, valueIsNotNull: resultOfIncrementIsNotNull); 
            return null;
        }

        public override BoundNode VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            VisitCompoundAssignmentTarget(node);

            bool? resultIsNotNull;
            Debug.Assert(!IsConditionalState);

            if (_performStaticNullChecks && this.State.Reachable)
            {
                bool? leftIsNotNull;
                if (RegularPropertyAccess(node.Left))
                {
                    PropertySymbol property = ((BoundPropertyAccess)node.Left).PropertySymbol;
                    leftIsNotNull = IsResultNotNull(node, property);
                }
                else
                {
                    leftIsNotNull = this.State.ResultIsNotNull;
                }

                if (node.LeftConversion.IsUserDefined && (object)node.LeftConversion.Method != null && node.LeftConversion.Method.ParameterCount == 1)
                {
                    WarnOnNullReferenceArgument(node.Left, leftIsNotNull, node.LeftConversion.Method.Parameters[0], expanded: false);
                }

                bool? resultOfLeftConversionIsNotNull;

                if ((object)node.Operator.LeftType != null)
                {
                    resultOfLeftConversionIsNotNull = InferResultNullability(node.LeftConversion.Kind,
                                                                             node.Left.Type,
                                                                             node.Operator.LeftType,
                                                                             node.LeftConversion.Method,
                                                                             leftIsNotNull);
                }
                else
                {
                    resultOfLeftConversionIsNotNull = null;
                }

                VisitRvalue(node.Right);
                bool? rightIsNotNull = this.State.ResultIsNotNull;

                AfterRightHasBeenVisited(node);

                if ((object)node.Operator.ReturnType != null)
                {
                    if (node.Operator.Kind.IsUserDefined() && (object)node.Operator.Method != null && node.Operator.Method.ParameterCount == 2)
                    { 
                        WarnOnNullReferenceArgument(node.Left, resultOfLeftConversionIsNotNull, node.Operator.Method.Parameters[0], expanded: false);
                        WarnOnNullReferenceArgument(node.Right, rightIsNotNull, node.Operator.Method.Parameters[1], expanded: false);
                    }

                    resultIsNotNull = InferResultNullability(node.Operator.Kind, node.Operator.Method, node.Operator.ReturnType, leftIsNotNull, rightIsNotNull);

                    if (node.FinalConversion.IsUserDefined && (object)node.FinalConversion.Method != null && node.FinalConversion.Method.ParameterCount == 1)
                    {
                        WarnOnNullReferenceArgument(node, resultIsNotNull, node.FinalConversion.Method.Parameters[0], expanded: false);
                    }

                    resultIsNotNull = InferResultNullability(node.FinalConversion.Kind,
                                                             node.Operator.ReturnType,
                                                             node.Type,
                                                             node.FinalConversion.Method,
                                                             resultIsNotNull);
                }
                else
                {
                    resultIsNotNull = null;
                }

                this.State.ResultIsNotNull = resultIsNotNull;
            }
            else
            {
                VisitRvalue(node.Right);
                AfterRightHasBeenVisited(node);
                resultIsNotNull = null;
            }

            Assign(node.Left, value: node, valueIsNotNull: resultIsNotNull); 
            return null;
        }

        public override BoundNode VisitAddressOfOperator(BoundAddressOfOperator node)
        {
            BoundExpression operand = node.Operand;
            bool shouldReadOperand = false;

            // If the node is a fixed statement address-of operator (e.g. fixed(int *p = &...)),
            // then we don't need to consider it for membership in unsafeAddressTakenVariables,
            // because it is either not a local/parameter/range variable (if the variable is
            // non-moveable) or it is and it has a RefKind other than None, in which case it can't
            // be referred to in a lambda (i.e. can't be captured).
            if (!node.IsFixedStatementAddressOf)
            {
                Symbol variable = UseNonFieldSymbolUnsafely(operand);
                if ((object)variable != null)
                {
                    // The goal here is to treat address-of as a read in cases where
                    // we (a) care about a read happening (e.g. for DataFlowsIn) and
                    // (b) have information indicating that this will not result in
                    // a read to an unassigned variable (i.e. the operand is definitely
                    // assigned).
                    if (_unassignedVariableAddressOfSyntaxes != null &&
                        !_unassignedVariableAddressOfSyntaxes.Contains(node.Syntax as PrefixUnaryExpressionSyntax))
                    {
                        shouldReadOperand = true;
                    }

                    if (!_unsafeAddressTakenVariables.ContainsKey(variable))
                    {
                        _unsafeAddressTakenVariables.Add(variable, node.Syntax.Location);
                    }
                }
            }

            VisitAddressOfOperator(node, shouldReadOperand);

            SetUnknownResultNullability();
            return null;
        }

        protected override void VisitArgumentAsRvalue(BoundExpression argument, ParameterSymbol parameter, bool expanded)
        {
            base.VisitArgumentAsRvalue(argument, parameter, expanded);
            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && (object)parameter != null && this.State.Reachable)
            {
                WarnOnNullReferenceArgument(argument, this.State.ResultIsNotNull, parameter, expanded);
            }
        }

        private void WarnOnNullReferenceArgument(BoundExpression argument, bool? argumentIsNotNull, ParameterSymbol parameter, bool expanded)
        {
            Debug.Assert(_performStaticNullChecks);
            TypeSymbolWithAnnotations paramType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(parameter);

            if (argumentIsNotNull == false)
            {
                if (expanded)
                {
                    paramType = ((ArrayTypeSymbol)parameter.Type.TypeSymbol).ElementType;
                }

                if (paramType.IsReferenceType && paramType.IsNullable == false)
                {
                    ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceArgument, argument.Syntax, 
                        new FormattedSymbol(parameter, SymbolDisplayFormat.ShortFormat),
                        new FormattedSymbol(parameter.ContainingSymbol, SymbolDisplayFormat.MinimallyQualifiedFormat));
                }
            }

            if ((object)argument.Type != null &&
                paramType.TypeSymbol.Equals(argument.Type, TypeSymbolEqualityOptions.SameType) &&
                !paramType.TypeSymbol.Equals(argument.Type,
                                             TypeSymbolEqualityOptions.CompareNullableModifiersForReferenceTypes | TypeSymbolEqualityOptions.UnknownNullableModifierMatchesAny))
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullabilityMismatchInArgument, argument.Syntax, argument.Type, paramType.TypeSymbol,
                        new FormattedSymbol(parameter, SymbolDisplayFormat.ShortFormat),
                        new FormattedSymbol(parameter.ContainingSymbol, SymbolDisplayFormat.MinimallyQualifiedFormat));
            }
        }

        private TypeSymbolWithAnnotations GetTypeOrReturnTypeWithAdjustedNullableAnnotations(Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Local:
                    return ((LocalSymbol)symbol).Type;

                case SymbolKind.Parameter:
                    var parameter = (ParameterSymbol)symbol;
                    if (parameter.IsThis)
                    {
                        return parameter.Type;
                    }

                    goto default;

                default:
                    return compilation.GetTypeOrReturnTypeWithAdjustedNullableAnnotations(symbol);
            }
        }

        protected override void WriteArgument(BoundExpression arg, RefKind refKind, MethodSymbol method, ParameterSymbol parameter)
        {
            if (refKind == RefKind.Ref)
            {
                // Though the method might write the argument, in the case of ref arguments it might not,
                // thus leaving the old value in the variable.  We model this as a read of the argument
                // by the method after the invocation.
                CheckAssigned(arg, arg.Syntax);
            }

            Debug.Assert(!IsConditionalState);
            bool? valueIsNotNull = null;
            BoundValuePlaceholder value = null;

            if ((object)parameter != null)
            {
                TypeSymbolWithAnnotations paramType;

                if (_performStaticNullChecks && this.State.Reachable)
                {
                    paramType = GetTypeOrReturnTypeWithAdjustedNullableAnnotations(parameter);

                    if (paramType.IsReferenceType)
                    {
                        valueIsNotNull = !paramType.IsNullable;
                    }
                }
                else
                {
                    paramType = parameter.Type; 
                }

                value = new BoundValuePlaceholder(arg.Syntax, paramType.TypeSymbol) { WasCompilerGenerated = true };
            }

            Assign(arg, value, valueIsNotNull); 

            // Imitate Dev10 behavior: if the argument is passed by ref/out to an external method, then
            // we assume that external method may write and/or read all of its fields (recursively).
            // Strangely, the native compiler requires the "ref", even for reference types, to exhibit
            // this behavior.
            if (refKind != RefKind.None && ((object)method == null || method.IsExtern))
            {
                MarkFieldsUsed(arg.Type);
            }
        }

        protected void CheckAssigned(BoundExpression expr, CSharpSyntaxNode node)
        {
            if (!this.State.Reachable) return;
            int slot = MakeSlot(expr);
            switch (expr.Kind)
            {
                case BoundKind.Local:
                    CheckAssigned(((BoundLocal)expr).LocalSymbol, node);
                    break;
                case BoundKind.Parameter:
                    CheckAssigned(((BoundParameter)expr).ParameterSymbol, node);
                    break;
                case BoundKind.FieldAccess:
                    var field = (BoundFieldAccess)expr;
                    var symbol = field.FieldSymbol;
                    if (!symbol.IsFixed && MayRequireTracking(field.ReceiverOpt, symbol))
                    {
                        CheckAssigned(expr, symbol, node);
                    }
                    break;
                case BoundKind.EventAccess:
                    var @event = (BoundEventAccess)expr;
                    FieldSymbol associatedField = @event.EventSymbol.AssociatedField;
                    if ((object)associatedField != null && MayRequireTracking(@event.ReceiverOpt, associatedField))
                    {
                        CheckAssigned(@event, associatedField, node);
                    }
                    break;
                case BoundKind.ThisReference:
                case BoundKind.BaseReference:
                    CheckAssigned(MethodThisParameter, node);
                    break;
                    //CheckAssigned(expr, 
            }
        }

        private void MarkFieldsUsed(TypeSymbol type)
        {
            switch (type.TypeKind)
            {
                case TypeKind.Array:
                    MarkFieldsUsed(((ArrayTypeSymbol)type).ElementType.TypeSymbol);
                    return;

                case TypeKind.Class:
                case TypeKind.Struct:
                    if (!type.IsFromCompilation(this.compilation))
                    {
                        return;
                    }

                    var namedType = (NamedTypeSymbol)type;
                    var assembly = type.ContainingAssembly as SourceAssemblySymbol;
                    if ((object)assembly == null)
                    {
                        return; // could be retargeting assembly
                    }

                    var seen = assembly.TypesReferencedInExternalMethods;
                    if (seen.Add(type))
                    {
                        foreach (var symbol in namedType.GetMembersUnordered())
                        {
                            if (symbol.Kind != SymbolKind.Field)
                            {
                                continue;
                            }

                            FieldSymbol field = (FieldSymbol)symbol;
                            assembly.NoteFieldAccess(field, read: true, write: true);
                            MarkFieldsUsed(field.Type.TypeSymbol);
                        }
                    }
                    return;
            }
        }

        public override BoundNode VisitBaseReference(BoundBaseReference node)
        {
            CheckAssigned(MethodThisParameter, node.Syntax);
            SetResultIsNotNull(node);
            return null;
        }

        #region TryStatements

        private LocalState? _tryState;

        protected override void VisitTryBlock(BoundStatement tryBlock, BoundTryStatement node, ref LocalState tryState)
        {
            if (trackUnassignments)
            {
                LocalState? oldTryState = _tryState;
                _tryState = AllBitsSet();
                base.VisitTryBlock(tryBlock, node, ref tryState);
                var tts = _tryState.Value;
                IntersectWith(ref tryState, ref tts);
                if (oldTryState.HasValue)
                {
                    var ots = oldTryState.Value;
                    IntersectWith(ref ots, ref tts);
                    oldTryState = ots;
                }
                _tryState = oldTryState;
            }
            else
            {
                base.VisitTryBlock(tryBlock, node, ref tryState);
            }
        }

        protected override void VisitCatchBlock(BoundCatchBlock catchBlock, ref LocalState finallyState)
        {
            if (trackUnassignments)
            {
                LocalState? oldTryState = _tryState;
                _tryState = AllBitsSet();
                VisitCatchBlockInternal(catchBlock, ref finallyState);
                var tts = _tryState.Value;
                IntersectWith(ref finallyState, ref tts);
                if (oldTryState.HasValue)
                {
                    var ots = oldTryState.Value;
                    IntersectWith(ref ots, ref tts);
                    oldTryState = ots;
                }
                _tryState = oldTryState;
            }
            else
            {
                VisitCatchBlockInternal(catchBlock, ref finallyState);
            }
        }

        private void VisitCatchBlockInternal(BoundCatchBlock catchBlock, ref LocalState finallyState)
        {
            if ((object)catchBlock.LocalOpt != null)
            {
                DeclareVariable(catchBlock.LocalOpt);
            }

            var exceptionSource = catchBlock.ExceptionSourceOpt;
            if (exceptionSource != null)
            {
                Assign(exceptionSource, value: null, read: false, valueIsNotNull: true);
            }

            base.VisitCatchBlock(catchBlock, ref finallyState);

            if ((object)catchBlock.LocalOpt != null)
            {
                ReportIfUnused(catchBlock.LocalOpt, assigned: false);
            }
        }

        protected override void VisitFinallyBlock(BoundStatement finallyBlock, ref LocalState unsetInFinally)
        {
            if (trackUnassignments)
            {
                LocalState? oldTryState = _tryState;
                _tryState = AllBitsSet();
                base.VisitFinallyBlock(finallyBlock, ref unsetInFinally);
                var tts = _tryState.Value;
                IntersectWith(ref unsetInFinally, ref tts);
                if (oldTryState.HasValue)
                {
                    var ots = oldTryState.Value;
                    IntersectWith(ref ots, ref tts);
                    oldTryState = ots;
                }

                _tryState = oldTryState;
            }
            else
            {
                base.VisitFinallyBlock(finallyBlock, ref unsetInFinally);
            }
        }

        #endregion TryStatements

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            var result = base.VisitFieldAccess(node);
            NoteRead(node.FieldSymbol);

            if (node.FieldSymbol.IsFixed && node.Syntax != null && !SyntaxFacts.IsFixedStatementExpression(node.Syntax))
            {
                Symbol receiver = UseNonFieldSymbolUnsafely(node.ReceiverOpt);
                if ((object)receiver != null)
                {
                    CheckCaptured(receiver);
                    if (!_unsafeAddressTakenVariables.ContainsKey(receiver))
                    {
                        _unsafeAddressTakenVariables.Add(receiver, node.Syntax.Location);
                    }
                }
            }
            else if (MayRequireTracking(node.ReceiverOpt, node.FieldSymbol))
            {
                // special definite assignment behavior for fields of struct local variables.
                CheckAssigned(node, node.FieldSymbol, node.Syntax);
            }

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                this.State.ResultIsNotNull = IsResultNotNull(node, node.FieldSymbol);
            }

            return result;
        }

        public override BoundNode VisitPropertyAccess(BoundPropertyAccess node)
        {
            var result = base.VisitPropertyAccess(node);

            if (Binder.AccessingAutopropertyFromConstructor(node, this.currentMethodOrLambda))
            {
                var property = node.PropertySymbol;
                var backingField = (property as SourcePropertySymbol)?.BackingField;
                if (backingField != null)
                {
                    if (MayRequireTracking(node.ReceiverOpt, backingField))
                    {
                        // special definite assignment behavior for fields of struct local variables.
                        int unassignedSlot;
                        if (this.State.Reachable && !IsAssigned(node, out unassignedSlot))
                        {
                            ReportUnassigned(backingField, unassignedSlot, node.Syntax);
                        }
                    }
                }
            }

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                var property = node.PropertySymbol;
                this.State.ResultIsNotNull = IsResultNotNull(node, property);
            }

            return result;
        }

        public override BoundNode VisitIndexerAccess(BoundIndexerAccess node)
        {
            var result = base.VisitIndexerAccess(node);

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                this.State.ResultIsNotNull = IsResultNotNull(node.Indexer);
            }

            return result;
        }

        public override BoundNode VisitEventAccess(BoundEventAccess node)
        {
            var result = base.VisitEventAccess(node);
            // special definite assignment behavior for events of struct local variables.

            FieldSymbol associatedField = node.EventSymbol.AssociatedField;
            if ((object)associatedField != null)
            {
                NoteRead(associatedField);
                if (MayRequireTracking(node.ReceiverOpt, associatedField))
                {
                    CheckAssigned(node, associatedField, node.Syntax);
                }
            }

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                this.State.ResultIsNotNull = IsResultNotNull(node, node.EventSymbol);
            }

            return result;
        }

        public override void VisitForEachIterationVariable(BoundForEachStatement node)
        {
            var local = node.IterationVariable;
            if ((object)local != null)
            {
                GetOrCreateSlot(local);
                Assign(node, value: null, valueIsNotNull: null); // TODO: valueIsNotNull
                // TODO: node needed? NoteRead(local); // Never warn about unused foreach variables.
            }
        }

        public override BoundNode VisitObjectInitializerMember(BoundObjectInitializerMember node)
        {
            var result = base.VisitObjectInitializerMember(node);

            if ((object)_sourceAssembly != null && node.MemberSymbol != null && node.MemberSymbol.Kind == SymbolKind.Field)
            {
                _sourceAssembly.NoteFieldAccess((FieldSymbol)node.MemberSymbol.OriginalDefinition, read: false, write: true);
            }

            SetUnknownResultNullability();

            return result;
        }

        public override BoundNode VisitDynamicObjectInitializerMember(BoundDynamicObjectInitializerMember node)
        {
            SetUnknownResultNullability();
            return null;
        }

        public override BoundNode VisitDup(BoundDup node)
        {
            Debug.Assert(!_performStaticNullChecks);
            return base.VisitDup(node);
        }

        public override BoundNode VisitBadExpression(BoundBadExpression node)
        {
            var result = base.VisitBadExpression(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitTypeExpression(BoundTypeExpression node)
        {
            var result = base.VisitTypeExpression(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitTypeOrValueExpression(BoundTypeOrValueExpression node)
        {
            var result = base.VisitTypeOrValueExpression(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitUnaryOperator(BoundUnaryOperator node)
        {
            Debug.Assert(!IsConditionalState);

            var result = base.VisitUnaryOperator(node);

            Debug.Assert(!IsConditionalState || node.OperatorKind == UnaryOperatorKind.BoolLogicalNegation);
            if (_performStaticNullChecks)
            {
                if (IsConditionalState)
                {
                    if (this.StateWhenFalse.Reachable)
                    {
                        this.StateWhenFalse.ResultIsNotNull = null;
                    }

                    if (this.StateWhenTrue.Reachable)
                    {
                        this.StateWhenTrue.ResultIsNotNull = null;
                    }
                }
                else if (this.State.Reachable)
                {
                    if (node.OperatorKind.IsUserDefined() && (object)node.MethodOpt != null && node.MethodOpt.ParameterCount == 1)
                    {
                        WarnOnNullReferenceArgument(node.Operand, this.State.ResultIsNotNull, node.MethodOpt.Parameters[0], expanded: false);
                    }

                    this.State.ResultIsNotNull = InferResultNullability(node);
                }
            }

            return null;
        }

        private bool? InferResultNullability(BoundUnaryOperator node)
        {
            if (node.OperatorKind.IsUserDefined())
            {
                if ((object)node.MethodOpt != null && node.MethodOpt.ParameterCount == 1)
                {
                    return IsResultNotNull(node.MethodOpt);
                }
                else
                {
                    return null;
                }
            }
            else if (node.OperatorKind.IsDynamic())
            {
                return null;
            }
            else if (node.Type?.IsReferenceType == true)
            {
                return true;
            }
            else
            {
                return null;
            }
        }

        public override BoundNode VisitPointerIndirectionOperator(BoundPointerIndirectionOperator node)
        {
            var result = base.VisitPointerIndirectionOperator(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitPointerElementAccess(BoundPointerElementAccess node)
        {
            var result = base.VisitPointerElementAccess(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitRefTypeOperator(BoundRefTypeOperator node)
        {
            // Inherit nullable state from the argument.
            return base.VisitRefTypeOperator(node);
        }

        public override BoundNode VisitMakeRefOperator(BoundMakeRefOperator node)
        {
            var result = base.VisitMakeRefOperator(node);
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitRefValueOperator(BoundRefValueOperator node)
        {
            var result = base.VisitRefValueOperator(node);
            SetUnknownResultNullability();
            return result;
        }

        private bool? InferResultNullability(BoundUserDefinedConditionalLogicalOperator node)
        {
            if ((object)node.LogicalOperator != null && node.LogicalOperator.ParameterCount == 2)
            {
                return IsResultNotNull(node.LogicalOperator);
            }
            else
            {
                return null;
            }
        }

        protected override void AfterLeftChildOfBinaryLogicalOperatorHasBeenVisited(BoundExpression node, BoundExpression right, bool isAnd, bool isBool, ref LocalState leftTrue, ref LocalState leftFalse)
        {
            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                bool? leftIsNotNull = this.State.ResultIsNotNull;
                MethodSymbol logicalOperator = null;
                MethodSymbol trueFalseOperator = null;
                BoundExpression left = null;

                switch (node.Kind)
                {
                    case BoundKind.BinaryOperator:
                        Debug.Assert(!((BoundBinaryOperator)node).OperatorKind.IsUserDefined());
                        break;
                    case BoundKind.UserDefinedConditionalLogicalOperator:
                        var binary = (BoundUserDefinedConditionalLogicalOperator)node;
                        if (binary.LogicalOperator != null && binary.LogicalOperator.ParameterCount == 2)
                        {
                            logicalOperator = binary.LogicalOperator;
                            left = binary.Left;
                            trueFalseOperator = isAnd ? binary.FalseOperator : binary.TrueOperator;

                            if ((object)trueFalseOperator != null && trueFalseOperator.ParameterCount != 1)
                            {
                                trueFalseOperator = null;
                            }
                        }
                        break;
                    default:
                        throw ExceptionUtilities.Unreachable;
                }

                Debug.Assert((object)trueFalseOperator == null || ((object)logicalOperator != null && left != null));

                if ((object)trueFalseOperator != null)
                {
                    WarnOnNullReferenceArgument(left, leftIsNotNull, trueFalseOperator.Parameters[0], expanded: false);
                }

                if ((object)logicalOperator != null)
                {
                    WarnOnNullReferenceArgument(left, leftIsNotNull, logicalOperator.Parameters[0], expanded: false);
                }

                Visit(right);

                Debug.Assert(IsConditionalState ? (this.StateWhenFalse.Reachable || this.StateWhenTrue.Reachable) : this.State.Reachable);
                bool? rightIsNotNull = null;

                if (IsConditionalState)
                {
                    if (this.StateWhenFalse.Reachable)
                    {
                        rightIsNotNull = this.StateWhenFalse.ResultIsNotNull;
                        this.StateWhenFalse.ResultIsNotNull = InferResultNullabilityOfBinaryLogicalOperator(node, leftIsNotNull, rightIsNotNull);
                    }

                    if (this.StateWhenTrue.Reachable)
                    {
                        bool? saveRightIsNotNull = rightIsNotNull;
                        rightIsNotNull = this.StateWhenTrue.ResultIsNotNull;
                        this.StateWhenTrue.ResultIsNotNull = InferResultNullabilityOfBinaryLogicalOperator(node, leftIsNotNull, rightIsNotNull);

                        if (this.StateWhenFalse.Reachable)
                        {
                            rightIsNotNull &= saveRightIsNotNull;
                        }
                    }
                }
                else if (this.State.Reachable)
                {
                    rightIsNotNull = this.State.ResultIsNotNull;
                    this.State.ResultIsNotNull = InferResultNullabilityOfBinaryLogicalOperator(node, leftIsNotNull, rightIsNotNull);
                }

                if ((object)logicalOperator != null)
                {
                    WarnOnNullReferenceArgument(right, rightIsNotNull, logicalOperator.Parameters[1], expanded: false);
                }

                AfterRightChildOfBinaryLogicalOperatorHasBeenVisited(node, right, isAnd, isBool, ref leftTrue, ref leftFalse);
            }
            else
            {
                base.AfterLeftChildOfBinaryLogicalOperatorHasBeenVisited(node, right, isAnd, isBool, ref leftTrue, ref leftFalse);
            }
        }

        private bool? InferResultNullabilityOfBinaryLogicalOperator(BoundExpression node, bool? leftIsNotNull, bool? rightIsNotNull)
        {
            switch (node.Kind)
            {
                case BoundKind.BinaryOperator:
                    return InferResultNullability((BoundBinaryOperator)node, leftIsNotNull, rightIsNotNull);
                case BoundKind.UserDefinedConditionalLogicalOperator:
                    return InferResultNullability((BoundUserDefinedConditionalLogicalOperator)node);
                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        public override BoundNode VisitArrayLength(BoundArrayLength node)
        {
            Debug.Assert(!_performStaticNullChecks);
            return base.VisitArrayLength(node);
        }

        public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
        {
            var result = base.VisitAwaitExpression(node);

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                if (!node.Type.IsReferenceType || node.HasErrors || (object)node.GetResult == null)
                {
                    SetUnknownResultNullability();
                }
                else
                {
                    this.State.ResultIsNotNull = IsResultNotNull(node.GetResult);
                }
            }

            return result;
        }

        public override BoundNode VisitTypeOfOperator(BoundTypeOfOperator node)
        {
            var result = base.VisitTypeOfOperator(node);
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitMethodInfo(BoundMethodInfo node)
        {
            var result = base.VisitMethodInfo(node);
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitFieldInfo(BoundFieldInfo node)
        {
            var result = base.VisitFieldInfo(node);
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitDefaultOperator(BoundDefaultOperator node)
        {
            var result = base.VisitDefaultOperator(node);

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                if (node.Type.IsReferenceType == true)
                {
                    this.State.ResultIsNotNull = false;
                }
                else
                {
                    this.State.ResultIsNotNull = null;
                }
            }

            return result;
        }

        public override BoundNode VisitIsOperator(BoundIsOperator node)
        {
            var result = base.VisitIsOperator(node);
            Debug.Assert(node.Type.SpecialType == SpecialType.System_Boolean);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitAsOperator(BoundAsOperator node)
        {
            var result = base.VisitAsOperator(node);

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                if (node.Type.IsReferenceType)
                {
                    switch (node.Conversion.Kind)
                    {
                        case ConversionKind.Identity:
                        case ConversionKind.ImplicitReference:
                            // Inherit nullability from the operand
                            break;

                        case ConversionKind.Boxing:
                            if (node.Operand.Type?.IsValueType == true)
                            {
                                if (node.Operand.Type.IsNullableType())
                                {
                                    // TODO: Should we worry about a pathological case of boxing nullable value known to be not null?
                                    //       For example, new int?(0)
                                    this.State.ResultIsNotNull = false;
                                }
                                else
                                {
                                    this.State.ResultIsNotNull = true;
                                }
                            }
                            else
                            {
                                Debug.Assert(node.Operand.Type?.IsReferenceType != true);
                                this.State.ResultIsNotNull = false;
                            }
                            break;

                        default:
                            this.State.ResultIsNotNull = false;
                            break;
                    }
                }
                else
                {
                    SetUnknownResultNullability();
                }
            }

            return result;
        }

        public override BoundNode VisitSizeOfOperator(BoundSizeOfOperator node)
        {
            var result = base.VisitSizeOfOperator(node);
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitArgList(BoundArgList node)
        {
            var result = base.VisitArgList(node);
            Debug.Assert(node.Type.SpecialType == SpecialType.System_RuntimeArgumentHandle);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitArgListOperator(BoundArgListOperator node)
        {
            var result = base.VisitArgListOperator(node);
            Debug.Assert((object)node.Type == null);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitFixedLocalCollectionInitializer(BoundFixedLocalCollectionInitializer node)
        {
            var result = base.VisitFixedLocalCollectionInitializer(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitLiteral(BoundLiteral node)
        {
            var result = base.VisitLiteral(node);

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                var constant = node.ConstantValue;

                if (constant != null &&
                    ((object)node.Type != null ? node.Type.IsReferenceType : constant.IsNull))
                {
                    this.State.ResultIsNotNull = !constant.IsNull;
                }
                else
                {
                    this.State.ResultIsNotNull = null;
                }
            }

            return result;
        }

        public override BoundNode VisitPreviousSubmissionReference(BoundPreviousSubmissionReference node)
        {
            var result = base.VisitPreviousSubmissionReference(node);
            Debug.Assert(node.WasCompilerGenerated);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitHostObjectMemberReference(BoundHostObjectMemberReference node)
        {
            var result = base.VisitHostObjectMemberReference(node);
            Debug.Assert(node.WasCompilerGenerated);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitPseudoVariable(BoundPseudoVariable node)
        {
            var result = base.VisitPseudoVariable(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitRangeVariable(BoundRangeVariable node)
        {
            var result = base.VisitRangeVariable(node);
            SetUnknownResultNullability(); // TODO
            return result;
        }

        public override BoundNode VisitLabel(BoundLabel node)
        {
            var result = base.VisitLabel(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitDynamicMemberAccess(BoundDynamicMemberAccess node)
        {
            var result = base.VisitDynamicMemberAccess(node);

            Debug.Assert(node.Type.IsDynamic());
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitDynamicInvocation(BoundDynamicInvocation node)
        {
            var result = base.VisitDynamicInvocation(node);

            Debug.Assert(node.Type.IsDynamic());
            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                if (node.Type?.IsReferenceType == true)
                {
                    this.State.ResultIsNotNull = InferResultNullabilityFromApplicableCandidates(StaticCast<Symbol>.From(node.ApplicableMethods));
                }
                else
                {
                    this.State.ResultIsNotNull = null;
                }
            }

            return result;
        }

        public override BoundNode VisitEventAssignmentOperator(BoundEventAssignmentOperator node)
        {
            var result = base.VisitEventAssignmentOperator(node);
            SetUnknownResultNullability();
            return result;
        }

        protected override void VisitReceiverOfEventAssignmentAsRvalue(BoundEventAssignmentOperator node)
        {
            base.VisitReceiverOfEventAssignmentAsRvalue(node);

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable &&
                node.ReceiverOpt != null && (object)node.ReceiverOpt.Type != null && node.ReceiverOpt.Type.IsReferenceType && this.State.ResultIsNotNull == false &&
                !node.Event.IsStatic)
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceReceiver, node.ReceiverOpt.Syntax);
            }
        }

        public override BoundNode VisitDynamicObjectCreationExpression(BoundDynamicObjectCreationExpression node)
        {
            var result = base.VisitDynamicObjectCreationExpression(node);
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitObjectInitializerExpression(BoundObjectInitializerExpression node)
        {
            var result = base.VisitObjectInitializerExpression(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitCollectionInitializerExpression(BoundCollectionInitializerExpression node)
        {
            var result = base.VisitCollectionInitializerExpression(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitCollectionElementInitializer(BoundCollectionElementInitializer node)
        {
            var result = base.VisitCollectionElementInitializer(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitDynamicCollectionElementInitializer(BoundDynamicCollectionElementInitializer node)
        {
            var result = base.VisitDynamicCollectionElementInitializer(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitImplicitReceiver(BoundImplicitReceiver node)
        {
            var result = base.VisitImplicitReceiver(node);
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitAnonymousPropertyDeclaration(BoundAnonymousPropertyDeclaration node)
        {
            var result = base.VisitAnonymousPropertyDeclaration(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitNoPiaObjectCreationExpression(BoundNoPiaObjectCreationExpression node)
        {
            var result = base.VisitNoPiaObjectCreationExpression(node);
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitNewT(BoundNewT node)
        {
            var result = base.VisitNewT(node);
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitArrayInitialization(BoundArrayInitialization node)
        {
            var result = base.VisitArrayInitialization(node);
            SetUnknownResultNullability();
            return result;
        }

        private void SetUnknownResultNullability()
        {
            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                this.State.ResultIsNotNull = null;
            }
        }

        public override BoundNode VisitStackAllocArrayCreation(BoundStackAllocArrayCreation node)
        {
            var result = base.VisitStackAllocArrayCreation(node);
            Debug.Assert(node.Type.IsPointerType());
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitHoistedFieldAccess(BoundHoistedFieldAccess node)
        {
            Debug.Assert(!_performStaticNullChecks);
            return base.VisitHoistedFieldAccess(node);
        }

        public override BoundNode VisitDynamicIndexerAccess(BoundDynamicIndexerAccess node)
        {
            var result = base.VisitDynamicIndexerAccess(node);

            Debug.Assert(node.Type.IsDynamic());
            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable)
            {
                if (node.Type?.IsReferenceType == true)
                {
                    this.State.ResultIsNotNull = InferResultNullabilityFromApplicableCandidates(StaticCast<Symbol>.From(node.ApplicableIndexers));
                }
                else
                {
                    this.State.ResultIsNotNull = null;
                }
            }

            return result;
        }

        protected override void VisitReceiverOfDynamicAccessAsRvalue(BoundExpression receiverOpt)
        {
            base.VisitReceiverOfDynamicAccessAsRvalue(receiverOpt);

            Debug.Assert(!IsConditionalState);
            if (_performStaticNullChecks && this.State.Reachable &&
                receiverOpt != null && (object)receiverOpt.Type != null && receiverOpt.Type.IsReferenceType && this.State.ResultIsNotNull == false)
            {
                ReportStaticNullCheckingDiagnostics(ErrorCode.WRN_NullReferenceReceiver, receiverOpt.Syntax);
            }
        }

        private bool? InferResultNullabilityFromApplicableCandidates(ImmutableArray<Symbol> applicableMembers)
        {
            if (applicableMembers.IsDefaultOrEmpty)
            {
                return null;
            }

            bool? resultIsNotNull = true;

            foreach (Symbol member in applicableMembers)
            {
                TypeSymbolWithAnnotations type = member.GetTypeOrReturnType();

                if (type.IsReferenceType)
                {
                    bool? memberResultIsNotNull = IsResultNotNull(member);
                    if (memberResultIsNotNull == false)
                    {
                        // At least one candidate can produce null, assume dynamic access can produce null as well
                        resultIsNotNull = false;
                        break;
                    }
                    else if (memberResultIsNotNull == null)
                    {
                        // At least one candidate can produce result of an unknow nullability.
                        // At best, dynamic access can produce result of an unknown nullability as well.
                        resultIsNotNull = null;
                    }
                }
                else if (!type.IsValueType)
                {
                    resultIsNotNull = null;
                }
            }

            return resultIsNotNull;
        }

        public override BoundNode VisitQueryClause(BoundQueryClause node)
        {
            var result = base.VisitQueryClause(node);
            SetUnknownResultNullability(); // TODO
            return result;
        }

        public override BoundNode VisitNameOfOperator(BoundNameOfOperator node)
        {
            var result = base.VisitNameOfOperator(node);
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitNamespaceExpression(BoundNamespaceExpression node)
        {
            var result = base.VisitNamespaceExpression(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitInterpolatedString(BoundInterpolatedString node)
        {
            var result = base.VisitInterpolatedString(node);
            SetResultIsNotNull(node);
            return result;
        }

        public override BoundNode VisitStringInsert(BoundStringInsert node)
        {
            var result = base.VisitStringInsert(node);
            SetUnknownResultNullability();
            return result;
        }

        public override BoundNode VisitPropertyGroup(BoundPropertyGroup node)
        {
            Debug.Assert(!_performStaticNullChecks);
            return base.VisitPropertyGroup(node);
        }

        #endregion Visitors

        protected override string Dump(LocalState state)
        {
            var builder = new StringBuilder();
            builder.Append("[assigned ");
            AppendBitNames(state.Assigned, builder);
            builder.Append("]");
            return builder.ToString();
        }

        protected void AppendBitNames(BitVector a, StringBuilder builder)
        {
            bool any = false;
            foreach (int bit in a.TrueBits())
            {
                if (any) builder.Append(", ");
                any = true;
                AppendBitName(bit, builder);
            }
        }

        protected void AppendBitName(int bit, StringBuilder builder)
        {
            VariableIdentifier id = variableBySlot[bit];
            if (id.ContainingSlot > 0)
            {
                AppendBitName(id.ContainingSlot, builder);
                builder.Append(".");
            }

            builder.Append(
                bit == 0 ? "<unreachable>" :
                string.IsNullOrEmpty(id.Symbol.Name) ? "<anon>" + id.Symbol.GetHashCode() :
                id.Symbol.Name);
        }

        protected override void UnionWith(ref LocalState self, ref LocalState other)
        {
            if (self.Assigned.Capacity != other.Assigned.Capacity)
            {
                NormalizeAssigned(ref self);
                NormalizeAssigned(ref other);
            }

            if (other.Assigned[0]) self.Assigned[0] = true;
            for (int slot = 1; slot < self.Assigned.Capacity; slot++)
            {
                if (other.Assigned[slot] && !self.Assigned[slot])
                {
                    SetSlotAssigned(slot, ref self);
                }
            }

            if (_performStaticNullChecks)
            {
                if (self.KnownNullState.Capacity != other.KnownNullState.Capacity)
                {
                    NormalizeNullable(ref self);
                    NormalizeNullable(ref other);
                }

                for (int slot = 1; slot < self.KnownNullState.Capacity; slot++)
                {
                    bool? selfSlotIsNotNull = self.KnownNullState[slot] ? self.NotNull[slot] : (bool?)null;
                    bool? union = selfSlotIsNotNull | (other.KnownNullState[slot] ? other.NotNull[slot] : (bool?)null);
                    if (selfSlotIsNotNull != union)
                    {
                        self.KnownNullState[slot] = union.HasValue;
                        self.NotNull[slot] = union.GetValueOrDefault();
                    }
                }

                self.ResultIsNotNull |= other.ResultIsNotNull;
            }
        }

        protected override bool IntersectWith(ref LocalState self, ref LocalState other)
        {

            if (self.Reachable == other.Reachable)
            {
                if (self.Assigned.Capacity != other.Assigned.Capacity)
                {
                    NormalizeAssigned(ref self);
                    NormalizeAssigned(ref other);
                }

                bool result = self.Assigned.IntersectWith(other.Assigned);

                if (_performStaticNullChecks)
                {
                    if (self.KnownNullState.Capacity != other.KnownNullState.Capacity)
                    {
                        NormalizeNullable(ref self);
                        NormalizeNullable(ref other);
                    }

                    for (int slot = 1; slot < self.KnownNullState.Capacity; slot++)
                    {
                        bool? selfSlotIsNotNull = self.KnownNullState[slot] ? self.NotNull[slot] : (bool?)null;
                        bool? intersection = selfSlotIsNotNull & (other.KnownNullState[slot] ? other.NotNull[slot] : (bool?)null);
                        if (selfSlotIsNotNull != intersection)
                        {
                            self.KnownNullState[slot] = intersection.HasValue;
                            self.NotNull[slot] = intersection.GetValueOrDefault();
                            result = true;
                        }
                    }

                    bool? resultIsNotNull = self.ResultIsNotNull;
                    self.ResultIsNotNull &= other.ResultIsNotNull;

                    if (self.ResultIsNotNull != resultIsNotNull)
                    {
                        result = true;
                    }
                }

                return result;
            }
            else if (!self.Reachable)
            {
                self.Assigned = other.Assigned.Clone();

                if (_performStaticNullChecks)
                {
                    self.KnownNullState = other.KnownNullState.Clone();
                    self.NotNull = other.NotNull.Clone();
                    self.ResultIsNotNull = other.ResultIsNotNull;
                }

                return true;
            }
            else
            {
                Debug.Assert(!other.Reachable);
                return false;
            }
        }

        internal struct LocalState : AbstractLocalState
        {
            internal BitVector Assigned;
            internal BitVector KnownNullState; // No diagnostics should be derived from a variable with a bit set to 0.
            internal BitVector NotNull;
            internal bool? ResultIsNotNull; 

            internal LocalState(BitVector assigned, BitVector unknownNullState, BitVector notNull)
            {
                this.Assigned = assigned;
                Debug.Assert(!assigned.IsNull);
                this.KnownNullState = unknownNullState;
                Debug.Assert(!unknownNullState.IsNull);
                this.NotNull = notNull;
                Debug.Assert(!notNull.IsNull);
                ResultIsNotNull = null;
            }

            /// <summary>
            /// Produce a duplicate of this flow analysis state.
            /// </summary>
            /// <returns></returns>
            public LocalState Clone()
            {
                return new LocalState(Assigned.Clone(), KnownNullState.Clone(), NotNull.Clone()) { ResultIsNotNull = this.ResultIsNotNull };
            }

            public bool IsAssigned(int slot)
            {
                return /*(slot == -1) || */Assigned[slot];
            }

            public void Assign(int slot)
            {
                if (slot == -1)
                    return;
                Assigned[slot] = true;
            }

            public void Unassign(int slot)
            {
                if (slot == -1)
                    return;
                Assigned[slot] = false;
            }

            public bool Reachable
            {
                get
                {
                    return Assigned.Capacity <= 0 || !IsAssigned(0);
                }
            }
        }
    }
}
