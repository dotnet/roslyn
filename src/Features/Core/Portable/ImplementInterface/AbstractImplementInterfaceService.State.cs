// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementInterface
{
    internal abstract partial class AbstractImplementInterfaceService
    {
        internal class State
        {
            public SyntaxNode Location { get; }
            public SyntaxNode ClassOrStructDecl { get; }
            public INamedTypeSymbol ClassOrStructType { get; }
            public IEnumerable<INamedTypeSymbol> InterfaceTypes { get; }
            public SemanticModel Model { get; }

            // The members that are not implemented at all.
            public ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> UnimplementedMembers { get; private set; }
                = ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)>.Empty;

            // The members that have no explicit implementation.
            public ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> UnimplementedExplicitMembers { get; private set; }
                = ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)>.Empty;

            public State(SyntaxNode interfaceNode, SyntaxNode classOrStructDecl, INamedTypeSymbol classOrStructType, IEnumerable<INamedTypeSymbol> interfaceTypes, SemanticModel model)
            {
                Location = interfaceNode;
                ClassOrStructDecl = classOrStructDecl;
                ClassOrStructType = classOrStructType;
                InterfaceTypes = interfaceTypes;
                Model = model;
            }

            public static State Generate(
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

                var state = new State(interfaceNode, classOrStructDecl, classOrStructType, interfaceTypes, model);

                if (service.CanImplementImplicitly)
                {
                    state.UnimplementedMembers = state.ClassOrStructType.GetAllUnimplementedMembers(
                        interfaceTypes, cancellationToken);

                    state.UnimplementedExplicitMembers = state.ClassOrStructType.GetAllUnimplementedExplicitMembers(
                        interfaceTypes, cancellationToken);

                    var allMembersImplemented = state.UnimplementedMembers.Length == 0;
                    var allMembersImplementedExplicitly = state.UnimplementedExplicitMembers.Length == 0;

                    return !allMembersImplementedExplicitly || !allMembersImplemented ? state : null;
                }
                else
                {
                    state.UnimplementedMembers = state.ClassOrStructType.GetAllUnimplementedExplicitMembers(
                        interfaceTypes, cancellationToken);

                    var allMembersImplemented = state.UnimplementedMembers.Length == 0;
                    return !allMembersImplemented ? state : null;
                }
            }
        }
    }
}
