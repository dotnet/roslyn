// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementInterface;

internal abstract partial class AbstractImplementInterfaceService<TTypeDeclarationSyntax>
{
    internal sealed class State(
        Document document,
        SyntaxNode contextNode,
        INamedTypeSymbol classOrStructType,
        ImmutableArray<INamedTypeSymbol> interfaceTypes,
        SemanticModel model)
    {
        public ImplementInterfaceInfo Info { get; private set; } = new()
        {
            ClassOrStructType = classOrStructType,
            ContextNode = contextNode,
            InterfaceTypes = interfaceTypes,
        };

        public SyntaxNode ContextNode => Info.ContextNode;
        public INamedTypeSymbol ClassOrStructType => Info.ClassOrStructType;
        public ImmutableArray<INamedTypeSymbol> InterfaceTypes => Info.InterfaceTypes;
        public SemanticModel Model { get; } = model;

        public readonly Document Document = document;

        // The members that are not implemented at all.
        public ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented => Info.MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented;
        public ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> MembersWithoutExplicitOrImplicitImplementation => Info.MembersWithoutExplicitOrImplicitImplementation;

        // The members that have no explicit implementation.
        public ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> MembersWithoutExplicitImplementation => Info.MembersWithoutExplicitImplementation;

        public static State? Generate(
            AbstractImplementInterfaceService<TTypeDeclarationSyntax> service,
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

            var state = new State(document, classOrStructDecl, classOrStructType, interfaceTypes, model);

            if (service.CanImplementImplicitly)
            {
                state.Info = state.Info with
                {
                    MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented = state.ClassOrStructType.GetAllUnimplementedMembers(
                        interfaceTypes, includeMembersRequiringExplicitImplementation: false, cancellationToken)
                };

                state.Info = state.Info with
                {
                    MembersWithoutExplicitOrImplicitImplementation = state.ClassOrStructType.GetAllUnimplementedMembers(
                        interfaceTypes, includeMembersRequiringExplicitImplementation: true, cancellationToken)
                };

                state.Info = state.Info with
                {
                    MembersWithoutExplicitImplementation = state.ClassOrStructType.GetAllUnimplementedExplicitMembers(
                        interfaceTypes, cancellationToken)
                };

                var allMembersImplemented = state.MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented.Length == 0;
                var allMembersImplementedExplicitly = state.MembersWithoutExplicitImplementation.Length == 0;

                return !allMembersImplementedExplicitly || !allMembersImplemented ? state : null;
            }
            else
            {
                // We put the members in this bucket so that the code fix title is "Implement Interface"
                state.Info = state.Info with
                {
                    MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented = state.ClassOrStructType.GetAllUnimplementedExplicitMembers(
                        interfaceTypes, cancellationToken)
                };

                var allMembersImplemented = state.MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented.Length == 0;
                return !allMembersImplemented ? state : null;
            }
        }
    }
}
