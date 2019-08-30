// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ImplementAbstractClass
{
    internal partial class AbstractImplementAbstractClassService<TClassSyntax>
    {
        private class State
        {
            public TClassSyntax Location { get; }
            public INamedTypeSymbol ClassType { get; }
            public INamedTypeSymbol AbstractClassType { get; }

            // The members that are not implemented at all.
            public ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> UnimplementedMembers { get; }

            private State(
                TClassSyntax node,
                INamedTypeSymbol classType,
                INamedTypeSymbol abstractClassType,
                ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> unimplementedMembers)
            {
                Location = node;
                ClassType = classType;
                AbstractClassType = abstractClassType;
                UnimplementedMembers = unimplementedMembers;
            }

            public static State Generate(
                AbstractImplementAbstractClassService<TClassSyntax> service,
                Document document,
                SemanticModel model,
                TClassSyntax node,
                CancellationToken cancellationToken)
            {
                if (!service.TryInitializeState(document, model, node, cancellationToken,
                    out var classType, out var abstractClassType))
                {
                    return null;
                }

                if (!CodeGenerator.CanAdd(document.Project.Solution, classType, cancellationToken))
                {
                    return null;
                }

                if (classType.IsAbstract)
                {
                    return null;
                }

                var unimplementedMembers = classType.GetAllUnimplementedMembers(
                    SpecializedCollections.SingletonEnumerable(abstractClassType), cancellationToken);

                if (unimplementedMembers.Length >= 1)
                {
                    return new State(node, classType, abstractClassType, unimplementedMembers);
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
