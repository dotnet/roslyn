// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// TypeDefinition that represents &lt;PrivateImplementationDetails&gt; class.
    /// The main purpose of this class so far is to contain mapped fields and their types.
    /// </summary>
    internal sealed class PrivateImplementationDetails : DefaultTypeDef, Microsoft.Cci.INamespaceTypeDefinition
    {
        // Note: Dev11 uses the source method token as the prefix, rather than a fixed token
        // value, and data field offsets are unique within the method, not across all methods.
        private const string MemberNamePrefix = "$$method0x6000001-";
        internal const string SynthesizedStringHashFunctionName = MemberNamePrefix + "ComputeStringHash";

        private readonly Microsoft.Cci.IModule module;                     //parent unit
        private readonly Microsoft.Cci.ITypeReference systemObject;        //base type
        private readonly Microsoft.Cci.ITypeReference systemValueType;     //base for nested structs

        private readonly Microsoft.Cci.ITypeReference systemInt8Type;         //for metadata init of short arrays
        private readonly Microsoft.Cci.ITypeReference systemInt16Type;        //for metadata init of short arrays
        private readonly Microsoft.Cci.ITypeReference systemInt32Type;        //for metadata init of short arrays
        private readonly Microsoft.Cci.ITypeReference systemInt64Type;        //for metadata init of short arrays

        private readonly Microsoft.Cci.ICustomAttribute compilerGeneratedAttribute;

        private readonly string name;

        // Once frozen the collections of fields, methods and types are immutable.
        private int frozen;

        // fields mapped to metadata blocks
        private readonly List<MappedField> mappedFields = new List<MappedField>();

        // synthesized methods
        private readonly ConcurrentDictionary<string, Microsoft.Cci.IMethodDefinition> synthesizedMethods =
            new ConcurrentDictionary<string, Microsoft.Cci.IMethodDefinition>();

        // field types for different block sizes.
        private readonly ConcurrentDictionary<uint, Microsoft.Cci.ITypeReference> proxyTypes = new ConcurrentDictionary<uint, Microsoft.Cci.ITypeReference>();

        internal PrivateImplementationDetails(
            Microsoft.Cci.IModule module,
            Microsoft.Cci.ITypeReference systemObject,
            Microsoft.Cci.ITypeReference systemValueType,
            Microsoft.Cci.ITypeReference systemInt8Type,
            Microsoft.Cci.ITypeReference systemInt16Type,
            Microsoft.Cci.ITypeReference systemInt32Type,
            Microsoft.Cci.ITypeReference systemInt64Type,
            Microsoft.Cci.ICustomAttribute compilerGeneratedAttribute)
        {
            Debug.Assert(module != null);
            Debug.Assert(systemObject != null);
            Debug.Assert(systemValueType != null);

            this.module = module;
            this.systemObject = systemObject;
            this.systemValueType = systemValueType;

            this.systemInt8Type = systemInt8Type;
            this.systemInt16Type = systemInt16Type;
            this.systemInt32Type = systemInt32Type;
            this.systemInt64Type = systemInt64Type;

            this.compilerGeneratedAttribute = compilerGeneratedAttribute;
            this.name = GetClassName(module.PersistentIdentifier);
        }

        internal static string GetClassName(Guid persistentIdentifier)
        {
            return "<PrivateImplementationDetails>" + persistentIdentifier.ToString("B");
        }

        internal void Freeze()
        {
            var wasFrozen = Interlocked.Exchange(ref this.frozen, 1);
            if (wasFrozen != 0)
            {
                //TODO EDMAURER consider what to do as part of synchronization review
                throw new InvalidOperationException();
            }
        }

        private bool IsFrozen
        {
            get { return frozen != 0; }
        }

        internal Microsoft.Cci.IFieldReference CreateDataField(byte[] data)
        {
            Debug.Assert(!IsFrozen);

            Microsoft.Cci.ITypeReference type = this.proxyTypes.GetOrAdd((uint)data.Length, size => GetStorageStruct(size));

            DebuggerUtilities.CallBeforeAcquiringLock(); //see method comment

            var block = new MetadataBlock(data);

            //This object may be accessed concurrently
            //it is not expected to have a lot of contention here so we will just use lock
            //if it becomes an issue we can switch to lock-free data structures.
            lock (this.mappedFields)
            {
                var name = GenerateDataFieldName(this.mappedFields.Count);
                var newField = new MappedField(name, this, type, block);
                this.mappedFields.Add(newField);

                return newField;
            }
        }

        private Microsoft.Cci.ITypeReference GetStorageStruct(uint size)
        {
            switch (size)
            {
                case 1:
                    return this.systemInt8Type ?? new ExplicitSizeStruct(1, this, this.systemValueType);
                case 2:
                    return this.systemInt16Type ?? new ExplicitSizeStruct(2, this, this.systemValueType);
                case 4:
                    return this.systemInt32Type ?? new ExplicitSizeStruct(4, this, this.systemValueType);
                case 8:
                    return this.systemInt64Type ?? new ExplicitSizeStruct(8, this, this.systemValueType);
            }

            return new ExplicitSizeStruct(size, this, this.systemValueType);
        }


        // Add a new synthesized method indexed by it's name if the method isn't already present.
        internal bool TryAddSynthesizedMethod(Microsoft.Cci.IMethodDefinition method)
        {
            Debug.Assert(!IsFrozen);
            return this.synthesizedMethods.TryAdd(method.Name, method);
        }

        public override IEnumerable<Microsoft.Cci.IFieldDefinition> GetFields(Microsoft.CodeAnalysis.Emit.Context context)
        {
            Debug.Assert(IsFrozen);
            return mappedFields;
        }

        public override IEnumerable<Microsoft.Cci.IMethodDefinition> GetMethods(Microsoft.CodeAnalysis.Emit.Context context)
        {
            Debug.Assert(IsFrozen);
            return synthesizedMethods.Values;
        }

        // Get method by name, if one exists. Otherwise return null.
        internal Microsoft.Cci.IMethodDefinition GetMethod(string name)
        {
            Microsoft.Cci.IMethodDefinition method;
            synthesizedMethods.TryGetValue(name, out method);
            return method;
        }

        public override IEnumerable<Microsoft.Cci.INestedTypeDefinition> GetNestedTypes(Microsoft.CodeAnalysis.Emit.Context context)
        {
            Debug.Assert(IsFrozen);
            return System.Linq.Enumerable.OfType<ExplicitSizeStruct>(this.proxyTypes.Values);
        }

        public override string ToString()
        {
            return this.Name;
        }

        public override Microsoft.Cci.ITypeReference GetBaseClass(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this.systemObject;
        }

        public override IEnumerable<Microsoft.Cci.ICustomAttribute> GetAttributes(Microsoft.CodeAnalysis.Emit.Context context)
        {
            if (compilerGeneratedAttribute != null)
            {
                return SpecializedCollections.SingletonEnumerable(this.compilerGeneratedAttribute);
            }

            return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.ICustomAttribute>();
        }

        public override void Dispatch(Microsoft.Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.INamespaceTypeDefinition)this);
        }

        public override Microsoft.Cci.INamespaceTypeDefinition AsNamespaceTypeDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this;
        }

        public override Microsoft.Cci.INamespaceTypeReference AsNamespaceTypeReference
        {
            get { return this; }
        }

        public string Name
        {
            get { return this.name; }
        }

        public bool IsPublic
        {
            get { return false; }
        }

        public Microsoft.Cci.IUnitReference GetUnit(Microsoft.CodeAnalysis.Emit.Context context)
        {
            Debug.Assert(context.Module == this.module);
            return this.module;
        }

        public string NamespaceName
        {
            get { return ""; }
        }

        internal static string GenerateDataFieldName(int offset)
        {
            return MemberNamePrefix + offset;
        }

        internal static bool TryParseDataFieldName(string name, out int offset)
        {
            if (name.StartsWith(MemberNamePrefix, StringComparison.Ordinal) &&
                int.TryParse(name.Substring(MemberNamePrefix.Length), out offset))
            {
                return true;
            }

            offset = 0;
            return false;
        }
    }

    /// <summary>
    /// Represents a block in .data
    /// </summary>
    internal sealed class MetadataBlock : Microsoft.Cci.ISectionBlock
    {
        private readonly byte[] data;
        private readonly int offset;

        private static int curOffset = 0;

        internal MetadataBlock(byte[] data)
        {
            var length = data.Length;
            offset = Interlocked.Add(ref curOffset, length) - length;
            this.data = data;
        }

        public Microsoft.Cci.PESectionKind PESectionKind
        {
            //TODO: why this is not "Constant"  ?
            get { return Microsoft.Cci.PESectionKind.Text; }
        }

        public uint Offset
        {
            get { return (uint)offset; }
        }

        public uint Size
        {
            get { return (uint)data.Length; }
        }

        public IEnumerable<byte> Data
        {
            get { return data; }
        }
    }

    /// <summary>
    /// Simple struct type with explicit size and no members.
    /// </summary>
    internal sealed class ExplicitSizeStruct : DefaultTypeDef, Microsoft.Cci.INestedTypeDefinition
    {
        private readonly uint size;
        private readonly Microsoft.Cci.INamedTypeDefinition containingType;
        private readonly Microsoft.Cci.ITypeReference sysValueType;

        internal ExplicitSizeStruct(uint size, PrivateImplementationDetails containingType, Microsoft.Cci.ITypeReference sysValueType)
        {
            this.size = size;
            this.containingType = containingType;
            this.sysValueType = sysValueType;
        }

        public override string ToString()
        {
            return containingType.ToString() + "." + this.Name;
        }

        override public ushort Alignment
        {
            get { return 1; }
        }

        override public Microsoft.Cci.ITypeReference GetBaseClass(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this.sysValueType;
        }

        override public LayoutKind Layout
        {
            get { return LayoutKind.Explicit; }
        }

        override public uint SizeOf
        {
            get { return size; }
        }

        override public void Dispatch(Microsoft.Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.INestedTypeDefinition)this);
        }

        public string Name
        {
            get { return "__StaticArrayInitTypeSize=" + this.size; }
        }

        public Microsoft.Cci.ITypeDefinition ContainingTypeDefinition
        {
            get { return this.containingType; }
        }

        public Microsoft.Cci.TypeMemberVisibility Visibility
        {
            get { return Microsoft.Cci.TypeMemberVisibility.Private; }
        }

        public override bool IsValueType
        {
            get { return true; }
        }

        public Microsoft.Cci.ITypeReference GetContainingType(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this.containingType;
        }

        public override Microsoft.Cci.INestedTypeDefinition AsNestedTypeDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this;
        }

        public override Microsoft.Cci.INestedTypeReference AsNestedTypeReference
        {
            get { return this; }
        }
    }

    /// <summary>
    /// Definition of a simple field mapped to a metadata block
    /// </summary>
    internal sealed class MappedField : Microsoft.Cci.IFieldDefinition
    {
        private readonly Microsoft.Cci.INamedTypeDefinition containingType;
        private readonly Microsoft.Cci.ITypeReference type;
        private readonly Microsoft.Cci.ISectionBlock block;
        private readonly string name;

        internal MappedField(string name, Microsoft.Cci.INamedTypeDefinition containingType, Microsoft.Cci.ITypeReference type, Microsoft.Cci.ISectionBlock block)
        {
            this.containingType = containingType;
            this.type = type;
            this.block = block;
            this.name = name;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}.{2}", type, containingType, this.Name);
        }

        public Microsoft.Cci.IMetadataConstant GetCompileTimeValue(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return null;
        }

        public Microsoft.Cci.ISectionBlock FieldMapping
        {
            get { return this.block; }
        }

        public bool IsCompileTimeConstant
        {
            get { return false; }
        }

        public bool IsMapped
        {
            get { return true; }
        }

        public bool IsNotSerialized
        {
            get { return false; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool IsRuntimeSpecial
        {
            get { return false; }
        }

        public bool IsSpecialName
        {
            get { return false; }
        }

        public bool IsStatic
        {
            get { return true; }
        }

        public bool IsMarshalledExplicitly
        {
            get { return false; }
        }

        public Microsoft.Cci.IMarshallingInformation MarshallingInformation
        {
            get { return null; }
        }

        public ImmutableArray<byte> MarshallingDescriptor
        {
            get { return default(ImmutableArray<byte>); }
        }

        public uint Offset
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public Microsoft.Cci.ITypeDefinition ContainingTypeDefinition
        {
            get { return this.containingType; }
        }

        public Microsoft.Cci.TypeMemberVisibility Visibility
        {
            get { return Microsoft.Cci.TypeMemberVisibility.Assembly; }
        }

        public Microsoft.Cci.ITypeReference GetContainingType(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this.containingType;
        }

        public IEnumerable<Microsoft.Cci.ICustomAttribute> GetAttributes(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.ICustomAttribute>();
        }

        public void Dispatch(Microsoft.Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Microsoft.Cci.IFieldDefinition)this);
        }

        public Microsoft.Cci.IDefinition AsDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public string Name
        {
            get { return this.name; }
        }

        public bool IsContextualNamedEntity
        {
            get { return false; }
        }

        public Microsoft.Cci.ITypeReference GetType(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this.type;
        }

        public Microsoft.Cci.IFieldDefinition GetResolvedField(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this;
        }

        public Microsoft.Cci.ISpecializedFieldReference AsSpecializedFieldReference
        {
            get { return null; }
        }

        public Microsoft.Cci.IMetadataConstant Constant
        {
            get { throw ExceptionUtilities.Unreachable; }
        }
    }

    /// <summary>
    /// Just a default implementation of a type definition.
    /// </summary>
    internal abstract class DefaultTypeDef : Microsoft.Cci.ITypeDefinition
    {
        public IEnumerable<Microsoft.Cci.IEventDefinition> Events
        {
            get { return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.IEventDefinition>(); }
        }

        public IEnumerable<Microsoft.Cci.IMethodImplementation> GetExplicitImplementationOverrides(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.IMethodImplementation>();
        }

        virtual public IEnumerable<Microsoft.Cci.IFieldDefinition> GetFields(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.IFieldDefinition>();
        }

        public IEnumerable<Microsoft.Cci.IGenericTypeParameter> GenericParameters
        {
            get { return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.IGenericTypeParameter>(); }
        }

        public ushort GenericParameterCount
        {
            get { return 0; }
        }

        public bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        public IEnumerable<Microsoft.Cci.ITypeReference> Interfaces(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.ITypeReference>();
        }

        public bool IsAbstract
        {
            get { return false; }
        }

        public bool IsBeforeFieldInit
        {
            get { return false; }
        }

        public bool IsComObject
        {
            get { return false; }
        }

        public bool IsGeneric
        {
            get { return false; }
        }

        public bool IsInterface
        {
            get { return false; }
        }

        public bool IsRuntimeSpecial
        {
            get { return false; }
        }

        public bool IsSerializable
        {
            get { return false; }
        }

        public bool IsSpecialName
        {
            get { return false; }
        }

        public bool IsWindowsRuntimeImport
        {
            get { return false; }
        }

        public bool IsSealed
        {
            get { return true; }
        }

        public virtual IEnumerable<Microsoft.Cci.IMethodDefinition> GetMethods(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.IMethodDefinition>();
        }

        public virtual IEnumerable<Microsoft.Cci.INestedTypeDefinition> GetNestedTypes(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.INestedTypeDefinition>();
        }

        public IEnumerable<Microsoft.Cci.IPropertyDefinition> GetProperties(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.IPropertyDefinition>();
        }

        public IEnumerable<Microsoft.Cci.SecurityAttribute> SecurityAttributes
        {
            get { return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.SecurityAttribute>(); }
        }

        public CharSet StringFormat
        {
            get { return CharSet.Ansi; }
        }

        public virtual IEnumerable<Microsoft.Cci.ICustomAttribute> GetAttributes(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.ICustomAttribute>();
        }

        public Microsoft.Cci.IDefinition AsDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this;
        }

        public bool IsEnum
        {
            get { return false; }
        }

        public Microsoft.Cci.ITypeDefinition GetResolvedType(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this;
        }

        public Microsoft.Cci.PrimitiveTypeCode TypeCode(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return Microsoft.Cci.PrimitiveTypeCode.NotPrimitive;
        }

        public TypeHandle TypeDef
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public Microsoft.Cci.IGenericMethodParameterReference AsGenericMethodParameterReference
        {
            get { return null; }
        }

        public Microsoft.Cci.IGenericTypeInstanceReference AsGenericTypeInstanceReference
        {
            get { return null; }
        }

        public Microsoft.Cci.IGenericTypeParameterReference AsGenericTypeParameterReference
        {
            get { return null; }
        }

        public virtual Microsoft.Cci.INamespaceTypeDefinition AsNamespaceTypeDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return null;
        }

        public virtual Microsoft.Cci.INamespaceTypeReference AsNamespaceTypeReference
        {
            get { return null; }
        }

        public Microsoft.Cci.ISpecializedNestedTypeReference AsSpecializedNestedTypeReference
        {
            get { return null; }
        }

        public virtual Microsoft.Cci.INestedTypeDefinition AsNestedTypeDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return null;
        }

        public virtual Microsoft.Cci.INestedTypeReference AsNestedTypeReference
        {
            get { return null; }
        }

        public Microsoft.Cci.ITypeDefinition AsTypeDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this;
        }

        public bool MangleName
        {
            get { return false; }
        }

        public virtual ushort Alignment
        {
            get { return 0; }
        }

        public virtual Microsoft.Cci.ITypeReference GetBaseClass(Microsoft.CodeAnalysis.Emit.Context context)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public virtual LayoutKind Layout
        {
            get { return LayoutKind.Auto; }
        }

        public virtual uint SizeOf
        {
            get { return 0; }
        }

        public virtual void Dispatch(Microsoft.Cci.MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public virtual bool IsValueType
        {
            get { return false; }
        }
    }
}
