// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Cci
{
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
}
