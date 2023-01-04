// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateDefaultConstructors
{
    internal abstract partial class AbstractGenerateDefaultConstructorsService<TService>
    {
        private class State
        {
            public INamedTypeSymbol? ClassType { get; private set; }

            public ImmutableArray<IMethodSymbol> UnimplementedConstructors { get; private set; }

            private State()
            {
            }

            public static State? Generate(
                TService service,
                SemanticDocument document,
                TextSpan textSpan,
                bool forRefactoring,
                CancellationToken cancellationToken)
            {
                var state = new State();
                if (!state.TryInitialize(service, document, textSpan, forRefactoring, cancellationToken))
                {
                    return null;
                }

                return state;
            }

            private bool TryInitialize(
                TService service,
                SemanticDocument semanticDocument,
                TextSpan textSpan,
                bool forRefactoring,
                CancellationToken cancellationToken)
            {
                if (!service.TryInitializeState(semanticDocument, textSpan, cancellationToken, out var classType))
                    return false;

                ClassType = classType;

                var baseType = ClassType.BaseType;
                if (ClassType.IsStatic ||
                    baseType == null ||
                    baseType.TypeKind == TypeKind.Error)
                {
                    return false;
                }

                // if this is for the refactoring, then don't offer this if the compiler is reporting an
                // error here.  We'll let the code fix take care of that.
                //
                // Similarly if this is for the codefix only offer if we do see that there's an error.
                var syntaxFacts = semanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();
                var headerFacts = semanticDocument.Document.GetRequiredLanguageService<IHeaderFactsService>();
                if (headerFacts.IsOnTypeHeader(semanticDocument.Root, textSpan.Start, fullHeader: true, out _))
                {
                    var fixesError = FixesError(classType, baseType);
                    if (forRefactoring == fixesError)
                        return false;
                }

                var semanticFacts = semanticDocument.Document.GetLanguageService<ISemanticFactsService>();
                var classConstructors = ClassType.InstanceConstructors;

                var destinationProvider = semanticDocument.Project.Solution.Services.GetLanguageServices(ClassType.Language);
                var isCaseSensitive = syntaxFacts.IsCaseSensitive;

                UnimplementedConstructors =
                    baseType.InstanceConstructors
                            .WhereAsArray(c => c.IsAccessibleWithin(ClassType) &&
                                               IsMissing(c, classConstructors, isCaseSensitive));

                return UnimplementedConstructors.Length > 0;
            }

            private static bool FixesError(INamedTypeSymbol classType, INamedTypeSymbol baseType)
            {
                // See if the user didn't supply a constructor, and thus the compiler automatically generated
                // one for them.   If so, also see if there's an accessible no-arg contructor in the base.
                // If not, then the compiler will error and we want the code-fix to take over solving this problem.
                if (classType.Constructors.Any(static c => c.Parameters.Length == 0 && c.IsImplicitlyDeclared))
                {
                    var baseNoArgConstructor = baseType.Constructors.FirstOrDefault(c => c.Parameters.Length == 0);
                    if (baseNoArgConstructor == null ||
                        !baseNoArgConstructor.IsAccessibleWithin(classType))
                    {
                        // this code is in error, but we're the refactoring codepath.  Offer nothing
                        // and let the code fix provider handle it instead.
                        return true;
                    }

                    // If this is a struct that has initializers, but is missing a parameterless constructor then we are fixing
                    // an error (CS8983) but since this is the only scenario where we support structs we don't need to actually
                    // check for anything else.
                    if (classType.TypeKind == TypeKind.Struct)
                    {
                        return true;
                    }
                }

                return false;
            }

            private static bool IsMissing(
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
