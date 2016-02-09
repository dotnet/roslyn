// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;

#if SRM
namespace System.Reflection.Metadata.Decoding
#else

namespace Roslyn.Reflection.Metadata.Decoding
#endif
{
#if SRM && FUTURE
    public
#endif
    interface ISignatureTypeProvider<TType> : IPrimitiveTypeProvider<TType>, ITypeProvider<TType>, IConstructedTypeProvider<TType>
    {
        /// <summary>
        /// Gets the a type symbol for the function pointer type of the given method signature.
        /// </summary>
        TType GetFunctionPointerType(MethodSignature<TType> signature);

        /// <summary>
        /// Gets the type symbol for the generic method parameter at the given zero-based index.
        /// </summary>
        TType GetGenericMethodParameter(int index);

        /// <summary>
        /// Gets the type symbol for the generic type parameter at the given zero-based index.
        /// </summary>
        TType GetGenericTypeParameter(int index);

        /// <summary>
        /// Gets the type symbol for a type with a custom modifier applied.
        /// </summary>
        /// <param name="reader">The metadata reader that was passed to the <see cref="SignatureDecoder{TType}"/>. It may be null.</param>
        /// <param name="isRequired">True if the modifier is required, false if it's optional.</param>
        /// <param name="modifier">The modifier type applied. </param>
        /// <param name="unmodifiedType">The type symbol of the underlying type without modifiers applied.</param>
        TType GetModifiedType(MetadataReader reader, bool isRequired, TType modifier, TType unmodifiedType);

        /// <summary>
        /// Gets the type symbol for a local variable type that is marked as pinned.
        /// </summary>
        TType GetPinnedType(TType elementType);
    }
}
