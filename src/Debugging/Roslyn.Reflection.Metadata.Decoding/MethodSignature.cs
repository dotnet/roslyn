// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Roslyn.Reflection.Metadata.Decoding
{
    internal struct MethodSignature<TType>
    {
        private readonly SignatureHeader _header;
        private readonly TType _returnType;
        private readonly int _requiredParameterCount;
        private readonly int _genericParameterCount;
        private readonly ImmutableArray<TType> _parameterTypes;

        public MethodSignature(SignatureHeader header, TType returnType, int requiredParameterCount, int genericParameterCount, ImmutableArray<TType> parameterTypes)
        {
            _header = header;
            _returnType = returnType;
            _genericParameterCount = genericParameterCount;
            _requiredParameterCount = requiredParameterCount;
            _parameterTypes = parameterTypes;
        }

        public SignatureHeader Header
        {
            get { return _header; }
        }

        public TType ReturnType
        {
            get { return _returnType; }
        }

        public int RequiredParameterCount
        {
            get { return _requiredParameterCount; }
        }

        public int GenericParameterCount
        {
            get { return _genericParameterCount; }
        }

        public ImmutableArray<TType> ParameterTypes
        {
            get { return _parameterTypes; }
        }
    }
}
