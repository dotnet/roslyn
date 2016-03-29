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

        protected override void Free()
        {
            _usedVariables.Free();
            _usedLocalFunctions.Free();
            _writtenVariables.Free();
            _capturedVariables.Free();
            _unsafeAddressTakenVariables.Free();
            _variableSlot.Free();

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

            Normalize(ref this.State);
            return slot;
        }

        private void Normalize(ref LocalState state)
        {
            int oldNext = state.Assigned.Capacity;
            state.Assigned.EnsureCapacity(nextVariableSlot);
            for (int i = oldNext; i < nextVariableSlot; i++)
            {
                var id = variableBySlot[i];
                state.Assigned[i] = (id.ContainingSlot > 0) && state.Assigned[id.ContainingSlot];
            }
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
                    {
                        var fieldAccess = (BoundFieldAccess)node;
                        var fieldSymbol = fieldAccess.FieldSymbol;
                        var receiverOpt = fieldAccess.ReceiverOpt;
                        if (fieldSymbol.IsStatic || receiverOpt == null || receiverOpt.Kind == BoundKind.TypeExpression) return -1; // access of static field
                        if (fieldSymbol.IsFixed) return -1; // fixed buffers are not tracked
                        if ((object)receiverOpt.Type == null || receiverOpt.Type.TypeKind != TypeKind.Struct) return -1; // field of non-struct
                        int containingSlot = MakeSlot(receiverOpt);
                        return (containingSlot == -1) ? -1 : GetOrCreateSlot(fieldSymbol, containingSlot);
                    }
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

                        if (Binder.AccessingAutopropertyFromConstructor(propAccess, this.currentMethodOrLambda))
                        {
                            var propSymbol = propAccess.PropertySymbol;
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

                        goto default;
                    }
                default:
                    return -1;
            }
        }

        /// <summary>
        /// Check that the given variable is definitely assigned.  If not, produce an error.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="node"></param>
        protected void CheckAssigned(Symbol symbol, CSharpSyntaxNode node)
        {
            Debug.Assert(!IsConditionalState);
            if ((object)symbol != null)
            {
                if (this.State.Reachable)
                {
                    int slot = VariableSlot(symbol);
                    if (slot >= this.State.Assigned.Capacity) Normalize(ref this.State);
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

        protected Symbol GetNonFieldSymbol(int slot)
        {
            VariableIdentifier variableId = variableBySlot[slot];
            while (variableId.ContainingSlot > 0)
            {
                Debug.Assert(variableId.Symbol.Kind == SymbolKind.Field);
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

        protected void Assign(BoundNode node, BoundExpression value, RefKind refKind = RefKind.None, bool read = true)
        {
            AssignImpl(node, value, written: true, refKind: refKind, read: read);
        }

        /// <summary>
        /// Mark a variable as assigned (or unassigned).
        /// </summary>
        /// <param name="node">Node being assigned to.</param>
        /// <param name="value">The value being assigned.</param>
        /// <param name="written">True if target location is considered written to.</param>
        /// <param name="refKind">Target kind (by-ref or not).</param>
        /// <param name="read">True if target location is considered read from.</param>
        protected virtual void AssignImpl(BoundNode node, BoundExpression value, RefKind refKind, bool written, bool read)
        {
            switch (node.Kind)
            {
                case BoundKind.DeclarationPattern:
                    {
                        var local = (BoundDeclarationPattern)node;
                        LocalSymbol symbol = local.LocalSymbol;
                        int slot = GetOrCreateSlot(symbol);
                        SetSlotState(slot, assigned: written || !this.State.Reachable);
                        if (written) NoteWrite(symbol, value, read);
                        break;
                    }

                case BoundKind.LocalDeclaration:
                    {
                        var local = (BoundLocalDeclaration)node;
                        Debug.Assert(local.InitializerOpt == value);
                        LocalSymbol symbol = local.LocalSymbol;
                        int slot = GetOrCreateSlot(symbol);
                        SetSlotState(slot, assigned: written || !this.State.Reachable);
                        if (written) NoteWrite(symbol, value, read);
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
                        }
                        else
                        {
                            int slot = MakeSlot(local);
                            SetSlotState(slot, written);
                            if (written) NoteWrite(local, value, read);
                        }
                        break;
                    }

                case BoundKind.Parameter:
                case BoundKind.ThisReference:
                case BoundKind.FieldAccess:
                case BoundKind.EventAccess:
                case BoundKind.PropertyAccess:
                    {
                        var expression = (BoundExpression)node;
                        int slot = MakeSlot(expression);
                        SetSlotState(slot, written);
                        if (written) NoteWrite(expression, value, read);
                        break;
                    }

                case BoundKind.RangeVariable:
                    AssignImpl(((BoundRangeVariable)node).Value, value, refKind, written, read);
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
                            AssignImpl(bad.ChildBoundNodes[0], value, refKind, written, read);
                        }
                        break;
                    }

                default:
                    // Other kinds of left-hand-sides either represent things not tracked (e.g. array elements)
                    // or errors that have been reported earlier (e.g. assignment to a unary increment)
                    break;
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
            if (slot >= state.Assigned.Capacity) Normalize(ref state);
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
            return new LocalState(BitVector.Empty);
        }

        protected override LocalState AllBitsSet()
        {
            var result = new LocalState(BitVector.AllSet(nextVariableSlot));
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

        public override void VisitPattern(BoundExpression expression, BoundPattern pattern)
        {
            base.VisitPattern(expression, pattern);
            var whenFail = StateWhenFalse;
            SetState(StateWhenTrue);
            AssignPatternVariables(pattern);
            SetConditionalState(this.State, whenFail);
        }

        private void AssignPatternVariables(BoundPattern pattern)
        {
            switch (pattern.Kind)
            {
                case BoundKind.DeclarationPattern:
                    {
                        var pat = (BoundDeclarationPattern)pattern;
                        Assign(pat, null, RefKind.None, false);
                        break;
                    }
                case BoundKind.PropertyPattern:
                    {
                        var pat = (BoundPropertyPattern)pattern;
                        foreach (var prop in pat.Subpatterns)
                        {
                            AssignPatternVariables(prop.Pattern);
                        }
                        break;
                    }
                case BoundKind.RecursivePattern:
                    {
                        var pat = (BoundRecursivePattern)pattern;
                        foreach (var prop in pat.Patterns)
                        {
                            AssignPatternVariables(prop);
                        }
                        break;
                    }

                case BoundKind.WildcardPattern:
                case BoundKind.ConstantPattern:
                default:
                    break;
            }
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            DeclareVariables(node.Locals);
            var result = base.VisitBlock(node);
            ReportUnusedVariables(node.Locals);
            ReportUnusedVariables(node.LocalFunctions);
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

        public override BoundNode VisitPatternSwitchStatement(BoundPatternSwitchStatement node)
        {
            DeclareVariables(node.InnerLocals);
            var result = base.VisitPatternSwitchStatement(node);
            ReportUnusedVariables(node.InnerLocals);
            ReportUnusedVariables(node.InnerLocalFunctions);
            return result;
        }

        protected override void VisitPatternSwitchSection(BoundPatternSwitchSection node, BoundExpression switchExpression, bool isLastSection)
        {
            // TODO: this an probably depend more heavily on the base class implementation.
            DeclareVariables(node.Locals);
            base.VisitPatternSwitchSection(node, switchExpression, isLastSection);
        }

        public override BoundNode VisitLetStatement(BoundLetStatement node)
        {
            CreateSlots(node.Pattern);
            return base.VisitLetStatement(node);
        }

        private void CreateSlots(BoundPattern pattern)
        {
            switch (pattern.Kind)
            {
                case BoundKind.DeclarationPattern:
                    {
                        int slot = GetOrCreateSlot(((BoundDeclarationPattern)pattern).LocalSymbol);
                        break;
                    }
                case BoundKind.PropertyPattern:
                    {
                        var pat = (BoundPropertyPattern)pattern;
                        foreach (var prop in pat.Subpatterns)
                        {
                            CreateSlots(prop.Pattern);
                        }
                        break;
                    }
                case BoundKind.RecursivePattern:
                    {
                        var pat = (BoundRecursivePattern)pattern;
                        foreach (var prop in pat.Patterns)
                        {
                            CreateSlots(prop);
                        }
                        break;
                    }

                case BoundKind.WildcardPattern:
                case BoundKind.ConstantPattern:
                default:
                    break;
            }
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
                switch (local.DeclarationKind)
                {
                    case LocalDeclarationKind.RegularVariable:
                    case LocalDeclarationKind.PatternVariable:
                    DeclareVariable(local);
                        break;

                    default:
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
                        break;
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
                if (symbol.DeclarationKind != LocalDeclarationKind.PatternVariable && !string.IsNullOrEmpty(symbol.Name)) // avoid diagnostics for parser-inserted names
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
                Assign(node, node.InitializerOpt);
            }

            if (node.InitializerOpt != null)
            {
                base.VisitLocalDeclaration(node);
                Assign(node, node.InitializerOpt);
            }
            return null;
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            if (node.Method.MethodKind == MethodKind.LocalFunction)
            {
                CheckAssigned(node.Method.OriginalDefinition, node.Syntax);
            }
            return base.VisitCall(node);
        }

        public override BoundNode VisitConversion(BoundConversion node)
        {
            if (node.ConversionKind == ConversionKind.MethodGroup && node.SymbolOpt?.MethodKind == MethodKind.LocalFunction)
            {
                CheckAssigned(node.SymbolOpt.OriginalDefinition, node.Syntax);
            }
            return base.VisitConversion(node);
        }

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            if (node.MethodOpt?.MethodKind == MethodKind.LocalFunction)
            {
                CheckAssigned(node.MethodOpt.OriginalDefinition, node.Syntax);
            }
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
            return base.VisitMethodGroup(node);
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            return VisitLambdaOrLocalFunction(node);
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            Assign(node, value: null);
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
            return null;
        }

        public override BoundNode VisitParameter(BoundParameter node)
        {
            if (!node.WasCompilerGenerated)
            {
                CheckAssigned(node.ParameterSymbol, node.Syntax);
            }

            return null;
        }

        public override BoundNode VisitAssignmentOperator(BoundAssignmentOperator node)
        {
            base.VisitAssignmentOperator(node);
            Assign(node.Left, node.Right, refKind: node.RefKind);
            return null;
        }

        public override BoundNode VisitIncrementOperator(BoundIncrementOperator node)
        {
            base.VisitIncrementOperator(node);
            Assign(node.Operand, node.Operand);
            return null;
        }

        public override BoundNode VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            base.VisitCompoundAssignmentOperator(node);
            Assign(node.Left, node.Right);
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

            return null;
        }

        protected override void WriteArgument(BoundExpression arg, RefKind refKind, MethodSymbol method)
        {
            if (refKind == RefKind.Ref)
            {
                // Though the method might write the argument, in the case of ref arguments it might not,
                // thus leaving the old value in the variable.  We model this as a read of the argument
                // by the method after the invocation.
                CheckAssigned(arg, arg.Syntax);
            }

            Assign(arg, value: null);

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
                Assign(exceptionSource, value: null, read: false);
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

            return result;
        }

        public override void VisitForEachIterationVariable(BoundForEachStatement node)
        {
            var local = node.IterationVariable;
            if ((object)local != null)
            {
                GetOrCreateSlot(local);
                Assign(node, value: null);
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

            return result;
        }

        public override BoundNode VisitDynamicObjectInitializerMember(BoundDynamicObjectInitializerMember node)
        {
            return null;
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
                Normalize(ref self);
                Normalize(ref other);
            }

            if (other.Assigned[0]) self.Assigned[0] = true;
            for (int slot = 1; slot < self.Assigned.Capacity; slot++)
            {
                if (other.Assigned[slot] && !self.Assigned[slot])
                {
                    SetSlotAssigned(slot, ref self);
                }
            }
        }

        protected override bool IntersectWith(ref LocalState self, ref LocalState other)
        {
            if (self.Reachable == other.Reachable)
            {
                if (self.Assigned.Capacity != other.Assigned.Capacity)
                {
                    Normalize(ref self);
                    Normalize(ref other);
                }

                return self.Assigned.IntersectWith(other.Assigned);
            }
            else if (!self.Reachable)
            {
                self.Assigned = other.Assigned.Clone();
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

            internal LocalState(BitVector assigned)
            {
                this.Assigned = assigned;
                Debug.Assert(!assigned.IsNull);
            }

            /// <summary>
            /// Produce a duplicate of this flow analysis state.
            /// </summary>
            /// <returns></returns>
            public LocalState Clone()
            {
                return new LocalState(Assigned.Clone());
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
