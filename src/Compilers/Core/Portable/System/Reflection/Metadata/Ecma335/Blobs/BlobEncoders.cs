// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable RS0008 // Implement IEquatable<T> when overriding Object.Equals

using System;
using System.Collections.Immutable;
using System.ComponentModel;
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
    // TODO: Skip- for each Add- method of all enumerators that count elements (see local vars)

    //[EditorBrowsable(EditorBrowsableState.Never)]
    //public override bool Equals(object obj) => base.Equals(obj);
    //[EditorBrowsable(EditorBrowsableState.Never)]
    //public override int GetHashCode() => base.GetHashCode();
    //[EditorBrowsable(EditorBrowsableState.Never)]
    //public override string ToString() => base.ToString();

#if SRM
    public
#endif
    interface IBlobEncoder
    {
        BlobBuilder Builder { get; }
    }

#if SRM
    public
#endif
    struct BlobEncoder : IBlobEncoder
    {
        public BlobBuilder Builder { get; }

        public BlobEncoder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public SignatureTypeEncoder<BlobEncoder> FieldSignature()
        {
            Builder.WriteByte((byte)SignatureKind.Field);
            return new SignatureTypeEncoder<BlobEncoder>(this);
        }

        public GenericTypeArgumentsEncoder<BlobEncoder> MethodSpecificationSignature(int genericArgumentCount)
        {
            // TODO: arg validation

            Builder.WriteByte((byte)SignatureKind.MethodSpecification);
            Builder.WriteCompressedInteger((uint)genericArgumentCount);

            return new GenericTypeArgumentsEncoder<BlobEncoder>(this, genericArgumentCount);
        }

        public MethodSignatureEncoder<BlobEncoder> MethodSignature(
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
                Builder.WriteCompressedInteger((uint)genericParameterCount);
            }

            return new MethodSignatureEncoder<BlobEncoder>(this, isVarArg: convention == SignatureCallingConvention.VarArgs);
        }

        public MethodSignatureEncoder<BlobEncoder> PropertySignature(bool isInstanceProperty = false)
        {
            Builder.WriteByte(SignatureHeader(SignatureKind.Property, SignatureCallingConvention.Default, (isInstanceProperty ? SignatureAttributes.Instance : 0)).RawValue);
            return new MethodSignatureEncoder<BlobEncoder>(this, isVarArg: false);
        }

        public FixedArgumentsEncoder<NamedArgumentsEncoder<BlobEncoder>> CustomAttributeSignature(int namedArgumentCount)
        {
            if (unchecked((ushort)namedArgumentCount) > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(namedArgumentCount));
            }

            Builder.WriteUInt16(0x0001);

            return new FixedArgumentsEncoder<NamedArgumentsEncoder<BlobEncoder>>(
                new NamedArgumentsEncoder<BlobEncoder>(this, (ushort)namedArgumentCount, writeCount: true));
        }

        public LocalVariablesEncoder<BlobEncoder> LocalVariableSignature(int count)
        {
            Builder.WriteByte((byte)SignatureKind.LocalVariables);
            Builder.WriteCompressedInteger((uint)count);
            return new LocalVariablesEncoder<BlobEncoder>(this, count);
        }

        // TODO: TypeSpec is limited to structured types (doesn't have primitive types, TypeDefRefSpec, custom modifiers)
        public SignatureTypeEncoder<BlobEncoder> TypeSpecificationSignature()
        {
            return new SignatureTypeEncoder<BlobEncoder>(this);
        }

        public PermissionSetEncoder<BlobEncoder> PermissionSetBlob(int attributeCount)
        {
            Builder.WriteByte((byte)'.');
            Builder.WriteCompressedInteger((uint)attributeCount);

            return new PermissionSetEncoder<BlobEncoder>(this, attributeCount);
        }

        public NamedArgumentsEncoder<BlobEncoder> PermissionSetArguments(int argumentCount)
        {
            if (unchecked((ushort)argumentCount) > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(argumentCount));
            }

            Builder.WriteCompressedInteger((uint)argumentCount);
            return new NamedArgumentsEncoder<BlobEncoder>(this, (ushort)argumentCount, writeCount: false);
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
    struct MethodSignatureEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;
        private readonly bool _isVarArg;

        internal MethodSignatureEncoder(T continuation, bool isVarArg)
        {
            _continuation = continuation;
            _isVarArg = isVarArg;
        }

        public ReturnTypeEncoder<ParametersEncoder<T>> Parameters(int parameterCount)
        {
            Builder.WriteCompressedInteger((uint)parameterCount);

            return new ReturnTypeEncoder<ParametersEncoder<T>>(
                new ParametersEncoder<T>(_continuation, parameterCount, allowOptional: _isVarArg));
        }
    }

#if SRM
    public
#endif
    struct LocalVariablesEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;
        private readonly int _count;

        public LocalVariablesEncoder(T continuation, int count)
        {
            if (count < 0)
            {
                throw new InvalidOperationException();
            }

            _count = count;
            _continuation = continuation;
        }

        public LocalVariableTypeEncoder<LocalVariablesEncoder<T>> AddVariable()
        {
            return new LocalVariableTypeEncoder<LocalVariablesEncoder<T>>(
                new LocalVariablesEncoder<T>(_continuation, _count - 1));
        }

        /// <summary>
        /// The variable blob is written directly to the underlying <see cref="Builder"/>.
        /// </summary>
        public LocalVariablesEncoder<T> SkipVariable()
        {
            return new LocalVariablesEncoder<T>(_continuation, _count - 1);
        }

        public T EndVariables()
        {
            if (_count > 0)
            {
                throw new InvalidOperationException();
            }

            return _continuation;
        }
    }

#if SRM
    public
#endif
    struct LocalVariableTypeEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        public LocalVariableTypeEncoder(T continuation)
        {
            _continuation = continuation;
        }

        public CustomModifiersEncoder<LocalVariableTypeEncoder<T>> ModifiedType()
        {
            return new CustomModifiersEncoder<LocalVariableTypeEncoder<T>>(
                new LocalVariableTypeEncoder<T>(_continuation));
        }

        public SignatureTypeEncoder<T> Type(bool isByRef = false, bool isPinned = false)
        {
            if (isPinned)
            {
                Builder.WriteByte((byte)SignatureTypeCode.Pinned);
            }

            if (isByRef)
            {
                Builder.WriteByte((byte)SignatureTypeCode.ByReference);
            }

            return new SignatureTypeEncoder<T>(_continuation);
        }

        public T TypedReference()
        {
            Builder.WriteByte((byte)SignatureTypeCode.TypedReference);
            return _continuation;
        }
    }

#if SRM
    public
#endif
    struct ParameterTypeEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        public ParameterTypeEncoder(T continuation)
        {
            _continuation = continuation;
        }

        public CustomModifiersEncoder<ParameterTypeEncoder<T>> ModifiedType()
        {
            return new CustomModifiersEncoder<ParameterTypeEncoder<T>>(this);
        }

        public SignatureTypeEncoder<T> Type(bool isByRef = false)
        {
            if (isByRef)
            {
                Builder.WriteByte((byte)SignatureTypeCode.ByReference);
            }

            return new SignatureTypeEncoder<T>(_continuation);
        }

        public T TypedReference()
        {
            Builder.WriteByte((byte)SignatureTypeCode.TypedReference);
            return _continuation;
        }
    }

#if SRM
    public
#endif
    struct PermissionSetEncoder<T>
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;
        private readonly int _count;

        public PermissionSetEncoder(T continuation, int count)
        {
            if (count < 0)
            {
                throw new InvalidOperationException();
            }

            _count = count;
            _continuation = continuation;
        }

        public PermissionSetEncoder<T> AddPermission(string typeName, BlobBuilder arguments)
        {
            Builder.WriteSerializedString(typeName);
            //return new NamedArgumentsBuilder<T>(_continuation, propertyCount, CountFormat.Compressed);
            Builder.WriteCompressedInteger((uint)arguments.Count);
            arguments.WriteContentTo(Builder);
            return new PermissionSetEncoder<T>(_continuation, _count - 1);
        }

        public T End()
        {
            if (_count > 0)
            {
                throw new InvalidOperationException();
            }

            return _continuation;
        }
    }

#if SRM
    public
#endif
    struct GenericTypeArgumentsEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;
        private readonly int _count;

        internal GenericTypeArgumentsEncoder(T continuation, int count)
        {
            if (count < 0)
            {
                throw new InvalidOperationException();
            }

            _continuation = continuation;
            _count = count;
        }

        public SignatureTypeEncoder<GenericTypeArgumentsEncoder<T>> AddArgument()
        {
            return new SignatureTypeEncoder<GenericTypeArgumentsEncoder<T>>(
                new GenericTypeArgumentsEncoder<T>(_continuation, _count - 1));
        }

        public T EndArguments()
        {
            if (_count > 0)
            {
                throw new InvalidOperationException();
            }

            return _continuation;
        }
    }

#if SRM
    public
#endif
    struct FixedArgumentsEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        internal FixedArgumentsEncoder(T continuation)
        {
            _continuation = continuation;
        }

        public LiteralEncoder<FixedArgumentsEncoder<T>> AddArgument()
        {
            return new LiteralEncoder<FixedArgumentsEncoder<T>>(this);
        }

        public T EndArguments()
        {
            return _continuation;
        }
    }

#if SRM
    public
#endif
    struct LiteralEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        internal LiteralEncoder(T continuation)
        {
            _continuation = continuation;
        }

        public VectorEncoder<T> Vector()
        {
            return new VectorEncoder<T>(_continuation);
        }

        public CustomAttributeArrayTypeEncoder<VectorEncoder<T>> TaggedVector()
        {
            return new CustomAttributeArrayTypeEncoder<VectorEncoder<T>>(
                new VectorEncoder<T>(_continuation));
        }

        public ScalarEncoder<T> Scalar()
        {
            return new ScalarEncoder<T>(_continuation);
        }

        public CustomAttributeElementTypeEncoder<ScalarEncoder<T>> TaggedScalar()
        {
            return new CustomAttributeElementTypeEncoder<ScalarEncoder<T>>(
                new ScalarEncoder<T>(_continuation));
        }
    }

#if SRM
    public
#endif
    struct ScalarEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        internal ScalarEncoder(T continuation)
        {
            _continuation = continuation;
        }

        public T NullArray()
        {
            Builder.WriteInt32(-1);
            return _continuation;
        }

        public T Constant(object value)
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

            return _continuation;
        }

        public T SystemType(string serializedTypeName)
        {
            String(serializedTypeName);
            return _continuation;
        }

        private T String(string value)
        {
            Builder.WriteSerializedString(value);
            return _continuation;
        }
    }

#if SRM
    public
#endif
    struct LiteralsEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;
        private readonly int _count;

        internal LiteralsEncoder(T continuation, int count)
        {
            if (count < 0)
            {
                throw new InvalidOperationException();
            }

            _continuation = continuation;
            _count = count;
        }

        public LiteralEncoder<LiteralsEncoder<T>> AddLiteral()
        {
            return new LiteralEncoder<LiteralsEncoder<T>>(new LiteralsEncoder<T>(_continuation, _count - 1));
        }

        public T EndLiterals()
        {
            if (_count > 0)
            {
                throw new InvalidOperationException();
            }

            return _continuation;
        }
    }

#if SRM
    public
#endif
    struct VectorEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;

        private readonly T _continuation;

        internal VectorEncoder(T continuation)
        {
            _continuation = continuation;
        }

        public LiteralsEncoder<T> Count(int count)
        {
            Builder.WriteUInt32((uint)count);
            return new LiteralsEncoder<T>(_continuation, count);
        }
    }

#if SRM
    public
#endif
    struct NameEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        public NameEncoder(T continuation)
        {
            _continuation = continuation;
        }

        public T Name(string name)
        {
            Builder.WriteSerializedString(name);
            return _continuation;
        }
    }

#if SRM
    public
#endif
    struct NamedArgumentsEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;

        private readonly T _continuation;
        private readonly ushort _count;
        private readonly bool _writeCount;

        internal NamedArgumentsEncoder(T continuation, ushort count, bool writeCount)
        {
            _continuation = continuation;
            _count = count;
            _writeCount = writeCount;
        }

        public NamedArgumentTypeEncoder<NameEncoder<LiteralEncoder<NamedArgumentsEncoder<T>>>> AddArgument(bool isField)
        {
            if (_count == 0)
            {
                throw new InvalidOperationException();
            }

            if (_writeCount)
            {
                Builder.WriteUInt16(_count);
            }

            Builder.WriteByte(isField ? (byte)0x53 : (byte)0x54);
            
            return new NamedArgumentTypeEncoder<NameEncoder<LiteralEncoder<NamedArgumentsEncoder<T>>>>(
                new NameEncoder<LiteralEncoder<NamedArgumentsEncoder<T>>>(
                    new LiteralEncoder<NamedArgumentsEncoder<T>>(
                        new NamedArgumentsEncoder<T>(_continuation, (ushort)(_count - 1), writeCount: false))));
        }

        public T EndArguments()
        {
            if (_count > 0)
            {
                throw new InvalidOperationException();
            }

            if (_writeCount)
            {
                Builder.WriteUInt16(0);
            }

            return _continuation;
        }
    }

#if SRM
    public
#endif
    struct NamedArgumentTypeEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        internal NamedArgumentTypeEncoder(T continuation)
        {
            _continuation = continuation;
        }

        public CustomAttributeElementTypeEncoder<T> ScalarType()
        {
            return new CustomAttributeElementTypeEncoder<T>(_continuation);
        }

        public T Object()
        {
            Builder.WriteByte(0x51); // OBJECT
            return _continuation;
        }

        public CustomAttributeArrayTypeEncoder<T> SZArray()
        {
            return new CustomAttributeArrayTypeEncoder<T>(_continuation);
        }
    }

#if SRM
    public
#endif
    struct CustomAttributeArrayTypeEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;

        private readonly T _continuation;

        internal CustomAttributeArrayTypeEncoder(T continuation)
        {
            _continuation = continuation;
        }

        public T ObjectArray()
        {
            Builder.WriteByte((byte)SignatureTypeCode.SZArray);
            Builder.WriteByte(0x51); // OBJECT
            return _continuation;
        }

        public CustomAttributeElementTypeEncoder<T> ElementType()
        {
            Builder.WriteByte((byte)SignatureTypeCode.SZArray);
            return new CustomAttributeElementTypeEncoder<T>(_continuation);
        }
    }

#if SRM
    public
#endif
    struct CustomAttributeElementTypeEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;

        private readonly T _continuation;

        internal CustomAttributeElementTypeEncoder(T continuation)
        {
            _continuation = continuation;
        }

        private T WriteTypeCode(SignatureTypeCode value)
        {
            Builder.WriteByte((byte)value);
            return _continuation;
        }

        public T Boolean() => WriteTypeCode(SignatureTypeCode.Boolean);
        public T Char() => WriteTypeCode(SignatureTypeCode.Char);
        public T Int8() => WriteTypeCode(SignatureTypeCode.SByte);
        public T UInt8() => WriteTypeCode(SignatureTypeCode.Byte);
        public T Int16() => WriteTypeCode(SignatureTypeCode.Int16);
        public T UInt16() => WriteTypeCode(SignatureTypeCode.UInt16);
        public T Int32() => WriteTypeCode(SignatureTypeCode.Int32);
        public T UInt32() => WriteTypeCode(SignatureTypeCode.UInt32);
        public T Int64() => WriteTypeCode(SignatureTypeCode.Int64);
        public T UInt64() => WriteTypeCode(SignatureTypeCode.UInt64);
        public T Float32() => WriteTypeCode(SignatureTypeCode.Single);
        public T Float64() => WriteTypeCode(SignatureTypeCode.Double);
        public T String() => WriteTypeCode(SignatureTypeCode.String);
        public T IntPtr() => WriteTypeCode(SignatureTypeCode.IntPtr);
        public T UIntPtr() => WriteTypeCode(SignatureTypeCode.UIntPtr);

#if !SRM
        public T PrimitiveType(PrimitiveTypeCode type)
        {
            switch (type)
            {
                case PrimitiveTypeCode.Boolean: return Boolean();
                case PrimitiveTypeCode.Char: return Char();
                case PrimitiveTypeCode.Int8: return Int8();
                case PrimitiveTypeCode.UInt8: return UInt8();
                case PrimitiveTypeCode.Int16: return Int16();
                case PrimitiveTypeCode.UInt16: return UInt16();
                case PrimitiveTypeCode.Int32: return Int32();
                case PrimitiveTypeCode.UInt32: return UInt32();
                case PrimitiveTypeCode.Int64: return Int64();
                case PrimitiveTypeCode.UInt64: return UInt64();
                case PrimitiveTypeCode.Float32: return Float32();
                case PrimitiveTypeCode.Float64: return Float64();
                case PrimitiveTypeCode.String: return String();
                case PrimitiveTypeCode.IntPtr: return IntPtr();
                case PrimitiveTypeCode.UIntPtr: return UIntPtr();

                default:
                    throw new InvalidOperationException();
            }
        }
#endif
        public T SystemType()
        {
            Builder.WriteByte(0x50); // TYPE
            return _continuation;
        }

        public T Enum(string enumTypeName)
        {
            Builder.WriteByte(0x55); // ENUM
            Builder.WriteSerializedString(enumTypeName);
            return _continuation;
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
    struct SignatureTypeEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        public SignatureTypeEncoder(T continuation)
        {
            _continuation = continuation;
        }

        private T WriteTypeCode(SignatureTypeCode value)
        {
            Builder.WriteByte((byte)value);
            return _continuation;
        }

        private void ClassOrValue(bool isValueType)
        {
            Builder.WriteByte(isValueType ? (byte)0x11 : (byte)0x12); // CLASS|VALUETYPE
        }

        public T Boolean() => WriteTypeCode(SignatureTypeCode.Boolean);
        public T Char() => WriteTypeCode(SignatureTypeCode.Char);
        public T Int8() => WriteTypeCode(SignatureTypeCode.SByte);
        public T UInt8() => WriteTypeCode(SignatureTypeCode.Byte);
        public T Int16() => WriteTypeCode(SignatureTypeCode.Int16);
        public T UInt16() => WriteTypeCode(SignatureTypeCode.UInt16);
        public T Int32() => WriteTypeCode(SignatureTypeCode.Int32);
        public T UInt32() => WriteTypeCode(SignatureTypeCode.UInt32);
        public T Int64() => WriteTypeCode(SignatureTypeCode.Int64);
        public T UInt64() => WriteTypeCode(SignatureTypeCode.UInt64);
        public T Float32() => WriteTypeCode(SignatureTypeCode.Single);
        public T Float64() => WriteTypeCode(SignatureTypeCode.Double);
        public T String() => WriteTypeCode(SignatureTypeCode.String);
        public T IntPtr() => WriteTypeCode(SignatureTypeCode.IntPtr);
        public T UIntPtr() => WriteTypeCode(SignatureTypeCode.UIntPtr);

#if !SRM
        public T PrimitiveType(PrimitiveTypeCode type)
        {
            switch (type)
            {
                case PrimitiveTypeCode.Boolean: return Boolean();
                case PrimitiveTypeCode.Char: return Char();
                case PrimitiveTypeCode.Int8: return Int8();
                case PrimitiveTypeCode.UInt8: return UInt8();
                case PrimitiveTypeCode.Int16: return Int16();
                case PrimitiveTypeCode.UInt16: return UInt16();
                case PrimitiveTypeCode.Int32: return Int32();
                case PrimitiveTypeCode.UInt32: return UInt32();
                case PrimitiveTypeCode.Int64: return Int64();
                case PrimitiveTypeCode.UInt64: return UInt64();
                case PrimitiveTypeCode.Float32: return Float32();
                case PrimitiveTypeCode.Float64: return Float64();
                case PrimitiveTypeCode.String: return String();
                case PrimitiveTypeCode.IntPtr: return IntPtr();
                case PrimitiveTypeCode.UIntPtr: return UIntPtr();
                default:
                    throw new InvalidOperationException();
            }
        }
#endif
        public T Object() => WriteTypeCode(SignatureTypeCode.Object);

        public SignatureTypeEncoder<ArrayShapeEncoder<T>> Array()
        {
            Builder.WriteByte((byte)SignatureTypeCode.Array);

            return new SignatureTypeEncoder<ArrayShapeEncoder<T>>(new ArrayShapeEncoder<T>(_continuation));
        }

        public T TypeDefOrRefOrSpec(bool isValueType, EntityHandle typeRefDefSpec)
        {
            ClassOrValue(isValueType);
            Builder.WriteCompressedInteger((uint)CodedIndex.ToTypeDefOrRef(typeRefDefSpec));
            return _continuation;
        }

        public MethodSignatureEncoder<T> FunctionPointer(SignatureCallingConvention convention, FunctionPointerAttributes attributes, int genericParameterCount)
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
                Builder.WriteCompressedInteger((uint)genericParameterCount);
            }

            return new MethodSignatureEncoder<T>(_continuation, isVarArg: convention == SignatureCallingConvention.VarArgs);
        }

        public GenericTypeArgumentsEncoder<T> GenericInstantiation(bool isValueType, EntityHandle typeRefDefSpec, int genericArgumentCount)
        {
            Builder.WriteByte((byte)SignatureTypeCode.GenericTypeInstance);
            ClassOrValue(isValueType);
            Builder.WriteCompressedInteger((uint)CodedIndex.ToTypeDefOrRef(typeRefDefSpec));
            Builder.WriteCompressedInteger((uint)genericArgumentCount);
            return new GenericTypeArgumentsEncoder<T>(_continuation, genericArgumentCount);
        }

        public T GenericMethodTypeParameter(int parameterIndex)
        {
            Builder.WriteByte((byte)SignatureTypeCode.GenericMethodParameter);
            Builder.WriteCompressedInteger((uint)parameterIndex);
            return _continuation;
        }

        public T GenericTypeParameter(uint parameterIndex)
        {
            Builder.WriteByte((byte)SignatureTypeCode.GenericTypeParameter);
            Builder.WriteCompressedInteger(parameterIndex);
            return _continuation;
        }

        public SignatureTypeEncoder<T> Pointer()
        {
            Builder.WriteByte((byte)SignatureTypeCode.Pointer);
            return this;
        }

        public T VoidPointer()
        {
            Builder.WriteByte((byte)SignatureTypeCode.Pointer);
            Builder.WriteByte((byte)SignatureTypeCode.Void);
            return _continuation;
        }

        public SignatureTypeEncoder<T> SZArray()
        {
            Builder.WriteByte((byte)SignatureTypeCode.SZArray);
            return this;
        }

        public CustomModifiersEncoder<SignatureTypeEncoder<T>> ModifiedType()
        {
            return new CustomModifiersEncoder<SignatureTypeEncoder<T>>(this);
        }
    }

#if SRM
    public
#endif
    struct CustomModifiersEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        public CustomModifiersEncoder(T continuation)
        {
            _continuation = continuation;
        }

        public CustomModifiersEncoder<T> AddModifier(bool isOptional, EntityHandle typeDefRefSpec)
        {
            if (isOptional)
            {
                Builder.WriteByte((byte)SignatureTypeCode.OptionalModifier);
            }
            else
            {
                Builder.WriteByte((byte)SignatureTypeCode.RequiredModifier);
            }

            Builder.WriteCompressedInteger((uint)CodedIndex.ToTypeDefOrRef(typeDefRefSpec));
            return this;
        }

        public T EndModifiers() => _continuation;
    }

#if SRM
    public
#endif
    struct ArrayShapeEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        public ArrayShapeEncoder(T continuation)
        {
            _continuation = continuation;
        }

        public T Shape(int rank, ImmutableArray<int> sizes, ImmutableArray<int> lowerBounds)
        {
            Builder.WriteCompressedInteger((uint)rank);
            Builder.WriteCompressedInteger((uint)sizes.Length);
            foreach (int size in sizes)
            {
                Builder.WriteCompressedInteger((uint)size);
            }

            if (lowerBounds.IsDefault)
            {
                Builder.WriteCompressedInteger((uint)rank);
                for (int i = 0; i < rank; i++)
                {
                    Builder.WriteCompressedSignedInteger(0);
                }
            }
            else
            {
                Builder.WriteCompressedInteger((uint)lowerBounds.Length);
                foreach (int lowerBound in lowerBounds)
                {
                    Builder.WriteCompressedSignedInteger(lowerBound);
                }
            }

            return _continuation;
        }
    }

#if SRM
    public
#endif
    struct ReturnTypeEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        public ReturnTypeEncoder(T continuation)
        {
            _continuation = continuation;
        }

        public CustomModifiersEncoder<ReturnTypeEncoder<T>> ModifiedType()
        {
            return new CustomModifiersEncoder<ReturnTypeEncoder<T>>(this);
        }

        public SignatureTypeEncoder<T> Type(bool isByRef = false)
        {
            if (isByRef)
            {
                Builder.WriteByte((byte)SignatureTypeCode.ByReference);
            }

            return new SignatureTypeEncoder<T>(_continuation);
        }

        public T TypedReference()
        {
            Builder.WriteByte((byte)SignatureTypeCode.TypedReference);
            return _continuation;
        }

        public T Void()
        {
            Builder.WriteByte((byte)SignatureTypeCode.Void);
            return _continuation;
        }
    }

#if SRM
    public
#endif
    struct ParametersEncoder<T> : IBlobEncoder
        where T : IBlobEncoder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;
        private readonly int _count;
        private readonly bool _allowOptional;

        internal ParametersEncoder(T continuation, int count, bool allowOptional)
        {
            if (count < 0)
            {
                throw new InvalidOperationException();
            }

            _continuation = continuation;
            _count = count;
            _allowOptional = allowOptional;
        }

        public ParameterTypeEncoder<ParametersEncoder<T>> AddParameter()
        {
            return new ParameterTypeEncoder<ParametersEncoder<T>>(
                new ParametersEncoder<T>(_continuation, _count - 1, _allowOptional));
        }

        public ParametersEncoder<T> StartVarArgs()
        {
            if (!_allowOptional)
            {
                throw new InvalidOperationException();
            }

            Builder.WriteByte((byte)SignatureTypeCode.Sentinel);
            return new ParametersEncoder<T>(_continuation, _count, allowOptional: false);
        }

        public T EndParameters()
        {
            if (_count > 0)
            {
                throw new InvalidOperationException();
            }

            return _continuation;
        }
    }
}
