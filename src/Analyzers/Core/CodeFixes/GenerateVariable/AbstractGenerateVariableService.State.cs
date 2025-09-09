// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateVariable;

internal abstract partial class AbstractGenerateVariableService<TService, TSimpleNameSyntax, TExpressionSyntax>
{
    private sealed partial class State
    {
        private readonly TService _service;
        private readonly SemanticDocument _document;

        public INamedTypeSymbol ContainingType { get; private set; }
        public INamedTypeSymbol TypeToGenerateIn { get; private set; }
        public IMethodSymbol ContainingMethod { get; private set; }
        public bool IsStatic { get; private set; }
        public bool IsConstant { get; private set; }
        public bool IsIndexer { get; private set; }
        public bool IsContainedInUnsafeType { get; private set; }
        public ImmutableArray<IParameterSymbol> Parameters { get; private set; }

        // Just the name of the method.  i.e. "Goo" in "Goo" or "X.Goo"
        public SyntaxToken IdentifierToken { get; private set; }

        // The entire expression containing the name.  i.e. "X.Goo"
        public TExpressionSyntax SimpleNameOrMemberAccessExpressionOpt { get; private set; }

        public ITypeSymbol TypeMemberType { get; private set; }
        public ITypeSymbol LocalType { get; private set; }

        public bool OfferReadOnlyFieldFirst { get; private set; }

        public bool IsWrittenTo { get; private set; }
        public bool IsOnlyWrittenTo { get; private set; }

        public bool IsInConstructor { get; private set; }
        public bool IsInRefContext { get; private set; }
        public bool IsInInContext { get; private set; }
        public bool IsInOutContext { get; private set; }
        public bool IsInMemberContext { get; private set; }

        public bool IsInSourceGeneratedDocument { get; private set; }
        public bool IsInExecutableBlock { get; private set; }
        public bool IsInConditionalAccessExpression { get; private set; }

        public Location AfterThisLocation { get; private set; }
        public Location BeforeThisLocation { get; private set; }

        private State(
            TService service,
            SemanticDocument document)
        {
            _service = service;
            _document = document;
        }

        public static async ValueTask<State> GenerateAsync(
            TService service,
            SemanticDocument document,
            SyntaxNode interfaceNode,
            CancellationToken cancellationToken)
        {
            var state = new State(service, document);
            return await state.TryInitializeAsync(interfaceNode, cancellationToken).ConfigureAwait(false) ? state : null;
        }

        public Accessibility DetermineMaximalAccessibility()
        {
            if (this.TypeToGenerateIn.TypeKind == TypeKind.Interface)
                return Accessibility.NotApplicable;

            var accessibility = Accessibility.Public;

            // Ensure that we're not overly exposing a type.
            var containingTypeAccessibility = this.TypeToGenerateIn.DetermineMinimalAccessibility();
            var effectiveAccessibility = AccessibilityUtilities.Minimum(
                containingTypeAccessibility, accessibility);

            var returnTypeAccessibility = this.TypeMemberType.DetermineMinimalAccessibility();

            if (AccessibilityUtilities.Minimum(effectiveAccessibility, returnTypeAccessibility) !=
                effectiveAccessibility)
            {
                return returnTypeAccessibility;
            }

            return accessibility;
        }

        private async ValueTask<bool> TryInitializeAsync(
            SyntaxNode node, CancellationToken cancellationToken)
        {
            if (_service.IsIdentifierNameGeneration(node))
            {
                // Cases that we deal with currently:
                //
                // 1) expr.Goo
                // 2) expr->Goo
                // 3) Goo
                if (!TryInitializeSimpleName((TSimpleNameSyntax)node, cancellationToken))
                {
                    return false;
                }
            }
            else if (_service.IsExplicitInterfaceGeneration(node))
            {
                // 4)  bool IGoo.NewProp
                if (!TryInitializeExplicitInterface(node, cancellationToken))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            // Ok.  It either didn't bind to any symbols, or it bound to a symbol but with
            // errors.  In the former case we definitely want to offer to generate a field.  In
            // the latter case, we want to generate a field *unless* there's an existing member
            // with the same name.  Note: it's ok if there's a  method with the same name.
            var existingMembers = TypeToGenerateIn.GetMembers(IdentifierToken.ValueText)
                                                       .Where(m => m.Kind != SymbolKind.Method);
            if (existingMembers.Any())
            {
                // TODO: Code coverage
                // There was an existing method that the new method would clash with.  
                return false;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            TypeToGenerateIn = await SymbolFinder.FindSourceDefinitionAsync(
                TypeToGenerateIn, _document.Project.Solution, cancellationToken).ConfigureAwait(false) as INamedTypeSymbol;

            if (!ValidateTypeToGenerateIn(TypeToGenerateIn, IsStatic, ClassInterfaceModuleStructTypes))
            {
                return false;
            }

            IsContainedInUnsafeType = _service.ContainingTypesOrSelfHasUnsafeKeyword(TypeToGenerateIn);

            return CanGenerateLocal() || CodeGenerator.CanAdd(_document.Project.Solution, TypeToGenerateIn, cancellationToken);
        }

        internal bool CanGeneratePropertyOrField()
        {
            return this.TypeToGenerateIn is { IsImplicitClass: false }
                && TypeToGenerateIn.GetMembers(WellKnownMemberNames.TopLevelStatementsEntryPointMethodName).IsEmpty;
        }

        internal bool CanGenerateLocal()
        {
            // !this.IsInMemberContext prevents us offering this fix for `x.goo` where `goo` does not exist
            return !IsInMemberContext && IsInExecutableBlock && !IsInSourceGeneratedDocument;
        }

        internal bool CanGenerateParameter()
        {
            // !this.IsInMemberContext prevents us offering this fix for `x.goo` where `goo` does not exist
            // Workaround: The compiler returns IsImplicitlyDeclared = false for <Main>$.
            return ContainingMethod is { IsImplicitlyDeclared: false, Name: not WellKnownMemberNames.TopLevelStatementsEntryPointMethodName }
                && !IsInMemberContext && !IsConstant && !IsInSourceGeneratedDocument;
        }

        private bool TryInitializeExplicitInterface(
            SyntaxNode propertyDeclaration,
            CancellationToken cancellationToken)
        {
            if (!_service.TryInitializeExplicitInterfaceState(
                    _document, propertyDeclaration, cancellationToken,
                    out var identifierToken, out var propertySymbol, out var typeToGenerateIn))
            {
                return false;
            }

            IdentifierToken = identifierToken;
            TypeToGenerateIn = typeToGenerateIn;

            if (propertySymbol.ExplicitInterfaceImplementations.Any())
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var semanticModel = _document.SemanticModel;
            ContainingType = semanticModel.GetEnclosingNamedType(IdentifierToken.SpanStart, cancellationToken);
            if (ContainingType == null)
            {
                return false;
            }

            if (!ContainingType.Interfaces.OfType<INamedTypeSymbol>().Contains(TypeToGenerateIn))
            {
                return false;
            }

            IsIndexer = propertySymbol.IsIndexer;
            Parameters = propertySymbol.Parameters;
            TypeMemberType = propertySymbol.Type;

            // By default, make it readonly, unless there's already an setter defined.
            IsWrittenTo = propertySymbol.SetMethod != null;

            return true;
        }

        private bool TryInitializeSimpleName(
            TSimpleNameSyntax simpleName,
            CancellationToken cancellationToken)
        {
            if (!_service.TryInitializeIdentifierNameState(
                    _document, simpleName, cancellationToken,
                    out var identifierToken, out var simpleNameOrMemberAccessExpression, out var isInExecutableBlock, out var isInConditionalAccessExpression))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(identifierToken.ValueText))
            {
                return false;
            }

            IdentifierToken = identifierToken;
            SimpleNameOrMemberAccessExpressionOpt = simpleNameOrMemberAccessExpression;
            IsInExecutableBlock = isInExecutableBlock;
            IsInConditionalAccessExpression = isInConditionalAccessExpression;

            // If we're in a type context then we shouldn't offer to generate a field or
            // property.
            var syntaxFacts = _document.Document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFacts.IsInNamespaceOrTypeContext(SimpleNameOrMemberAccessExpressionOpt))
            {
                return false;
            }

            IsConstant = syntaxFacts.IsInConstantContext(SimpleNameOrMemberAccessExpressionOpt);

            // If we're not in a type, don't even bother.  NOTE(cyrusn): We'll have to rethink this
            // for C# Script.
            cancellationToken.ThrowIfCancellationRequested();
            var semanticModel = _document.SemanticModel;
            ContainingType = semanticModel.GetEnclosingNamedType(IdentifierToken.SpanStart, cancellationToken);
            if (ContainingType == null)
            {
                return false;
            }

            // Now, try to bind the invocation and see if it succeeds or not.  if it succeeds and
            // binds uniquely, then we don't need to offer this quick fix.
            cancellationToken.ThrowIfCancellationRequested();
            var semanticInfo = semanticModel.GetSymbolInfo(SimpleNameOrMemberAccessExpressionOpt, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (semanticInfo.Symbol != null)
            {
                return false;
            }

            // Either we found no matches, or this was ambiguous. Either way, we might be able
            // to generate a method here.  Determine where the user wants to generate the method
            // into, and if it's valid then proceed.
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryDetermineTypeToGenerateIn(_document, ContainingType, SimpleNameOrMemberAccessExpressionOpt, cancellationToken,
                    out var typeToGenerateIn, out var isStatic, out _))
            {
                return false;
            }

            TypeToGenerateIn = typeToGenerateIn;
            IsStatic = isStatic;

            if (!TryDetermineFieldType(cancellationToken))
                return false;

            var semanticFacts = _document.Document.GetLanguageService<ISemanticFactsService>();
            IsInRefContext = semanticFacts.IsInRefContext(semanticModel, SimpleNameOrMemberAccessExpressionOpt, cancellationToken);
            IsInInContext = semanticFacts.IsInInContext(semanticModel, SimpleNameOrMemberAccessExpressionOpt, cancellationToken);
            IsInOutContext = semanticFacts.IsInOutContext(semanticModel, SimpleNameOrMemberAccessExpressionOpt, cancellationToken);
            IsWrittenTo = semanticFacts.IsWrittenTo(semanticModel, SimpleNameOrMemberAccessExpressionOpt, cancellationToken);
            IsOnlyWrittenTo = semanticFacts.IsOnlyWrittenTo(semanticModel, SimpleNameOrMemberAccessExpressionOpt, cancellationToken);
            IsInConstructor = DetermineIsInConstructor(simpleName);
            IsInMemberContext =
                simpleName != SimpleNameOrMemberAccessExpressionOpt ||
                syntaxFacts.IsMemberInitializerNamedAssignmentIdentifier(SimpleNameOrMemberAccessExpressionOpt);
            IsInSourceGeneratedDocument = _document.Document is SourceGeneratedDocument;

            ContainingMethod = FindContainingMethodSymbol(IdentifierToken.SpanStart, semanticModel, cancellationToken);

            CheckSurroundingContext(SymbolKind.Field, cancellationToken);
            CheckSurroundingContext(SymbolKind.Property, cancellationToken);

            return true;
        }

        private void CheckSurroundingContext(
            SymbolKind symbolKind, CancellationToken cancellationToken)
        {
            // See if we're being assigned to.  If so, look at the before/after statements
            // to see if either is an assignment.  If so, we can use that to try to determine
            // user patterns that can be used when generating the member.  For example,
            // if the sibling assignment is to a readonly field, then we want to offer to 
            // generate a readonly field vs a writable field.
            //
            // Also, because users often like to keep members/assignments in the same order
            // we can pick a good place for the new member based on the surrounding assignments.
            var syntaxFacts = _document.Document.GetLanguageService<ISyntaxFactsService>();
            var simpleName = SimpleNameOrMemberAccessExpressionOpt;

            if (syntaxFacts.IsLeftSideOfAssignment(simpleName))
            {
                var assignmentStatement = simpleName.Ancestors().FirstOrDefault(syntaxFacts.IsSimpleAssignmentStatement);
                if (assignmentStatement != null)
                {
                    syntaxFacts.GetPartsOfAssignmentStatement(
                        assignmentStatement, out var left, out var right);

                    if (left == simpleName)
                    {
                        var block = assignmentStatement.Parent;
                        var children = block.ChildNodesAndTokens();

                        var statementindex = GetStatementIndex(children, assignmentStatement);

                        var previousAssignedSymbol = TryGetAssignedSymbol(symbolKind, children, statementindex - 1, cancellationToken);
                        var nextAssignedSymbol = TryGetAssignedSymbol(symbolKind, children, statementindex + 1, cancellationToken);

                        if (symbolKind == SymbolKind.Field)
                        {
                            OfferReadOnlyFieldFirst =
                                FieldIsReadOnly(previousAssignedSymbol) || FieldIsReadOnly(nextAssignedSymbol);
                        }

                        AfterThisLocation ??= previousAssignedSymbol?.Locations.FirstOrDefault();
                        BeforeThisLocation ??= nextAssignedSymbol?.Locations.FirstOrDefault();
                    }
                }
            }
        }

        private ISymbol TryGetAssignedSymbol(
            SymbolKind symbolKind,
            ChildSyntaxList children, int index,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = _document.Document.GetLanguageService<ISyntaxFactsService>();
            if (index >= 0 && index < children.Count)
            {
                var sibling = children[index];
                if (sibling.IsNode)
                {
                    var siblingNode = sibling.AsNode();
                    if (syntaxFacts.IsSimpleAssignmentStatement(siblingNode))
                    {
                        syntaxFacts.GetPartsOfAssignmentStatement(
                            siblingNode, out var left, out _);

                        var symbol = _document.SemanticModel.GetSymbolInfo(left, cancellationToken).Symbol;
                        if (symbol?.Kind == symbolKind &&
                            symbol.ContainingType.Equals(ContainingType))
                        {
                            return symbol;
                        }
                    }
                }
            }

            return null;
        }

        private static IMethodSymbol FindContainingMethodSymbol(int position, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var symbol = semanticModel.GetEnclosingSymbol(position, cancellationToken);
            while (symbol != null)
            {
                if (symbol is IMethodSymbol method && !method.IsAnonymousFunction())
                {
                    return method;
                }

                symbol = symbol.ContainingSymbol;
            }

            return null;
        }

        private static bool FieldIsReadOnly(ISymbol symbol)
            => symbol is IFieldSymbol field && field.IsReadOnly;

        private static int GetStatementIndex(ChildSyntaxList children, SyntaxNode statement)
        {
            var index = 0;
            foreach (var child in children)
            {
                if (child == statement)
                {
                    return index;
                }

                index++;
            }

            throw ExceptionUtilities.Unreachable();
        }

        private bool TryDetermineFieldType(CancellationToken cancellationToken)
        {
            var typeInference = _document.Document.GetLanguageService<ITypeInferenceService>();
            var inferredType = typeInference.InferType(
                _document.SemanticModel, SimpleNameOrMemberAccessExpressionOpt, objectAsDefault: true,
                name: IdentifierToken.ValueText, cancellationToken: cancellationToken);

            // If you have `&X` and 'X' is some delegate type, then there's no variable that can be created that
            // will be legal there.  The only things X could be are a static method or a local function, not an
            // arbitrary variable (field, local, etc.).
            if (inferredType.IsDelegateType())
            {
                var syntaxKinds = _document.Document.GetRequiredLanguageService<ISyntaxKindsService>();
                if (syntaxKinds.AddressOfExpression == SimpleNameOrMemberAccessExpressionOpt.Parent?.RawKind)
                    return false;
            }

            var compilation = _document.SemanticModel.Compilation;
            inferredType = inferredType.SpecialType == SpecialType.System_Void
                ? compilation.ObjectType
                : inferredType;

            if (IsInConditionalAccessExpression)
            {
                inferredType = inferredType.RemoveNullableIfPresent();
            }

            if (inferredType.IsDelegateType() && !inferredType.CanBeReferencedByName)
            {
                var namedDelegateType = inferredType.GetDelegateType(compilation)?.DelegateInvokeMethod?.ConvertToType(compilation);
                if (namedDelegateType != null)
                {
                    inferredType = namedDelegateType;
                }
            }

            // Substitute 'object' for all captured method type parameters.  Note: we may need to
            // do this for things like anonymous types, as well as captured type parameters that
            // aren't in scope in the destination type.
            using var _1 = ArrayBuilder<ITypeParameterSymbol>.GetInstance(out var capturedMethodTypeParameters);
            inferredType.AddReferencedMethodTypeParameters(capturedMethodTypeParameters);

            var mapping = capturedMethodTypeParameters.ToDictionary(tp => tp,
                tp => compilation.ObjectType);

            TypeMemberType = inferredType.SubstituteTypes(mapping, compilation);
            var availableTypeParameters = TypeToGenerateIn.GetAllTypeParameters();
            TypeMemberType = TypeMemberType.RemoveUnavailableTypeParameters(
                compilation, availableTypeParameters);

            var enclosingMethodSymbol = _document.SemanticModel.GetEnclosingSymbol<IMethodSymbol>(SimpleNameOrMemberAccessExpressionOpt.SpanStart, cancellationToken);
            if (enclosingMethodSymbol != null && enclosingMethodSymbol.TypeParameters != null && enclosingMethodSymbol.TypeParameters.Length != 0)
            {
                using var _2 = ArrayBuilder<ITypeParameterSymbol>.GetInstance(out var combinedTypeParameters);
                combinedTypeParameters.AddRange(availableTypeParameters);
                combinedTypeParameters.AddRange(enclosingMethodSymbol.TypeParameters);
                LocalType = inferredType.RemoveUnavailableTypeParameters(compilation, combinedTypeParameters);
            }
            else
            {
                LocalType = TypeMemberType;
            }

            return true;
        }

        private bool DetermineIsInConstructor(SyntaxNode simpleName)
        {
            if (!ContainingType.OriginalDefinition.Equals(TypeToGenerateIn.OriginalDefinition))
                return false;

            // If we're in an lambda/local function we're not actually 'in' the constructor.
            // i.e. we can't actually write to read-only fields here.
            var syntaxFacts = _document.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            if (simpleName.AncestorsAndSelf().Any(syntaxFacts.IsAnonymousOrLocalFunction))
                return false;

            return syntaxFacts.IsInConstructor(simpleName);
        }
    }
}
