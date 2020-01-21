// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;

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
        ImmutableArray<IMetadataExpression> GetArguments(EmitContext context);

        /// <summary>
        /// A reference to the constructor that will be used to instantiate this custom attribute during execution (if the attribute is inspected via Reflection).
        /// </summary>
        IMethodReference Constructor(EmitContext context, bool reportDiagnostics);

        /// <summary>
        /// Zero or more named arguments that specify values for fields and properties of the attribute.
        /// </summary>
        ImmutableArray<IMetadataNamedArgument> GetNamedArguments(EmitContext context);

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
        ITypeReference GetType(EmitContext context);

        /// <summary>
        /// Whether attribute allows multiple.
        /// </summary>
        bool AllowMultiple { get; }
    }
}
