// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// TypeDefinition that represents &lt;PrivateImplementationDetails&gt; class.
    /// The main purpose of this class so far is to contain mapped fields and their types.
    /// </summary>
    internal sealed class PrivateImplementationDetails : DefaultTypeDef, Cci.INamespaceTypeDefinition
    {
        private const string TypeNamePrefix = "<PrivateImplementationDetails>";

        // Note: Dev11 uses the source method token as the prefix, rather than a fixed token
        // value, and data field offsets are unique within the method, not across all methods.
        internal const string SynthesizedStringHashFunctionName = "ComputeStringHash";
        internal const string SynthesizedReadOnlySpanHashFunctionName = "ComputeReadOnlySpanHash";
        internal const string SynthesizedSpanHashFunctionName = "ComputeSpanHash";

        internal const string SynthesizedThrowSwitchExpressionExceptionFunctionName = "ThrowSwitchExpressionException";
        internal const string SynthesizedThrowSwitchExpressionExceptionParameterlessFunctionName = "ThrowSwitchExpressionExceptionParameterless";
        internal const string SynthesizedThrowInvalidOperationExceptionFunctionName = "ThrowInvalidOperationException";

        internal const string SynthesizedInlineArrayAsSpanName = "InlineArrayAsSpan";
        internal const string SynthesizedInlineArrayAsReadOnlySpanName = "InlineArrayAsReadOnlySpan";

        internal const string SynthesizedInlineArrayElementRefName = "InlineArrayElementRef";
        internal const string SynthesizedInlineArrayElementRefReadOnlyName = "InlineArrayElementRefReadOnly";

        internal const string SynthesizedInlineArrayFirstElementRefName = "InlineArrayFirstElementRef";
        internal const string SynthesizedInlineArrayFirstElementRefReadOnlyName = "InlineArrayFirstElementRefReadOnly";

        private readonly CommonPEModuleBuilder _moduleBuilder;       //the module builder
        private readonly Cci.ITypeReference _systemObject;           //base type
        private readonly Cci.ITypeReference _systemValueType;        //base for nested structs

        private readonly Cci.ITypeReference _systemInt8Type;         //for metadata init of byte arrays
        private readonly Cci.ITypeReference _systemInt16Type;        //for metadata init of short arrays
        private readonly Cci.ITypeReference _systemInt32Type;        //for metadata init of int arrays
        private readonly Cci.ITypeReference _systemInt64Type;        //for metadata init of long arrays

        private readonly Cci.ICustomAttribute _compilerGeneratedAttribute;

        private readonly string _name;

        // Once frozen the collections of fields, methods and types are immutable.
        private int _frozen;

        private ImmutableArray<SynthesizedStaticField> _orderedSynthesizedFields;

        // fields mapped to metadata blocks
        private readonly ConcurrentDictionary<(ImmutableArray<byte> Data, ushort Alignment), MappedField> _mappedFields =
            new ConcurrentDictionary<(ImmutableArray<byte> Data, ushort Alignment), MappedField>(DataAndUShortEqualityComparer.Instance);

        // fields for cached arrays
        private readonly ConcurrentDictionary<(ImmutableArray<byte> Data, ushort ElementType), CachedArrayField> _cachedArrayFields =
            new ConcurrentDictionary<(ImmutableArray<byte> Data, ushort ElementType), CachedArrayField>(DataAndUShortEqualityComparer.Instance);

        private ModuleVersionIdField? _mvidField;
        // Dictionary that maps from analysis kind to instrumentation payload field.
        private readonly ConcurrentDictionary<int, InstrumentationPayloadRootField> _instrumentationPayloadRootFields = new ConcurrentDictionary<int, InstrumentationPayloadRootField>();

        // synthesized methods
        private ImmutableArray<Cci.IMethodDefinition> _orderedSynthesizedMethods;
        private readonly ConcurrentDictionary<string, Cci.IMethodDefinition> _synthesizedMethods =
            new ConcurrentDictionary<string, Cci.IMethodDefinition>();

        // synthesized top-level types (for inline arrays and collection expression types currently)
        private ImmutableArray<Cci.INamespaceTypeDefinition> _orderedTopLevelTypes;
        private readonly ConcurrentDictionary<string, Cci.INamespaceTypeDefinition> _synthesizedTopLevelTypes = new ConcurrentDictionary<string, Cci.INamespaceTypeDefinition>();

        // field types for different block sizes.
        private ImmutableArray<Cci.ITypeReference> _orderedProxyTypes;
        private readonly ConcurrentDictionary<(uint Size, ushort Alignment), Cci.ITypeReference> _proxyTypes = new ConcurrentDictionary<(uint Size, ushort Alignment), Cci.ITypeReference>();

        internal PrivateImplementationDetails(
            CommonPEModuleBuilder moduleBuilder,
            string moduleName,
            int submissionSlotIndex,
            Cci.ITypeReference systemObject,
            Cci.ITypeReference systemValueType,
            Cci.ITypeReference systemInt8Type,
            Cci.ITypeReference systemInt16Type,
            Cci.ITypeReference systemInt32Type,
            Cci.ITypeReference systemInt64Type,
            Cci.ICustomAttribute compilerGeneratedAttribute)
        {
            RoslynDebug.Assert(systemObject != null);
            RoslynDebug.Assert(systemValueType != null);

            _moduleBuilder = moduleBuilder;
            _systemObject = systemObject;
            _systemValueType = systemValueType;

            _systemInt8Type = systemInt8Type;
            _systemInt16Type = systemInt16Type;
            _systemInt32Type = systemInt32Type;
            _systemInt64Type = systemInt64Type;

            _compilerGeneratedAttribute = compilerGeneratedAttribute;

            _name = getClassName();

            string getClassName()
            {
                // we include the module name in the name of the PrivateImplementationDetails class so that more than
                // one of them can be included in an assembly as part of netmodules.    
                var name = (moduleBuilder.OutputKind == OutputKind.NetModule) ?
                    $"{TypeNamePrefix}<{MetadataHelpers.MangleForTypeNameIfNeeded(moduleName)}>" : TypeNamePrefix;

                if (submissionSlotIndex >= 0)
                {
                    name += submissionSlotIndex.ToString();
                }

                if (moduleBuilder.CurrentGenerationOrdinal > 0)
                {
                    name += "#" + moduleBuilder.CurrentGenerationOrdinal;
                }

                return name;
            }
        }

        internal void Freeze()
        {
            var wasFrozen = Interlocked.Exchange(ref _frozen, 1);
            if (wasFrozen != 0)
            {
                throw new InvalidOperationException();
            }

            // Sort fields.
            ArrayBuilder<SynthesizedStaticField> fieldsBuilder = ArrayBuilder<SynthesizedStaticField>.GetInstance(_mappedFields.Count + _cachedArrayFields.Count + (_mvidField != null ? 1 : 0));
            fieldsBuilder.AddRange(_mappedFields.Values);
            fieldsBuilder.AddRange(_cachedArrayFields.Values);
            if (_mvidField != null)
            {
                fieldsBuilder.Add(_mvidField);
            }
            fieldsBuilder.AddRange(_instrumentationPayloadRootFields.Values);
            fieldsBuilder.Sort(FieldComparer.Instance);
            _orderedSynthesizedFields = fieldsBuilder.ToImmutableAndFree();

            // Sort methods.
            _orderedSynthesizedMethods = _synthesizedMethods.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).AsImmutable();

            // Sort top-level types.
            _orderedTopLevelTypes = _synthesizedTopLevelTypes.OrderBy(kvp => kvp.Key).Select(kvp => (Cci.INamespaceTypeDefinition)kvp.Value).AsImmutable();

            // Sort proxy types.
            _orderedProxyTypes = _proxyTypes.OrderBy(kvp => kvp.Key.Size).ThenBy(kvp => kvp.Key.Alignment).Select(kvp => kvp.Value).AsImmutable();
        }

        internal bool IsFrozen => _frozen != 0;

        /// <summary>
        /// Gets a field that can be used to cache an array allocated to store data from a corresponding <see cref="CreateDataField"/> call.
        /// </summary>
        /// <param name="data">The data that will be used to initialize the field.</param>
        /// <param name="arrayType">The type of the field, e.g. int[].</param>
        /// <param name="emitContext">The emit context to use with the array type to extract its element type.</param>
        /// <returns>The field to use to cache an array for this data and alignment.</returns>
        internal Cci.IFieldReference CreateArrayCachingField(ImmutableArray<byte> data, Cci.IArrayTypeReference arrayType, EmitContext emitContext)
        {
            Debug.Assert(!IsFrozen);

            // Get the type code for the array's element type.
            Cci.PrimitiveTypeCode typeCode = arrayType.GetElementType(emitContext).TypeCode;
            Debug.Assert(typeCode is
                Cci.PrimitiveTypeCode.Int16 or Cci.PrimitiveTypeCode.UInt16 or Cci.PrimitiveTypeCode.Char or
                Cci.PrimitiveTypeCode.Int32 or Cci.PrimitiveTypeCode.UInt32 or Cci.PrimitiveTypeCode.Float32 or
                Cci.PrimitiveTypeCode.Int64 or Cci.PrimitiveTypeCode.UInt64 or Cci.PrimitiveTypeCode.Float64);

            // Create a dedicated mapped field for the array type, separate from the data that'll be stored into that array.
            // Call sites will lazily instantiate the array to cache in this field, rather than forcibly instantiating
            // all of them when the private implementation details class is first used.
            return _cachedArrayFields.GetOrAdd((data, (ushort)typeCode), key =>
            {
                // Hash the data to hex, but then tack on _A(ElementType). This is needed both to differentiate the array field from
                // the data field, but also to differentiate multiple fields that may have the same raw data but different array types.
                string name = $"{HashToHex(key.Data)}_A{key.ElementType}";

                return new CachedArrayField(name, this, arrayType);
            });
        }

        /// <summary>
        /// Gets a field that can be used to to store data directly in an RVA field.
        /// </summary>
        /// <param name="data">The data for the field.</param>
        /// <param name="alignment">
        /// The alignment value is the necessary alignment for addresses for the underlying element type of the array.
        /// The data is stored by using a type whose size is equal to the total size of the blob. If a built-in system
        /// type has an appropriate size and .pack, it can be used. Otherwise, a type is generated of the same size as
        /// the data, and that type needs its .pack set to the alignment required for the underlying data. While that
        /// .pack value isn't required by anything else in the compiler (the compiler always aligns RVA fields at 8-byte
        /// boundaries, which accomodates any element type that's relevant), it is necessary for IL rewriters. Such rewriters
        /// also need to ensure an appropriate alignment is maintained for the RVA field, and while they could also simplify
        /// by choosing a worst-case alignment as does the compiler, they may instead use the .pack value as the alignment
        /// to use for that field, since it's an opaque blob with no other indication as to what kind of data is
        /// stored and what alignment might be required.
        /// </param>
        /// <returns>The field. This may have been newly created or may be an existing field previously created for the same data and alignment.</returns>
        internal Cci.IFieldReference CreateDataField(ImmutableArray<byte> data, ushort alignment)
        {
            Debug.Assert(!IsFrozen);
            Debug.Assert(alignment is 1 or 2 or 4 or 8);
            Debug.Assert(data.Length != 1 || alignment == 1);

            Cci.ITypeReference type = _proxyTypes.GetOrAdd(
                ((uint)data.Length, Alignment: alignment), key =>
                {
                    // We need a type that's both the same size as the data and that has a .pack
                    // that matches the data's alignment requirements. If the size of the data
                    // is 1 byte, then the alignment will also be 1, and we can use byte as the type.
                    // If the size of the data is 2, 4, or 8 bytes, we can use short, int, or long rather than
                    // creating a custom type, but we can only do so if the required alignment is also 1, as
                    // these types have a .pack value of 1.
                    if (key.Alignment == 1)
                    {
                        switch (key.Size)
                        {
                            case 1 when _systemInt8Type is not null: return _systemInt8Type;
                            case 2 when _systemInt16Type is not null: return _systemInt16Type;
                            case 4 when _systemInt32Type is not null: return _systemInt32Type;
                            case 8 when _systemInt64Type is not null: return _systemInt64Type;
                        }
                    }

                    // Use a custom type.
                    return new ExplicitSizeStruct(key.Size, key.Alignment, this, _systemValueType);
                });

            return _mappedFields.GetOrAdd((data, alignment), key =>
            {
                // For alignment of 1 (which is used in cases other than in fields for ReadOnlySpan<byte>),
                // just use the hex value of the data hash.  For other alignments, tack on a '2', '4', or '8'
                // accordingly.  As every byte will yield two chars, the odd number of chars used for 2/4/8
                // alignments will never produce a name that conflicts with names for an alignment of 1.
                Debug.Assert(alignment is 1 or 2 or 4 or 8, $"Unexpected alignment: {alignment}");
                string hex = HashToHex(key.Data);
                string name = alignment switch
                {
                    2 => hex + "2",
                    4 => hex + "4",
                    8 => hex + "8",
                    _ => hex
                };

                return new MappedField(name, this, type, key.Data);
            });
        }

        internal Cci.IFieldReference GetModuleVersionId(Cci.ITypeReference mvidType)
        {
            if (_mvidField == null)
            {
                Debug.Assert(!IsFrozen);
                Interlocked.CompareExchange(ref _mvidField, new ModuleVersionIdField(this, mvidType), null);
            }

            Debug.Assert(_mvidField.Type == mvidType);
            return _mvidField;
        }

        internal Cci.IFieldReference GetOrAddInstrumentationPayloadRoot(int analysisKind, Cci.ITypeReference payloadRootType)
        {
            InstrumentationPayloadRootField? payloadRootField;
            if (!_instrumentationPayloadRootFields.TryGetValue(analysisKind, out payloadRootField))
            {
                Debug.Assert(!IsFrozen);
                payloadRootField = _instrumentationPayloadRootFields.GetOrAdd(analysisKind, kind => new InstrumentationPayloadRootField(this, kind, payloadRootType));
            }

            Debug.Assert(payloadRootField.Type == payloadRootType);
            return payloadRootField;
        }

        // Get the instrumentation payload roots ordered by analysis kind.
        internal IOrderedEnumerable<KeyValuePair<int, InstrumentationPayloadRootField>> GetInstrumentationPayloadRoots()
        {
            Debug.Assert(IsFrozen);
            return _instrumentationPayloadRootFields.OrderBy(analysis => analysis.Key);
        }

        // Add a new synthesized method indexed by its name if the method isn't already present.
        internal bool TryAddSynthesizedMethod(Cci.IMethodDefinition method)
        {
            Debug.Assert(!IsFrozen);
#nullable disable // Can 'method.Name' be null? https://github.com/dotnet/roslyn/issues/39166
            return _synthesizedMethods.TryAdd(method.Name, method);
#nullable enable
        }

        public override IEnumerable<Cci.IFieldDefinition> GetFields(EmitContext context)
        {
            Debug.Assert(IsFrozen);
            return _orderedSynthesizedFields;
        }

        public override IEnumerable<Cci.IMethodDefinition> GetMethods(EmitContext context)
        {
            Debug.Assert(IsFrozen);
            return _orderedSynthesizedMethods;
        }

        public IEnumerable<Cci.IMethodDefinition> GetTopLevelTypeMethods(EmitContext context)
        {
            Debug.Assert(IsFrozen);
            foreach (var type in _orderedTopLevelTypes)
            {
                foreach (var method in type.GetMethods(context))
                {
                    yield return method;
                }
            }
        }

        // Get method by name, if one exists. Otherwise return null.
        internal Cci.IMethodDefinition? GetMethod(string name)
        {
            Cci.IMethodDefinition? method;
            _synthesizedMethods.TryGetValue(name, out method);
            return method;
        }

        internal bool TryAddSynthesizedType(Cci.INamespaceTypeDefinition type)
        {
            Debug.Assert(!IsFrozen);
            Debug.Assert(type.Name is { });
            return _synthesizedTopLevelTypes.TryAdd(type.Name, type);
        }

        internal Cci.INamespaceTypeDefinition? GetSynthesizedType(string name)
        {
            _synthesizedTopLevelTypes.TryGetValue(name, out var type);
            return type;
        }

        internal IEnumerable<Cci.INamespaceTypeDefinition> GetAdditionalTopLevelTypes()
        {
            Debug.Assert(IsFrozen);
            return _orderedTopLevelTypes;
        }

        public override IEnumerable<Cci.INestedTypeDefinition> GetNestedTypes(EmitContext context)
        {
            Debug.Assert(IsFrozen);
            return _orderedProxyTypes.OfType<ExplicitSizeStruct>();
        }

        public override string ToString() => this.Name;

        public override Cci.ITypeReference GetBaseClass(EmitContext context) => _systemObject;

        public override IEnumerable<Cci.ICustomAttribute> GetAttributes(EmitContext context)
        {
            if (_compilerGeneratedAttribute != null)
            {
                return SpecializedCollections.SingletonEnumerable(_compilerGeneratedAttribute);
            }

            return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
        }

        public override void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override Cci.INamespaceTypeDefinition AsNamespaceTypeDefinition(EmitContext context) => this;

        public override Cci.INamespaceTypeReference AsNamespaceTypeReference => this;

        public string Name => _name;

        public bool IsPublic => false;

        public Cci.IUnitReference GetUnit(EmitContext context)
        {
            Debug.Assert(context.Module == _moduleBuilder);
            return _moduleBuilder;
        }

        public string NamespaceName => string.Empty;

        private static string HashToHex(ImmutableArray<byte> data)
        {
            ImmutableArray<byte> hash = CryptographicHashProvider.ComputeSourceHash(data);

#if NETCOREAPP2_1_OR_GREATER
            return string.Create(hash.Length * 2, hash, (destination, hash) => toHex(hash, destination));
#else
            char[] c = new char[hash.Length * 2];
            toHex(hash, c);
            return new string(c);
#endif

            static void toHex(ImmutableArray<byte> source, Span<char> destination)
            {
                int i = 0;
                foreach (var b in source.AsSpan())
                {
                    destination[i++] = hexchar(b >> 4);
                    destination[i++] = hexchar(b & 0xF);
                }
            }

            static char hexchar(int x) => (char)((x <= 9) ? (x + '0') : (x + ('A' - 10)));
        }

        private sealed class FieldComparer : IComparer<SynthesizedStaticField>
        {
            public static readonly FieldComparer Instance = new FieldComparer();

            private FieldComparer()
            {
            }

            public int Compare(SynthesizedStaticField? x, SynthesizedStaticField? y)
            {
                RoslynDebug.Assert(x is object && y is object);

                // Fields are always synthesized with non-null names.
                RoslynDebug.Assert(x.Name != null && y.Name != null);
                return x.Name.CompareTo(y.Name);
            }
        }

        private sealed class DataAndUShortEqualityComparer : EqualityComparer<(ImmutableArray<byte> Data, ushort Value)>
        {
            public static readonly DataAndUShortEqualityComparer Instance = new DataAndUShortEqualityComparer();

            private DataAndUShortEqualityComparer() { }

            public override bool Equals((ImmutableArray<byte> Data, ushort Value) x, (ImmutableArray<byte> Data, ushort Value) y) =>
                x.Value == y.Value &&
                ByteSequenceComparer.Equals(x.Data, y.Data);

            public override int GetHashCode((ImmutableArray<byte> Data, ushort Value) obj) =>
                ByteSequenceComparer.GetHashCode(obj.Data); // purposefully not including Value, as it won't add meaningfully to the hash code
        }
    }

    /// <summary>
    /// Simple struct type with explicit size and no members.
    /// </summary>
    internal sealed class ExplicitSizeStruct : DefaultTypeDef, Cci.INestedTypeDefinition
    {
        private readonly uint _size;
        private readonly ushort _alignment;
        private readonly Cci.INamedTypeDefinition _containingType;
        private readonly Cci.ITypeReference _sysValueType;

        internal ExplicitSizeStruct(uint size, ushort alignment, PrivateImplementationDetails containingType, Cci.ITypeReference sysValueType)
        {
            Debug.Assert(alignment is 1 or 2 or 4 or 8, $"Unexpected alignment: {alignment}");

            _size = size;
            _alignment = alignment;
            _containingType = containingType;
            _sysValueType = sysValueType;
        }

        public override string ToString()
            => _containingType.ToString() + "." + this.Name;

        public override ushort Alignment => _alignment;

        public override Cci.ITypeReference GetBaseClass(EmitContext context) => _sysValueType;

        public override LayoutKind Layout => LayoutKind.Explicit;

        public override uint SizeOf => _size;

        public override void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        public string Name => _alignment == 1 ?
            $"__StaticArrayInitTypeSize={_size}" :
            $"__StaticArrayInitTypeSize={_size}_Align={_alignment}";

        public Cci.ITypeDefinition ContainingTypeDefinition => _containingType;

        public Cci.TypeMemberVisibility Visibility => Cci.TypeMemberVisibility.Private;

        public override bool IsValueType => true;

        public Cci.ITypeReference GetContainingType(EmitContext context) => _containingType;

        public override Cci.INestedTypeDefinition AsNestedTypeDefinition(EmitContext context) => this;

        public override Cci.INestedTypeReference AsNestedTypeReference => this;
    }

    internal abstract class SynthesizedStaticField : Cci.IFieldDefinition
    {
        private readonly Cci.INamedTypeDefinition _containingType;
        private readonly Cci.ITypeReference _type;
        private readonly string _name;

        internal SynthesizedStaticField(string name, Cci.INamedTypeDefinition containingType, Cci.ITypeReference type)
        {
            RoslynDebug.Assert(name != null);
            RoslynDebug.Assert(containingType != null);
            RoslynDebug.Assert(type != null);

            _containingType = containingType;
            _type = type;
            _name = name;
        }

        public override string ToString() => $"{(object?)_type.GetInternalSymbol() ?? _type} {(object?)_containingType.GetInternalSymbol() ?? _containingType}.{this.Name}";

        public MetadataConstant? GetCompileTimeValue(EmitContext context) => null;

        public abstract ImmutableArray<byte> MappedData { get; }

        public bool IsCompileTimeConstant => false;

        public bool IsNotSerialized => false;

        public bool IsReadOnly => true;

        public bool IsRuntimeSpecial => false;

        public bool IsSpecialName => false;

        public bool IsStatic => true;

        public bool IsMarshalledExplicitly => false;

        public Cci.IMarshallingInformation? MarshallingInformation => null;

        public ImmutableArray<byte> MarshallingDescriptor => default(ImmutableArray<byte>);

        public int Offset
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public Cci.ITypeDefinition ContainingTypeDefinition => _containingType;

        public Cci.TypeMemberVisibility Visibility => Cci.TypeMemberVisibility.Assembly;

        public Cci.ITypeReference GetContainingType(EmitContext context) => _containingType;

        public IEnumerable<Cci.ICustomAttribute> GetAttributes(EmitContext context)
            => SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();

        public void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        public Cci.IDefinition AsDefinition(EmitContext context)
        {
            throw ExceptionUtilities.Unreachable();
        }

        Symbols.ISymbolInternal? Cci.IReference.GetInternalSymbol() => null;

        public string Name => _name;

        public bool IsContextualNamedEntity => false;

        public Cci.ITypeReference GetType(EmitContext context) => _type;

        public ImmutableArray<Cci.ICustomModifier> RefCustomModifiers => ImmutableArray<Cci.ICustomModifier>.Empty;

        public bool IsByReference => false;

        internal Cci.ITypeReference Type => _type;

        public Cci.IFieldDefinition GetResolvedField(EmitContext context) => this;

        public Cci.ISpecializedFieldReference? AsSpecializedFieldReference => null;

        public MetadataConstant Constant
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public sealed override bool Equals(object? obj)
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw Roslyn.Utilities.ExceptionUtilities.Unreachable();
        }

        public sealed override int GetHashCode()
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw Roslyn.Utilities.ExceptionUtilities.Unreachable();
        }
    }

    internal sealed class ModuleVersionIdField : SynthesizedStaticField
    {
        internal ModuleVersionIdField(Cci.INamedTypeDefinition containingType, Cci.ITypeReference type)
            : base("MVID", containingType, type)
        {
        }

        public override ImmutableArray<byte> MappedData => default(ImmutableArray<byte>);
    }

    internal sealed class InstrumentationPayloadRootField : SynthesizedStaticField
    {
        internal InstrumentationPayloadRootField(Cci.INamedTypeDefinition containingType, int analysisIndex, Cci.ITypeReference payloadType)
            : base("PayloadRoot" + analysisIndex.ToString(), containingType, payloadType)
        {
        }

        public override ImmutableArray<byte> MappedData => default(ImmutableArray<byte>);
    }

    /// <summary>
    /// Definition of a simple field mapped to a metadata block
    /// </summary>
    internal sealed class MappedField : SynthesizedStaticField
    {
        private readonly ImmutableArray<byte> _block;

        internal MappedField(string name, Cci.INamedTypeDefinition containingType, Cci.ITypeReference type, ImmutableArray<byte> block)
            : base(name, containingType, type)
        {
            Debug.Assert(!block.IsDefault);
            _block = block;
        }

        public override ImmutableArray<byte> MappedData => _block;
    }

    /// <summary>
    /// Definition of a field for storing an array caching the data from a metadata block.
    /// </summary>
    internal sealed class CachedArrayField : SynthesizedStaticField
    {
        internal CachedArrayField(string name, Cci.INamedTypeDefinition containingType, Cci.ITypeReference type)
            : base(name, containingType, type)
        {
        }

        public override ImmutableArray<byte> MappedData => default(ImmutableArray<byte>);
    }

    /// <summary>
    /// Just a default implementation of a type definition.
    /// </summary>
    internal abstract class DefaultTypeDef : Cci.ITypeDefinition
    {
        public IEnumerable<Cci.IEventDefinition> GetEvents(EmitContext context)
            => SpecializedCollections.EmptyEnumerable<Cci.IEventDefinition>();

        public IEnumerable<Cci.MethodImplementation> GetExplicitImplementationOverrides(EmitContext context)
            => SpecializedCollections.EmptyEnumerable<Cci.MethodImplementation>();

        public virtual IEnumerable<Cci.IFieldDefinition> GetFields(EmitContext context)
            => SpecializedCollections.EmptyEnumerable<Cci.IFieldDefinition>();

        public IEnumerable<Cci.IGenericTypeParameter> GenericParameters
            => SpecializedCollections.EmptyEnumerable<Cci.IGenericTypeParameter>();

        public ushort GenericParameterCount => 0;

        public bool HasDeclarativeSecurity => false;

        public IEnumerable<Cci.TypeReferenceWithAttributes> Interfaces(EmitContext context)
            => SpecializedCollections.EmptyEnumerable<Cci.TypeReferenceWithAttributes>();

        public bool IsAbstract => false;

        public bool IsBeforeFieldInit => false;

        public bool IsComObject => false;

        public bool IsGeneric => false;

        public bool IsInterface => false;

        public bool IsDelegate => false;

        public bool IsRuntimeSpecial => false;

        public bool IsSerializable => false;

        public bool IsSpecialName => false;

        public bool IsWindowsRuntimeImport => false;

        public bool IsSealed => true;

        public virtual IEnumerable<Cci.IMethodDefinition> GetMethods(EmitContext context)
            => SpecializedCollections.EmptyEnumerable<Cci.IMethodDefinition>();

        public virtual IEnumerable<Cci.INestedTypeDefinition> GetNestedTypes(EmitContext context)
            => SpecializedCollections.EmptyEnumerable<Cci.INestedTypeDefinition>();

        public IEnumerable<Cci.IPropertyDefinition> GetProperties(EmitContext context)
            => SpecializedCollections.EmptyEnumerable<Cci.IPropertyDefinition>();

        public IEnumerable<Cci.SecurityAttribute> SecurityAttributes
            => SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>();

        public CharSet StringFormat => CharSet.Ansi;

        public virtual IEnumerable<Cci.ICustomAttribute> GetAttributes(EmitContext context)
            => SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();

        public Cci.IDefinition AsDefinition(EmitContext context) => this;

        Symbols.ISymbolInternal? Cci.IReference.GetInternalSymbol() => null;

        public bool IsEnum => false;

        public Cci.ITypeDefinition GetResolvedType(EmitContext context) => this;

        public Cci.PrimitiveTypeCode TypeCode => Cci.PrimitiveTypeCode.NotPrimitive;

        public TypeDefinitionHandle TypeDef
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public Cci.IGenericMethodParameterReference? AsGenericMethodParameterReference => null;

        public Cci.IGenericTypeInstanceReference? AsGenericTypeInstanceReference => null;

        public Cci.IGenericTypeParameterReference? AsGenericTypeParameterReference => null;

        public virtual Cci.INamespaceTypeDefinition? AsNamespaceTypeDefinition(EmitContext context) => null;

        public virtual Cci.INamespaceTypeReference? AsNamespaceTypeReference => null;

        public Cci.ISpecializedNestedTypeReference? AsSpecializedNestedTypeReference => null;

        public virtual Cci.INestedTypeDefinition? AsNestedTypeDefinition(EmitContext context) => null;

        public virtual Cci.INestedTypeReference? AsNestedTypeReference => null;

        public Cci.ITypeDefinition AsTypeDefinition(EmitContext context) => this;

        public bool MangleName => false;

        public string? AssociatedFileIdentifier => null;

        public virtual ushort Alignment => 0;

        public virtual Cci.ITypeReference GetBaseClass(EmitContext context)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public virtual LayoutKind Layout => LayoutKind.Auto;

        public virtual uint SizeOf => 0;

        public virtual void Dispatch(Cci.MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable();
        }

        public virtual bool IsValueType => false;

        public sealed override bool Equals(object? obj)
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw Roslyn.Utilities.ExceptionUtilities.Unreachable();
        }

        public sealed override int GetHashCode()
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw Roslyn.Utilities.ExceptionUtilities.Unreachable();
        }
    }
}
