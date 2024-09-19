// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;

internal partial class AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
{
    internal abstract class State
    {
        public INamedTypeSymbol ContainingType { get; protected set; }
        public INamedTypeSymbol TypeToGenerateIn { get; protected set; }
        public bool IsStatic { get; protected set; }
        public bool IsContainedInUnsafeType { get; protected set; }

        // Just the name of the method.  i.e. "Goo" in "X.Goo" or "X.Goo()"
        public SyntaxToken IdentifierToken { get; protected set; }
        public TSimpleNameSyntax SimpleNameOpt { get; protected set; }

        // The entire expression containing the name, not including the invocation.  i.e. "X.Goo"
        // in "X.Goo()".
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

        protected async Task<bool> TryFinishInitializingStateAsync(TService service, SemanticDocument document, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TypeToGenerateIn = await SymbolFinder.FindSourceDefinitionAsync(TypeToGenerateIn, document.Project.Solution, cancellationToken).ConfigureAwait(false) as INamedTypeSymbol;
            if (TypeToGenerateIn.IsErrorType())
            {
                return false;
            }

            if (!ValidateTypeToGenerateIn(TypeToGenerateIn, IsStatic, ClassInterfaceModuleStructTypes))
            {
                return false;
            }

            if (!CodeGenerator.CanAdd(document.Project.Solution, TypeToGenerateIn, cancellationToken))
            {
                return false;
            }

            // Ok.  It either didn't bind to any symbols, or it bound to a symbol but with
            // errors.  In the former case we definitely want to offer to generate a method.  In
            // the latter case, we want to generate a method *unless* there's an existing method
            // with the same signature.
            var existingMethods = TypeToGenerateIn
                .GetMembers(IdentifierToken.ValueText)
                .OfType<IMethodSymbol>();

            var destinationProvider = document.Project.Solution.Workspace.Services.GetExtendedLanguageServices(TypeToGenerateIn.Language);

            var syntaxFacts = destinationProvider.GetService<ISyntaxFactsService>();
            var syntaxFactory = destinationProvider.GetService<SyntaxGenerator>();
            IsContainedInUnsafeType = service.ContainingTypesOrSelfHasUnsafeKeyword(TypeToGenerateIn);
            var generatedMethod = await SignatureInfo.GenerateMethodAsync(syntaxFactory, false, cancellationToken).ConfigureAwait(false);
            return !existingMethods.Any(m => SignatureComparer.Instance.HaveSameSignature(m, generatedMethod, caseSensitive: syntaxFacts.IsCaseSensitive, compareParameterName: true, isParameterCaseSensitive: syntaxFacts.IsCaseSensitive));
        }
    }
}
