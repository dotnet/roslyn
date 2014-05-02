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
using Microsoft.CodeAnalysis.Emit;

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
        private const string MemberNamePrefix = "$$method0x6000001-";
        internal const string SynthesizedStringHashFunctionName = MemberNamePrefix + "ComputeStringHash";

        private readonly Cci.IModule module;                     //parent unit
        private readonly Cci.ITypeReference systemObject;        //base type
        private readonly Cci.ITypeReference systemValueType;     //base for nested structs

        private readonly Cci.ITypeReference systemInt8Type;         //for metadata init of short arrays
        private readonly Cci.ITypeReference systemInt16Type;        //for metadata init of short arrays
        private readonly Cci.ITypeReference systemInt32Type;        //for metadata init of short arrays
        private readonly Cci.ITypeReference systemInt64Type;        //for metadata init of short arrays

        private readonly Cci.ICustomAttribute compilerGeneratedAttribute;

        private readonly string name;

        // Once frozen the collections of fields, methods and types are immutable.
        private int frozen;

        // fields mapped to metadata blocks
        private readonly List<MappedField> mappedFields = new List<MappedField>();

        // synthesized methods
        private readonly ConcurrentDictionary<string, Cci.IMethodDefinition> synthesizedMethods =
            new ConcurrentDictionary<string, Cci.IMethodDefinition>();

        // field types for different block sizes.
        private readonly ConcurrentDictionary<uint, Cci.ITypeReference> proxyTypes = new ConcurrentDictionary<uint, Cci.ITypeReference>();

        internal PrivateImplementationDetails(
            Cci.IModule module,
            int submissionSlotIndex,
            Cci.ITypeReference systemObject,
            Cci.ITypeReference systemValueType,
            Cci.ITypeReference systemInt8Type,
            Cci.ITypeReference systemInt16Type,
            Cci.ITypeReference systemInt32Type,
            Cci.ITypeReference systemInt64Type,
            Cci.ICustomAttribute compilerGeneratedAttribute)
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
            this.name = GetClassName(submissionSlotIndex);
        }

        internal static string GetClassName(int submissionSlotIndex)
        {
            return "<PrivateImplementationDetails>" + (submissionSlotIndex >= 0 ? submissionSlotIndex.ToString() : "");
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

        internal Cci.IFieldReference CreateDataField(byte[] data)
        {
            Debug.Assert(!IsFrozen);

            Cci.ITypeReference type = this.proxyTypes.GetOrAdd((uint)data.Length, size => GetStorageStruct(size));

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

        private Cci.ITypeReference GetStorageStruct(uint size)
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
        internal bool TryAddSynthesizedMethod(Cci.IMethodDefinition method)
        {
            Debug.Assert(!IsFrozen);
            return this.synthesizedMethods.TryAdd(method.Name, method);
        }

        public override IEnumerable<Cci.IFieldDefinition> GetFields(EmitContext context)
        {
            Debug.Assert(IsFrozen);
            return mappedFields;
        }

        public override IEnumerable<Cci.IMethodDefinition> GetMethods(EmitContext context)
        {
            Debug.Assert(IsFrozen);
            return synthesizedMethods.Values;
        }

        // Get method by name, if one exists. Otherwise return null.
        internal Cci.IMethodDefinition GetMethod(string name)
        {
            Cci.IMethodDefinition method;
            synthesizedMethods.TryGetValue(name, out method);
            return method;
        }

        public override IEnumerable<Cci.INestedTypeDefinition> GetNestedTypes(EmitContext context)
        {
            Debug.Assert(IsFrozen);
            return System.Linq.Enumerable.OfType<ExplicitSizeStruct>(this.proxyTypes.Values);
        }

        public override string ToString()
        {
            return this.Name;
        }

        public override Cci.ITypeReference GetBaseClass(EmitContext context)
        {
            return this.systemObject;
        }

        public override IEnumerable<Cci.ICustomAttribute> GetAttributes(EmitContext context)
        {
            if (compilerGeneratedAttribute != null)
            {
                return SpecializedCollections.SingletonEnumerable(this.compilerGeneratedAttribute);
            }

            return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
        }

        public override void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.INamespaceTypeDefinition)this);
        }

        public override Cci.INamespaceTypeDefinition AsNamespaceTypeDefinition(EmitContext context)
        {
            return this;
        }

        public override Cci.INamespaceTypeReference AsNamespaceTypeReference
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

        public Cci.IUnitReference GetUnit(EmitContext context)
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
    }

    /// <summary>
    /// Represents a block in .data
    /// </summary>
    internal sealed class MetadataBlock : Cci.ISectionBlock
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

        public Cci.PESectionKind PESectionKind
        {
            //TODO: why this is not "Constant"  ?
            get { return Cci.PESectionKind.Text; }
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
    internal sealed class ExplicitSizeStruct : DefaultTypeDef, Cci.INestedTypeDefinition
    {
        private readonly uint size;
        private readonly Cci.INamedTypeDefinition containingType;
        private readonly Cci.ITypeReference sysValueType;

        internal ExplicitSizeStruct(uint size, PrivateImplementationDetails containingType, Cci.ITypeReference sysValueType)
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

        override public Cci.ITypeReference GetBaseClass(EmitContext context)
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

        override public void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.INestedTypeDefinition)this);
        }

        public string Name
        {
            get { return "__StaticArrayInitTypeSize=" + this.size; }
        }

        public Cci.ITypeDefinition ContainingTypeDefinition
        {
            get { return this.containingType; }
        }

        public Cci.TypeMemberVisibility Visibility
        {
            get { return Cci.TypeMemberVisibility.Private; }
        }

        public override bool IsValueType
        {
            get { return true; }
        }

        public Cci.ITypeReference GetContainingType(EmitContext context)
        {
            return this.containingType;
        }

        public override Cci.INestedTypeDefinition AsNestedTypeDefinition(EmitContext context)
        {
            return this;
        }

        public override Cci.INestedTypeReference AsNestedTypeReference
        {
            get { return this; }
        }
    }

    /// <summary>
    /// Definition of a simple field mapped to a metadata block
    /// </summary>
    internal sealed class MappedField : Cci.IFieldDefinition
    {
        private readonly Cci.INamedTypeDefinition containingType;
        private readonly Cci.ITypeReference type;
        private readonly Cci.ISectionBlock block;
        private readonly string name;

        internal MappedField(string name, Cci.INamedTypeDefinition containingType, Cci.ITypeReference type, Cci.ISectionBlock block)
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

        public Cci.IMetadataConstant GetCompileTimeValue(EmitContext context)
        {
            return null;
        }

        public Cci.ISectionBlock FieldMapping
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

        public Cci.IMarshallingInformation MarshallingInformation
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

        public Cci.ITypeDefinition ContainingTypeDefinition
        {
            get { return this.containingType; }
        }

        public Cci.TypeMemberVisibility Visibility
        {
            get { return Cci.TypeMemberVisibility.Assembly; }
        }

        public Cci.ITypeReference GetContainingType(EmitContext context)
        {
            return this.containingType;
        }

        public IEnumerable<Cci.ICustomAttribute> GetAttributes(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
        }

        public void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.IFieldDefinition)this);
        }

        public Cci.IDefinition AsDefinition(EmitContext context)
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

        public Cci.ITypeReference GetType(EmitContext context)
        {
            return this.type;
        }

        public Cci.IFieldDefinition GetResolvedField(EmitContext context)
        {
            return this;
        }

        public Cci.ISpecializedFieldReference AsSpecializedFieldReference
        {
            get { return null; }
        }

        public Cci.IMetadataConstant Constant
        {
            get { throw ExceptionUtilities.Unreachable; }
        }
    }

    /// <summary>
    /// Just a default implementation of a type definition.
    /// </summary>
    internal abstract class DefaultTypeDef : Cci.ITypeDefinition
    {
        public IEnumerable<Cci.IEventDefinition> Events
        {
            get { return SpecializedCollections.EmptyEnumerable<Cci.IEventDefinition>(); }
        }

        public IEnumerable<Cci.IMethodImplementation> GetExplicitImplementationOverrides(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.IMethodImplementation>();
        }

        virtual public IEnumerable<Cci.IFieldDefinition> GetFields(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.IFieldDefinition>();
        }

        public IEnumerable<Cci.IGenericTypeParameter> GenericParameters
        {
            get { return SpecializedCollections.EmptyEnumerable<Cci.IGenericTypeParameter>(); }
        }

        public ushort GenericParameterCount
        {
            get { return 0; }
        }

        public bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        public IEnumerable<Cci.ITypeReference> Interfaces(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.ITypeReference>();
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

        public virtual IEnumerable<Cci.IMethodDefinition> GetMethods(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.IMethodDefinition>();
        }

        public virtual IEnumerable<Cci.INestedTypeDefinition> GetNestedTypes(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.INestedTypeDefinition>();
        }

        public IEnumerable<Cci.IPropertyDefinition> GetProperties(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.IPropertyDefinition>();
        }

        public IEnumerable<Cci.SecurityAttribute> SecurityAttributes
        {
            get { return SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>(); }
        }

        public CharSet StringFormat
        {
            get { return CharSet.Ansi; }
        }

        public virtual IEnumerable<Cci.ICustomAttribute> GetAttributes(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
        }

        public Cci.IDefinition AsDefinition(EmitContext context)
        {
            return this;
        }

        public bool IsEnum
        {
            get { return false; }
        }

        public Cci.ITypeDefinition GetResolvedType(EmitContext context)
        {
            return this;
        }

        public Cci.PrimitiveTypeCode TypeCode(EmitContext context)
        {
            return Cci.PrimitiveTypeCode.NotPrimitive;
        }

        public TypeHandle TypeDef
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public Cci.IGenericMethodParameterReference AsGenericMethodParameterReference
        {
            get { return null; }
        }

        public Cci.IGenericTypeInstanceReference AsGenericTypeInstanceReference
        {
            get { return null; }
        }

        public Cci.IGenericTypeParameterReference AsGenericTypeParameterReference
        {
            get { return null; }
        }

        public virtual Cci.INamespaceTypeDefinition AsNamespaceTypeDefinition(EmitContext context)
        {
            return null;
        }

        public virtual Cci.INamespaceTypeReference AsNamespaceTypeReference
        {
            get { return null; }
        }

        public Cci.ISpecializedNestedTypeReference AsSpecializedNestedTypeReference
        {
            get { return null; }
        }

        public virtual Cci.INestedTypeDefinition AsNestedTypeDefinition(EmitContext context)
        {
            return null;
        }

        public virtual Cci.INestedTypeReference AsNestedTypeReference
        {
            get { return null; }
        }

        public Cci.ITypeDefinition AsTypeDefinition(EmitContext context)
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

        public virtual Cci.ITypeReference GetBaseClass(EmitContext context)
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

        public virtual void Dispatch(Cci.MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public virtual bool IsValueType
        {
            get { return false; }
        }
    }
}
