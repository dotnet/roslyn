// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection.Metadata;

#if SRM
namespace System.Reflection.Metadata.Decoding
#else
namespace Roslyn.Reflection.Metadata.Decoding
#endif
{
    /// <summary>
    /// Represents a method (definition, reference, or standalone) or property signature.
    /// In the case of properties, the signature matches that of a getter with a distinguishing <see cref="SignatureHeader"/>.
    /// </summary>
#if SRM && FUTURE
    public
#endif
    struct MethodSignature<TType>
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

        /// <summary>
        /// Represents the information in the leading byte of the signature (kind, calling convention, flags).
        /// </summary>
        public SignatureHeader Header
        {
            get { return _header; }
        }

        /// <summary>
        /// Gets the method's return type.
        /// </summary>
        public TType ReturnType
        {
            get { return _returnType; }
        }

        /// <summary>
        /// Gets the number of parameters that are required. Will be equal to the length <see cref="ParameterTypes"/> of
        /// unless this signature represents the standalone call site of a vararg method, in which case the entries
        /// extra entries in <see cref="ParameterTypes"/> are the types used for the optional parameters.
        /// </summary>
        public int RequiredParameterCount
        {
            get { return _requiredParameterCount; }
        }

        /// <summary>
        /// Gets the number of generic type parameters of the method. Will be 0 for non-generic methods.
        /// </summary>
        public int GenericParameterCount
        {
            get { return _genericParameterCount; }
        }

        /// <summary>
        /// Gets the method's parameter types.
        /// </summary>
        public ImmutableArray<TType> ParameterTypes
        {
            get { return _parameterTypes; }
        }
    }
}
