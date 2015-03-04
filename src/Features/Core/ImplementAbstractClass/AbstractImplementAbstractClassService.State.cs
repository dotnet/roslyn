// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ImplementAbstractClass
{
    internal partial class AbstractImplementAbstractClassService
    {
        private class State
        {
            public SyntaxNode Location { get; }
            public INamedTypeSymbol ClassType { get; }
            public INamedTypeSymbol AbstractClassType { get; }

            // The members that are not implemented at all.
            public IList<Tuple<INamedTypeSymbol, IList<ISymbol>>> UnimplementedMembers { get; }

            private State(SyntaxNode node, INamedTypeSymbol classType, INamedTypeSymbol abstractClassType, IList<Tuple<INamedTypeSymbol, IList<ISymbol>>> unimplementedMembers)
            {
                this.Location = node;
                this.ClassType = classType;
                this.AbstractClassType = abstractClassType;
                this.UnimplementedMembers = unimplementedMembers;
            }

            public static State Generate(
                AbstractImplementAbstractClassService service,
                Document document,
                SemanticModel model,
                SyntaxNode node,
                CancellationToken cancellationToken)
            {
                INamedTypeSymbol classType, abstractClassType;
                if (!service.TryInitializeState(document, model, node, cancellationToken,
                    out classType, out abstractClassType))
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

                if (unimplementedMembers != null && unimplementedMembers.Count >= 1)
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
