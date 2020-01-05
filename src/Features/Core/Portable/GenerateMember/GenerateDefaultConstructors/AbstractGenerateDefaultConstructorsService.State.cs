// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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

            public ImmutableArray<IMethodSymbol> UnimplementedConstructors { get; private set; }

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
                SemanticDocument semanticDocument,
                TextSpan textSpan,
                CancellationToken cancellationToken)
            {
                if (!service.TryInitializeState(semanticDocument, textSpan, cancellationToken, out var classType))
                {
                    return false;
                }

                ClassType = classType;

                var baseType = ClassType.BaseType;
                if (ClassType.IsStatic ||
                    baseType == null ||
                    baseType.TypeKind == TypeKind.Error)
                {
                    return false;
                }

                var semanticFacts = semanticDocument.Document.GetLanguageService<ISemanticFactsService>();
                var classConstructors = ClassType.InstanceConstructors;

                var destinationProvider = semanticDocument.Project.Solution.Workspace.Services.GetLanguageServices(ClassType.Language);
                var syntaxFacts = destinationProvider.GetService<ISyntaxFactsService>();
                var isCaseSensitive = syntaxFacts.IsCaseSensitive;

                UnimplementedConstructors =
                    baseType.InstanceConstructors
                            .WhereAsArray(c => c.IsAccessibleWithin(ClassType) &&
                                               IsMissing(c, classConstructors, isCaseSensitive));

                return UnimplementedConstructors.Length > 0;
            }

            private bool IsMissing(
                IMethodSymbol constructor,
                ImmutableArray<IMethodSymbol> classConstructors,
                bool isCaseSensitive)
            {
                var matchingConstructor = classConstructors.FirstOrDefault(
                    c => SignatureComparer.Instance.HaveSameSignature(
                        constructor.Parameters, c.Parameters, compareParameterName: true, isCaseSensitive: isCaseSensitive));

                if (matchingConstructor == null)
                {
                    return true;
                }

                // We have a matching constructor in this type.  But we'll still offer to create the
                // constructor if the constructor that we have is implicit. 
                return matchingConstructor.IsImplicitlyDeclared;
            }
        }
    }
}
