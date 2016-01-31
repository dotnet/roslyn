// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable RS0008 // Implement IEquatable<T> when overriding Object.Equals

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Metadata;

#if !SRM
using PrimitiveTypeCode = Microsoft.Cci.PrimitiveTypeCode;
#endif

#if SRM
namespace System.Reflection.Metadata.Ecma335.Blobs
#else
namespace Roslyn.Reflection.Metadata.Ecma335.Blobs
#endif
{
    // TODO: arg validation
    // TODO: can we hide useless inherited methods?
    // TODO: debug metadata blobs
    // TODO: revisit ctors (public vs internal)?

    //[EditorBrowsable(EditorBrowsableState.Never)]
    //public override bool Equals(object obj) => base.Equals(obj);
    //[EditorBrowsable(EditorBrowsableState.Never)]
    //public override int GetHashCode() => base.GetHashCode();
    //[EditorBrowsable(EditorBrowsableState.Never)]
    //public override string ToString() => base.ToString();

#if SRM
    public
#endif
    struct BlobEncoder
    {
        public BlobBuilder Builder { get; }

        public BlobEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public SignatureTypeEncoder FieldSignature()
        {
            Builder.WriteByte((byte)SignatureKind.Field);
            return new SignatureTypeEncoder(Builder);
        }

        public GenericTypeArgumentsEncoder MethodSpecificationSignature(int genericArgumentCount)
        {
            // TODO: arg validation

            Builder.WriteByte((byte)SignatureKind.MethodSpecification);
            Builder.WriteCompressedInteger(genericArgumentCount);

            return new GenericTypeArgumentsEncoder(Builder);
        }

        public MethodSignatureEncoder MethodSignature(
            SignatureCallingConvention convention = SignatureCallingConvention.Default,
            int genericParameterCount = 0, 
            bool isInstanceMethod = false)
        {
            // TODO: arg validation

            var attributes = 
                (genericParameterCount != 0 ? SignatureAttributes.Generic : 0) | 
                (isInstanceMethod ? SignatureAttributes.Instance : 0);

            Builder.WriteByte(SignatureHeader(SignatureKind.Method, convention, attributes).RawValue);

            if (genericParameterCount != 0)
            {
                Builder.WriteCompressedInteger(genericParameterCount);
            }

            return new MethodSignatureEncoder(Builder, isVarArg: convention == SignatureCallingConvention.VarArgs);
        }

        public MethodSignatureEncoder PropertySignature(bool isInstanceProperty = false)
        {
            Builder.WriteByte(SignatureHeader(SignatureKind.Property, SignatureCallingConvention.Default, (isInstanceProperty ? SignatureAttributes.Instance : 0)).RawValue);
            return new MethodSignatureEncoder(Builder, isVarArg: false);
        }

        public void CustomAttributeSignature(out FixedArgumentsEncoder fixedArguments, out CustomAttributeNamedArgumentsEncoder namedArguments)
        {
            Builder.WriteUInt16(0x0001);

            fixedArguments = new FixedArgumentsEncoder(Builder);
            namedArguments = new CustomAttributeNamedArgumentsEncoder(Builder);
        }

        public LocalVariablesEncoder LocalVariableSignature(int count)
        {
            Builder.WriteByte((byte)SignatureKind.LocalVariables);
            Builder.WriteCompressedInteger(count);
            return new LocalVariablesEncoder(Builder);
        }

        // TODO: TypeSpec is limited to structured types (doesn't have primitive types, TypeDefRefSpec, custom modifiers)
        public SignatureTypeEncoder TypeSpecificationSignature()
        {
            return new SignatureTypeEncoder(Builder);
        }

        public PermissionSetEncoder PermissionSetBlob(int attributeCount)
        {
            Builder.WriteByte((byte)'.');
            Builder.WriteCompressedInteger(attributeCount);
            return new PermissionSetEncoder(Builder);
        }

        public NamedArgumentsEncoder PermissionSetArguments(int argumentCount)
        {
            Builder.WriteCompressedInteger(argumentCount);
            return new NamedArgumentsEncoder(Builder);
        }

        // TOOD: add ctor to SignatureHeader
        internal static SignatureHeader SignatureHeader(SignatureKind kind, SignatureCallingConvention convention, SignatureAttributes attributes)
        {
            return new SignatureHeader((byte)((int)kind | (int)convention | (int)attributes));
        }
    }

#if SRM
    public
#endif
    struct MethodSignatureEncoder
    {
        public BlobBuilder Builder { get; }
        private readonly bool _isVarArg;

        public MethodSignatureEncoder(BlobBuilder builder, bool isVarArg)
        {
            Builder = builder;
            _isVarArg = isVarArg;
        }

        public void Parameters(int parameterCount, out ReturnTypeEncoder returnType, out ParametersEncoder parameters)
        {
            Builder.WriteCompressedInteger(parameterCount);

            returnType = new ReturnTypeEncoder(Builder);
            parameters = new ParametersEncoder(Builder, allowVarArgs: _isVarArg);
        }
    }

#if SRM
    public
#endif
    struct LocalVariablesEncoder
    {
        public BlobBuilder Builder { get; }

        public LocalVariablesEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public LocalVariableTypeEncoder AddVariable()
        {
            return new LocalVariableTypeEncoder(Builder);
        }
        
        public void EndVariables()
        {
        }
    }

#if SRM
    public
#endif
    struct LocalVariableTypeEncoder
    {
        public BlobBuilder Builder { get; }

        public LocalVariableTypeEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public CustomModifiersEncoder CustomModifiers()
        {
            return new CustomModifiersEncoder(Builder);
        }

        public SignatureTypeEncoder Type(bool isByRef = false, bool isPinned = false)
        {
            if (isPinned)
            {
                Builder.WriteByte((byte)SignatureTypeCode.Pinned);
            }

            if (isByRef)
            {
                Builder.WriteByte((byte)SignatureTypeCode.ByReference);
            }

            return new SignatureTypeEncoder(Builder);
        }

        public void TypedReference()
        {
            Builder.WriteByte((byte)SignatureTypeCode.TypedReference);
        }
    }

#if SRM
    public
#endif
    struct ParameterTypeEncoder
    {
        public BlobBuilder Builder { get; }

        public ParameterTypeEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public CustomModifiersEncoder CustomModifiers()
        {
            return new CustomModifiersEncoder(Builder);
        }

        public SignatureTypeEncoder Type(bool isByRef = false)
        {
            if (isByRef)
            {
                Builder.WriteByte((byte)SignatureTypeCode.ByReference);
            }

            return new SignatureTypeEncoder(Builder);
        }

        public void TypedReference()
        {
            Builder.WriteByte((byte)SignatureTypeCode.TypedReference);
        }
    }

#if SRM
    public
#endif
    struct PermissionSetEncoder
    {
        public BlobBuilder Builder { get; }

        public PermissionSetEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public PermissionSetEncoder AddPermission(string typeName, BlobBuilder arguments)
        {
            Builder.WriteSerializedString(typeName);
            Builder.WriteCompressedInteger(arguments.Count);
            arguments.WriteContentTo(Builder);
            return new PermissionSetEncoder(Builder);
        }

        public void EndPermissions()
        {
        }
    }

#if SRM
    public
#endif
    struct GenericTypeArgumentsEncoder
    {
        public BlobBuilder Builder { get; }

        public GenericTypeArgumentsEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public SignatureTypeEncoder AddArgument()
        {
            return new SignatureTypeEncoder(Builder);
        }

        public void EndArguments()
        {
        }
    }

#if SRM
    public
#endif
    struct FixedArgumentsEncoder
    {
        public BlobBuilder Builder { get; }

        public FixedArgumentsEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public LiteralEncoder AddArgument()
        {
            return new LiteralEncoder(Builder);
        }

        public void EndArguments()
        {
        }
    }

#if SRM
    public
#endif
    struct LiteralEncoder
    {
        public BlobBuilder Builder { get; }

        public LiteralEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public VectorEncoder Vector()
        {
            return new VectorEncoder(Builder);
        }

        public void TaggedVector(out CustomAttributeArrayTypeEncoder arrayType, out VectorEncoder vector)
        {
            arrayType = new CustomAttributeArrayTypeEncoder(Builder);
            vector = new VectorEncoder(Builder);
        }

        public ScalarEncoder Scalar()
        {
            return new ScalarEncoder(Builder);
        }

        public void TaggedScalar(out CustomAttributeElementTypeEncoder type, out ScalarEncoder scalar)
        {
            type = new CustomAttributeElementTypeEncoder(Builder);
            scalar = new ScalarEncoder(Builder);
        }
    }

#if SRM
    public
#endif
    struct ScalarEncoder
    {
        public BlobBuilder Builder { get; }

        public ScalarEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public void NullArray()
        {
            Builder.WriteInt32(-1);
        }

        public void Constant(object value)
        {
            string str = value as string;
            if (str != null || value == null)
            {
                String(str);
            }
            else
            {
                Builder.WriteConstant(value);
            }
        }

        public void SystemType(string serializedTypeName)
        {
            String(serializedTypeName);
        }

        private void String(string value)
        {
            Builder.WriteSerializedString(value);
        }
    }

#if SRM
    public
#endif
    struct LiteralsEncoder
    {
        public BlobBuilder Builder { get; }

        public LiteralsEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public LiteralEncoder AddLiteral()
        {
            return new LiteralEncoder(Builder);
        }

        public void EndLiterals()
        {
        }
    }

#if SRM
    public
#endif
    struct VectorEncoder
    {
        public BlobBuilder Builder { get; }

        public VectorEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public LiteralsEncoder Count(int count)
        {
            Builder.WriteUInt32((uint)count);
            return new LiteralsEncoder(Builder);
        }
    }

#if SRM
    public
#endif
    struct NameEncoder
    {
        public BlobBuilder Builder { get; }

        public NameEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public void Name(string name)
        {
            Builder.WriteSerializedString(name);
        }
    }

#if SRM
    public
#endif
    struct CustomAttributeNamedArgumentsEncoder
    {
        public BlobBuilder Builder { get; }

        public CustomAttributeNamedArgumentsEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public NamedArgumentsEncoder Count(int count)
        {
            if (unchecked((ushort)count) > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            
            Builder.WriteUInt16((ushort)count);
            return new NamedArgumentsEncoder(Builder);
        }
    }

#if SRM
    public
#endif
    struct NamedArgumentsEncoder
    {
        public BlobBuilder Builder { get; }

        public NamedArgumentsEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public void AddArgument(bool isField, out NamedArgumentTypeEncoder typeEncoder, out NameEncoder name, out LiteralEncoder literal)
        {
            Builder.WriteByte(isField ? (byte)0x53 : (byte)0x54);
            typeEncoder = new NamedArgumentTypeEncoder(Builder);
            name = new NameEncoder(Builder);
            literal = new LiteralEncoder(Builder);
        }

        public void EndArguments()
        {
        }
    }

#if SRM
    public
#endif
    struct NamedArgumentTypeEncoder
    {
        public BlobBuilder Builder { get; }

        public NamedArgumentTypeEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public CustomAttributeElementTypeEncoder ScalarType()
        {
            return new CustomAttributeElementTypeEncoder(Builder);
        }

        public void Object()
        {
            Builder.WriteByte(0x51); // OBJECT
        }

        public CustomAttributeArrayTypeEncoder SZArray()
        {
            return new CustomAttributeArrayTypeEncoder(Builder);
        }
    }

#if SRM
    public
#endif
    struct CustomAttributeArrayTypeEncoder
    {
        public BlobBuilder Builder { get; }

        public CustomAttributeArrayTypeEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public void ObjectArray()
        {
            Builder.WriteByte((byte)SignatureTypeCode.SZArray);
            Builder.WriteByte(0x51); // OBJECT
        }

        public CustomAttributeElementTypeEncoder ElementType()
        {
            Builder.WriteByte((byte)SignatureTypeCode.SZArray);
            return new CustomAttributeElementTypeEncoder(Builder);
        }
    }

#if SRM
    public
#endif
    struct CustomAttributeElementTypeEncoder
    {
        public BlobBuilder Builder { get; }

        public CustomAttributeElementTypeEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        private void WriteTypeCode(SignatureTypeCode value)
        {
            Builder.WriteByte((byte)value);
        }

        public void Boolean() => WriteTypeCode(SignatureTypeCode.Boolean);
        public void Char() => WriteTypeCode(SignatureTypeCode.Char);
        public void Int8() => WriteTypeCode(SignatureTypeCode.SByte);
        public void UInt8() => WriteTypeCode(SignatureTypeCode.Byte);
        public void Int16() => WriteTypeCode(SignatureTypeCode.Int16);
        public void UInt16() => WriteTypeCode(SignatureTypeCode.UInt16);
        public void Int32() => WriteTypeCode(SignatureTypeCode.Int32);
        public void UInt32() => WriteTypeCode(SignatureTypeCode.UInt32);
        public void Int64() => WriteTypeCode(SignatureTypeCode.Int64);
        public void UInt64() => WriteTypeCode(SignatureTypeCode.UInt64);
        public void Float32() => WriteTypeCode(SignatureTypeCode.Single);
        public void Float64() => WriteTypeCode(SignatureTypeCode.Double);
        public void String() => WriteTypeCode(SignatureTypeCode.String);
        public void IntPtr() => WriteTypeCode(SignatureTypeCode.IntPtr);
        public void UIntPtr() => WriteTypeCode(SignatureTypeCode.UIntPtr);

#if !SRM
        public void PrimitiveType(PrimitiveTypeCode type)
        {
            switch (type)
            {
                case PrimitiveTypeCode.Boolean: Boolean(); return;
                case PrimitiveTypeCode.Char: Char(); return;
                case PrimitiveTypeCode.Int8: Int8(); return;
                case PrimitiveTypeCode.UInt8: UInt8(); return;
                case PrimitiveTypeCode.Int16: Int16(); return;
                case PrimitiveTypeCode.UInt16: UInt16(); return;
                case PrimitiveTypeCode.Int32: Int32(); return;
                case PrimitiveTypeCode.UInt32: UInt32(); return;
                case PrimitiveTypeCode.Int64: Int64(); return;
                case PrimitiveTypeCode.UInt64: UInt64(); return;
                case PrimitiveTypeCode.Float32: Float32(); return;
                case PrimitiveTypeCode.Float64: Float64(); return;
                case PrimitiveTypeCode.String: String(); return;
                case PrimitiveTypeCode.IntPtr: IntPtr(); return;
                case PrimitiveTypeCode.UIntPtr: UIntPtr(); return;

                default:
                    throw new InvalidOperationException();
            }
        }
#endif
        public void SystemType()
        {
            Builder.WriteByte(0x50); // TYPE
        }

        public void Enum(string enumTypeName)
        {
            Builder.WriteByte(0x55); // ENUM
            Builder.WriteSerializedString(enumTypeName);
        }
    }

#if SRM
    public
#endif
    enum FunctionPointerAttributes
    {
        None = SignatureAttributes.None,
        HasThis = SignatureAttributes.Instance,
        HasExplicitThis = SignatureAttributes.Instance | SignatureAttributes.ExplicitThis
    }

#if SRM
    public
#endif
    struct SignatureTypeEncoder
    {
        public BlobBuilder Builder { get; }

        public SignatureTypeEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        private void WriteTypeCode(SignatureTypeCode value)
        {
            Builder.WriteByte((byte)value);
        }

        private void ClassOrValue(bool isValueType)
        {
            Builder.WriteByte(isValueType ? (byte)0x11 : (byte)0x12); // CLASS|VALUETYPE
        }

        public void Boolean() => WriteTypeCode(SignatureTypeCode.Boolean);
        public void Char() => WriteTypeCode(SignatureTypeCode.Char);
        public void Int8() => WriteTypeCode(SignatureTypeCode.SByte);
        public void UInt8() => WriteTypeCode(SignatureTypeCode.Byte);
        public void Int16() => WriteTypeCode(SignatureTypeCode.Int16);
        public void UInt16() => WriteTypeCode(SignatureTypeCode.UInt16);
        public void Int32() => WriteTypeCode(SignatureTypeCode.Int32);
        public void UInt32() => WriteTypeCode(SignatureTypeCode.UInt32);
        public void Int64() => WriteTypeCode(SignatureTypeCode.Int64);
        public void UInt64() => WriteTypeCode(SignatureTypeCode.UInt64);
        public void Float32() => WriteTypeCode(SignatureTypeCode.Single);
        public void Float64() => WriteTypeCode(SignatureTypeCode.Double);
        public void String() => WriteTypeCode(SignatureTypeCode.String);
        public void IntPtr() => WriteTypeCode(SignatureTypeCode.IntPtr);
        public void UIntPtr() => WriteTypeCode(SignatureTypeCode.UIntPtr);

#if !SRM
        public void PrimitiveType(PrimitiveTypeCode type)
        {
            switch (type)
            {
                case PrimitiveTypeCode.Boolean: Boolean(); return;
                case PrimitiveTypeCode.Char: Char(); return;
                case PrimitiveTypeCode.Int8: Int8(); return;
                case PrimitiveTypeCode.UInt8: UInt8(); return;
                case PrimitiveTypeCode.Int16: Int16(); return;
                case PrimitiveTypeCode.UInt16: UInt16(); return;
                case PrimitiveTypeCode.Int32: Int32(); return;
                case PrimitiveTypeCode.UInt32: UInt32(); return;
                case PrimitiveTypeCode.Int64: Int64(); return;
                case PrimitiveTypeCode.UInt64: UInt64(); return;
                case PrimitiveTypeCode.Float32: Float32(); return;
                case PrimitiveTypeCode.Float64: Float64(); return;
                case PrimitiveTypeCode.String: String(); return;
                case PrimitiveTypeCode.IntPtr: IntPtr(); return;
                case PrimitiveTypeCode.UIntPtr: UIntPtr(); return;
                default:
                    throw new InvalidOperationException();
            }
        }
#endif
        public void Object() => WriteTypeCode(SignatureTypeCode.Object);

        public void Array(out SignatureTypeEncoder elementType, out ArrayShapeEncoder arrayShape)
        {
            Builder.WriteByte((byte)SignatureTypeCode.Array);
            elementType = this;
            arrayShape = new ArrayShapeEncoder(Builder);
        }

        public void TypeDefOrRefOrSpec(bool isValueType, EntityHandle typeRefDefSpec)
        {
            ClassOrValue(isValueType);
            Builder.WriteCompressedInteger(CodedIndex.ToTypeDefOrRefOrSpec(typeRefDefSpec));
        }

        public MethodSignatureEncoder FunctionPointer(SignatureCallingConvention convention, FunctionPointerAttributes attributes, int genericParameterCount)
        {
            // Spec:
            // The EXPLICITTHIS (0x40) bit can be set only in signatures for function pointers.
            // If EXPLICITTHIS (0x40) in the signature is set, then HASTHIS (0x20) shall also be set.

            if (attributes != FunctionPointerAttributes.None &&
                attributes != FunctionPointerAttributes.HasThis &&
                attributes != FunctionPointerAttributes.HasExplicitThis)
            {
                throw new ArgumentException(nameof(attributes));
            }

            Builder.WriteByte((byte)SignatureTypeCode.FunctionPointer);
            Builder.WriteByte(BlobEncoder.SignatureHeader(SignatureKind.Method, convention, (SignatureAttributes)attributes).RawValue);

            if (genericParameterCount != 0)
            {
                Builder.WriteCompressedInteger(genericParameterCount);
            }

            return new MethodSignatureEncoder(Builder, isVarArg: convention == SignatureCallingConvention.VarArgs);
        }

        public GenericTypeArgumentsEncoder GenericInstantiation(bool isValueType, EntityHandle typeRefDefSpec, int genericArgumentCount)
        {
            Builder.WriteByte((byte)SignatureTypeCode.GenericTypeInstance);
            ClassOrValue(isValueType);
            Builder.WriteCompressedInteger(CodedIndex.ToTypeDefOrRefOrSpec(typeRefDefSpec));
            Builder.WriteCompressedInteger(genericArgumentCount);
            return new GenericTypeArgumentsEncoder(Builder);
        }

        public void GenericMethodTypeParameter(int parameterIndex)
        {
            Builder.WriteByte((byte)SignatureTypeCode.GenericMethodParameter);
            Builder.WriteCompressedInteger(parameterIndex);
        }

        public void GenericTypeParameter(int parameterIndex)
        {
            Builder.WriteByte((byte)SignatureTypeCode.GenericTypeParameter);
            Builder.WriteCompressedInteger(parameterIndex);
        }

        public SignatureTypeEncoder Pointer()
        {
            Builder.WriteByte((byte)SignatureTypeCode.Pointer);
            return this;
        }

        public void VoidPointer()
        {
            Builder.WriteByte((byte)SignatureTypeCode.Pointer);
            Builder.WriteByte((byte)SignatureTypeCode.Void);
        }

        public SignatureTypeEncoder SZArray()
        {
            Builder.WriteByte((byte)SignatureTypeCode.SZArray);
            return this;
        }

        public CustomModifiersEncoder CustomModifiers()
        {
            return new CustomModifiersEncoder(Builder);
        }
    }

#if SRM
    public
#endif
    struct CustomModifiersEncoder
    {
        public BlobBuilder Builder { get; }

        public CustomModifiersEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public CustomModifiersEncoder AddModifier(bool isOptional, EntityHandle typeDefRefSpec)
        {
            if (isOptional)
            {
                Builder.WriteByte((byte)SignatureTypeCode.OptionalModifier);
            }
            else
            {
                Builder.WriteByte((byte)SignatureTypeCode.RequiredModifier);
            }

            Builder.WriteCompressedInteger(CodedIndex.ToTypeDefOrRefOrSpec(typeDefRefSpec));
            return this;
        }

        public void EndModifiers()
        {
        }
    }

#if SRM
    public
#endif
    struct ArrayShapeEncoder
    {
        public BlobBuilder Builder { get; }

        public ArrayShapeEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public void Shape(int rank, ImmutableArray<int> sizes, ImmutableArray<int> lowerBounds)
        {
            Builder.WriteCompressedInteger(rank);
            Builder.WriteCompressedInteger(sizes.Length);
            foreach (int size in sizes)
            {
                Builder.WriteCompressedInteger(size);
            }

            if (lowerBounds.IsDefault)
            {
                Builder.WriteCompressedInteger(rank);
                for (int i = 0; i < rank; i++)
                {
                    Builder.WriteCompressedSignedInteger(0);
                }
            }
            else
            {
                Builder.WriteCompressedInteger(lowerBounds.Length);
                foreach (int lowerBound in lowerBounds)
                {
                    Builder.WriteCompressedSignedInteger(lowerBound);
                }
            }
        }
    }

#if SRM
    public
#endif
    struct ReturnTypeEncoder
    {
        public BlobBuilder Builder { get; }

        public ReturnTypeEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public CustomModifiersEncoder CustomModifiers()
        {
            return new CustomModifiersEncoder(Builder);
        }

        public SignatureTypeEncoder Type(bool isByRef = false)
        {
            if (isByRef)
            {
                Builder.WriteByte((byte)SignatureTypeCode.ByReference);
            }

            return new SignatureTypeEncoder(Builder);
        }

        public void TypedReference()
        {
            Builder.WriteByte((byte)SignatureTypeCode.TypedReference);
        }

        public void Void()
        {
            Builder.WriteByte((byte)SignatureTypeCode.Void);
        }
    }

#if SRM
    public
#endif
    struct ParametersEncoder
    {
        public BlobBuilder Builder { get; }
        private readonly bool _allowOptional;

        public ParametersEncoder(BlobBuilder builder, bool allowVarArgs)
        {
            Builder = builder;
            _allowOptional = allowVarArgs;
        }

        public ParameterTypeEncoder AddParameter()
        {
            return new ParameterTypeEncoder(Builder);
        }

        public ParametersEncoder StartVarArgs()
        {
            if (!_allowOptional)
            {
                throw new InvalidOperationException();
            }

            Builder.WriteByte((byte)SignatureTypeCode.Sentinel);
            return new ParametersEncoder(Builder, allowVarArgs: false);
        }

        public void EndParameters()
        {
        }
    }
}
