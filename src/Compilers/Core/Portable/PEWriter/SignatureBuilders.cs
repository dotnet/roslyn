// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;

namespace System.Reflection.Metadata.Ecma335
{
    internal interface ISignatureBuilder
    {
        BlobBuilder Builder { get; } 
    }

    internal struct SignatureBuilder : ISignatureBuilder
    {
        public BlobBuilder Builder { get; }

        public SignatureBuilder(BlobBuilder builder)
        {
            Builder = builder;
        }

        public SignatureTypeBuilder<SignatureBuilder> FieldSignature()
        {
            Builder.WriteByte(0x06); // FIELD
            return new SignatureTypeBuilder<SignatureBuilder>(this);
        }

        public SignatureTypesBuilder<SignatureBuilder> MethodSpecificationSignature(int genericArgumentCount)
        {
            Builder.WriteByte(0x0a); // GENERICINST
            Builder.WriteCompressedInteger((uint)genericArgumentCount);

            return new SignatureTypesBuilder<SignatureBuilder>(this, genericArgumentCount);
        }

        public CustomAttributeSignatureBuilder<SignatureBuilder> CustomAttributeSignature()
        {
            Builder.WriteUInt16(0x0001);
            return new CustomAttributeSignatureBuilder<SignatureBuilder>(this);
        }

        public PermissionSetBuilder<SignatureBuilder> PermissionSetBlob(int attributeCount)
        {
            Builder.WriteByte((byte)'.');
            Builder.WriteCompressedInteger((uint)attributeCount);

            return new PermissionSetBuilder<SignatureBuilder>(this, attributeCount);
        }

        public NamedArgumentsBuilder<SignatureBuilder> NamedArgumentsSignatureBuilder(int argumentCount)
        {
            return new NamedArgumentsBuilder<SignatureBuilder>(this, argumentCount, CountFormat.Compressed);
        }
    }

    internal struct PermissionSetBuilder<T>
        where T : ISignatureBuilder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;
        private readonly int _count;

        public PermissionSetBuilder(T continuation, int count)
        {
            if (count < 0)
            {
                throw new InvalidOperationException();
            }

            _count = count;
            _continuation = continuation;
        }

        public PermissionSetBuilder<T> AddPermission(string typeName, BlobBuilder arguments)
        {
            Builder.WriteSerializedString(typeName);
            //return new NamedArgumentsBuilder<T>(_continuation, propertyCount, CountFormat.Compressed);
            Builder.WriteCompressedInteger((uint)arguments.Count);
            arguments.WriteContentTo(Builder);
            return new PermissionSetBuilder<T>(_continuation, _count - 1);
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

    internal struct SignatureTypesBuilder<T> : ISignatureBuilder
        where T : ISignatureBuilder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;
        private readonly int _count;

        internal SignatureTypesBuilder(T continuation, int count)
        {
            if (count < 0)
            {
                throw new InvalidOperationException();
            }

            _continuation = continuation;
            _count = count;
        }

        public SignatureTypeBuilder<SignatureTypesBuilder<T>> AddType()
        {
            return new SignatureTypeBuilder<SignatureTypesBuilder<T>>(new SignatureTypesBuilder<T>(_continuation, _count - 1));
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

    internal struct CustomAttributeSignatureBuilder<T> : ISignatureBuilder
        where T : ISignatureBuilder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        internal CustomAttributeSignatureBuilder(T continuation)
        {
            _continuation = continuation;
        }

        public FixedArgumentsBuilder<NamedArgumentsBuilder<T>> Arguments(int namedArgumentCount)
        {
            Builder.WriteUInt16(0x0001);
            return new FixedArgumentsBuilder<NamedArgumentsBuilder<T>>(
                new NamedArgumentsBuilder<T>(_continuation, namedArgumentCount, CountFormat.Uncompressed));
        }
    }

    internal struct FixedArgumentsBuilder<T> : ISignatureBuilder
        where T : ISignatureBuilder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        internal FixedArgumentsBuilder(T continuation)
        {
            _continuation = continuation;
        }

        public FixedArgumentBuilder<FixedArgumentsBuilder<T>> AddArgument()
        {
            return new FixedArgumentBuilder<FixedArgumentsBuilder<T>>(new FixedArgumentsBuilder<T>(_continuation));
        }

        public T End()
        {
            return _continuation;
        }
    }

    internal struct FixedArgumentBuilder<T> : ISignatureBuilder
        where T : ISignatureBuilder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        internal FixedArgumentBuilder(T continuation)
        {
            _continuation = continuation;
        }

        public ElementsBuilder<T> Scalar()
        {
            return new ElementsBuilder<T>(_continuation, 1);
        }

        public ElementsBuilder<T> Vector(int elementCount)
        {
            Builder.WriteInt32(elementCount);
            return new ElementsBuilder<T>(_continuation, elementCount);
        }

        public T NullVector()
        {
            Builder.WriteInt32(-1);
            return _continuation;
        }

        public CustomAttributeElementTypeBuilder<ElementsBuilder<T>> TypedVector(int elementCount)
        {
            Builder.WriteInt32(elementCount);
            Builder.WriteByte(0x1d);

            return new CustomAttributeElementTypeBuilder<ElementsBuilder<T>>(
                new ElementsBuilder<T>(_continuation, elementCount),
                allowArray: false);
        }
    }

    internal struct ElementsBuilder<T> : ISignatureBuilder
        where T : ISignatureBuilder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;
        private readonly int _count;

        internal ElementsBuilder(T continuation, int count)
        {
            if (count < 0)
            {
                throw new InvalidOperationException();
            }

            _continuation = continuation;
            _count = count;
        }

        public ElementBuilder<ElementsBuilder<T>> AddElement()
        {
            return new ElementBuilder<ElementsBuilder<T>>(new ElementsBuilder<T>(_continuation, _count - 1));
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

    internal struct ElementBuilder<T> : ISignatureBuilder
        where T : ISignatureBuilder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        internal ElementBuilder(T continuation)
        {
            _continuation = continuation;
        }

        public ValueBuilder<T> Value()
        {
            return new ValueBuilder<T>(_continuation);
        }

        public T TypedNull()
        {
            Builder.WriteByte(0xe0); // STRING
            Builder.WriteSerializedString(null);
            return _continuation;
        }

        public CustomAttributeElementTypeBuilder<ValueBuilder<T>> TypedValue()
        {
            return new CustomAttributeElementTypeBuilder<ValueBuilder<T>>(new ValueBuilder<T>(_continuation), allowArray: false);
        }
    }

    internal struct ValueBuilder<T> : ISignatureBuilder
        where T : ISignatureBuilder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        public ValueBuilder(T continuation)
        {
            _continuation = continuation;
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

        public T Type(string serializedTypeName)
        {
            String(serializedTypeName);
            return _continuation;
        }

        public T String(string value)
        {
            Builder.WriteSerializedString(value);
            return _continuation;
        }
    }

    internal struct NameBuilder<T> : ISignatureBuilder
        where T : ISignatureBuilder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        public NameBuilder(T continuation)
        {
            _continuation = continuation;
        }

        public T Name(string name)
        {
            Builder.WriteSerializedString(name);
            return _continuation;
        }
    }

    // non-public
    internal enum CountFormat
    {
        // this is not the first attribute
        None = 0,
        // permission sets
        Compressed = 1,
        // custom attributes
        Uncompressed = 2
    }

    internal struct NamedArgumentsBuilder<T> : ISignatureBuilder
        where T : ISignatureBuilder
    {
        public BlobBuilder Builder => _continuation.Builder;

        private readonly T _continuation;
        private readonly int _count;
        private readonly CountFormat _countFormat;

        internal NamedArgumentsBuilder(T continuation, int count, CountFormat countFormat)
        {
            if (count < 0)
            {
                throw new InvalidOperationException();
            }

            _continuation = continuation;
            _count = count;
            _countFormat = countFormat;
        }

        public CustomAttributeElementTypeBuilder<NameBuilder<FixedArgumentBuilder<NamedArgumentsBuilder<T>>>> Add(bool isField)
        {
            switch (_countFormat)
            {
                case CountFormat.Compressed:
                    Builder.WriteCompressedInteger((uint)_count);
                    break;

                case CountFormat.Uncompressed:
                    Builder.WriteInt32(_count);
                    break;
            }

            Builder.WriteByte(isField ? (byte)0x53 : (byte)0x54);

            return new CustomAttributeElementTypeBuilder<NameBuilder<FixedArgumentBuilder<NamedArgumentsBuilder<T>>>>(
                new NameBuilder<FixedArgumentBuilder<NamedArgumentsBuilder<T>>>(
                    new FixedArgumentBuilder<NamedArgumentsBuilder<T>>(
                        new NamedArgumentsBuilder<T>(_continuation, _count - 1, CountFormat.None))),
                allowArray: true);
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

    internal struct CustomAttributeElementTypeBuilder<T> : ISignatureBuilder
        where T : ISignatureBuilder
    {
        public BlobBuilder Builder => _continuation.Builder;

        private readonly T _continuation;
        private readonly bool _allowArray;

        internal CustomAttributeElementTypeBuilder(T continuation, bool allowArray)
        {
            _continuation = continuation;
            _allowArray = allowArray;
        }

        private T WriteByte(byte value)
        {
            Builder.WriteByte(value);
            return _continuation;
        }

        public T Boolean() => WriteByte(0x02);
        public T Char() => WriteByte(0x03);
        public T Int8() => WriteByte(0x04);
        public T UInt8() => WriteByte(0x05);
        public T Int16() => WriteByte(0x06);
        public T UInt16() => WriteByte(0x07);
        public T Int32() => WriteByte(0x08);
        public T UInt32() => WriteByte(0x09);
        public T Int64() => WriteByte(0x0a);
        public T UInt64() => WriteByte(0x0b);
        public T Float32() => WriteByte(0x0c);
        public T Float64() => WriteByte(0x0d);
        public T String() => WriteByte(0x0e);
        public T IntPtr() => WriteByte(0x18);
        public T UIntPtr() => WriteByte(0x19);
               
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

        public T SystemObject() => WriteByte(0x51);
        public T SystemType() => WriteByte(0x50);

        public CustomModifiersBuilder<CustomAttributeElementTypeBuilder<T>> SZArray()
        {
            if (!_allowArray)
            {
                throw new InvalidOperationException();
            }

            Builder.WriteByte(0x1d);
            return new CustomModifiersBuilder<CustomAttributeElementTypeBuilder<T>>(new CustomAttributeElementTypeBuilder<T>(_continuation, allowArray: false));
        }

        public T Enum(string enumTypeName)
        {
            Builder.WriteByte(0x55);
            Builder.WriteSerializedString(enumTypeName);
            return _continuation;
        }
    }

    internal struct SignatureTypeBuilder<T> : ISignatureBuilder
        where T : ISignatureBuilder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        public SignatureTypeBuilder(T continuation)
        {
            _continuation = continuation;
        }

        private T WriteByte(byte value)
        {
            Builder.WriteByte(value);
            return _continuation;
        }

        private void ClassOrValue(bool isValueType)
        {
            Builder.WriteByte(isValueType ? (byte)0x11 : (byte)0x12); // CLASS|VALUETYPE
        }

        public T Void() => WriteByte(0x01);
        public T Boolean() => WriteByte(0x02);
        public T Char() => WriteByte(0x03);
        public T Int8() => WriteByte(0x04);
        public T UInt8() => WriteByte(0x05);
        public T Int16() => WriteByte(0x06);
        public T UInt16() => WriteByte(0x07);
        public T Int32() => WriteByte(0x08);
        public T UInt32() => WriteByte(0x09);
        public T Int64() => WriteByte(0x0a);
        public T UInt64() => WriteByte(0x0b);
        public T Float32() => WriteByte(0x0c);
        public T Float64() => WriteByte(0x0d);
        public T String() => WriteByte(0x0e);
        public T IntPtr() => WriteByte(0x18);
        public T UIntPtr() => WriteByte(0x19);

        public T PrimitiveType(PrimitiveTypeCode type)
        {
            switch (type)
            {
                case PrimitiveTypeCode.Void: return Void();
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

        public T TypedReference() => WriteByte(0x16);
        public T Object() => WriteByte(0x1c);

        public SignatureTypeBuilder<ArrayShapeBuilder<T>> Array()
        {
            Builder.WriteByte(0x14); // ARRAY
            return new SignatureTypeBuilder<ArrayShapeBuilder<T>>(new ArrayShapeBuilder<T>(_continuation));
        }

        public T TypeDefOrRefOrSpec(bool isValueType, uint typeRefDefSpecCodedIndex)
        {
            ClassOrValue(isValueType);
            Builder.WriteCompressedInteger(typeRefDefSpecCodedIndex);
            return _continuation;
        }

        public MethodSignatureBuilder<T> FunctionPointer()
        {
            Builder.WriteByte(0x1b); // FNPTR
            return new MethodSignatureBuilder<T>(_continuation);
        }

        public SignatureTypesBuilder<T> GenericInstantiation(bool isValueType, uint typeRefDefSpecCodedIndex, int genericArgumentCount)
        {
            Builder.WriteByte(0x15); // GENERICINST
            ClassOrValue(isValueType);
            Builder.WriteCompressedInteger(typeRefDefSpecCodedIndex);
            Builder.WriteCompressedInteger((uint)genericArgumentCount);
            return new SignatureTypesBuilder<T>(_continuation, genericArgumentCount);
        }

        public T GenericMethodTypeParameter(int parameterIndex)
        {
            Builder.WriteByte(0x1e); // MVAR
            Builder.WriteCompressedInteger((uint)parameterIndex);
            return _continuation;
        }

        public T GenericTypeParameter(uint parameterIndex)
        {
            Builder.WriteByte(0x13); // VAR
            Builder.WriteCompressedInteger(parameterIndex);
            return _continuation;
        }

        public CustomModifiersBuilder<SignatureTypeBuilder<T>> Pointer()
        {
            Builder.WriteByte(0x0f);
            return new CustomModifiersBuilder<SignatureTypeBuilder<T>>(this);
        }

        public CustomModifiersBuilder<SignatureTypeBuilder<T>> SZArray()
        {
            Builder.WriteByte(0x1d);
            return new CustomModifiersBuilder<SignatureTypeBuilder<T>>(this);
        }

        public T End() => _continuation;
    }

    internal struct CustomModifiersBuilder<T> : ISignatureBuilder
        where T : ISignatureBuilder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        public CustomModifiersBuilder(T continuation)
        {
            _continuation = continuation;
        }

        public CustomModifiersBuilder<T> AddModifier(bool isOptional, uint typeDefRefSpecCodedIndex)
        {
            if (isOptional)
            {
                Builder.WriteByte(0x20);
            }
            else
            {
                Builder.WriteByte(0x1f);
            }

            Builder.WriteCompressedInteger(typeDefRefSpecCodedIndex);
            return this;
        }

        public T End() => _continuation;
    }

    internal struct ArrayShapeBuilder<T> : ISignatureBuilder
        where T : ISignatureBuilder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        public ArrayShapeBuilder(T continuation)
        {
            _continuation = continuation;
        }

        public Sizes RankAndSizes(uint rank, uint sizesCount)
        {
            Builder.WriteCompressedInteger(rank);
            Builder.WriteCompressedInteger(sizesCount);
            return new Sizes(Builder);
        }

        public T End() => _continuation;

        // TODO:
        public struct Sizes
        {
            public BlobBuilder Builder { get; }

            public Sizes(BlobBuilder builder)
            {
                Builder = builder;
            }

            public void AddSize(int size)
            {
                Builder.WriteCompressedInteger((uint)size);
            }

            public Bounds LowerBounds(uint count)
            {
                Builder.WriteCompressedInteger(count);
                return new Bounds(Builder);
            }
        }

        public struct Bounds
        {
            public BlobBuilder Builder { get; }

            public Bounds(BlobBuilder builder)
            {
                Builder = builder;
            }

            public void AddBound(int lowBound)
            {
                Builder.WriteCompressedInteger((uint)lowBound);
            }
        }
    }

    internal struct MethodSignatureBuilder<T> : ISignatureBuilder
        where T : ISignatureBuilder
    {
        public BlobBuilder Builder => _continuation.Builder;
        private readonly T _continuation;

        public MethodSignatureBuilder(T continuation)
        {
            _continuation = continuation;
        }

        public void WriteMethodDefHeader(SignatureAttributes attributes, bool isVararg, int genericParameterCount, int parameterCount)
        {
            // TODO:
            throw new NotImplementedException();
        }
    }

    internal struct ReturnTypeSigBuilder : ISignatureBuilder
    {
        public BlobBuilder Builder { get; }
        private readonly int _parameterCount;

        internal ReturnTypeSigBuilder(BlobBuilder builder, int parameterCount)
        {
            Builder = builder;
            _parameterCount = parameterCount;
        }
    }
}
