// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

// ^ using Microsoft.Contracts;

namespace Microsoft.Cci
{
    /// <summary>
    /// An expression that does not change its value at runtime and can be evaluated at compile time.
    /// </summary>
    internal interface IMetadataConstant : IMetadataExpression
    {
        /// <summary>
        /// The compile time value of the expression. Null to represent a null object reference or a null array.
        /// </summary>
        object Value { get; }
    }

    /// <summary>
    /// An expression that creates an array instance in metadata. Only for use in custom attributes.
    /// </summary>
    internal interface IMetadataCreateArray : IMetadataExpression
    {
        /// <summary>
        /// The element type of the array.
        /// </summary>
        ITypeReference ElementType { get; }

        /// <summary>
        /// The values of the array elements. May be empty to represent an empty array.
        /// </summary>
        IEnumerable<IMetadataExpression> Elements
        {
            get;
        }

        /// <summary>
        /// The number of elements in the array.
        /// </summary>
        uint ElementCount
        {
            get;
        }
    }

    /// <summary>
    /// An expression that can be represented directly in metadata.
    /// </summary>
    internal interface IMetadataExpression
    {
        /// <summary>
        /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
        /// of the object implementing IStatement. The dispatch method does not invoke Dispatch on any child objects. If child traversal
        /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
        /// </summary>
        void Dispatch(MetadataVisitor visitor);

        /// <summary>
        /// The type of value the expression represents.
        /// </summary>
        ITypeReference Type { get; }
    }

    /// <summary>
    /// An expression that represents a (name, value) pair and that is typically used in method calls, custom attributes and object initializers.
    /// </summary>
    internal interface IMetadataNamedArgument : IMetadataExpression
    {
        /// <summary>
        /// The name of the parameter or property or field that corresponds to the argument.
        /// </summary>
        string ArgumentName { get; }

        /// <summary>
        /// The value of the argument.
        /// </summary>
        IMetadataExpression ArgumentValue { get; }

        /// <summary>
        /// True if the named argument provides the value of a field.
        /// </summary>
        bool IsField { get; }
    }

    /// <summary>
    /// An expression that results in a System.Type instance.
    /// </summary>
    internal interface IMetadataTypeOf : IMetadataExpression
    {
        /// <summary>
        /// The type that will be represented by the System.Type instance.
        /// </summary>
        ITypeReference TypeToGet { get; }
    }
}
