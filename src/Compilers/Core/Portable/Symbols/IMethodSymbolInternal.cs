// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal interface IMethodSymbolInternal : ISymbolInternal
    {
        /// <summary>
        /// True if the method is a source method implemented as an iterator.
        /// </summary>
        bool IsIterator { get; }

        /// <summary>
        /// Returns true if this method is an async method
        /// </summary>
        bool IsAsync { get; }

        /// <summary>
        /// Returns whether this method is generic; i.e., does it have any type parameters?
        /// </summary>
        bool IsGenericMethod { get; }

        /// <summary>
        /// Returns true if this method has no return type; i.e., returns "void".
        /// </summary>
        bool ReturnsVoid { get; }

        int ParameterCount { get; }

        ImmutableArray<IParameterSymbolInternal> Parameters { get; }

        bool HasDeclarativeSecurity { get; }
        bool IsAccessCheckedOnOverride { get; }
        bool IsExternal { get; }
        bool IsHiddenBySignature { get; }
        bool IsMetadataNewSlot { get; }
        bool IsPlatformInvoke { get; }
        bool IsMetadataFinal { get; }
        bool HasSpecialName { get; }
        bool HasRuntimeSpecialName { get; }
        bool RequiresSecurityObject { get; }
        MethodImplAttributes ImplementationAttributes { get; }

        ISymbolInternal? AssociatedSymbol { get; }
        IMethodSymbolInternal? PartialImplementationPart { get; }
        IMethodSymbolInternal? PartialDefinitionPart { get; }

        /// <summary>
        /// Handle of the method signature blob or nil if not a PE symbol.
        /// </summary>
        BlobHandle MetadataSignatureHandle { get; }

        int CalculateLocalSyntaxOffset(int declaratorPosition, SyntaxTree declaratorTree);

        /// <summary>
        /// Returns a constructed method given its type arguments.
        /// </summary>
        /// <param name="typeArguments">The immediate type arguments to be replaced for type
        /// parameters in the method.</param>
        IMethodSymbolInternal Construct(params ITypeSymbolInternal[] typeArguments);
    }
}
