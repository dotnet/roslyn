// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ImplementInterface;

internal interface IImplementInterfaceInfo
{
    /// <summary>
    /// The class or struct that is implementing the interface.
    /// </summary>
    INamedTypeSymbol ClassOrStructType { get; }

    /// <summary>
    /// The specific declaration node for <see cref="ClassOrStructType"/> that the interface implementations should be
    /// added to.
    /// </summary>
    SyntaxNode ClassOrStructDecl { get; }

    SyntaxNode InterfaceNode { get; }

    /// <summary>
    /// Set of interfaces to implement.  Normally a single interface (when a user invokes the code action on a single
    /// entry in the interface-list for a type).  However, it may be multiple in the VB case where a user presses
    /// 'enter' at the end of the interfaces list, where we'll implement all the missing members for all listed interfaces.
    /// </summary>
    ImmutableArray<INamedTypeSymbol> InterfaceTypes { get; }

    ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented { get; }
    ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> MembersWithoutExplicitOrImplicitImplementation { get; }
    ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> MembersWithoutExplicitImplementation { get; }
}
