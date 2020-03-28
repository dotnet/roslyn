// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Class to represent a synthesized attribute
    /// </summary>
    internal sealed class SynthesizedAttributeData : SourceAttributeData
    {
        internal SynthesizedAttributeData(MethodSymbol wellKnownMember, ImmutableArray<TypedConstant> arguments, ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments)
            : base(
            applicationNode: null,
            attributeClass: wellKnownMember.ContainingType,
            attributeConstructor: wellKnownMember,
            constructorArguments: arguments,
            constructorArgumentsSourceIndices: default,
            namedArguments: namedArguments,
            hasErrors: false,
            isConditionallyOmitted: false)
        {
            Debug.Assert((object)wellKnownMember != null);
            Debug.Assert(!arguments.IsDefault);
            Debug.Assert(!namedArguments.IsDefault); // Frequently empty though.
        }

        internal SynthesizedAttributeData(SourceAttributeData original)
            : base(
            applicationNode: original.ApplicationSyntaxReference,
            attributeClass: original.AttributeClass,
            attributeConstructor: original.AttributeConstructor,
            constructorArguments: original.CommonConstructorArguments,
            constructorArgumentsSourceIndices: original.ConstructorArgumentsSourceIndices,
            namedArguments: original.CommonNamedArguments,
            hasErrors: original.HasErrors,
            isConditionallyOmitted: original.IsConditionallyOmitted)
        {
        }
    }
}
