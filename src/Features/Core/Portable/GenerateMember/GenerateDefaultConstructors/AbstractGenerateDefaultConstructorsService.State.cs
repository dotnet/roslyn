// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors
{
    internal abstract partial class AbstractGenerateDefaultConstructorsService<TService>
    {
        private class State
        {
            public INamedTypeSymbol ClassType { get; private set; }

            public IList<IMethodSymbol> UnimplementedConstructors { get; private set; }
            public IMethodSymbol UnimplementedDefaultConstructor { get; private set; }

            public SyntaxNode BaseTypeNode { get; private set; }

            private State()
            {
            }

            public static State Generate(
                TService service,
                SemanticDocument document,
                TextSpan textSpan,
                CancellationToken cancellationToken)
            {
                var state = new State();
                if (!state.TryInitialize(service, document, textSpan, cancellationToken))
                {
                    return null;
                }

                return state;
            }

            private bool TryInitialize(
                TService service,
                SemanticDocument document,
                TextSpan textSpan,
                CancellationToken cancellationToken)
            {
                SyntaxNode baseTypeNode;
                INamedTypeSymbol classType;
                if (!service.TryInitializeState(document, textSpan, cancellationToken, out baseTypeNode, out classType))
                {
                    return false;
                }

                if (!baseTypeNode.Span.IntersectsWith(textSpan.Start))
                {
                    return false;
                }

                this.BaseTypeNode = baseTypeNode;
                this.ClassType = classType;

                var baseType = this.ClassType.BaseType;

                if (this.ClassType.TypeKind != TypeKind.Class ||
                    this.ClassType.IsStatic ||
                    baseType == null ||
                    baseType.SpecialType == SpecialType.System_Object ||
                    baseType.TypeKind == TypeKind.Error)
                {
                    return false;
                }

                var semanticFacts = document.Project.LanguageServices.GetService<ISemanticFactsService>();
                var classConstructors = this.ClassType.InstanceConstructors;
                var baseTypeConstructors =
                    baseType.InstanceConstructors
                            .Where(c => c.IsAccessibleWithin(this.ClassType));

                var destinationProvider = document.Project.Solution.Workspace.Services.GetLanguageServices(this.ClassType.Language);
                var syntaxFacts = destinationProvider.GetService<ISyntaxFactsService>();

                var missingConstructors =
                    baseTypeConstructors.Where(c1 => !classConstructors.Any(
                        c2 => SignatureComparer.Instance.HaveSameSignature(c1.Parameters, c2.Parameters, compareParameterName: true, isCaseSensitive: syntaxFacts.IsCaseSensitive))).ToList();

                this.UnimplementedConstructors = missingConstructors;

                this.UnimplementedDefaultConstructor = baseTypeConstructors.FirstOrDefault(c => c.Parameters.Length == 0);
                if (this.UnimplementedDefaultConstructor != null)
                {
                    if (classConstructors.Any(c => c.Parameters.Length == 0 && !c.IsImplicitlyDeclared))
                    {
                        this.UnimplementedDefaultConstructor = null;
                    }
                }

                return this.UnimplementedConstructors.Count > 0;
            }
        }
    }
}
