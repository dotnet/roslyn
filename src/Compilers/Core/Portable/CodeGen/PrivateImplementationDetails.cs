// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Hashing;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
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

        internal const string SynthesizedBytesToStringFunctionName = "BytesToString";

        internal readonly CommonPEModuleBuilder ModuleBuilder;       //the module builder
        internal readonly Cci.ITypeReference SystemObject;           //base type
        private readonly Cci.ITypeReference _systemValueType;        //base for nested structs

        private readonly Cci.ITypeReference _systemInt8Type;         //for metadata init of byte arrays
        private readonly Cci.ITypeReference _systemInt16Type;        //for metadata init of short arrays
        private readonly Cci.ITypeReference _systemInt32Type;        //for metadata init of int arrays
        private readonly Cci.ITypeReference _systemInt64Type;        //for metadata init of long arrays

        private readonly Cci.ICustomAttribute? _compilerGeneratedAttribute;

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

        // fields for cached arrays for constants
        private readonly ConcurrentDictionary<(ImmutableArray<ConstantValue> Constants, ushort ElementType), CachedArrayField> _cachedArrayFieldsForConstants =
            new ConcurrentDictionary<(ImmutableArray<ConstantValue> Constants, ushort ElementType), CachedArrayField>(ConstantValueAndUShortEqualityComparer.Instance);

        private ModuleVersionIdField? _mvidField;
        private ModuleCancellationTokenField? _moduleCancellationTokenField;

        // Dictionary that maps from analysis kind to instrumentation payload field.
        private readonly ConcurrentDictionary<int, InstrumentationPayloadRootField> _instrumentationPayloadRootFields = new ConcurrentDictionary<int, InstrumentationPayloadRootField>();

        // synthesized methods
        private ImmutableArray<Cci.IMethodDefinition> _orderedSynthesizedMethods;
        private readonly ConcurrentDictionary<string, Cci.IMethodDefinition> _synthesizedMethods =
            new ConcurrentDictionary<string, Cci.IMethodDefinition>();

        // field types for different block sizes.
        private readonly ConcurrentDictionary<(uint Size, ushort Alignment), Cci.ITypeReference> _dataFieldTypes = new ConcurrentDictionary<(uint Size, ushort Alignment), Cci.ITypeReference>();

        // data section string literal holders (key is the full string literal)
        private readonly ConcurrentDictionary<string, DataSectionStringType> _dataSectionStringLiteralTypes = new ConcurrentDictionary<string, DataSectionStringType>();

        // map of data section string literal generated type names (<S> + hash) to the full text
        private readonly ConcurrentDictionary<string, string> _dataSectionStringLiteralNames = new ConcurrentDictionary<string, string>();

        private ImmutableArray<Cci.INestedTypeDefinition> _orderedNestedTypes;

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
            Cci.ICustomAttribute? compilerGeneratedAttribute)
        {
            RoslynDebug.Assert(systemObject != null);
            RoslynDebug.Assert(systemValueType != null);

            ModuleBuilder = moduleBuilder;
            SystemObject = systemObject;
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
                    name += submissionSlotIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
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
            ArrayBuilder<SynthesizedStaticField> fieldsBuilder = ArrayBuilder<SynthesizedStaticField>.GetInstance(
                _mappedFields.Count + _cachedArrayFields.Count + _cachedArrayFieldsForConstants.Count + (_mvidField != null ? 1 : 0));

            fieldsBuilder.AddRange(_mappedFields.Values);
            fieldsBuilder.AddRange(_cachedArrayFields.Values);
            fieldsBuilder.AddRange(_cachedArrayFieldsForConstants.Values);

            if (_mvidField != null)
                fieldsBuilder.Add(_mvidField);

            if (_moduleCancellationTokenField != null)
                fieldsBuilder.Add(_moduleCancellationTokenField);

            fieldsBuilder.AddRange(_instrumentationPayloadRootFields.Values);
            fieldsBuilder.Sort(FieldComparer.Instance);
            _orderedSynthesizedFields = fieldsBuilder.ToImmutableAndFree();

            // Sort methods.
            _orderedSynthesizedMethods = _synthesizedMethods.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).AsImmutable();

            // Sort nested types.
            _orderedNestedTypes = _dataFieldTypes.OrderBy(kvp => kvp.Key.Size).ThenBy(kvp => kvp.Key.Alignment).Select(kvp => kvp.Value).OfType<ExplicitSizeStruct>()
                .Concat<Cci.INestedTypeDefinition>(_dataSectionStringLiteralTypes.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value)).AsImmutable();
        }

        internal bool IsFrozen => _frozen != 0;

        /// <summary>
        /// Gets a field that can be used to cache an array allocated to store data from a corresponding <see cref="GetOrAddDataField"/> call.
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
                string name = $"{DataToHex(key.Data)}_A{key.ElementType}";

                return new CachedArrayField(name, this, arrayType);
            });
        }

        internal Cci.IFieldReference CreateArrayCachingField(ImmutableArray<ConstantValue> constants, Cci.IArrayTypeReference arrayType, EmitContext emitContext)
        {
            Debug.Assert(!IsFrozen);
            Cci.PrimitiveTypeCode typeCode = arrayType.GetElementType(emitContext).TypeCode;
            Debug.Assert(typeCode is not Cci.PrimitiveTypeCode.Reference);

            // Call sites will lazily instantiate the array to cache in this field, rather than forcibly instantiating
            // all of them when the private implementation details class is first used.
            return _cachedArrayFieldsForConstants.GetOrAdd((constants, (ushort)typeCode), key =>
            {
                // Hash the data to hex, but then tack on _B(ElementType). This is needed to differentiate multiple fields
                // that may have the same raw data but different array types.
                string name = $"{ConstantsToHex(key.Constants)}_B{key.ElementType}";
                return new CachedArrayField(name, this, arrayType);
            });
        }

        /// <summary>
        /// Gets a struct type of the given size and alignment or creates it if it does not exist yet.
        /// </summary>
        private Cci.ITypeReference GetOrAddDataFieldType(int length, ushort alignment)
        {
            Debug.Assert(!IsFrozen);
            Debug.Assert(alignment is 1 or 2 or 4 or 8);
            Debug.Assert(length != 1 || alignment == 1);

            return _dataFieldTypes.GetOrAdd(
                ((uint)length, Alignment: alignment), key =>
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
        /// boundaries, which accommodates any element type that's relevant), it is necessary for IL rewriters. Such rewriters
        /// also need to ensure an appropriate alignment is maintained for the RVA field, and while they could also simplify
        /// by choosing a worst-case alignment as does the compiler, they may instead use the .pack value as the alignment
        /// to use for that field, since it's an opaque blob with no other indication as to what kind of data is
        /// stored and what alignment might be required.
        /// </param>
        /// <returns>The field. This may have been newly created or may be an existing field previously created for the same data and alignment.</returns>
        internal MappedField GetOrAddDataField(ImmutableArray<byte> data, ushort alignment)
        {
            return _mappedFields.GetOrAdd((data, alignment), static (key, @this) =>
            {
                var (data, alignment) = key;

                Cci.ITypeReference type = @this.GetOrAddDataFieldType(data.Length, alignment);

                // For alignment of 1 (which is used in cases other than in fields for ReadOnlySpan<byte>),
                // just use the hex value of the data hash.  For other alignments, tack on a '2', '4', or '8'
                // accordingly.  As every byte will yield two chars, the odd number of chars used for 2/4/8
                // alignments will never produce a name that conflicts with names for an alignment of 1.
                RoslynDebug.Assert(alignment is 1 or 2 or 4 or 8, $"Unexpected alignment: {alignment}");
                string hex = DataToHex(data);
                string name = alignment switch
                {
                    2 => hex + "2",
                    4 => hex + "4",
                    8 => hex + "8",
                    _ => hex
                };

                return new MappedField(name, @this, type, data);
            },
            this);
        }

        /// <summary>
        /// Gets the field of <see cref="DataSectionStringType"/> or creates one
        /// if the type does not exist yet for the given <paramref name="text"/>.
        /// If the text cannot be encoded, returns <see langword="null"/>.
        /// </summary>
        internal static Cci.IFieldReference? TryGetOrCreateFieldForStringValue(
            string text,
            CommonPEModuleBuilder moduleBuilder,
            SyntaxNode? syntaxNode,
            DiagnosticBag diagnostics)
        {
            if (!text.TryGetUtf8ByteRepresentation(out byte[]? data, out _))
            {
                return null;
            }

            var @this = moduleBuilder.GetPrivateImplClass(syntaxNode, diagnostics);
            return @this._dataSectionStringLiteralTypes.GetOrAdd(text, static (text, arg) =>
            {
                var (@this, data, syntaxNode, diagnostics) = arg;

                string name = "<S>" + @this.DataToHexViaXxHash128(data);

                MappedField dataField = @this.GetOrAddDataField(data, alignment: 1);

                Cci.IMethodDefinition bytesToStringHelper = @this.GetOrSynthesizeBytesToStringHelper(diagnostics);

                var previousText = @this._dataSectionStringLiteralNames.GetOrAdd(name, text);
                if (previousText != text)
                {
                    // If there is a hash collision, we cannot fallback to normal string literal emit strategy
                    // because the selection of which literal would get which emit strategy would not be deterministic.
                    var messageProvider = @this.ModuleBuilder.CommonCompilation.MessageProvider;
                    diagnostics.Add(messageProvider.CreateDiagnostic(
                        messageProvider.ERR_DataSectionStringLiteralHashCollision,
                        syntaxNode?.GetLocation() ?? Location.None,
                        previousText[..Math.Min(previousText.Length, 500)]));
                }

                return new DataSectionStringType(
                    name: name,
                    containingType: @this,
                    dataField: dataField,
                    bytesToStringHelper: bytesToStringHelper,
                    diagnostics: diagnostics);
            },
            (@this, ImmutableCollectionsMarshal.AsImmutableArray(data), syntaxNode, diagnostics)).Field;
        }

        /// <summary>
        /// Gets the <see cref="BytesToStringHelper"/> or creates it if it does not exist yet.
        /// </summary>
        private Cci.IMethodDefinition GetOrSynthesizeBytesToStringHelper(DiagnosticBag diagnostics)
        {
            var method = GetMethod(SynthesizedBytesToStringFunctionName);

            if (method is null)
            {
                var compilation = ModuleBuilder.CommonCompilation;
                var encodingUtf8 = getWellKnownTypeMember(compilation, WellKnownMember.System_Text_Encoding__get_UTF8);
                var encodingGetString = getWellKnownTypeMember(compilation, WellKnownMember.System_Text_Encoding__GetString);

                TryAddSynthesizedMethod(BytesToStringHelper.Create(
                    moduleBuilder: ModuleBuilder,
                    containingType: this,
                    encodingUtf8: encodingUtf8,
                    encodingGetString: encodingGetString,
                    diagnostics: diagnostics));

                method = GetMethod(SynthesizedBytesToStringFunctionName);
                Debug.Assert(method is not null);
            }

            return method;

            static Cci.IMethodReference getWellKnownTypeMember(
                Compilation compilation,
                WellKnownMember member)
            {
                ISymbolInternal? symbol = compilation.CommonGetWellKnownTypeMember(member);
                Debug.Assert(symbol is not null, "The emit layer should check the helpers exist.");
                return (Cci.IMethodReference)symbol.GetCciAdapter();
            }
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

        internal Cci.IFieldReference GetModuleCancellationToken(Cci.ITypeReference cancellationTokenType)
        {
            if (_moduleCancellationTokenField == null)
            {
                Debug.Assert(!IsFrozen);
                Interlocked.CompareExchange(ref _moduleCancellationTokenField, new ModuleCancellationTokenField(this, cancellationTokenType), null);
            }

            Debug.Assert(_moduleCancellationTokenField.Type == cancellationTokenType);
            return _moduleCancellationTokenField;
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

        // Get method by name, if one exists. Otherwise return null.
        internal Cci.IMethodDefinition? GetMethod(string name)
        {
            Cci.IMethodDefinition? method;
            _synthesizedMethods.TryGetValue(name, out method);
            return method;
        }

        public override IEnumerable<Cci.INestedTypeDefinition> GetNestedTypes(EmitContext context)
        {
            Debug.Assert(IsFrozen);
            return _orderedNestedTypes;
        }

        public override string ToString() => this.Name;

        public override Cci.ITypeReference GetBaseClass(EmitContext context) => SystemObject;

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
            Debug.Assert(context.Module == ModuleBuilder);
            return ModuleBuilder;
        }

        public string NamespaceName => string.Empty;

        private static string DataToHex(ImmutableArray<byte> data)
        {
            ImmutableArray<byte> hash = CryptographicHashProvider.ComputeSourceHash(data);
            return HashToHex(hash.AsSpan());
        }

        private string DataToHexViaXxHash128(ImmutableArray<byte> data)
        {
            if (ModuleBuilder.EmitOptions.TestOnly_DataToHexViaXxHash128 is { } handler)
            {
                return handler(data);
            }

            Span<byte> hash = stackalloc byte[sizeof(ulong) * 2];
            int bytesWritten = XxHash128.Hash(data.AsSpan(), hash);
            Debug.Assert(bytesWritten == hash.Length);
            return HashToHex(hash);
        }

        private static string ConstantsToHex(ImmutableArray<ConstantValue> constants)
        {
            ImmutableArray<byte> hash = CryptographicHashProvider.ComputeSourceHash(constants);
            return HashToHex(hash.AsSpan());
        }

        public static string HashToHex(ReadOnlySpan<byte> hash)
        {
#if NET9_0_OR_GREATER
            return string.Create(hash.Length * 2, hash, (destination, hash) => toHex(hash, destination));
#else
            char[] c = new char[hash.Length * 2];
            toHex(hash, c);
            return new string(c);
#endif

            static void toHex(ReadOnlySpan<byte> source, Span<char> destination)
            {
                int i = 0;
                foreach (var b in source)
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

        private sealed class ConstantValueAndUShortEqualityComparer : EqualityComparer<(ImmutableArray<ConstantValue> Constants, ushort Value)>
        {
            public static readonly ConstantValueAndUShortEqualityComparer Instance = new ConstantValueAndUShortEqualityComparer();

            private ConstantValueAndUShortEqualityComparer() { }

            public override bool Equals((ImmutableArray<ConstantValue> Constants, ushort Value) x, (ImmutableArray<ConstantValue> Constants, ushort Value) y)
            {
                if (x.Value != y.Value)
                {
                    return false;
                }

                if (x.Constants.Length != y.Constants.Length)
                {
                    return false;
                }

                for (int i = 0; i < x.Constants.Length; i++)
                {
                    if (x.Constants[i] != y.Constants[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public override int GetHashCode((ImmutableArray<ConstantValue> Constants, ushort Value) obj)
            {
                int hash = 0;
                foreach (var constant in obj.Constants)
                {
                    Hash.Combine(constant.GetHashCode(), hash);
                }

                // purposefully not including Value, as it won't add meaningfully to the hash code
                return hash;
            }
        }
    }

    /// <summary>
    /// Simple struct type with explicit size and no members.
    /// </summary>
    internal sealed class ExplicitSizeStruct : NestedTypeDefinition
    {
        private readonly uint _size;
        private readonly ushort _alignment;
        private readonly Cci.INamedTypeDefinition _containingType;
        private readonly Cci.ITypeReference _sysValueType;

        internal ExplicitSizeStruct(uint size, ushort alignment, PrivateImplementationDetails containingType, Cci.ITypeReference sysValueType)
        {
            RoslynDebug.Assert(alignment is 1 or 2 or 4 or 8, $"Unexpected alignment: {alignment}");

            _size = size;
            _alignment = alignment;
            _containingType = containingType;
            _sysValueType = sysValueType;
        }

        public override ushort Alignment => _alignment;

        public override Cci.ITypeReference GetBaseClass(EmitContext context) => _sysValueType;

        public override LayoutKind Layout => LayoutKind.Explicit;

        public override uint SizeOf => _size;

        public override string Name => _alignment == 1 ?
            $"__StaticArrayInitTypeSize={_size}" :
            $"__StaticArrayInitTypeSize={_size}_Align={_alignment}";

        public override Cci.ITypeDefinition ContainingTypeDefinition => _containingType;

        public override Cci.TypeMemberVisibility Visibility => Cci.TypeMemberVisibility.Assembly;

        public override bool IsValueType => true;
    }

    /// <summary>
    /// A type synthesized for each eligible string literal to hold the lazily-initialized string.
    ///
    /// https://github.com/dotnet/roslyn/blob/main/docs/features/string-literals-data-section.md
    /// </summary>
    internal sealed class DataSectionStringType : NestedTypeDefinition
    {
        private readonly string _name;
        private readonly PrivateImplementationDetails _containingType;
        private readonly ImmutableArray<Cci.IFieldDefinition> _fields;
        private readonly ImmutableArray<Cci.IMethodDefinition> _methods;

        public DataSectionStringType(
            string name,
            PrivateImplementationDetails containingType,
            MappedField dataField,
            Cci.IMethodDefinition bytesToStringHelper,
            DiagnosticBag diagnostics)
        {
            _name = name;
            _containingType = containingType;

            var stringField = new DataSectionStringField("s", this);

            var staticConstructor = synthesizeStaticConstructor(containingType.ModuleBuilder, this, dataField, stringField, bytesToStringHelper, diagnostics);

            _fields = [stringField];
            _methods = [staticConstructor];

            static Cci.IMethodDefinition synthesizeStaticConstructor(
                CommonPEModuleBuilder module,
                Cci.ITypeDefinition containingType,
                MappedField dataField,
                DataSectionStringField stringField,
                Cci.IMethodDefinition bytesToStringHelper,
                DiagnosticBag diagnostics)
            {
                var ilBuilder = new ILBuilder(
                    module,
                    new LocalSlotManager(slotAllocator: null),
                    diagnostics,
                    OptimizationLevel.Release,
                    areLocalsZeroed: false);

                // Push the `byte*` field's address.
                ilBuilder.EmitOpCode(ILOpCode.Ldsflda);
                ilBuilder.EmitToken(dataField, null);

                // Push the byte size.
                ilBuilder.EmitIntConstant(dataField.MappedData.Length);

                // Call `<PrivateImplementationDetails>.BytesToString(byte*, int)`.
                ilBuilder.EmitOpCode(ILOpCode.Call, -1);
                ilBuilder.EmitToken(bytesToStringHelper, null);

                // Store into the corresponding `string` field.
                ilBuilder.EmitOpCode(ILOpCode.Stsfld);
                ilBuilder.EmitToken(stringField, null);

                ilBuilder.EmitRet(isVoid: true);
                ilBuilder.Realize();

                return new Cci.StaticConstructor(containingType, ilBuilder.MaxStack, ilBuilder.RealizedIL);
            }
        }

        public Cci.IFieldDefinition Field => _fields[0];
        public override string Name => _name;
        public override Cci.ITypeDefinition ContainingTypeDefinition => _containingType;
        public override Cci.TypeMemberVisibility Visibility => Cci.TypeMemberVisibility.Assembly;
        public override Cci.ITypeReference GetBaseClass(EmitContext context) => _containingType.SystemObject;
        public override IEnumerable<Cci.IFieldDefinition> GetFields(EmitContext context) => _fields;
        public override IEnumerable<Cci.IMethodDefinition> GetMethods(EmitContext context) => _methods;
        public override bool IsBeforeFieldInit => true;

        private sealed class DataSectionStringField(
            string name, Cci.INamedTypeDefinition containingType)
            : SynthesizedStaticFieldBase(name, containingType)
        {
            public override ImmutableArray<byte> MappedData => default;
            public override bool IsReadOnly => true;

            public override Cci.ITypeReference GetType(EmitContext context)
            {
                return context.Module.GetPlatformType(Cci.PlatformType.SystemString, context);
            }

            public override string ToString()
            {
                return $"string {(object?)ContainingTypeDefinition.GetInternalSymbol() ?? ContainingTypeDefinition}.{this.Name}";
            }
        }
    }

    internal abstract class SynthesizedStaticField : SynthesizedStaticFieldBase
    {
        private readonly Cci.ITypeReference _type;

        internal SynthesizedStaticField(string name, Cci.INamedTypeDefinition containingType, Cci.ITypeReference type)
            : base(name, containingType)
        {
            RoslynDebug.Assert(type != null);

            _type = type;
        }

        internal Cci.ITypeReference Type => _type;

        public override string ToString() => $"{(object?)_type.GetInternalSymbol() ?? _type} {(object?)ContainingTypeDefinition.GetInternalSymbol() ?? ContainingTypeDefinition}.{this.Name}";

        public override Cci.ITypeReference GetType(EmitContext context) => _type;
    }

    internal abstract class SynthesizedStaticFieldBase : Cci.IFieldDefinition
    {
        private readonly Cci.INamedTypeDefinition _containingType;
        private readonly string _name;

        internal SynthesizedStaticFieldBase(string name, Cci.INamedTypeDefinition containingType)
        {
            RoslynDebug.Assert(name != null);
            RoslynDebug.Assert(containingType != null);

            _containingType = containingType;
            _name = name;
        }

        public MetadataConstant? GetCompileTimeValue(EmitContext context) => null;

        public abstract ImmutableArray<byte> MappedData { get; }

        public bool IsEncDeleted => false;

        public bool IsCompileTimeConstant => false;

        public bool IsNotSerialized => false;

        public abstract bool IsReadOnly { get; }

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

        public abstract Cci.ITypeReference GetType(EmitContext context);

        public ImmutableArray<Cci.ICustomModifier> RefCustomModifiers => ImmutableArray<Cci.ICustomModifier>.Empty;

        public bool IsByReference => false;

        public Cci.IFieldDefinition GetResolvedField(EmitContext context) => this;

        public Cci.ISpecializedFieldReference? AsSpecializedFieldReference => null;

        public MetadataConstant Constant
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public sealed override bool Equals(object? obj)
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw ExceptionUtilities.Unreachable();
        }

        public sealed override int GetHashCode()
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw ExceptionUtilities.Unreachable();
        }
    }

    internal sealed class ModuleVersionIdField : SynthesizedStaticField
    {
        internal ModuleVersionIdField(Cci.INamedTypeDefinition containingType, Cci.ITypeReference type)
            : base("MVID", containingType, type)
        {
        }

        public override ImmutableArray<byte> MappedData => default(ImmutableArray<byte>);
        public override bool IsReadOnly => true;
    }

    /// <summary>
    /// Synthesized by <see cref="InstrumentationKind.ModuleCancellation"/> instrumentation.
    /// </summary>
    internal sealed class ModuleCancellationTokenField(Cci.INamedTypeDefinition containingType, Cci.ITypeReference type)
        : SynthesizedStaticField("ModuleCancellationToken", containingType, type)
    {
        public override ImmutableArray<byte> MappedData => default;
        public override bool IsReadOnly => false;
    }

    internal sealed class InstrumentationPayloadRootField : SynthesizedStaticField
    {
        internal InstrumentationPayloadRootField(Cci.INamedTypeDefinition containingType, int analysisIndex, Cci.ITypeReference payloadType)
            : base("PayloadRoot" + analysisIndex.ToString(System.Globalization.CultureInfo.InvariantCulture), containingType, payloadType)
        {
        }

        public override ImmutableArray<byte> MappedData => default(ImmutableArray<byte>);
        public override bool IsReadOnly => true;
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
        public override bool IsReadOnly => true;
    }

    /// <summary>
    /// Definition of a field for storing an array caching the data from a metadata block or array of constants.
    /// </summary>
    internal sealed class CachedArrayField : SynthesizedStaticField
    {
        internal CachedArrayField(string name, Cci.INamedTypeDefinition containingType, Cci.ITypeReference type)
            : base(name, containingType, type)
        {
        }

        public override ImmutableArray<byte> MappedData => default(ImmutableArray<byte>);
        public override bool IsReadOnly => false;
    }

    internal abstract class NestedTypeDefinition : DefaultTypeDef, Cci.INestedTypeDefinition
    {
        public sealed override string ToString()
            => ContainingTypeDefinition.ToString() + "." + this.Name;

        public sealed override void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        public abstract string Name { get; }

        public abstract Cci.ITypeDefinition ContainingTypeDefinition { get; }

        public abstract Cci.TypeMemberVisibility Visibility { get; }

        public Cci.ITypeReference GetContainingType(EmitContext context) => ContainingTypeDefinition;

        public sealed override Cci.INestedTypeDefinition AsNestedTypeDefinition(EmitContext context) => this;

        public sealed override Cci.INestedTypeReference AsNestedTypeReference => this;

        bool Cci.INestedTypeReference.InheritsEnclosingTypeTypeParameters => true;
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

        public bool IsEncDeleted => false;

        public bool IsAbstract => false;

        public virtual bool IsBeforeFieldInit => false;

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

        public abstract Cci.ITypeReference GetBaseClass(EmitContext context);

        public virtual LayoutKind Layout => LayoutKind.Auto;

        public virtual uint SizeOf => 0;

        public abstract void Dispatch(Cci.MetadataVisitor visitor);

        public virtual bool IsValueType => false;

        public sealed override bool Equals(object? obj)
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw ExceptionUtilities.Unreachable();
        }

        public sealed override int GetHashCode()
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw ExceptionUtilities.Unreachable();
        }
    }

    /// <summary>
    /// A helper method used in static constructor of <see cref="DataSectionStringType"/>.
    /// to share IL and hence save assembly size.
    /// </summary>
    /// <remarks>
    /// The method is equivalent to:
    /// <code>
    /// private unsafe static string BytesToString(byte* bytes, int length)
    /// {
    ///     return Encoding.UTF8.GetString(bytes, length);
    /// }
    /// </code>
    /// </remarks>
    file sealed class BytesToStringHelper : Cci.MethodDefinitionBase
    {
        private readonly ImmutableArray<Cci.IParameterDefinition> _parameters;

        private BytesToStringHelper(
            Cci.INamespaceTypeDefinition containingType,
            Cci.IMethodReference encodingGetString,
            ushort maxStack,
            ImmutableArray<byte> il)
            : base(containingType, maxStack, il)
        {
            _parameters =
            [
                new BytesParameter(encodingGetString), // byte* bytes
                LengthParameterDefinition.Instance,    // int length
            ];
        }

        public static BytesToStringHelper Create(
            CommonPEModuleBuilder moduleBuilder,
            Cci.INamespaceTypeDefinition containingType,
            Cci.IMethodReference encodingUtf8,
            Cci.IMethodReference encodingGetString,
            DiagnosticBag diagnostics)
        {
            var ilBuilder = new ILBuilder(
                moduleBuilder,
                new LocalSlotManager(slotAllocator: null),
                diagnostics,
                OptimizationLevel.Release,
                areLocalsZeroed: false);

            // Call `Encoding.get_UTF8()`.
            ilBuilder.EmitOpCode(ILOpCode.Call, 1);
            ilBuilder.EmitToken(encodingUtf8, null);

            // Push the `byte*`.
            ilBuilder.EmitOpCode(ILOpCode.Ldarg_0);

            // Push the byte size.
            ilBuilder.EmitOpCode(ILOpCode.Ldarg_1);

            // Call `Encoding.GetString(byte*, int)`.
            ilBuilder.EmitOpCode(ILOpCode.Callvirt, -2);
            ilBuilder.EmitToken(encodingGetString, null);

            // Return.
            ilBuilder.EmitRet(isVoid: false);
            ilBuilder.Realize();

            return new BytesToStringHelper(
                containingType: containingType,
                encodingGetString: encodingGetString,
                maxStack: ilBuilder.MaxStack,
                il: ilBuilder.RealizedIL);
        }

        public override string Name => PrivateImplementationDetails.SynthesizedBytesToStringFunctionName;
        public override Cci.TypeMemberVisibility Visibility => Cci.TypeMemberVisibility.Private;
        public override ImmutableArray<Cci.IParameterDefinition> Parameters => _parameters;
        public override Cci.ITypeReference GetType(EmitContext context) => context.Module.GetPlatformType(Cci.PlatformType.SystemString, context);

        private sealed class BytesParameter(
            Cci.IMethodReference encodingGetString)
            : Cci.ParameterDefinitionBase
        {
            private readonly Cci.IMethodReference _encodingGetString = encodingGetString;

            public override ushort Index => 0;
            public override string Name => "bytes";
            public override Cci.ITypeReference GetType(EmitContext context)
            {
                return _encodingGetString.GetParameters(context)[0].GetType(context);
            }
        }

        private sealed class LengthParameterDefinition : Cci.ParameterDefinitionBase
        {
            private LengthParameterDefinition() { }

            public static readonly LengthParameterDefinition Instance = new LengthParameterDefinition();

            public override ushort Index => 1;
            public override string Name => "length";
            public override Cci.ITypeReference GetType(EmitContext context) => context.Module.GetPlatformType(Cci.PlatformType.SystemInt32, context);
        }
    }
}
