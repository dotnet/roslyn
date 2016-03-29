// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// TypeDefinition that represents &lt;PrivateImplementationDetails&gt; class.
    /// The main purpose of this class so far is to contain mapped fields and their types.
    /// </summary>
    internal sealed class PrivateImplementationDetails : DefaultTypeDef, Cci.INamespaceTypeDefinition
    {
        // Note: Dev11 uses the source method token as the prefix, rather than a fixed token
        // value, and data field offsets are unique within the method, not across all methods.
        internal const string SynthesizedStringHashFunctionName = "ComputeStringHash";

        private readonly Cci.IModule _moduleBuilder;                 //the module builder
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

        // fields mapped to metadata blocks
        private ImmutableArray<SynthesizedStaticField> _orderedSynthesizedFields;
        private readonly ConcurrentDictionary<ImmutableArray<byte>, MappedField> _mappedFields =
            new ConcurrentDictionary<ImmutableArray<byte>, MappedField>(ByteSequenceComparer.Instance);

        private ModuleVersionIdField _mvidField;

        // synthesized methods
        private ImmutableArray<Cci.IMethodDefinition> _orderedSynthesizedMethods;
        private readonly ConcurrentDictionary<string, Cci.IMethodDefinition> _synthesizedMethods =
            new ConcurrentDictionary<string, Cci.IMethodDefinition>();

        // field types for different block sizes.
        private ImmutableArray<Cci.ITypeReference> _orderedProxyTypes;
        private readonly ConcurrentDictionary<uint, Cci.ITypeReference> _proxyTypes = new ConcurrentDictionary<uint, Cci.ITypeReference>();

        internal PrivateImplementationDetails(
            Cci.IModule moduleBuilder,
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
            Debug.Assert(systemObject != null);
            Debug.Assert(systemValueType != null);

            _moduleBuilder = moduleBuilder;
            _systemObject = systemObject;
            _systemValueType = systemValueType;

            _systemInt8Type = systemInt8Type;
            _systemInt16Type = systemInt16Type;
            _systemInt32Type = systemInt32Type;
            _systemInt64Type = systemInt64Type;

            _compilerGeneratedAttribute = compilerGeneratedAttribute;

            var isNetModule = moduleBuilder.AsAssembly == null;
            _name = GetClassName(moduleName, submissionSlotIndex, isNetModule);
        }

        private static string GetClassName(string moduleName, int submissionSlotIndex, bool isNetModule)
        {
            // we include the module name in the name of the PrivateImplementationDetails class so that more than
            // one of them can be included in an assembly as part of netmodules.    
            var name = isNetModule ?
                        $"<PrivateImplementationDetails><{MetadataHelpers.MangleForTypeNameIfNeeded(moduleName)}>" :
                        $"<PrivateImplementationDetails>";

            if (submissionSlotIndex >= 0)
            {
                name += submissionSlotIndex.ToString();
            }

            return name;
        }

        internal void Freeze()
        {
            var wasFrozen = Interlocked.Exchange(ref _frozen, 1);
            if (wasFrozen != 0)
            {
                throw new InvalidOperationException();
            }

            // Sort fields.
            ArrayBuilder<SynthesizedStaticField> fieldsBuilder = ArrayBuilder<SynthesizedStaticField>.GetInstance(_mappedFields.Count + (_mvidField != null ? 1 : 0));
            fieldsBuilder.AddRange(_mappedFields.Values);
            if (_mvidField != null)
            {
                fieldsBuilder.Add(_mvidField);
            }
            fieldsBuilder.Sort(FieldComparer.Instance);
            _orderedSynthesizedFields = fieldsBuilder.ToImmutableAndFree();

            // Sort methods.
            _orderedSynthesizedMethods = _synthesizedMethods.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).AsImmutable();
           
            // Sort proxy types.
            _orderedProxyTypes = _proxyTypes.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).AsImmutable();
        }

        private bool IsFrozen => _frozen != 0;

        internal Cci.IFieldReference CreateDataField(ImmutableArray<byte> data)
        {
            Debug.Assert(!IsFrozen);
            Cci.ITypeReference type = _proxyTypes.GetOrAdd((uint)data.Length, GetStorageStruct);
            return _mappedFields.GetOrAdd(data, data0 =>
            {
                var name = GenerateDataFieldName(data0);
                var newField = new MappedField(name, this, type, data0);
                return newField;
            });
        }

        private Cci.ITypeReference GetStorageStruct(uint size)
        {
            switch (size)
            {
                case 1:
                    return _systemInt8Type ?? new ExplicitSizeStruct(1, this, _systemValueType);
                case 2:
                    return _systemInt16Type ?? new ExplicitSizeStruct(2, this, _systemValueType);
                case 4:
                    return _systemInt32Type ?? new ExplicitSizeStruct(4, this, _systemValueType);
                case 8:
                    return _systemInt64Type ?? new ExplicitSizeStruct(8, this, _systemValueType);
            }

            return new ExplicitSizeStruct(size, this, _systemValueType);
        }

        internal Cci.IFieldReference GetModuleVersionId(Cci.ITypeReference mvidType)
        {
            if (_mvidField == null)
            {
                Interlocked.CompareExchange(ref _mvidField, new ModuleVersionIdField(this, mvidType), null);
            }

            Debug.Assert(_mvidField.Type.Equals(mvidType));
            return _mvidField;
        }

        // Add a new synthesized method indexed by its name if the method isn't already present.
        internal bool TryAddSynthesizedMethod(Cci.IMethodDefinition method)
        {
            Debug.Assert(!IsFrozen);
            return _synthesizedMethods.TryAdd(method.Name, method);
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
        internal Cci.IMethodDefinition GetMethod(string name)
        {
            Cci.IMethodDefinition method;
            _synthesizedMethods.TryGetValue(name, out method);
            return method;
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

        internal static string GenerateDataFieldName(ImmutableArray<byte> data)
        {
            var hash = CryptographicHashProvider.ComputeSha1(data);
            char[] c = new char[hash.Length * 2];
            int i = 0;
            foreach (var b in hash)
            {
                c[i++] = Hexchar(b >> 4);
                c[i++] = Hexchar(b & 0xF);
            }

            return new string(c);
        }

        private static char Hexchar(int x)
            => (char)((x <= 9) ? (x + '0') : (x + ('A' - 10)));

        private sealed class FieldComparer : IComparer<SynthesizedStaticField>
        {
            public static readonly FieldComparer Instance = new FieldComparer();

            private FieldComparer()
            {
            }

            public int Compare(SynthesizedStaticField x, SynthesizedStaticField y)
            {
                // Fields are always synthesized with non-null names.
                Debug.Assert(x.Name != null && y.Name != null);
                return x.Name.CompareTo(y.Name);
            }
        }
    }

    /// <summary>
    /// Simple struct type with explicit size and no members.
    /// </summary>
    internal sealed class ExplicitSizeStruct : DefaultTypeDef, Cci.INestedTypeDefinition
    {
        private readonly uint _size;
        private readonly Cci.INamedTypeDefinition _containingType;
        private readonly Cci.ITypeReference _sysValueType;

        internal ExplicitSizeStruct(uint size, PrivateImplementationDetails containingType, Cci.ITypeReference sysValueType)
        {
            _size = size;
            _containingType = containingType;
            _sysValueType = sysValueType;
        }

        public override string ToString()
            => _containingType.ToString() + "." + this.Name;

        override public ushort Alignment => 1;

        override public Cci.ITypeReference GetBaseClass(EmitContext context) => _sysValueType;

        override public LayoutKind Layout => LayoutKind.Explicit;

        override public uint SizeOf => _size;

        override public void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        public string Name => "__StaticArrayInitTypeSize=" + _size;

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
            Debug.Assert(name != null);
            Debug.Assert(containingType != null);
            Debug.Assert(type != null);

            _containingType = containingType;
            _type = type;
            _name = name;
        }

        public override string ToString() => $"{_type} {_containingType}.{this.Name}";

        public Cci.IMetadataConstant GetCompileTimeValue(EmitContext context) => null;

        public abstract ImmutableArray<byte> MappedData { get; }

        public bool IsCompileTimeConstant => false;

        public bool IsNotSerialized => false;

        public bool IsReadOnly => true;

        public bool IsRuntimeSpecial => false;

        public bool IsSpecialName => false;

        public bool IsStatic => true;

        public bool IsMarshalledExplicitly => false;

        public Cci.IMarshallingInformation MarshallingInformation => null;

        public ImmutableArray<byte> MarshallingDescriptor => default(ImmutableArray<byte>);

        public uint Offset
        {
            get { throw ExceptionUtilities.Unreachable; }
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
            throw ExceptionUtilities.Unreachable;
        }

        public string Name => _name;

        public bool IsContextualNamedEntity => false;

        public Cci.ITypeReference GetType(EmitContext context) => _type;

        internal Cci.ITypeReference Type => _type;

        public Cci.IFieldDefinition GetResolvedField(EmitContext context) => this;

        public Cci.ISpecializedFieldReference AsSpecializedFieldReference => null;

        public Cci.IMetadataConstant Constant
        {
            get { throw ExceptionUtilities.Unreachable; }
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
    /// Just a default implementation of a type definition.
    /// </summary>
    internal abstract class DefaultTypeDef : Cci.ITypeDefinition
    {
        public IEnumerable<Cci.IEventDefinition> Events
            => SpecializedCollections.EmptyEnumerable<Cci.IEventDefinition>();

        public IEnumerable<Cci.MethodImplementation> GetExplicitImplementationOverrides(EmitContext context)
            => SpecializedCollections.EmptyEnumerable<Cci.MethodImplementation>();

        virtual public IEnumerable<Cci.IFieldDefinition> GetFields(EmitContext context)
            => SpecializedCollections.EmptyEnumerable<Cci.IFieldDefinition>();

        public IEnumerable<Cci.IGenericTypeParameter> GenericParameters
            => SpecializedCollections.EmptyEnumerable<Cci.IGenericTypeParameter>();

        public ushort GenericParameterCount => 0;

        public bool HasDeclarativeSecurity => false;

        public IEnumerable<Cci.ITypeReference> Interfaces(EmitContext context)
            => SpecializedCollections.EmptyEnumerable<Cci.ITypeReference>();

        public bool IsAbstract => false;

        public bool IsBeforeFieldInit => false;

        public bool IsComObject => false;

        public bool IsGeneric => false;

        public bool IsInterface => false;

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

        public bool IsEnum => false;

        public Cci.ITypeDefinition GetResolvedType(EmitContext context) => this;

        public Cci.PrimitiveTypeCode TypeCode(EmitContext context) => Cci.PrimitiveTypeCode.NotPrimitive;

        public TypeDefinitionHandle TypeDef
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public Cci.IGenericMethodParameterReference AsGenericMethodParameterReference => null;

        public Cci.IGenericTypeInstanceReference AsGenericTypeInstanceReference => null;

        public Cci.IGenericTypeParameterReference AsGenericTypeParameterReference => null;

        public virtual Cci.INamespaceTypeDefinition AsNamespaceTypeDefinition(EmitContext context) => null;

        public virtual Cci.INamespaceTypeReference AsNamespaceTypeReference => null;

        public Cci.ISpecializedNestedTypeReference AsSpecializedNestedTypeReference => null;

        public virtual Cci.INestedTypeDefinition AsNestedTypeDefinition(EmitContext context) => null;

        public virtual Cci.INestedTypeReference AsNestedTypeReference => null;

        public Cci.ITypeDefinition AsTypeDefinition(EmitContext context) => this;

        public bool MangleName => false;

        public virtual ushort Alignment => 0;

        public virtual Cci.ITypeReference GetBaseClass(EmitContext context)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public virtual LayoutKind Layout => LayoutKind.Auto;

        public virtual uint SizeOf => 0;

        public virtual void Dispatch(Cci.MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public virtual bool IsValueType => false;
    }
}
