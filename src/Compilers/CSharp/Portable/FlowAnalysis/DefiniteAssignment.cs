// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if DEBUG
// We use a struct rather than a class to represent the state for efficiency
// for data flow analysis, with 32 bits of data inline. Merely copying the state
// variable causes the first 32 bits to be cloned, as they are inline. This can
// hide a plethora of errors that would only be exhibited in programs with more
// than 32 variables to be tracked. However, few of our tests have that many
// variables.
//
// To help diagnose these problems, we use the preprocessor symbol REFERENCE_STATE
// to cause the data flow state be a class rather than a struct. When it is a class,
// this category of problems would be exhibited in programs with a small number of
// tracked variables. But it is slower, so we only do it in DEBUG mode.
#define REFERENCE_STATE
#endif

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Implement C# definite assignment.
    /// </summary>
    internal partial class DefiniteAssignmentPass : LocalDataFlowPass<
        DefiniteAssignmentPass.LocalState,
        DefiniteAssignmentPass.LocalFunctionState>
    {
        /// <summary>
        /// Some variables that should be considered initially assigned.  Used for region analysis.
        /// </summary>
        private readonly HashSet<Symbol> initiallyAssignedVariables;

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

        private readonly PooledHashSet<Symbol> _capturedInside = PooledHashSet<Symbol>.GetInstance();
        private readonly PooledHashSet<Symbol> _capturedOutside = PooledHashSet<Symbol>.GetInstance();

        /// <summary>
        /// The current source assembly.
        /// </summary>
        private readonly SourceAssemblySymbol _sourceAssembly;

        /// <summary>
        /// A set of address-of expressions for which the operand is not definitely assigned.
        /// </summary>
        private readonly HashSet<PrefixUnaryExpressionSyntax> _unassignedVariableAddressOfSyntaxes;

        /// <summary>
        /// Tracks variables for which we have already reported a definite assignment error.  This
        /// allows us to report at most one such error per variable.
        /// </summary>
        private BitVector _alreadyReported;

        /// <summary>
        /// true if we should check to ensure that out parameters are assigned on every exit point.
        /// </summary>
        private readonly bool _requireOutParamsAssigned;

        /// <summary>
        /// Track fields of classes in addition to structs.
        /// </summary>
        private readonly bool _trackClassFields;

        /// <summary>
        /// Track static fields, properties, events, in addition to instance members.
        /// </summary>
        private readonly bool _trackStaticMembers;

        /// <summary>
        /// The topmost method of this analysis.
        /// </summary>
        protected MethodSymbol topLevelMethod;

        protected bool _convertInsufficientExecutionStackExceptionToCancelledByStackGuardException = false; // By default, just let the original exception to bubble up.

        /// <summary>
        /// Check that every rvalue has been converted in the definite assignment pass only (not later passes deriving from it).
        /// </summary>
        private readonly bool _shouldCheckConverted;

        internal DefiniteAssignmentPass(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            bool trackUnassignments = false,
            HashSet<PrefixUnaryExpressionSyntax> unassignedVariableAddressOfSyntaxes = null,
            bool requireOutParamsAssigned = true,
            bool trackClassFields = false,
            bool trackStaticMembers = false)
            : base(compilation, member, node,
                   compilation.FeatureStrictEnabled ? EmptyStructTypeCache.CreatePrecise() : EmptyStructTypeCache.CreateForDev12Compatibility(compilation),
                   trackUnassignments)
        {
            this.initiallyAssignedVariables = null;
            _sourceAssembly = ((object)member == null) ? null : (SourceAssemblySymbol)member.ContainingAssembly;
            this.CurrentSymbol = member;
            _unassignedVariableAddressOfSyntaxes = unassignedVariableAddressOfSyntaxes;
            _requireOutParamsAssigned = requireOutParamsAssigned;
            _trackClassFields = trackClassFields;
            _trackStaticMembers = trackStaticMembers;
            this.topLevelMethod = member as MethodSymbol;
            _shouldCheckConverted = this.GetType() == typeof(DefiniteAssignmentPass);
        }

        internal DefiniteAssignmentPass(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            EmptyStructTypeCache emptyStructs,
            bool trackUnassignments = false,
            HashSet<Symbol> initiallyAssignedVariables = null)
            : base(compilation, member, node,
                   emptyStructs ?? (compilation.FeatureStrictEnabled ? EmptyStructTypeCache.CreatePrecise() : EmptyStructTypeCache.CreateForDev12Compatibility(compilation)),
                   trackUnassignments)
        {
            this.initiallyAssignedVariables = initiallyAssignedVariables;
            _sourceAssembly = ((object)member == null) ? null : (SourceAssemblySymbol)member.ContainingAssembly;
            this.CurrentSymbol = member;
            _unassignedVariableAddressOfSyntaxes = null;
            _requireOutParamsAssigned = true;
            this.topLevelMethod = member as MethodSymbol;
            _shouldCheckConverted = this.GetType() == typeof(DefiniteAssignmentPass);
        }

        /// <summary>
        /// Constructor to be used for region analysis, for which a struct type should never be considered empty.
        /// </summary>
        internal DefiniteAssignmentPass(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            BoundNode firstInRegion,
            BoundNode lastInRegion,
            HashSet<Symbol> initiallyAssignedVariables,
            HashSet<PrefixUnaryExpressionSyntax> unassignedVariableAddressOfSyntaxes,
            bool trackUnassignments)
            : base(compilation, member, node, EmptyStructTypeCache.CreateNeverEmpty(), firstInRegion, lastInRegion, trackRegions: true, trackUnassignments: trackUnassignments)
        {
            this.initiallyAssignedVariables = initiallyAssignedVariables;
            _sourceAssembly = null;
            this.CurrentSymbol = member;
            _unassignedVariableAddressOfSyntaxes = unassignedVariableAddressOfSyntaxes;
            _shouldCheckConverted = this.GetType() == typeof(DefiniteAssignmentPass);
        }

        protected override void Free()
        {
            _usedVariables.Free();
            _usedLocalFunctions.Free();
            _writtenVariables.Free();
            _capturedVariables.Free();
            _capturedInside.Free();
            _capturedOutside.Free();
            _unsafeAddressTakenVariables.Free();

            base.Free();
        }

#if DEBUG
        protected override void VisitRvalue(BoundExpression node, bool isKnownToBeAnLvalue = false)
        {
            Debug.Assert(
                node is null ||
                !_shouldCheckConverted ||
                isKnownToBeAnLvalue ||
                !node.NeedsToBeConverted() ||
                node.WasCompilerGenerated, "expressions should have been converted");
            base.VisitRvalue(node, isKnownToBeAnLvalue);
        }
#endif

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
                    Join(ref savedState, ref this.State);
                }

                this.State = savedState;
            }

            return pendingReturns;
        }

        protected override ImmutableArray<PendingBranch> RemoveReturns()
        {
            var result = base.RemoveReturns();

            if (CurrentSymbol is MethodSymbol currentMethod && currentMethod.IsAsync && !currentMethod.IsImplicitlyDeclared)
            {
                var foundAwait = result.Any(pending => HasAwait(pending));
                if (!foundAwait)
                {
                    // If we're on a LambdaSymbol, then use its 'DiagnosticLocation'.  That will be
                    // much better than using its 'Location' (which is the entire span of the lambda).
                    var diagnosticLocation = CurrentSymbol is LambdaSymbol lambda
                        ? lambda.DiagnosticLocation
                        : CurrentSymbol.Locations[0];

                    Diagnostics.Add(ErrorCode.WRN_AsyncLacksAwaits, diagnosticLocation);
                }
            }

            return result;
        }

        private static bool HasAwait(PendingBranch pending)
        {
            var pendingBranch = pending.Branch;
            if (pendingBranch is null)
            {
                return false;
            }

            BoundKind kind = pendingBranch.Kind;
            switch (kind)
            {
                case BoundKind.AwaitExpression:
                    return true;
                case BoundKind.UsingStatement:
                    var usingStatement = (BoundUsingStatement)pendingBranch;
                    return usingStatement.AwaitOpt != null;
                case BoundKind.ForEachStatement:
                    var foreachStatement = (BoundForEachStatement)pendingBranch;
                    return foreachStatement.AwaitOpt != null;
                case BoundKind.UsingLocalDeclarations:
                    var localDeclaration = (BoundUsingLocalDeclarations)pendingBranch;
                    return localDeclaration.AwaitOpt != null;
                default:
                    return false;
            }
        }

        // For purpose of definite assignment analysis, awaits create pending branches, so async usings and foreachs do too
        public sealed override bool AwaitUsingAndForeachAddsPendingBranch => true;

        protected virtual void ReportUnassignedOutParameter(ParameterSymbol parameter, SyntaxNode node, Location location)
        {
            if (!_requireOutParamsAssigned && ReferenceEquals(topLevelMethod, CurrentSymbol))
            {
                return;
            }

            // If node and location are null "new SourceLocation(node);" will throw a NullReferenceException
            Debug.Assert(node != null || location != null);

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
                        foreach (var field in _emptyStructTypeCache.GetStructInstanceFields(parameter.Type))
                        {
                            if (_emptyStructTypeCache.IsEmptyStructType(field.Type)) continue;

                            if (HasInitializer(field)) continue;

                            int fieldSlot = VariableSlot(field, thisSlot);
                            if (fieldSlot == -1 || !this.State.IsAssigned(fieldSlot))
                            {
                                Symbol associatedPropertyOrEvent = field.AssociatedSymbol;
                                if (associatedPropertyOrEvent?.Kind == SymbolKind.Property)
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

        protected static bool HasInitializer(Symbol field) => field switch
        {
            SourceMemberFieldSymbol f => f.HasInitializer,
            SynthesizedBackingFieldSymbol f => f.HasInitializer,
            SourceFieldLikeEventSymbol e => e.AssociatedEventField?.HasInitializer == true,
            _ => false
        };

        /// <summary>
        /// Perform data flow analysis, reporting all necessary diagnostics.
        /// </summary>
        public static void Analyze(CSharpCompilation compilation, MethodSymbol member, BoundNode node, DiagnosticBag diagnostics, bool requireOutParamsAssigned = true)
        {
            Debug.Assert(diagnostics != null);

            var walker = new DefiniteAssignmentPass(
                compilation,
                member,
                node,
                requireOutParamsAssigned: requireOutParamsAssigned);
            walker._convertInsufficientExecutionStackExceptionToCancelledByStackGuardException = true;

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

            if (compilation.LanguageVersion >= MessageID.IDS_FeatureNullableReferenceTypes.RequiredVersion() && compilation.ShouldRunNullableWalker)
            {
                NullableWalker.Analyze(compilation, member, node, diagnostics);
            }
#if DEBUG
            else
            {
                NullableWalker.Analyze(compilation, member, node, new DiagnosticBag());
            }
#endif
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
            if (CurrentSymbol is SourceMethodSymbol sourceMethod &&
                Symbol.IsCaptured(rangeVariableUnderlyingParameter ?? variable, sourceMethod))
            {
                NoteCaptured(variable);
            }
        }

        /// <summary>
        /// Add the variable to the captured set. For range variables we only add it if inside the region.
        /// </summary>
        /// <param name="variable"></param>
        private void NoteCaptured(Symbol variable)
        {
            if (this.regionPlace == RegionPlace.Inside)
            {
                _capturedInside.Add(variable);
                _capturedVariables.Add(variable);
            }
            else if (variable.Kind != SymbolKind.RangeVariable)
            {
                _capturedOutside.Add(variable);
                _capturedVariables.Add(variable);
            }
        }

        // do not expose PooledHashSet<T> outside of this class
        protected IEnumerable<Symbol> GetCapturedInside() => _capturedInside.ToArray();
        protected IEnumerable<Symbol> GetCapturedOutside() => _capturedOutside.ToArray();
        protected IEnumerable<Symbol> GetCaptured() => _capturedVariables.ToArray();
        protected IEnumerable<Symbol> GetUnsafeAddressTaken() => _unsafeAddressTakenVariables.Keys.ToArray();

        #region Tracking reads/writes of variables for warnings

        protected virtual void NoteRead(
            Symbol variable,
            ParameterSymbol rangeVariableUnderlyingParameter = null)
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
                    _sourceAssembly.NoteFieldAccess((FieldSymbol)variable.OriginalDefinition,
                                                    read: true,
                                                    write: false);
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
                    _sourceAssembly.NoteFieldAccess(field, read: read && WriteConsideredUse(field.Type, value), write: true);
                }

                var local = variable as LocalSymbol;
                if ((object)local != null && read && WriteConsideredUse(local.Type, value))
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
                case BoundKind.DefaultLiteral:
                case BoundKind.DefaultExpression:
                    return false;
                case BoundKind.ObjectCreationExpression:
                    var init = (BoundObjectCreationExpression)value;
                    return !init.Constructor.IsImplicitlyDeclared || init.InitializerExpressionOpt != null;
                case BoundKind.TupleLiteral:
                case BoundKind.ConvertedTupleLiteral:
                    return false;
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
                                _sourceAssembly.NoteFieldAccess(field, read: value == null || WriteConsideredUse(fieldAccess.FieldSymbol.Type, value), write: true);
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
                                    _sourceAssembly.NoteFieldAccess(field, read: value == null || WriteConsideredUse(associatedField.Type, value), write: true);
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

        protected override void Normalize(ref LocalState state)
        {
            int oldNext = state.Assigned.Capacity;
            state.Assigned.EnsureCapacity(nextVariableSlot);
            for (int i = oldNext; i < nextVariableSlot; i++)
            {
                var id = variableBySlot[i];
                int slot = id.ContainingSlot;
                state.Assigned[i] = (slot > 0) &&
                    state.Assigned[slot] &&
                    variableBySlot[slot].Symbol.GetTypeOrReturnType().TypeKind == TypeKind.Struct;
            }
        }

        protected override bool TryGetReceiverAndMember(BoundExpression expr, out BoundExpression receiver, out Symbol member)
        {
            receiver = null;
            member = null;

            switch (expr.Kind)
            {
                case BoundKind.FieldAccess:
                    {
                        var fieldAccess = (BoundFieldAccess)expr;
                        var fieldSymbol = fieldAccess.FieldSymbol;
                        member = fieldSymbol;
                        if (fieldSymbol.IsFixedSizeBuffer)
                        {
                            return false;
                        }
                        if (fieldSymbol.IsStatic)
                        {
                            return _trackStaticMembers;
                        }
                        receiver = fieldAccess.ReceiverOpt;
                        break;
                    }
                case BoundKind.EventAccess:
                    {
                        var eventAccess = (BoundEventAccess)expr;
                        var eventSymbol = eventAccess.EventSymbol;
                        member = eventSymbol.AssociatedField;
                        if (eventSymbol.IsStatic)
                        {
                            return _trackStaticMembers;
                        }
                        receiver = eventAccess.ReceiverOpt;
                        break;
                    }
                case BoundKind.PropertyAccess:
                    {
                        var propAccess = (BoundPropertyAccess)expr;

                        if (Binder.AccessingAutoPropertyFromConstructor(propAccess, this.CurrentSymbol))
                        {
                            var propSymbol = propAccess.PropertySymbol;
                            member = (propSymbol as SourcePropertySymbol)?.BackingField;
                            if (member is null)
                            {
                                return false;
                            }
                            if (propSymbol.IsStatic)
                            {
                                return _trackStaticMembers;
                            }
                            receiver = propAccess.ReceiverOpt;
                        }
                        break;
                    }
            }

            return (object)member != null &&
                (object)receiver != null &&
                receiver.Kind != BoundKind.TypeExpression &&
                MayRequireTrackingReceiverType(receiver.Type);
        }

        private bool MayRequireTrackingReceiverType(TypeSymbol type)
        {
            return (object)type != null &&
                (_trackClassFields || type.TypeKind == TypeKind.Struct);
        }

        protected bool MayRequireTracking(BoundExpression receiverOpt, FieldSymbol fieldSymbol)
        {
            return
                (object)fieldSymbol != null && //simplifies calling pattern for events
                receiverOpt != null &&
                !fieldSymbol.IsStatic &&
                !fieldSymbol.IsFixedSizeBuffer &&
                receiverOpt.Kind != BoundKind.TypeExpression &&
                MayRequireTrackingReceiverType(receiverOpt.Type) &&
                !receiverOpt.Type.IsPrimitiveRecursiveStruct();
        }

        #endregion Tracking reads/writes of variables for warnings

        /// <summary>
        /// Check that the given variable is definitely assigned.  If not, produce an error.
        /// </summary>
        protected void CheckAssigned(Symbol symbol, SyntaxNode node)
        {
            Debug.Assert(!IsConditionalState);
            if ((object)symbol != null)
            {
                NoteRead(symbol);

                if (this.State.Reachable)
                {
                    int slot = VariableSlot(symbol);
                    if (slot >= this.State.Assigned.Capacity) Normalize(ref this.State);
                    if (slot > 0 && !this.State.IsAssigned(slot))
                    {
                        ReportUnassignedIfNotCapturedInLocalFunction(symbol, node, slot);
                    }
                }
            }
        }

        private void ReportUnassignedIfNotCapturedInLocalFunction(Symbol symbol, SyntaxNode node, int slot, bool skipIfUseBeforeDeclaration = true)
        {
            // If the symbol is captured by the nearest
            // local function, record the read and skip the diagnostic
            if (IsCapturedInLocalFunction(slot))
            {
                RecordReadInLocalFunction(slot);
                return;
            }

            ReportUnassigned(symbol, node, slot, skipIfUseBeforeDeclaration);
        }

        /// <summary>
        /// Report a given variable as not definitely assigned.  Once a variable has been so
        /// reported, we suppress further reports of that variable.
        /// </summary>
        protected virtual void ReportUnassigned(Symbol symbol, SyntaxNode node, int slot, bool skipIfUseBeforeDeclaration)
        {
            if (slot <= 0)
            {
                return;
            }

            // If this is a constant, constants are always definitely assigned
            // so we should skip reporting. This can happen in a local function
            // where we use a constant before we actually visit its definition
            // (since local function declarations are visited before other statements)
            // e.g.
            // void M()
            // {
            //   L();
            //   const int x = 0;
            //   int L() => x;
            // }
            if (symbol is LocalSymbol local && local.IsConst)
            {
                return;
            }

            if (slot >= _alreadyReported.Capacity)
            {
                _alreadyReported.EnsureCapacity(nextVariableSlot);
            }

            if (skipIfUseBeforeDeclaration &&
                symbol.Kind == SymbolKind.Local &&
                (symbol.Locations.Length == 0 || node.Span.End < symbol.Locations[0].SourceSpan.Start))
            {
                // We've already reported the use of a local before its declaration.  No need to emit
                // another diagnostic for the same issue.
            }
            else if (!_alreadyReported[slot] && !symbol.GetTypeOrReturnType().Type.IsErrorType())
            {
                // CONSIDER: could suppress this diagnostic in cases where the local was declared in a using
                // or fixed statement because there's a special error code for not initializing those.

                ErrorCode errorCode;
                string symbolName = symbol.Name;

                if (symbol.Kind == SymbolKind.Field)
                {
                    var fieldSymbol = (FieldSymbol)symbol;
                    var associatedSymbol = fieldSymbol.AssociatedSymbol;
                    if (associatedSymbol?.Kind == SymbolKind.Property)
                    {
                        errorCode = ErrorCode.ERR_UseDefViolationProperty;
                        symbolName = associatedSymbol.Name;
                    }
                    else
                    {
                        errorCode = ErrorCode.ERR_UseDefViolationField;
                    }
                }
                else if (symbol.Kind == SymbolKind.Parameter &&
                         ((ParameterSymbol)symbol).RefKind == RefKind.Out)
                {
                    if (((ParameterSymbol)symbol).IsThis)
                    {
                        errorCode = ErrorCode.ERR_UseDefViolationThis;
                    }
                    else
                    {
                        errorCode = ErrorCode.ERR_UseDefViolationOut;
                    }
                }
                else
                {
                    errorCode = ErrorCode.ERR_UseDefViolation;
                }
                Diagnostics.Add(errorCode, new SourceLocation(node), symbolName);
            }

            // mark the variable's slot so that we don't complain about the variable again
            _alreadyReported[slot] = true;
        }

        protected virtual void CheckAssigned(BoundExpression expr, FieldSymbol fieldSymbol, SyntaxNode node)
        {
            if (this.State.Reachable && !IsAssigned(expr, out int unassignedSlot))
            {
                ReportUnassignedIfNotCapturedInLocalFunction(fieldSymbol, node, unassignedSlot);
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
                        if (Binder.AccessingAutoPropertyFromConstructor(propertyAccess, this.CurrentSymbol))
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

        protected void Assign(BoundNode node, BoundExpression value, bool isRef = false, bool read = true)
        {
            AssignImpl(node, value, written: true, isRef: isRef, read: read);
        }

        /// <summary>
        /// Mark a variable as assigned (or unassigned).
        /// </summary>
        /// <param name="node">Node being assigned to.</param>
        /// <param name="value">The value being assigned.</param>
        /// <param name="written">True if target location is considered written to.</param>
        /// <param name="isRef">Ref assignment or value assignment.</param>
        /// <param name="read">True if target location is considered read from.</param>
        protected virtual void AssignImpl(BoundNode node, BoundExpression value, bool isRef, bool written, bool read)
        {
            Debug.Assert(!IsConditionalState);

            switch (node.Kind)
            {
                case BoundKind.DeclarationPattern:
                    {
                        var pattern = (BoundDeclarationPattern)node;
                        var symbol = pattern.Variable as LocalSymbol;
                        if ((object)symbol != null)
                        {
                            // we do not track definite assignment for pattern variables when they are
                            // promoted to fields for top-level code in scripts and interactive
                            int slot = GetOrCreateSlot(symbol);
                            SetSlotState(slot, assigned: written || !this.State.Reachable);
                        }

                        if (written) NoteWrite(pattern.VariableAccess, value, read);
                        break;
                    }

                case BoundKind.RecursivePattern:
                    {
                        var pattern = (BoundRecursivePattern)node;
                        var symbol = pattern.Variable as LocalSymbol;
                        if ((object)symbol != null)
                        {
                            // we do not track definite assignment for pattern variables when they are
                            // promoted to fields for top-level code in scripts and interactive
                            int slot = GetOrCreateSlot(symbol);
                            SetSlotState(slot, assigned: written || !this.State.Reachable);
                        }

                        if (written) NoteWrite(pattern.VariableAccess, value, read);
                        break;
                    }

                case BoundKind.LocalDeclaration:
                    {
                        var local = (BoundLocalDeclaration)node;
                        Debug.Assert(local.InitializerOpt == value || value == null);
                        LocalSymbol symbol = local.LocalSymbol;
                        int slot = GetOrCreateSlot(symbol);
                        SetSlotState(slot, assigned: written || !this.State.Reachable);
                        if (written) NoteWrite(symbol, value, read);
                        break;
                    }

                case BoundKind.Local:
                    {
                        var local = (BoundLocal)node;
                        if (local.LocalSymbol.RefKind != RefKind.None && !isRef)
                        {
                            // Writing through the (reference) value of a reference local
                            // requires us to read the reference itself.
                            if (written) VisitRvalue(local, isKnownToBeAnLvalue: true);
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
                    {
                        var paramExpr = (BoundParameter)node;
                        var param = paramExpr.ParameterSymbol;
                        // If we're ref-reassigning an out parameter we're effectively
                        // leaving the original
                        if (isRef && param.RefKind == RefKind.Out)
                        {
                            LeaveParameter(param, node.Syntax, paramExpr.Syntax.Location);
                        }

                        int slot = MakeSlot(paramExpr);
                        SetSlotState(slot, written);
                        if (written) NoteWrite(paramExpr, value, read);
                        break;
                    }

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
                    AssignImpl(((BoundRangeVariable)node).Value, value, isRef, written, read);
                    break;

                case BoundKind.BadExpression:
                    {
                        // Sometimes a bad node is not so bad that we cannot analyze it at all.
                        var bad = (BoundBadExpression)node;
                        if (!bad.ChildBoundNodes.IsDefault && bad.ChildBoundNodes.Length == 1)
                        {
                            AssignImpl(bad.ChildBoundNodes[0], value, isRef, written, read);
                        }
                        break;
                    }

                case BoundKind.TupleLiteral:
                case BoundKind.ConvertedTupleLiteral:
                    ((BoundTupleExpression)node).VisitAllElements((x, self) => self.Assign(x, value: null, isRef: isRef), this);
                    break;

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
            TypeSymbol structType = variable.Symbol.GetTypeOrReturnType().Type;
            foreach (var field in _emptyStructTypeCache.GetStructInstanceFields(structType))
            {
                if (_emptyStructTypeCache.IsEmptyStructType(field.Type)) continue;
                int slot = VariableSlot(field, containingSlot);
                if (slot == -1 || !state.IsAssigned(slot)) return false;
            }

            return true;
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
            TypeSymbol type = id.Symbol.GetTypeOrReturnType().Type;
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
            TypeSymbol type = id.Symbol.GetTypeOrReturnType().Type;
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
            if (NonMonotonicState.HasValue)
            {
                var state = NonMonotonicState.Value;
                SetSlotUnassigned(slot, ref state);
                NonMonotonicState = state;
            }

            SetSlotUnassigned(slot, ref this.State);
        }

        protected override LocalState TopState()
        {
            return new LocalState(BitVector.Empty);
        }

        protected override LocalState ReachableBottomState()
        {
            var result = new LocalState(BitVector.AllSet(nextVariableSlot));
            result.Assigned[0] = false; // make the state reachable
            return result;
        }

        protected override void EnterParameter(ParameterSymbol parameter)
        {
            int slot = GetOrCreateSlot(parameter);
            if (parameter.RefKind == RefKind.Out && !(this.CurrentSymbol is MethodSymbol currentMethod && currentMethod.IsAsync)) // out parameters not allowed in async
            {
                if (slot > 0) SetSlotState(slot, initiallyAssignedVariables?.Contains(parameter) == true);
            }
            else
            {
                // this code has no effect except in region analysis APIs such as DataFlowsOut where we unassign things
                if (slot > 0) SetSlotState(slot, true);
                NoteWrite(parameter, value: null, read: true);
            }
        }

        protected override void LeaveParameters(ImmutableArray<ParameterSymbol> parameters, SyntaxNode syntax, Location location)
        {
            Debug.Assert(!this.IsConditionalState);
            if (!this.State.Reachable)
            {
                // if the code is not reachable, then it doesn't matter if out parameters are assigned.
                return;
            }
            base.LeaveParameters(parameters, syntax, location);
        }

        protected override void LeaveParameter(ParameterSymbol parameter, SyntaxNode syntax, Location location)
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

        public override void VisitPattern(BoundPattern pattern)
        {
            base.VisitPattern(pattern);
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
                        Assign(pat, value: null, isRef: false, read: false);
                        break;
                    }
                case BoundKind.DiscardPattern:
                    break;
                case BoundKind.ConstantPattern:
                    {
                        var pat = (BoundConstantPattern)pattern;
                        this.VisitRvalue(pat.Value);
                        break;
                    }
                case BoundKind.RecursivePattern:
                    {
                        var pat = (BoundRecursivePattern)pattern;
                        if (!pat.Deconstruction.IsDefaultOrEmpty)
                        {
                            foreach (var subpat in pat.Deconstruction)
                            {
                                AssignPatternVariables(subpat.Pattern);
                            }
                        }
                        if (!pat.Properties.IsDefaultOrEmpty)
                        {
                            foreach (BoundSubpattern sub in pat.Properties)
                            {
                                AssignPatternVariables(sub.Pattern);
                            }
                        }
                        Assign(pat, null, false, false);
                        break;
                    }
                case BoundKind.ITuplePattern:
                    {
                        var pat = (BoundITuplePattern)pattern;
                        foreach (var subpat in pat.Subpatterns)
                        {
                            AssignPatternVariables(subpat.Pattern);
                        }
                        break;
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(pattern.Kind);
            }
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            DeclareVariables(node.Locals);

            VisitStatementsWithLocalFunctions(node);

            // any local using symbols are implicitly read at the end of the block when they get disposed
            foreach (var local in node.Locals)
            {
                if (local.IsUsing)
                {
                    NoteRead(local);
                }
            }

            ReportUnusedVariables(node.Locals);
            ReportUnusedVariables(node.LocalFunctions);

            return null;
        }

        private void VisitStatementsWithLocalFunctions(BoundBlock block)
        {
            if (!TrackingRegions && !block.LocalFunctions.IsDefaultOrEmpty)
            {
                // Visit the statements in two phases:
                //   1. Local function declarations
                //   2. Everything else
                //
                // The idea behind visiting local functions first is
                // that we may be able to gather the captured variables
                // they read and write ahead of time in a single pass, so
                // when they are used by other statements in the block we
                // won't have to recompute the set by doing multiple passes.
                //
                // If the local functions contain forward calls to other local
                // functions then we may have to do another pass regardless,
                // but hopefully that will be an uncommon case in real-world code.

                // First phase
                foreach (var stmt in block.Statements)
                {
                    if (stmt.Kind == BoundKind.LocalFunctionStatement)
                    {
                        VisitAlways(stmt);
                    }
                }

                // Second phase
                foreach (var stmt in block.Statements)
                {
                    if (stmt.Kind != BoundKind.LocalFunctionStatement)
                    {
                        VisitStatement(stmt);
                    }
                }
            }
            else
            {
                foreach (var stmt in block.Statements)
                {
                    VisitStatement(stmt);
                }
            }
        }

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            DeclareVariables(node.InnerLocals);
            var result = base.VisitSwitchStatement(node);
            ReportUnusedVariables(node.InnerLocals);
            ReportUnusedVariables(node.InnerLocalFunctions);
            return result;
        }

        protected override void VisitSwitchSection(BoundSwitchSection node, bool isLastSection)
        {
            DeclareVariables(node.Locals);
            base.VisitSwitchSection(node, isLastSection);
        }

        public override BoundNode VisitForStatement(BoundForStatement node)
        {
            DeclareVariables(node.OuterLocals);
            DeclareVariables(node.InnerLocals);
            var result = base.VisitForStatement(node);
            ReportUnusedVariables(node.InnerLocals);
            ReportUnusedVariables(node.OuterLocals);
            return result;
        }

        public override BoundNode VisitDoStatement(BoundDoStatement node)
        {
            DeclareVariables(node.Locals);
            var result = base.VisitDoStatement(node);
            ReportUnusedVariables(node.Locals);
            return result;
        }

        public override BoundNode VisitWhileStatement(BoundWhileStatement node)
        {
            DeclareVariables(node.Locals);
            var result = base.VisitWhileStatement(node);
            ReportUnusedVariables(node.Locals);
            return result;
        }

        /// <remarks>
        /// Variables declared in a using statement are always considered used, so this is just an assert.
        /// </remarks>
        public override BoundNode VisitUsingStatement(BoundUsingStatement node)
        {
            var localsOpt = node.Locals;
            DeclareVariables(localsOpt);
            var result = base.VisitUsingStatement(node);
            if (!localsOpt.IsDefaultOrEmpty)
            {
                foreach (LocalSymbol local in localsOpt)
                {
                    if (local.DeclarationKind == LocalDeclarationKind.UsingVariable)
                    {
                        // At the end of the statement, there's an implied read when the local is disposed
                        NoteRead(local);
                        Debug.Assert(_usedVariables.Contains(local));
                    }
                }
            }

            return result;
        }

        public override BoundNode VisitFixedStatement(BoundFixedStatement node)
        {
            DeclareVariables(node.Locals);
            return base.VisitFixedStatement(node);
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
                initiallyAssignedVariables?.Contains(symbol) == true;
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
                    Diagnostics.Add(ErrorCode.WRN_UnreferencedLocalFunction, symbol.Locations[0], symbol.Name);
                }
            }
        }

        public override BoundNode VisitLocal(BoundLocal node)
        {
            // Note: the caller should avoid allowing this to be called for the left-hand-side of
            // an assignment (if a simple variable or this-qualified or deconstruction variables) or an out parameter.
            // That's because this code assumes the variable is being read, not written.
            LocalSymbol localSymbol = node.LocalSymbol;
            CheckAssigned(localSymbol, node.Syntax);

            if (localSymbol.IsFixed && this.CurrentSymbol is MethodSymbol currentMethod &&
                (currentMethod.MethodKind == MethodKind.AnonymousFunction ||
                 currentMethod.MethodKind == MethodKind.LocalFunction) &&
                _capturedVariables.Contains(localSymbol))
            {
                Diagnostics.Add(ErrorCode.ERR_FixedLocalInLambda, new SourceLocation(node.Syntax), localSymbol);
            }
            return null;
        }

        public override BoundNode VisitLocalDeclaration(BoundLocalDeclaration node)
        {
            int slot = GetOrCreateSlot(node.LocalSymbol); // not initially assigned
            if (initiallyAssignedVariables?.Contains(node.LocalSymbol) == true)
            {
                // When data flow analysis determines that the variable is sometimes
                // used without being assigned first, we want to treat that variable, during region analysis,
                // as assigned at its point of declaration.
                Assign(node, value: null);
            }

            var result = base.VisitLocalDeclaration(node);
            if (node.InitializerOpt != null)
            {
                Assign(node, node.InitializerOpt);
            }
            return result;
        }

        public override BoundNode VisitMethodGroup(BoundMethodGroup node)
        {
            foreach (var method in node.Methods)
            {
                if (method.MethodKind == MethodKind.LocalFunction)
                {
                    _usedLocalFunctions.Add((LocalFunctionSymbol)method);
                }
            }
            return base.VisitMethodGroup(node);
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            var oldSymbol = this.CurrentSymbol;
            this.CurrentSymbol = node.Symbol;

            var oldPending = SavePending(); // we do not support branches into a lambda

            // State after the lambda declaration
            LocalState stateAfterLambda = this.State;

            this.State = this.State.Reachable ? this.State.Clone() : ReachableBottomState();

            if (!node.WasCompilerGenerated) EnterParameters(node.Symbol.Parameters);
            var oldPending2 = SavePending();
            VisitAlways(node.Body);
            RestorePending(oldPending2); // process any forward branches within the lambda body
            ImmutableArray<PendingBranch> pendingReturns = RemoveReturns();
            RestorePending(oldPending);
            LeaveParameters(node.Symbol.Parameters, node.Syntax, null);

            Join(ref stateAfterLambda, ref this.State); // a no-op except in region analysis
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

                Join(ref stateAfterLambda, ref this.State); // a no-op except in region analysis
            }

            this.State = stateAfterLambda;

            this.CurrentSymbol = oldSymbol;
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
            Assign(node.Left, node.Right, isRef: node.IsRef);
            return null;
        }

        public override BoundNode VisitDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator node)
        {
            base.VisitDeconstructionAssignmentOperator(node);
            Assign(node.Left, node.Right);
            return null;
        }

        public override BoundNode VisitIncrementOperator(BoundIncrementOperator node)
        {
            base.VisitIncrementOperator(node);
            Assign(node.Operand, value: node);
            return null;
        }

        public override BoundNode VisitCompoundAssignmentOperator(BoundCompoundAssignmentOperator node)
        {
            VisitCompoundAssignmentTarget(node);
            VisitRvalue(node.Right);
            AfterRightHasBeenVisited(node);
            Assign(node.Left, value: node);
            return null;
        }

        public override BoundNode VisitFixedLocalCollectionInitializer(BoundFixedLocalCollectionInitializer node)
        {
            var initializer = node.Expression;

            if (initializer.Kind == BoundKind.AddressOfOperator)
            {
                initializer = ((BoundAddressOfOperator)initializer).Operand;
            }

            // If the node is a fixed statement address-of operator (e.g. fixed(int *p = &...)),
            // then we don't need to consider it for membership in unsafeAddressTakenVariables,
            // because it is either not a local/parameter/range variable (if the variable is
            // non-moveable) or it is and it has a RefKind other than None, in which case it can't
            // be referred to in a lambda (i.e. can't be captured).
            VisitAddressOfOperand(initializer, shouldReadOperand: false);
            return null;
        }

        public override BoundNode VisitAddressOfOperator(BoundAddressOfOperator node)
        {
            BoundExpression operand = node.Operand;
            bool shouldReadOperand = false;

            Symbol variable = UseNonFieldSymbolUnsafely(operand);
            if ((object)variable != null)
            {
                // The goal here is to treat address-of as a read in cases where
                // we (a) care about a read happening (e.g. for DataFlowsIn) and
                // (b) have information indicating that this will not result in
                // a read to an unassigned variable (i.e. the operand is definitely
                // assigned).
                if (_unassignedVariableAddressOfSyntaxes?.Contains(node.Syntax as PrefixUnaryExpressionSyntax) == false)
                {
                    shouldReadOperand = true;
                }

                if (!_unsafeAddressTakenVariables.ContainsKey(variable))
                {
                    _unsafeAddressTakenVariables.Add(variable, node.Syntax.Location);
                }
            }

            VisitAddressOfOperand(node.Operand, shouldReadOperand);

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

        protected void CheckAssigned(BoundExpression expr, SyntaxNode node)
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
                    if (!symbol.IsFixedSizeBuffer && MayRequireTracking(field.ReceiverOpt, symbol))
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
            }
        }

        private void MarkFieldsUsed(TypeSymbol type)
        {
            switch (type.TypeKind)
            {
                case TypeKind.Array:
                    MarkFieldsUsed(((ArrayTypeSymbol)type).ElementType);
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
                            MarkFieldsUsed(field.Type);
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

        protected override void VisitCatchBlock(BoundCatchBlock catchBlock, ref LocalState finallyState)
        {
            DeclareVariables(catchBlock.Locals);

            var exceptionSource = catchBlock.ExceptionSourceOpt;
            if (exceptionSource != null)
            {
                Assign(exceptionSource, value: null, read: false);
            }

            base.VisitCatchBlock(catchBlock, ref finallyState);

            foreach (var local in catchBlock.Locals)
            {
                ReportIfUnused(local, assigned: local.DeclarationKind != LocalDeclarationKind.CatchVariable);
            }
        }

        public override BoundNode VisitFieldAccess(BoundFieldAccess node)
        {
            var result = base.VisitFieldAccess(node);
            NoteRead(node.FieldSymbol);

            if (node.FieldSymbol.IsFixedSizeBuffer && node.Syntax != null && !SyntaxFacts.IsFixedStatementExpression(node.Syntax))
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
            if (Binder.AccessingAutoPropertyFromConstructor(node, this.CurrentSymbol))
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
                            ReportUnassignedIfNotCapturedInLocalFunction(backingField, node.Syntax, unassignedSlot);
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

        public override void VisitForEachIterationVariables(BoundForEachStatement node)
        {
            // declare and assign all iteration variables
            foreach (var iterationVariable in node.IterationVariables)
            {
                Debug.Assert((object)iterationVariable != null);
                int slot = GetOrCreateSlot(iterationVariable);
                if (slot > 0) SetSlotAssigned(slot);
                // NOTE: do not report unused iteration variables. They are always considered used.
                NoteWrite(iterationVariable, null, read: true);
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

        protected override void VisitAssignmentOfNullCoalescingAssignment(
            BoundNullCoalescingAssignmentOperator node,
            BoundPropertyAccess propertyAccessOpt)
        {
            base.VisitAssignmentOfNullCoalescingAssignment(node, propertyAccessOpt);
            Assign(node.LeftOperand, node.RightOperand);
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

        protected override bool Meet(ref LocalState self, ref LocalState other)
        {
            if (self.Assigned.Capacity != other.Assigned.Capacity)
            {
                Normalize(ref self);
                Normalize(ref other);
            }

            if (!other.Reachable)
            {
                self.Assigned[0] = true;
                return true;
            }

            bool changed = false;
            for (int slot = 1; slot < self.Assigned.Capacity; slot++)
            {
                if (other.Assigned[slot] && !self.Assigned[slot])
                {
                    SetSlotAssigned(slot, ref self);
                    changed = true;
                }
            }
            return changed;
        }

        protected override bool Join(ref LocalState self, ref LocalState other)
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

#if REFERENCE_STATE
        internal class LocalState : ILocalState
#else
        internal struct LocalState : ILocalState
#endif
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
