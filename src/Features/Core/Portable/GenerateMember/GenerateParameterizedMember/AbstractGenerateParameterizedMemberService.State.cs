// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
{
    internal partial class AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
    {
        internal abstract class State
        {
            public INamedTypeSymbol ContainingType { get; protected set; }
            public INamedTypeSymbol TypeToGenerateIn { get; protected set; }
            public bool IsStatic { get; protected set; }
            public bool IsContainedInUnsafeType { get; protected set; }

            // Just the name of the method.  i.e. "Foo" in "X.Foo" or "X.Foo()"
            public SyntaxToken IdentifierToken { get; protected set; }
            public TSimpleNameSyntax SimpleNameOpt { get; protected set; }

            // The entire expression containing the name, not including the invocation.  i.e. "X.Foo"
            // in "X.Foo()".
            public TExpressionSyntax SimpleNameOrMemberAccessExpression { get; protected set; }
            public TInvocationExpressionSyntax InvocationExpressionOpt { get; protected set; }
            public bool IsInConditionalAccessExpression { get; protected set; }

            public bool IsWrittenTo { get; protected set; }

            public SignatureInfo SignatureInfo { get; protected set; }
            public MethodKind MethodKind { get; internal set; }
            public MethodGenerationKind MethodGenerationKind { get; protected set; }
            protected Location location = null;
            public Location Location
            {
                get
                {
                    if (IdentifierToken.SyntaxTree != null)
                    {
                        return IdentifierToken.GetLocation();
                    }

                    return location;
                }
            }

            protected async Task<bool> TryFinishInitializingState(TService service, SemanticDocument document, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.TypeToGenerateIn = await SymbolFinder.FindSourceDefinitionAsync(this.TypeToGenerateIn, document.Project.Solution, cancellationToken).ConfigureAwait(false) as INamedTypeSymbol;
                if (this.TypeToGenerateIn.IsErrorType())
                {
                    return false;
                }

                if (!service.ValidateTypeToGenerateIn(document.Project.Solution, this.TypeToGenerateIn,
                        this.IsStatic, ClassInterfaceModuleStructTypes, cancellationToken))
                {
                    return false;
                }

                if (!CodeGenerator.CanAdd(document.Project.Solution, this.TypeToGenerateIn, cancellationToken))
                {
                    return false;
                }

                // Ok.  It either didn't bind to any symbols, or it bound to a symbol but with
                // errors.  In the former case we definitely want to offer to generate a method.  In
                // the latter case, we want to generate a method *unless* there's an existing method
                // with the same signature.
                var existingMethods = this.TypeToGenerateIn.GetMembers(this.IdentifierToken.ValueText)
                                                           .OfType<IMethodSymbol>();

                var destinationProvider = document.Project.Solution.Workspace.Services.GetLanguageServices(this.TypeToGenerateIn.Language);
                var syntaxFacts = destinationProvider.GetService<ISyntaxFactsService>();
                var syntaxFactory = destinationProvider.GetService<SyntaxGenerator>();
                this.IsContainedInUnsafeType = service.ContainingTypesOrSelfHasUnsafeKeyword(this.TypeToGenerateIn);
                var generatedMethod = this.SignatureInfo.GenerateMethod(syntaxFactory, false, cancellationToken);
                return !existingMethods.Any(m => SignatureComparer.Instance.HaveSameSignature(m, generatedMethod, caseSensitive: syntaxFacts.IsCaseSensitive, compareParameterName: true, isParameterCaseSensitive: syntaxFacts.IsCaseSensitive));
            }
        }
    }
}
