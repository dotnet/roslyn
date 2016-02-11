// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal interface ITypeProvider<TType>
    {
        /// <summary>
        /// Gets the type symbol for a type definition.
        /// </summary>
        /// <param name="reader">
        /// The metadata reader that was passed to the<see cref= "SignatureDecoder{TType}" />. It may be null.
        /// </param>
        /// <param name="handle">
        /// The type definition handle.
        /// </param>
        /// <param name="code">
        /// When <see cref="SignatureDecoderOptions.DifferentiateClassAndValueTypes"/> is used indicates whether
        /// the type reference is to class or value type. Otherwise <see cref="SignatureTypeHandleCode.Unresolved"/>
        /// will be passed.
        /// </param>
        TType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, SignatureTypeHandleCode code);

        /// <summary>
        /// Gets the type symbol for a type reference.
        /// </summary>
        /// <param name="reader">
        /// The metadata reader that was passed to the <see cref= "SignatureDecoder{TType}" />. It may be null.
        /// </param>
        /// <param name="handle">
        /// The type definition handle.
        /// </param>
        /// <param name="code">
        /// When <see cref="SignatureDecoderOptions.DifferentiateClassAndValueTypes"/> is used indicates whether
        /// the type reference is to class or value type. Otherwise <see cref="SignatureTypeHandleCode.Unresolved"/>
        /// will be passed.
        /// </param>
        TType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, SignatureTypeHandleCode code);


        /// <summary>
        /// Gets the type symbol for a type specification.
        /// </summary>
        /// <param name="reader">
        /// The metadata reader that was passed to the <see cref= "SignatureDecoder{TType}" />. It may be null.
        /// </param>
        /// <param name="handle">
        /// The type specification handle.
        /// </param>
        /// <param name="code">
        /// When <see cref="SignatureDecoderOptions.DifferentiateClassAndValueTypes"/> is used indicates whether
        /// the type reference is to class or value type. Otherwise <see cref="SignatureTypeHandleCode.Unresolved"/>
        /// will be passed.
        /// </param>
        TType GetTypeFromSpecification(MetadataReader reader, TypeSpecificationHandle handle, SignatureTypeHandleCode code);
    }
}
