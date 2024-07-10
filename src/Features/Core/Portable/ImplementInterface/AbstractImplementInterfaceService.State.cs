// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementInterface;

internal abstract partial class AbstractImplementInterfaceService
{
    internal sealed class State(
        Document document,
        SyntaxNode interfaceNode,
        SyntaxNode classOrStructDecl,
        INamedTypeSymbol classOrStructType,
        ImmutableArray<INamedTypeSymbol> interfaceTypes,
        SemanticModel model) : IImplementInterfaceInfo
    {
        public SyntaxNode Location { get; } = interfaceNode;
        public SyntaxNode ClassOrStructDecl { get; } = classOrStructDecl;
        public INamedTypeSymbol ClassOrStructType { get; } = classOrStructType;
        public ImmutableArray<INamedTypeSymbol> InterfaceTypes { get; } = interfaceTypes;
        public SemanticModel Model { get; } = model;

        public readonly Document Document = document;

        // The members that are not implemented at all.
        public ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented { get; private set; } = [];
        public ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> MembersWithoutExplicitOrImplicitImplementation { get; private set; } = [];

        // The members that have no explicit implementation.
        public ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> MembersWithoutExplicitImplementation { get; private set; } = [];

        public static State? Generate(
            AbstractImplementInterfaceService service,
            Document document,
            SemanticModel model,
            SyntaxNode interfaceNode,
            CancellationToken cancellationToken)
        {
            if (!service.TryInitializeState(document, model, interfaceNode, cancellationToken,
                    out var classOrStructDecl, out var classOrStructType, out var interfaceTypes))
            {
                return null;
            }

            if (!CodeGenerator.CanAdd(document.Project.Solution, classOrStructType, cancellationToken))
            {
                return null;
            }

            var state = new State(document, interfaceNode, classOrStructDecl, classOrStructType, interfaceTypes, model);

            if (service.CanImplementImplicitly)
            {
                state.MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented = state.ClassOrStructType.GetAllUnimplementedMembers(
                    interfaceTypes, includeMembersRequiringExplicitImplementation: false, cancellationToken);

                state.MembersWithoutExplicitOrImplicitImplementation = state.ClassOrStructType.GetAllUnimplementedMembers(
                    interfaceTypes, includeMembersRequiringExplicitImplementation: true, cancellationToken);

                state.MembersWithoutExplicitImplementation = state.ClassOrStructType.GetAllUnimplementedExplicitMembers(
                    interfaceTypes, cancellationToken);

                var allMembersImplemented = state.MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented.Length == 0;
                var allMembersImplementedExplicitly = state.MembersWithoutExplicitImplementation.Length == 0;

                return !allMembersImplementedExplicitly || !allMembersImplemented ? state : null;
            }
            else
            {
                // We put the members in this bucket so that the code fix title is "Implement Interface"
                state.MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented = state.ClassOrStructType.GetAllUnimplementedExplicitMembers(
                    interfaceTypes, cancellationToken);

                var allMembersImplemented = state.MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented.Length == 0;
                return !allMembersImplemented ? state : null;
            }
        }
    }
}
