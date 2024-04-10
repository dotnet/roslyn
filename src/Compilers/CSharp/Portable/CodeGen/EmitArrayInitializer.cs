// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGen
{
    internal partial class CodeGenerator
    {
        private enum ArrayInitializerStyle
        {
            // Initialize every element
            Element,

            // Initialize all elements at once from a metadata blob
            Block,

            // Mixed case where there are some initializers that are constants and
            // there is enough of them so that it makes sense to use block initialization
            // followed by individual initialization of non-constant elements
            Mixed,
        }

        /// <summary>
        /// Entry point to the array initialization.
        /// Assumes that we have newly created array on the stack.
        /// 
        /// inits could be an array of values for a single dimensional array
        /// or an array (of array)+ of values for a multidimensional case
        /// 
        /// in either case it is expected that number of leaf values will match number 
        /// of elements in the array and nesting level should match the rank of the array.
        /// </summary>
        private void EmitArrayInitializers(ArrayTypeSymbol arrayType, BoundArrayInitialization inits)
        {
            var initExprs = inits.Initializers;
            var initializationStyle = ShouldEmitBlockInitializer(arrayType.ElementType, initExprs);

            if (initializationStyle == ArrayInitializerStyle.Element)
            {
                this.EmitElementInitializers(arrayType, initExprs, true);
            }
            else
            {
                ImmutableArray<byte> data = this.GetRawData(initExprs);

                _builder.EmitArrayBlockInitializer(data, inits.Syntax, _diagnostics.DiagnosticBag);

                if (initializationStyle == ArrayInitializerStyle.Mixed)
                {
                    EmitElementInitializers(arrayType, initExprs, false);
                }
            }
        }

        private void EmitElementInitializers(ArrayTypeSymbol arrayType,
                                            ImmutableArray<BoundExpression> inits,
                                            bool includeConstants)
        {
            if (!IsMultidimensionalInitializer(inits))
            {
                EmitVectorElementInitializers(arrayType, inits, includeConstants);
            }
            else
            {
                EmitMultidimensionalElementInitializers(arrayType, inits, includeConstants);
            }
        }

        private void EmitVectorElementInitializers(ArrayTypeSymbol arrayType,
                    ImmutableArray<BoundExpression> inits,
                    bool includeConstants)
        {
            for (int i = 0; i < inits.Length; i++)
            {
                var init = inits[i];
                if (ShouldEmitInitExpression(includeConstants, init))
                {
                    _builder.EmitOpCode(ILOpCode.Dup);
                    _builder.EmitIntConstant(i);
                    EmitExpression(init, true);
                    EmitVectorElementStore(arrayType, init.Syntax);
                }
            }
        }

        // if element init is not a constant we have no choice - we need to emit it
        // if element is a default value - no need to emit initializer, arrays are created zero inited.
        // if element is a not a constant or includeConstants flag is set, return true
        private static bool ShouldEmitInitExpression(bool includeConstants, BoundExpression init)
        {
            if (init.IsDefaultValue())
            {
                return false;
            }

            return includeConstants || init.ConstantValueOpt == null;
        }

        /// <summary>
        /// To handle array initialization of arbitrary rank it is convenient to 
        /// approach multidimensional initialization as a recursively nested.
        /// 
        /// ForAll{i, j, k} Init(i, j, k) ===> 
        /// ForAll{i} ForAll{j, k} Init(i, j, k) ===>
        /// ForAll{i} ForAll{j} ForAll{k} Init(i, j, k)
        /// 
        /// This structure is used for capturing initializers of a given index and 
        /// the index value itself.
        /// </summary>
        private readonly struct IndexDesc
        {
            public IndexDesc(int index, ImmutableArray<BoundExpression> initializers)
            {
                this.Index = index;
                this.Initializers = initializers;
            }

            public readonly int Index;
            public readonly ImmutableArray<BoundExpression> Initializers;
        }

        private void EmitMultidimensionalElementInitializers(ArrayTypeSymbol arrayType,
                                                            ImmutableArray<BoundExpression> inits,
                                                            bool includeConstants)
        {
            // Using a List for the stack instead of the framework Stack because IEnumerable from Stack is top to bottom.
            // This algorithm requires the IEnumerable to be from bottom to top. See extensions for List in CollectionExtensions.vb.

            var indices = new ArrayBuilder<IndexDesc>();

            // emit initializers for all values of the leftmost index.
            for (int i = 0; i < inits.Length; i++)
            {
                indices.Push(new IndexDesc(i, ((BoundArrayInitialization)inits[i]).Initializers));
                EmitAllElementInitializersRecursive(arrayType, indices, includeConstants);
            }

            Debug.Assert(!indices.Any());
        }

        /// <summary>
        /// Emits all initializers that match indices on the stack recursively.
        /// 
        /// Example: 
        ///  if array has [0..2, 0..3, 0..2] shape
        ///  and we have {1, 2} indices on the stack
        ///  initializers for 
        ///              [1, 2, 0]
        ///              [1, 2, 1]
        ///              [1, 2, 2]
        /// 
        ///  will be emitted and the top index will be pushed off the stack 
        ///  as at that point we would be completely done with emitting initializers 
        ///  corresponding to that index.
        /// </summary>
        private void EmitAllElementInitializersRecursive(ArrayTypeSymbol arrayType,
                                                         ArrayBuilder<IndexDesc> indices,
                                                         bool includeConstants)
        {
            var top = indices.Peek();
            var inits = top.Initializers;

            if (IsMultidimensionalInitializer(inits))
            {
                // emit initializers for the less significant indices recursively
                for (int i = 0; i < inits.Length; i++)
                {
                    indices.Push(new IndexDesc(i, ((BoundArrayInitialization)inits[i]).Initializers));
                    EmitAllElementInitializersRecursive(arrayType, indices, includeConstants);
                }
            }
            else
            {
                // leaf case
                for (int i = 0; i < inits.Length; i++)
                {
                    var init = inits[i];
                    if (ShouldEmitInitExpression(includeConstants, init))
                    {
                        // emit array ref
                        _builder.EmitOpCode(ILOpCode.Dup);

                        Debug.Assert(indices.Count == arrayType.Rank - 1);

                        // emit values of all indices that are in progress
                        foreach (var row in indices)
                        {
                            _builder.EmitIntConstant(row.Index);
                        }

                        // emit the leaf index
                        _builder.EmitIntConstant(i);

                        var initExpr = inits[i];
                        EmitExpression(initExpr, true);
                        EmitArrayElementStore(arrayType, init.Syntax);
                    }
                }
            }

            indices.Pop();
        }

        private static ConstantValue AsConstOrDefault(BoundExpression init)
        {
            ConstantValue initConstantValueOpt = init.ConstantValueOpt;

            if (initConstantValueOpt != null)
            {
                return initConstantValueOpt;
            }

            TypeSymbol type = init.Type.EnumUnderlyingTypeOrSelf();
            return ConstantValue.Default(type.SpecialType);
        }

        /// <summary>
        /// Determine if enum arrays can be initialized using block initialization.
        /// </summary>
        /// <returns>True if it's safe to use block initialization for enum arrays.</returns>
        /// <remarks>
        /// In NetFx 4.0, block array initializers do not work on all combinations of {32/64 X Debug/Retail} when array elements are enums.
        /// This is fixed in 4.5 thus enabling block array initialization for a very common case.
        /// We look for the presence of <see cref="System.Runtime.GCLatencyMode.SustainedLowLatency"/> which was introduced in .NET Framework 4.5
        /// </remarks>
        private bool EnableEnumArrayBlockInitialization
        {
            get
            {
                return _module.Compilation.EnableEnumArrayBlockInitialization;
            }
        }

        private ArrayInitializerStyle ShouldEmitBlockInitializer(TypeSymbol elementType, ImmutableArray<BoundExpression> inits)
        {
            if (_module.IsEncDelta)
            {
                // Avoid using FieldRva table. Can be allowed if tested on all supported runtimes.
                // Consider removing: https://github.com/dotnet/roslyn/issues/69480
                return ArrayInitializerStyle.Element;
            }

            if (elementType.IsEnumType())
            {
                if (!EnableEnumArrayBlockInitialization)
                {
                    return ArrayInitializerStyle.Element;
                }
                elementType = elementType.EnumUnderlyingTypeOrSelf();
            }

            if (elementType.SpecialType.IsBlittable())
            {
                if (_module.GetInitArrayHelper() == null)
                {
                    return ArrayInitializerStyle.Element;
                }

                int initCount = 0;
                int constCount = 0;
                InitializerCountRecursive(inits, ref initCount, ref constCount);

                if (initCount > 2)
                {
                    if (initCount == constCount)
                    {
                        return ArrayInitializerStyle.Block;
                    }

                    int thresholdCnt = Math.Max(3, (initCount / 3));

                    if (constCount >= thresholdCnt)
                    {
                        return ArrayInitializerStyle.Mixed;
                    }
                }
            }

            return ArrayInitializerStyle.Element;
        }

        /// <summary>
        /// Count of all nontrivial initializers and count of those that are constants.
        /// </summary>
        private void InitializerCountRecursive(ImmutableArray<BoundExpression> inits, ref int initCount, ref int constInits)
        {
            if (inits.Length == 0)
            {
                return;
            }

            foreach (var init in inits)
            {
                var asArrayInit = init as BoundArrayInitialization;

                if (asArrayInit != null)
                {
                    InitializerCountRecursive(asArrayInit.Initializers, ref initCount, ref constInits);
                }
                else
                {
                    // NOTE: default values do not need to be initialized. 
                    //       .NET arrays are always zero-inited.
                    if (!init.IsDefaultValue())
                    {
                        initCount += 1;
                        if (init.ConstantValueOpt != null)
                        {
                            constInits += 1;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Produces a serialized blob of all constant initializers.
        /// Non-constant initializers are matched with a zero of corresponding size.
        /// </summary>
        private ImmutableArray<byte> GetRawData(ImmutableArray<BoundExpression> initializers)
        {
            // the initial size is a guess.
            // there is no point to be precise here as MemoryStream always has N + 1 storage 
            // and will need to be trimmed regardless
            var writer = new BlobBuilder(initializers.Length * 4);

            SerializeArrayRecursive(writer, initializers);

            return writer.ToImmutableArray();
        }

        private void SerializeArrayRecursive(BlobBuilder bw, ImmutableArray<BoundExpression> inits)
        {
            if (inits.Length != 0)
            {
                if (inits[0].Kind == BoundKind.ArrayInitialization)
                {
                    foreach (var init in inits)
                    {
                        SerializeArrayRecursive(bw, ((BoundArrayInitialization)init).Initializers);
                    }
                }
                else
                {
                    foreach (var init in inits)
                    {
                        AsConstOrDefault(init).Serialize(bw);
                    }
                }
            }
        }

        /// <summary>
        /// Check if it is a regular collection of expressions or there are nested initializers.
        /// </summary>
        private static bool IsMultidimensionalInitializer(ImmutableArray<BoundExpression> inits)
        {
            Debug.Assert(inits.All((init) => init.Kind != BoundKind.ArrayInitialization) ||
                         inits.All((init) => init.Kind == BoundKind.ArrayInitialization),
                         "all or none should be nested");

            return inits.Length != 0 && inits[0].Kind == BoundKind.ArrayInitialization;
        }

#nullable enable

        /// <summary>Tries to emit a ReadOnlySpan construction as a wrapper for a blob rather than as a wrapper for an array construction.</summary>
        /// <param name="spanType">The type of the span being constructed.</param>
        /// <param name="wrappedExpression">The expression being wrapped in a span.</param>
        /// <param name="used">true if the result of the expression is used; false if it's required only for its side effects.</param>
        /// <param name="inPlaceTarget">A non-null expression if the construction is initializing an existing local in-place; otherwise, null.</param>
        /// <param name="avoidInPlace">
        /// An output Boolean indicating whether a caller trying to perform in-place initialization should instead prefer to assign the local to a new value.
        /// Call sites may try to optimize an assignment to a newly-created struct by calling the constructor directly rather than assigning, but that
        /// may then inhibit the more valuable optimization of creating a span via RuntimeHelpers.CreateSpan, which needs to assign. When a caller has passed
        /// in an <paramref name="inPlaceTarget"/> but CreateSpan could be used if it weren't, this method may return false and set <paramref name="avoidInPlace"/>
        /// to true to inform the caller it can try again without the <paramref name="inPlaceTarget"/>.
        /// </param>
        /// <param name="start">The expression for the offset into the array being wrapped in a span.</param>
        /// <param name="length">The expression for the length of the subarray being wrapped in a span.</param>
        /// <returns>
        /// true if this method successfully emit a ReadOnlySpan as a wrapper for a blob; otherwise, false.  If false, nothing will have been emitted.
        /// And if false and <paramref name="avoidInPlace"/> is true (in which case <paramref name="inPlaceTarget"/> must have been non-null), the caller
        /// may try again but with a null <paramref name="inPlaceTarget"/>.
        /// </returns>
        private bool TryEmitOptimizedReadonlySpanCreation(NamedTypeSymbol spanType, BoundExpression wrappedExpression, bool used, BoundExpression inPlaceTarget, out bool avoidInPlace, BoundExpression? start = null, BoundExpression? length = null)
        {
            // The purpose of this optimization is to replace a BoundArrayCreation with better code generation.
            // We're looking for an expression like:
            //     new ReadOnlySpan<T>(new T[] { const, const, ... })
            //     new ReadOnlySpan<T>(new T[] { const, const, ... }, 0, length)
            //     (ReadOnlySpan<T>)new T[] { const, const, ... }
            // etc., and wrappedExpression is that array creation.  For single byte primitives, we can replace that
            // with the equivalent of:
            //     new ReadOnlySpan<T>((void*)PrivateImplementationDetails.DataField, Length)
            // on all target platforms.  For primitives larger than a single byte, if the target platform exposes
            // the RuntimeHelpers.CreateSpan method, we can emit it instead as:
            //     RuntimeHelpers.CreateSpan(PrivateImplementationDetails.DataFieldToken)
            // and for platforms that lack CreateSpan, as a span that wraps a lazily-initialized array:
            //     new ReadOnlySpan<T>(PrivateImplementationDetails.ArrayField ??= new T[] { ... })
            // For non-constant data, unsupported primitive types, and other variations, this optimization will fail
            // and the method will return false indicating that no code was emitted.
            //
            // A pattern like the following is also special-cased via the `inPlaceTarget` parameter:
            //     ReadOnlySpan<T> span = new ReadOnlySpan<T>(new T[] { const, const, ... });
            // Rather than constructing a span and assigning it to the local, the caller of this method
            // may try to initialize the local in-place.  In that case, this method is responsible for emitting
            // a call to the span's constructor.  It can do so for some cases, but in cases that
            // require the use of RuntimeHelpers.CreateSpan, assignment is a necessity, and as such calls
            // requiring in-place construction will fail; the code below may set `avoidInPlace` to true
            // indicating the caller can try again not in place and it should succeed.
            //
            // This optimization is also used as part of emitting UTF8 string literals. The code:
            //     ReadOnlySpan<byte> span = "abc"u8;
            // is lowered to the equivalent of:
            //     ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(new byte[] { (byte)'a', (byte)'b', (byte)'c', (byte)'\0' }, 0, 3);
            // with this optimization then being used to avoid that byte[] allocation. Support for u8
            // is also the reason this method accepts a start/length, in order to support trimming
            // the null terminator off as part of creating the span instance.

            Debug.Assert(inPlaceTarget is null || TargetIsNotOnHeap(inPlaceTarget), "in-place construction target should not be on heap");
            Debug.Assert(_diagnostics.DiagnosticBag is not null, $"Expected non-null {nameof(_diagnostics)}.{nameof(_diagnostics.DiagnosticBag)}");

            if (start is null != length is null)
            {
                // start and length always need to be provided as a pair.
                throw ExceptionUtilities.Unreachable();
            }

            avoidInPlace = false;
            SpecialType specialElementType = SpecialType.None;

            if (inPlaceTarget is null && !used)
            {
                // The caller has specified that we're creating a ReadOnlySpan expression that won't be used.
                // We needn't emit anything.
                return true;
            }

            if (_module.IsEncDelta)
            {
                // Avoid using FieldRva table. Can be allowed if tested on all supported runtimes.
                // Consider removing: https://github.com/dotnet/roslyn/issues/69480
                return false;
            }

            // The primary optimization here is for byte-sized primitives that can wrap a ReadOnlySpan directly around a pointer
            // into a blob.  That requires the ReadOnlySpan(void*, int) ctor.  If this constructor isn't available, we give up on
            // all optimizations.  Technically, if this ctor isn't available but the ReadOnlySpan(T[]) constructor is, we could still
            // proceed to use the cached array mechanism.  But all known ReadOnlySpan implementations have always provided both
            // constructors, and it's not worth trying to optimize here for an arbitrary implementation that has a different shape.
            var rosPointerCtor = (MethodSymbol?)Binder.GetWellKnownTypeMember(_module.Compilation, WellKnownMember.System_ReadOnlySpan_T__ctor_Pointer, _diagnostics, syntax: wrappedExpression.Syntax, isOptional: true);
            if (rosPointerCtor is null)
            {
                return false;
            }
            Debug.Assert(!rosPointerCtor.HasUnsupportedMetadata);

            ArrayTypeSymbol? arrayType = null;
            TypeSymbol? elementType = null;
            if (wrappedExpression is not BoundArrayCreation { InitializerOpt: { } initializer } ac)
            {
                return false;
            }

            // Get the array type and its element type.
            arrayType = (ArrayTypeSymbol)ac.Type;
            elementType = arrayType.ElementType;

            ImmutableArray<BoundExpression> initializers = initializer.Initializers;
            var elementCount = initializers.Length;
            if (elementCount == 0)
            {
                emitEmptyReadonlySpan(spanType, wrappedExpression, used, inPlaceTarget);
                return true;
            }

            if (initializers.Any(static init => init.ConstantValueOpt == null))
            {
                return false;
            }

            // The blob optimization is only supported for core primitive types that can be stored in metadata blobs.
            // For enums, we need to use the underlying type.
            specialElementType = elementType.EnumUnderlyingTypeOrSelf().SpecialType;
            if (!IsTypeAllowedInBlobWrapper(specialElementType))
            {
                return start is null && length is null
                    && tryEmitAsCachedArrayOfConstants(ac, arrayType, elementType, spanType, used, inPlaceTarget, out avoidInPlace);
            }

            if (IsPeVerifyCompatEnabled())
            {
                // After this point, we're emitting code that may cause PEVerify to warn, so stop if PEVerify compat is enabled.
                return false;
            }

            // Get the data and number of elements that compose the initialization.
            ImmutableArray<byte> data = GetRawDataForArrayInit(initializers);

            Debug.Assert(arrayType is not null);
            Debug.Assert(elementType is not null);

            int lengthForConstructor;

            if (start is not null)
            {
                // The start expression needs to be 0.
                if (start.ConstantValueOpt?.IsDefaultValue != true || start.ConstantValueOpt.Discriminator != ConstantValueTypeDiscriminator.Int32)
                {
                    return false;
                }

                // The length expression needs to be an Int32, and it needs to be in the range [0, elementCount].
                Debug.Assert(length is not null);
                if (length.ConstantValueOpt?.Discriminator != ConstantValueTypeDiscriminator.Int32)
                {
                    return false;
                }

                lengthForConstructor = length.ConstantValueOpt.Int32Value;

                if (lengthForConstructor > elementCount || lengthForConstructor < 0)
                {
                    return false;
                }
            }
            else
            {
                // There's no start/length, so the length to use with a constructor is the element count.
                lengthForConstructor = elementCount;
            }

            if (specialElementType.SizeInBytes() == 1)
            {
                // We're dealing with a ReadOnlySpan<byte/sbyte/bool>. We can optimize this on all target platforms,
                // whether the initialization is in-place or not.

                if (inPlaceTarget is not null)
                {
                    EmitAddress(inPlaceTarget, Binder.AddressKind.Writeable);
                }

                // Map a field to the block (that makes it addressable).
                var field = _builder.module.GetFieldForData(data, alignment: 1, wrappedExpression.Syntax, _diagnostics.DiagnosticBag);
                _builder.EmitOpCode(ILOpCode.Ldsflda);
                _builder.EmitToken(field, wrappedExpression.Syntax, _diagnostics.DiagnosticBag);

                _builder.EmitIntConstant(lengthForConstructor);

                if (inPlaceTarget is not null)
                {
                    // Consumes target ref, data ptr and size, pushes nothing.
                    _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: -3);
                }
                else
                {
                    // Consumes data ptr and size, pushes the instance.
                    Debug.Assert(used);
                    _builder.EmitOpCode(ILOpCode.Newobj, stackAdjustment: -1);
                }

                EmitSymbolToken(rosPointerCtor.AsMember(spanType), wrappedExpression.Syntax, optArgList: null);

                if (inPlaceTarget is not null && used)
                {
                    EmitExpression(inPlaceTarget, used: true);
                }

                return true;
            }

            // We're dealing with a primitive that's larger than a single byte.
            Debug.Assert(specialElementType.SizeInBytes() is 2 or 4 or 8, "Supported primitives are expected to be 2, 4, or 8 bytes");

            if (lengthForConstructor != elementCount)
            {
                // We need to use RuntimeHelpers.CreateSpan / cached array, but the code has requested a subset of the elements.
                // That means the code is something like `new ReadOnlySpan<char>(new[] { 'a', 'b', 'c' }, 1, 2)`
                // rather than `new ReadOnlySpan<char>(new[] { 'b', 'c' })`.  If such a pattern is found to be
                // common, this could be augmented to accommodate it.  For now, we just return false to fail
                // to optimize this case.
                return false;
            }

            if (inPlaceTarget is not null)
            {
                // We can use RuntimeHelpers.CreateSpan, but not for in-place initialization. Fail to optimize,
                // but tell the caller they can call this again with a null inPlaceTarget, at which point this
                // should be able to optimize the call.
                avoidInPlace = true;
                return false;
            }

            // As we're dealing with multi-byte types, endianness needs to be considered. Such handling is provided by the
            // runtime's RuntimeHelpers.CreateSpan, which will wrap a span around the blob on little endian and which will
            // allocate an array and cache it on big endian.
            MethodSymbol? createSpan = (MethodSymbol?)Binder.GetWellKnownTypeMember(_module.Compilation, WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__CreateSpanRuntimeFieldHandle, _diagnostics, syntax: wrappedExpression.Syntax, isOptional: true);
            if (createSpan is not null)
            {
                // CreateSpan was available. Use it.
                Debug.Assert(!createSpan.HasUnsupportedMetadata);

                // ldtoken <PrivateImplementationDetails>...
                // call ReadOnlySpan<elementType> RuntimeHelpers::CreateSpan<elementType>(fldHandle)
                var field = _builder.module.GetFieldForData(data, alignment: (ushort)specialElementType.SizeInBytes(), wrappedExpression.Syntax, _diagnostics.DiagnosticBag);
                _builder.EmitOpCode(ILOpCode.Ldtoken);
                _builder.EmitToken(field, wrappedExpression.Syntax, _diagnostics.DiagnosticBag);
                _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: 0);
                EmitSymbolToken(createSpan.Construct(elementType), wrappedExpression.Syntax, optArgList: null);
                return true;
            }

            // We're dealing with a multi-byte primitive, and CreateSpan was not available.  Get a static field from PrivateImplementationDetails,
            // and use it as a lazily-initialized cache for an array for this data:
            //     new ReadOnlySpan<T>(PrivateImplementationDetails.ArrayField ??= RuntimeHelpers.InitializeArray(new int[Length], PrivateImplementationDetails.DataField));
            return tryEmitAsCachedArrayFromBlob(spanType, wrappedExpression, elementCount, data, ref arrayType, elementType);

            // Emit: new ReadOnlySpan<T>(PrivateImplementationDetails.ArrayField ??= RuntimeHelpers.InitializeArray(new int[Length], PrivateImplementationDetails.DataField));
            bool tryEmitAsCachedArrayFromBlob(NamedTypeSymbol spanType, BoundExpression wrappedExpression, int elementCount, ImmutableArray<byte> data, ref ArrayTypeSymbol arrayType, TypeSymbol elementType)
            {
                if (!tryGetReadOnlySpanArrayCtor(wrappedExpression.Syntax, out var rosArrayCtor))
                {
                    return false;
                }

                // If we're dealing with an array of enums, we need to handle the possibility that the data blob
                // is the same for multiple enums all with the same underlying type, or even with the underlying type
                // itself. This is addressed by always caching an array for the underlying type, and then relying on
                // arrays being covariant between the underlying type and the enum type, so that it's safe to do:
                //     new ReadOnlySpan<EnumType>(arrayOfUnderlyingType);
                // It's important to have a consistent type here, as otherwise the type of the caching field could
                // end up changing non-deterministically based on which type for a given blob was encountered first.
                // Also, even if we're not dealing with an enum, we still create a new array type that drops any
                // annotations that may have initially been associated with the element type; this is similarly to
                // ensure deterministic behavior.
                arrayType = arrayType.WithElementType(TypeWithAnnotations.Create(elementType.EnumUnderlyingTypeOrSelf()));

                var cachingField = _builder.module.GetArrayCachingFieldForData(data, _module.Translate(arrayType), wrappedExpression.Syntax, _diagnostics.DiagnosticBag);
                var arrayNotNullLabel = new object();

                // T[]? array = PrivateImplementationDetails.cachingField;
                // if (array is not null) goto arrayNotNull;
                _builder.EmitOpCode(ILOpCode.Ldsfld);
                _builder.EmitToken(cachingField, wrappedExpression.Syntax, _diagnostics.DiagnosticBag);
                _builder.EmitOpCode(ILOpCode.Dup);
                _builder.EmitBranch(ILOpCode.Brtrue, arrayNotNullLabel);

                // array = new T[elementCount];
                // RuntimeHelpers.InitializeArray(token, array);
                // PrivateImplementationDetails.cachingField = array;
                _builder.EmitOpCode(ILOpCode.Pop);
                _builder.EmitIntConstant(elementCount);
                _builder.EmitOpCode(ILOpCode.Newarr);
                EmitSymbolToken(arrayType.ElementType, wrappedExpression.Syntax);
                _builder.EmitArrayBlockInitializer(data, wrappedExpression.Syntax, _diagnostics.DiagnosticBag);
                _builder.EmitOpCode(ILOpCode.Dup);
                _builder.EmitOpCode(ILOpCode.Stsfld);
                _builder.EmitToken(cachingField, wrappedExpression.Syntax, _diagnostics.DiagnosticBag);

                // arrayNotNullLabel:
                // new ReadOnlySpan<T>(array)
                _builder.MarkLabel(arrayNotNullLabel);
                _builder.EmitOpCode(ILOpCode.Newobj, 0);
                EmitSymbolToken(rosArrayCtor.AsMember(spanType), wrappedExpression.Syntax, optArgList: null);
                return true;
            }

            // Emit: new ReadOnlySpan<ElementType>(PrivateImplementationDetails.cachingField ??= new ElementType[] { ... constants ... })
            bool tryEmitAsCachedArrayOfConstants(BoundArrayCreation arrayCreation, ArrayTypeSymbol arrayType, TypeSymbol elementType, NamedTypeSymbol spanType, bool used, BoundExpression? inPlaceTarget, out bool avoidInPlace)
            {
                avoidInPlace = false;

                if (elementType.IsReferenceType && elementType.SpecialType != SpecialType.System_String)
                {
                    return false;
                }

                var initializer = arrayCreation.InitializerOpt;
                Debug.Assert(initializer != null);

                var initializers = initializer.Initializers;
                Debug.Assert(initializers.All(static init => init.ConstantValueOpt != null));
                Debug.Assert(!elementType.IsEnumType());

                if (!tryGetReadOnlySpanArrayCtor(arrayCreation.Syntax, out var rosArrayCtor))
                {
                    return false;
                }

                if (inPlaceTarget is not null)
                {
                    EmitAddress(inPlaceTarget, Binder.AddressKind.Writeable);
                }

                ImmutableArray<ConstantValue> constants = initializers.SelectAsArray(static init => init.ConstantValueOpt!);
                Cci.IFieldReference cachingField = _builder.module.GetArrayCachingFieldForConstants(constants, _module.Translate(arrayType),
                    arrayCreation.Syntax, _diagnostics.DiagnosticBag);

                var arrayNotNullLabel = new object();

                // T[]? array = PrivateImplementationDetails.cachingField;
                // if (array is not null) goto arrayNotNull;
                _builder.EmitOpCode(ILOpCode.Ldsfld);
                _builder.EmitToken(cachingField, arrayCreation.Syntax, _diagnostics.DiagnosticBag);
                _builder.EmitOpCode(ILOpCode.Dup);
                _builder.EmitBranch(ILOpCode.Brtrue, arrayNotNullLabel);

                // array = arrayCreation;
                // PrivateImplementationDetails.cachingField = array;
                _builder.EmitOpCode(ILOpCode.Pop);
                EmitExpression(arrayCreation, used: true);
                _builder.EmitOpCode(ILOpCode.Dup);
                _builder.EmitOpCode(ILOpCode.Stsfld);
                _builder.EmitToken(cachingField, arrayCreation.Syntax, _diagnostics.DiagnosticBag);

                // arrayNotNullLabel:
                // new ReadOnlySpan<T>(array)
                _builder.MarkLabel(arrayNotNullLabel);

                if (inPlaceTarget is not null)
                {
                    // Consumes target ref, array, pushes nothing.
                    _builder.EmitOpCode(ILOpCode.Call, stackAdjustment: -2);
                }
                else
                {
                    // Consumes array, pushes the instance.
                    Debug.Assert(used);
                    _builder.EmitOpCode(ILOpCode.Newobj, stackAdjustment: 0);
                }

                EmitSymbolToken(rosArrayCtor.AsMember(spanType), arrayCreation.Syntax, optArgList: null);

                if (inPlaceTarget is not null && used)
                {
                    EmitExpression(inPlaceTarget, used: true);
                }

                return true;
            }

            // The span is empty.  Optimize away the array.  This works regardless of the size of the type.
            void emitEmptyReadonlySpan(NamedTypeSymbol spanType, BoundExpression wrappedExpression, bool used, BoundExpression? inPlaceTarget)
            {
                // If this is in-place initialization, call the default ctor.
                if (inPlaceTarget is not null)
                {
                    EmitAddress(inPlaceTarget, Binder.AddressKind.Writeable);
                    _builder.EmitOpCode(ILOpCode.Initobj);
                    EmitSymbolToken(spanType, wrappedExpression.Syntax);
                    if (used)
                    {
                        EmitExpression(inPlaceTarget, used: true);
                    }
                }
                else
                {
                    // Otherwise, assign it to a default value / empty span.
                    Debug.Assert(used);
                    EmitDefaultValue(spanType, used, wrappedExpression.Syntax);
                }
            }

            bool tryGetReadOnlySpanArrayCtor(SyntaxNode syntax, [NotNullWhen(true)] out MethodSymbol? rosArrayCtor)
            {
                rosArrayCtor = (MethodSymbol?)Binder.GetWellKnownTypeMember(_module.Compilation, WellKnownMember.System_ReadOnlySpan_T__ctor_Array, _diagnostics, syntax: syntax, isOptional: true);
                if (rosArrayCtor is null)
                {
                    // The ReadOnlySpan<T>(T[] array) constructor we need is missing or something went wrong.
                    return false;
                }

                Debug.Assert(!rosArrayCtor.HasUnsupportedMetadata);
                return true;
            }
        }

        /// <summary>Gets whether the element type of an array is appropriate for storing in a blob.</summary>
        internal static bool IsTypeAllowedInBlobWrapper(SpecialType type) => type is
            // 1 byte
            // For primitives that are a single byte in size, a span can point directly to a blob
            // containing the constant data.
            SpecialType.System_SByte or SpecialType.System_Byte or SpecialType.System_Boolean or

            // For primitives that are > 1 byte in size, we can either use CreateSpan if it's available
            // or fall back to caching an array.

            // 2 bytes
            SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Char or

            // 4 bytes
            SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Single or

            // 8 bytes
            SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Double;

        /// <summary>
        /// Returns a byte blob that matches serialized content of single array initializer of constants.
        /// </summary>
        private ImmutableArray<byte> GetRawDataForArrayInit(ImmutableArray<BoundExpression> initializers)
        {
            Debug.Assert(initializers.Length > 0);
            Debug.Assert(initializers.All(static init => init.ConstantValueOpt != null));

            var writer = new BlobBuilder(initializers.Length * 4);

            foreach (var init in initializers)
            {
                init.ConstantValueOpt!.Serialize(writer);
            }

            return writer.ToImmutableArray();
        }
    }
}
