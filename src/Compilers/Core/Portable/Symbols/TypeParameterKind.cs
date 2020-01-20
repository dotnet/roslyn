// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents the different kinds of type parameters.
    /// </summary>
    public enum TypeParameterKind
    {
        /// <summary>
        /// Type parameter of a named type. For example: <c>T</c> in <c><![CDATA[List<T>]]></c>.
        /// </summary>
        Type = 0,

        /// <summary>
        /// Type parameter of a method. For example: <c>T</c> in <c><![CDATA[void M<T>()]]></c>.
        /// </summary>
        Method = 1,

        /// <summary>
        /// Type parameter in a <c>cref</c> attribute in XML documentation comments. For example: <c>T</c> in <c><![CDATA[<see cref="List{T}"/>]]></c>.
        /// </summary>
        Cref = 2,
    }
}
