// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.Cci
{
    /// <summary>
    /// A metadata custom attribute.
    /// </summary>
    internal interface ICustomAttribute
    {
        /// <summary>
        /// Zero or more positional arguments for the attribute constructor.
        /// </summary>
        ImmutableArray<IMetadataExpression> GetArguments(Microsoft.CodeAnalysis.Emit.Context context);

        /// <summary>
        /// A reference to the constructor that will be used to instantiate this custom attribute during execution (if the attribute is inspected via Reflection).
        /// </summary>
        IMethodReference Constructor(Microsoft.CodeAnalysis.Emit.Context context);

        /// <summary>
        /// Zero or more named arguments that specify values for fields and properties of the attribute.
        /// </summary>
        ImmutableArray<IMetadataNamedArgument> GetNamedArguments(Microsoft.CodeAnalysis.Emit.Context context);

        /// <summary>
        /// The number of positional arguments.
        /// </summary>
        int ArgumentCount
        {
            get;
        }

        /// <summary>
        /// The number of named arguments.
        /// </summary>
        ushort NamedArgumentCount
        {
            get;
        }

        /// <summary>
        /// The type of the attribute. For example System.AttributeUsageAttribute.
        /// </summary>
        ITypeReference GetType(Microsoft.CodeAnalysis.Emit.Context context);

        /// <summary>
        /// Whether attribute allows multiple.
        /// </summary>
        bool AllowMultiple { get; }
    }
}