
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp;

internal sealed class TransientInlineArrayRewriter : BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
{
    private static readonly ObjectPool<Dictionary<TypeSymbol, AllocatedArrayInfo>> s_allocatedLocalsPool =
        new ObjectPool<Dictionary<TypeSymbol, AllocatedArrayInfo>>(() => new Dictionary<TypeSymbol, AllocatedArrayInfo>(comparer: Symbols.SymbolEqualityComparer.IgnoringTupleNamesAndNullability), 10);

    private readonly Dictionary<TypeSymbol, AllocatedArrayInfo> _allocatedArrayLocals;
    private readonly SyntheticBoundNodeFactory _factory;
    private readonly BindingDiagnosticBag _diagnostics;
    private CachedWellKnownInfo _cachedWellKnownInfo;

    private TransientInlineArrayRewriter(Dictionary<TypeSymbol, AllocatedArrayInfo> allocatedArrayLocals, SyntheticBoundNodeFactory factory, BindingDiagnosticBag diagnostics, CachedWellKnownInfo cachedWellKnownInfo)
    {
        _allocatedArrayLocals = allocatedArrayLocals;
        _factory = factory;
        _diagnostics = diagnostics;
        _cachedWellKnownInfo = cachedWellKnownInfo;
    }

    public static BoundStatement Rewrite(BoundStatement node, ImmutableArray<TransientInlineArrayInfo> allocatedArrays, SyntheticBoundNodeFactory factory, BindingDiagnosticBag diagnostics)
    {
        Debug.Assert(!allocatedArrays.IsDefault);
        Debug.Assert(diagnostics.DiagnosticBag is not null);
        if (allocatedArrays is [])
        {
            return node;
        }

        var cachedWellKnownInfo = new CachedWellKnownInfo(factory, diagnostics.DiagnosticBag);
        var allocatedArrayLocals = AllocateArrays(allocatedArrays, node.Syntax, factory, ref cachedWellKnownInfo, diagnostics);
        var rewriter = new TransientInlineArrayRewriter(allocatedArrayLocals, factory, diagnostics, cachedWellKnownInfo);
        var result = (BoundStatement)rewriter.Visit(node);

        if (allocatedArrayLocals.Count > 0)
        {
            result = factory.Block(
                locals: allocatedArrayLocals.SelectAsArray(kvp => kvp.Value.Local),
                statements: result);
        }

        allocatedArrayLocals.Clear();
        s_allocatedLocalsPool.Free(allocatedArrayLocals);
        return result;
    }

    private static Dictionary<TypeSymbol, AllocatedArrayInfo> AllocateArrays(ImmutableArray<TransientInlineArrayInfo> allocatedArrays, SyntaxNode syntax, SyntheticBoundNodeFactory factory, ref CachedWellKnownInfo cachedWellKnownInfo, BindingDiagnosticBag diagnostics)
    {
        Debug.Assert(factory.ModuleBuilderOpt is not null);
        var allocatedArrayLocals = s_allocatedLocalsPool.Allocate();
#if DEBUG
        var actualCount = allocatedArrays.Select(a => a.ElementType).Distinct(Symbols.SymbolEqualityComparer.IgnoringTupleNamesAndNullability).Count();
        Debug.Assert(actualCount == allocatedArrays.Length);
#endif
        foreach (var arrayInfo in allocatedArrays)
        {
            Debug.Assert(arrayInfo.NumUses > 0);
            Debug.Assert(arrayInfo.MaxSpace > 0);
            if (arrayInfo.NumUses == 1)
            {
                // If the array is only used once, then we don't need a shared value. The rewrite will create the local
                // in place at the call site.
                continue;
            }

            if (tryUseSingleElement(arrayInfo, ref cachedWellKnownInfo, out var singleLocal))
            {
                allocatedArrayLocals.Add(arrayInfo.ElementType, new AllocatedArrayInfo(singleLocal, isArray: false, currentStart: 0, length: 1));
            }
            else
            {
                // Otherwise, we need to allocate an array.
                var inlineArrayType = factory.ModuleBuilderOpt.EnsureInlineArrayTypeExists(syntax, factory, arrayInfo.MaxSpace, diagnostics).Construct(ImmutableArray.Create(arrayInfo.ElementType));

                var arrayLocal = factory.SynthesizedLocal(
                    inlineArrayType,
                    kind: SynthesizedLocalKind.SharedInlineArraySpace,
                    syntax: syntax);
                allocatedArrayLocals.Add(arrayInfo.ElementType, new AllocatedArrayInfo(arrayLocal, isArray: true, currentStart: 0, length: arrayInfo.MaxSpace));
            }
        }

        return allocatedArrayLocals;

        bool tryUseSingleElement(TransientInlineArrayInfo arrayInfo, ref CachedWellKnownInfo cachedWellKnownInfo, [NotNullWhen(true)] out LocalSymbol? singleLocal)
        {
            singleLocal = null;

            // If the array only needs a single element, we can potentially just use a single element, assuming the right
            // span/readonlyspan constructor is available.
            if (arrayInfo.MaxSpace != 1)
            {
                return false;
            }

            if (arrayInfo.NeedsReadOnly && cachedWellKnownInfo.ReadOnlySpanRefConstructor is null
                || arrayInfo.NeedsMutable && cachedWellKnownInfo.SpanRefConstructor is null)
            {
                return false;
            }

            singleLocal = factory.SynthesizedLocal(
                arrayInfo.ElementType,
                kind: SynthesizedLocalKind.SharedInlineArraySpace,
                syntax: syntax);
            return true;
        }
    }

    public override BoundNode? VisitCall(BoundCall node)
    {
        if (!node.Arguments.Any(a => a is BoundTransientSpanFromInlineArray))
        {
            return base.VisitCall(node);
        }

        var rewrittenReceiver = (BoundExpression?)Visit(node.ReceiverOpt);
        var rewrittenArguments = ArrayBuilder<BoundExpression>.GetInstance(node.Arguments.Length);
        var allocatedSpace = ArrayBuilder<(TypeSymbol elementType, int length)>.GetInstance();
        ArrayBuilder<LocalSymbol>? temps = null;
        ArrayBuilder<BoundExpression>? clears = null;
        foreach (var argument in node.Arguments)
        {
            if (argument is not BoundTransientSpanFromInlineArray transientSpan)
            {
                rewrittenArguments.Add((BoundExpression)Visit(argument));
                continue;
            }

            var rewrittenElements = transientSpan.Elements.SelectAsArray(e => (BoundExpression)Visit(e));

            if (!_allocatedArrayLocals.TryGetValue(transientSpan.ElementType, out var allocatedInfo))
            {
                // The array was only used once, so we can create a local for it here.
                temps ??= ArrayBuilder<LocalSymbol>.GetInstance();
                rewrittenArguments.Add(LocalRewriter.CreateCreateAndPopulateSpanFromInlineArray(
                    transientSpan.Syntax,
                    TypeWithAnnotations.Create(transientSpan.ElementType),
                    transientSpan.Elements,
                    transientSpan.IsReadOnly,
                    _factory,
                    _factory.Compilation,
                    temps,
                    _diagnostics));
            }
            else
            {
                Debug.Assert(allocatedInfo.CurrentStart + transientSpan.Elements.Length <= allocatedInfo.Length, $"{allocatedInfo.CurrentStart} + {transientSpan.Elements.Length} <= {allocatedInfo.Length}");
                var start = allocatedInfo.CurrentStart;
                allocatedInfo.CurrentStart += transientSpan.Elements.Length;
                var needsClear = transientSpan.ElementType.IsManagedTypeNoUseSiteDiagnostics;
                allocatedSpace.Add((transientSpan.ElementType, transientSpan.Elements.Length));

                // If this is a single element local, create a span from the local
                if (!allocatedInfo.IsArray)
                {
                    // globalTemp = <element expression>;
                    // new Span<T>(ref globalTemp);
                    Debug.Assert(allocatedInfo.Length == 1);
                    Debug.Assert(rewrittenElements.Length == 1);
                    var local = _factory.Local(allocatedInfo.Local);
                    var constructor = transientSpan.IsReadOnly ? _cachedWellKnownInfo.ReadOnlySpanRefConstructor : _cachedWellKnownInfo.SpanRefConstructor;
                    Debug.Assert(constructor is not null);
                    constructor = constructor.AsMember(constructor.ContainingType.Construct(transientSpan.ElementType));
                    rewrittenArguments.Add(
                        _factory.Sequence(
                            sideEffects: [_factory.AssignmentExpression(local, rewrittenElements[0])],
                            result: _factory.New(
                                constructor,
                                arguments: [local],
                                argumentRefKinds: [transientSpan.IsReadOnly ? RefKindExtensions.StrictIn : RefKind.Ref])));
                    if (needsClear)
                    {
                        // We want to clear the local after the call to make sure that no references are held longer than necessary.
                        clears ??= ArrayBuilder<BoundExpression>.GetInstance();
                        clears.Add(_factory.AssignmentExpression(local, _factory.Default(local.Type)));
                    }

                    continue;
                }

                // Otherwise, we need to populate the shared inline array and take a span over the correct portion.
                Debug.Assert(allocatedInfo.Local.Type.HasInlineArrayAttribute(out _));
                var inlineArrayLocal = _factory.Local(allocatedInfo.Local);
                var elementRef = _cachedWellKnownInfo.InlineArrayElementRef.Construct(allocatedInfo.Local.Type, transientSpan.ElementType);
                var sideEffects = ArrayBuilder<BoundExpression>.GetInstance();

                // Populate the inline array.
                // InlineArrayElementRef<<>y__InlineArrayN<ElementType>, ElementType>(ref tmp, start + 0) = element0;
                // InlineArrayElementRef<<>y__InlineArrayN<ElementType>, ElementType>(ref tmp, start + 1) = element1;
                // ...
                for (int i = 0; i < rewrittenElements.Length; i++)
                {
                    var element = rewrittenElements[i];
                    var call = _factory.Call(null, elementRef, inlineArrayLocal, _factory.Literal(start + i), useStrictArgumentRefKinds: true);
                    var assignment = _factory.AssignmentExpression(call, element);
                    sideEffects.Add(assignment);
                }

                // Get a span to the inline array.
                // ... InlineArrayAsReadOnlySpan<<>y__InlineArrayN<ElementType>, ElementType>(in tmp, N)
                // or
                // ... InlineArrayAsSpan<<>y__InlineArrayN<ElementType>, ElementType>(ref tmp, N)
                MethodSymbol inlineArrayAsSpan = transientSpan.IsReadOnly ?
                    _cachedWellKnownInfo.InlineArrayAsReadOnlySpan :
                    _cachedWellKnownInfo.InlineArrayAsSpan;
                inlineArrayAsSpan = inlineArrayAsSpan.Construct(inlineArrayLocal.Type, transientSpan.ElementType);
                var span = _factory.Call(
                    receiver: null,
                    inlineArrayAsSpan,
                    inlineArrayLocal,
                    _factory.Literal(allocatedInfo.Length),
                    useStrictArgumentRefKinds: true);

                // If necessary, slice the array to the correct length.
                if (start != 0 || rewrittenElements.Length != allocatedInfo.Length)
                {
                    sideEffects.Add(span);
                    // ... .Slice(start, actualLength)
                    var sliceMethod = transientSpan.IsReadOnly ?
                        _cachedWellKnownInfo.ReadOnlySpanTSliceMethod :
                        _cachedWellKnownInfo.SpanTSliceMethod;
                    sliceMethod = sliceMethod.AsMember(sliceMethod.ContainingType.Construct(transientSpan.ElementType));
                    span = _factory.Call(
                        receiver: span,
                        sliceMethod,
                        _factory.Literal(start),
                        _factory.Literal(rewrittenElements.Length));
                }

                rewrittenArguments.Add(
                    _factory.Sequence(
                        locals: [],
                        sideEffects: sideEffects.ToImmutableAndFree(),
                        result: span));

                if (needsClear)
                {
                    clears ??= ArrayBuilder<BoundExpression>.GetInstance();
                    for (int i = 0; i < rewrittenElements.Length; i++)
                    {
                        var call = _factory.Call(null, elementRef, inlineArrayLocal, _factory.Literal(start + i), useStrictArgumentRefKinds: true);
                        clears.Add(_factory.AssignmentExpression(call, _factory.Default(call.Type)));
                    }
                }
            }
        }

        var rewrittenCall = node.Update(
            receiverOpt: rewrittenReceiver,
            initialBindingReceiverIsSubjectToCloning: node.InitialBindingReceiverIsSubjectToCloning,
            method: node.Method,
            arguments: rewrittenArguments.ToImmutableAndFree(),
            argumentNamesOpt: node.ArgumentNamesOpt,
            argumentRefKindsOpt: node.ArgumentRefKindsOpt,
            node.IsDelegateCall,
            expanded: node.Expanded,
            invokedAsExtensionMethod: node.InvokedAsExtensionMethod,
            argsToParamsOpt: node.ArgsToParamsOpt,
            node.DefaultArguments,
            resultKind: node.ResultKind,
            type: node.Type);

        foreach (var (elementType, length) in allocatedSpace)
        {
            // Return the allocated space.
            Debug.Assert(_allocatedArrayLocals.ContainsKey(elementType));
            var allocatedInfo = _allocatedArrayLocals[elementType];
            allocatedInfo.CurrentStart -= length;
            Debug.Assert(allocatedInfo.CurrentStart >= 0);
        }
        allocatedSpace.Free();

        if (clears is null && temps is null)
        {
            return rewrittenCall;
        }

        if (clears is null)
        {
            Debug.Assert(temps is not null);
            // We don't need to clear anything, just wrap with a sequence for the temps.
            return _factory.Sequence(
                locals: temps.ToImmutableAndFree(),
                sideEffects: [],
                result: rewrittenCall);
        }

        temps ??= ArrayBuilder<LocalSymbol>.GetInstance();
        // We need to clear some values after the call.
        // PROTOTYPE: Handle unused result, void-returning rewrittenCall, etc.
        var resultTemp = _factory.StoreToTemp(rewrittenCall, out var resultStore);
        temps.Add(resultTemp.LocalSymbol);
        clears.Insert(0, resultStore);

        return _factory.Sequence(
            locals: temps.ToImmutableAndFree(),
            sideEffects: clears.ToImmutableAndFree(),
            result: resultTemp);
    }

    public override BoundNode? VisitTransientSpanFromInlineArray(BoundTransientSpanFromInlineArray node)
    {
        // Should be handled during visiting method arguments
        throw ExceptionUtilities.Unreachable();
    }

    private class AllocatedArrayInfo(LocalSymbol local, bool isArray, int currentStart, int length)
    {
        public readonly LocalSymbol Local = local;
        public readonly bool IsArray = isArray;
        public int CurrentStart = currentStart;
        public readonly int Length = length;
    }

    private struct CachedWellKnownInfo(SyntheticBoundNodeFactory factory, DiagnosticBag diagnostics)
    {
        private readonly SyntheticBoundNodeFactory _factory = factory;
        private readonly DiagnosticBag _diagnostics = diagnostics;

        private bool? _hasSingleElementReadonlySpanCtor;
        public MethodSymbol? ReadOnlySpanRefConstructor
        {
            get
            {
                CheckMemberAndInitializeField(ref _hasSingleElementReadonlySpanCtor, WellKnownMember.System_ReadOnlySpan_T__ctor_ref_readonly_T, ref field);
                return field;
            }
        }

        private bool? _hasSingleElementSpanCtor;
        public MethodSymbol? SpanRefConstructor
        {
            get
            {
                CheckMemberAndInitializeField(ref _hasSingleElementSpanCtor, WellKnownMember.System_Span_T__ctor_ref_T, ref field);
                return field;
            }
        }

        private readonly void CheckMemberAndInitializeField(ref bool? hasMember, WellKnownMember member, ref MethodSymbol? field)
        {
            if (hasMember is null)
            {
                if (_factory.WellKnownMember(member) is MethodSymbol ctor)
                {
                    hasMember = true;
                    field = ctor;
                }
                else
                {
                    hasMember = false;
                }
            }
        }

        public MethodSymbol InlineArrayElementRef
        {
            get
            {
                Debug.Assert(_factory.ModuleBuilderOpt is not null);
                field ??=
                    _factory.ModuleBuilderOpt.EnsureInlineArrayElementRefExists(
                        _factory.Syntax,
                        Int32Type,
                        _diagnostics);
                return field;
            }
        }

        public MethodSymbol InlineArrayAsReadOnlySpan
        {
            get
            {
                Debug.Assert(_factory.ModuleBuilderOpt is not null);
                field ??=
                    _factory.ModuleBuilderOpt.EnsureInlineArrayAsReadOnlySpanExists(
                        _factory.Syntax,
                        ReadOnlySpanTType,
                        Int32Type,
                        _diagnostics);
                return field;
            }
        }

        public MethodSymbol InlineArrayAsSpan
        {
            get
            {
                Debug.Assert(_factory.ModuleBuilderOpt is not null);
                field ??=
                    _factory.ModuleBuilderOpt.EnsureInlineArrayAsSpanExists(
                        _factory.Syntax,
                        SpanTType,
                        Int32Type,
                        _diagnostics);
                return field;
            }
        }

        public MethodSymbol ReadOnlySpanTSliceMethod
        {
            get
            {
                // PROTOTYPE: Handle nulls?
                field ??= (MethodSymbol)_factory.Compilation.GetWellKnownTypeMember(WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int)!;
                return field;
            }
        }

        public MethodSymbol SpanTSliceMethod
        {
            get
            {
                // PROTOTYPE: Handle nulls?
                field ??= (MethodSymbol)_factory.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Span_T__Slice_Int_Int)!;
                return field;
            }
        }

        public NamedTypeSymbol ReadOnlySpanTType => field ??= _factory.WellKnownType(WellKnownType.System_ReadOnlySpan_T);
        public NamedTypeSymbol SpanTType => field ??= _factory.WellKnownType(WellKnownType.System_Span_T);
        public NamedTypeSymbol Int32Type => field ??= _factory.Compilation.GetSpecialType(SpecialType.System_Int32);
    }
}
