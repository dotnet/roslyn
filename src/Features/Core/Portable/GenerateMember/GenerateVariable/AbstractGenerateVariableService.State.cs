// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateVariable
{
    internal abstract partial class AbstractGenerateVariableService<TService, TSimpleNameSyntax, TExpressionSyntax>
    {
        private partial class State
        {
            public INamedTypeSymbol ContainingType { get; private set; }
            public INamedTypeSymbol TypeToGenerateIn { get; private set; }
            public bool IsStatic { get; private set; }
            public bool IsConstant { get; private set; }
            public bool IsIndexer { get; private set; }
            public bool IsContainedInUnsafeType { get; private set; }
            public ImmutableArray<IParameterSymbol> Parameters { get; private set; }

            // Just the name of the method.  i.e. "Goo" in "Goo" or "X.Goo"
            public SyntaxToken IdentifierToken { get; private set; }
            public TSimpleNameSyntax SimpleNameOpt { get; private set; }

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

            public bool IsInExecutableBlock { get; private set; }
            public bool IsInConditionalAccessExpression { get; private set; }

            public Location AfterThisLocation { get; private set; }
            public Location BeforeThisLocation { get; private set; }

            public static async Task<State> GenerateAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode interfaceNode,
                CancellationToken cancellationToken)
            {
                var state = new State();
                if (!await state.TryInitializeAsync(service, document, interfaceNode, cancellationToken).ConfigureAwait(false))
                {
                    return null;
                }

                return state;
            }

            private async Task<bool> TryInitializeAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode node,
                CancellationToken cancellationToken)
            {
                if (service.IsIdentifierNameGeneration(node))
                {
                    // Cases that we deal with currently:
                    //
                    // 1) expr.Goo
                    // 2) expr->Goo
                    // 3) Goo
                    if (!TryInitializeSimpleName(service, document, (TSimpleNameSyntax)node, cancellationToken))
                    {
                        return false;
                    }
                }
                else if (service.IsExplicitInterfaceGeneration(node))
                {
                    // 4)  bool IGoo.NewProp
                    if (!TryInitializeExplicitInterface(service, document, node, cancellationToken))
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
                var existingMembers = this.TypeToGenerateIn.GetMembers(this.IdentifierToken.ValueText)
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

                this.TypeToGenerateIn = await SymbolFinder.FindSourceDefinitionAsync(this.TypeToGenerateIn, document.Project.Solution, cancellationToken).ConfigureAwait(false) as INamedTypeSymbol;

                if (!service.ValidateTypeToGenerateIn(
                    document.Project.Solution, this.TypeToGenerateIn, this.IsStatic, ClassInterfaceModuleStructTypes, cancellationToken))
                {
                    return false;
                }

                this.IsContainedInUnsafeType = service.ContainingTypesOrSelfHasUnsafeKeyword(this.TypeToGenerateIn);

                return CanGenerateLocal() || CodeGenerator.CanAdd(document.Project.Solution, this.TypeToGenerateIn, cancellationToken);
            }

            internal bool CanGenerateLocal()
            {
                return !this.IsInMemberContext && this.IsInExecutableBlock;
            }

            private bool TryInitializeExplicitInterface(
                TService service,
                SemanticDocument document,
                SyntaxNode propertyDeclaration,
                CancellationToken cancellationToken)
            {
                if (!service.TryInitializeExplicitInterfaceState(
                    document, propertyDeclaration, cancellationToken,
                    out var identifierToken, out var propertySymbol, out var typeToGenerateIn))
                {
                    return false;
                }

                this.IdentifierToken = identifierToken;
                this.TypeToGenerateIn = typeToGenerateIn;

                if (propertySymbol.ExplicitInterfaceImplementations.Any())
                {
                    return false;
                }

                cancellationToken.ThrowIfCancellationRequested();

                var semanticModel = document.SemanticModel;
                this.ContainingType = semanticModel.GetEnclosingNamedType(this.IdentifierToken.SpanStart, cancellationToken);
                if (this.ContainingType == null)
                {
                    return false;
                }

                if (!this.ContainingType.Interfaces.OfType<INamedTypeSymbol>().Contains(this.TypeToGenerateIn))
                {
                    return false;
                }

                this.IsIndexer = propertySymbol.IsIndexer;
                this.Parameters = propertySymbol.Parameters;
                this.TypeMemberType = propertySymbol.Type;

                // By default, make it readonly, unless there's already an setter defined.
                this.IsWrittenTo = propertySymbol.SetMethod != null;

                return true;
            }

            private bool TryInitializeSimpleName(
                TService service,
                SemanticDocument semanticDocument,
                TSimpleNameSyntax simpleName,
                CancellationToken cancellationToken)
            {
                if (!service.TryInitializeIdentifierNameState(
                        semanticDocument, simpleName, cancellationToken,
                        out var identifierToken, out var simpleNameOrMemberAccessExpression, out var isInExecutableBlock, out var isInConditionalAccessExpression))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(identifierToken.ValueText))
                {
                    return false;
                }

                this.SimpleNameOpt = simpleName;
                this.IdentifierToken = identifierToken;
                this.SimpleNameOrMemberAccessExpressionOpt = simpleNameOrMemberAccessExpression;
                this.IsInExecutableBlock = isInExecutableBlock;
                this.IsInConditionalAccessExpression = isInConditionalAccessExpression;

                // If we're in a type context then we shouldn't offer to generate a field or
                // property.
                var syntaxFacts = semanticDocument.Document.GetLanguageService<ISyntaxFactsService>();
                if (syntaxFacts.IsInNamespaceOrTypeContext(this.SimpleNameOrMemberAccessExpressionOpt))
                {
                    return false;
                }

                this.IsConstant = syntaxFacts.IsInConstantContext(this.SimpleNameOrMemberAccessExpressionOpt);

                // If we're not in a type, don't even bother.  NOTE(cyrusn): We'll have to rethink this
                // for C# Script.
                cancellationToken.ThrowIfCancellationRequested();
                var semanticModel = semanticDocument.SemanticModel;
                this.ContainingType = semanticModel.GetEnclosingNamedType(this.IdentifierToken.SpanStart, cancellationToken);
                if (this.ContainingType == null)
                {
                    return false;
                }

                // Now, try to bind the invocation and see if it succeeds or not.  if it succeeds and
                // binds uniquely, then we don't need to offer this quick fix.
                cancellationToken.ThrowIfCancellationRequested();
                var semanticInfo = semanticModel.GetSymbolInfo(this.SimpleNameOrMemberAccessExpressionOpt, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                if (semanticInfo.Symbol != null)
                {
                    return false;
                }

                // Either we found no matches, or this was ambiguous. Either way, we might be able
                // to generate a method here.  Determine where the user wants to generate the method
                // into, and if it's valid then proceed.
                cancellationToken.ThrowIfCancellationRequested();
                if (!service.TryDetermineTypeToGenerateIn(semanticDocument, this.ContainingType, this.SimpleNameOrMemberAccessExpressionOpt, cancellationToken,
                    out var typeToGenerateIn, out var isStatic))
                {
                    return false;
                }

                this.TypeToGenerateIn = typeToGenerateIn;
                this.IsStatic = isStatic;

                DetermineFieldType(semanticDocument, cancellationToken);

                var semanticFacts = semanticDocument.Document.GetLanguageService<ISemanticFactsService>();
                this.IsInRefContext = semanticFacts.IsInRefContext(semanticModel, this.SimpleNameOrMemberAccessExpressionOpt, cancellationToken);
                this.IsInInContext = semanticFacts.IsInInContext(semanticModel, this.SimpleNameOrMemberAccessExpressionOpt, cancellationToken);
                this.IsInOutContext = semanticFacts.IsInOutContext(semanticModel, this.SimpleNameOrMemberAccessExpressionOpt, cancellationToken);
                this.IsWrittenTo = semanticFacts.IsWrittenTo(semanticModel, this.SimpleNameOrMemberAccessExpressionOpt, cancellationToken);
                this.IsOnlyWrittenTo = semanticFacts.IsOnlyWrittenTo(semanticModel, this.SimpleNameOrMemberAccessExpressionOpt, cancellationToken);
                this.IsInConstructor = DetermineIsInConstructor(semanticDocument);
                this.IsInMemberContext = this.SimpleNameOpt != this.SimpleNameOrMemberAccessExpressionOpt ||
                                         syntaxFacts.IsObjectInitializerNamedAssignmentIdentifier(this.SimpleNameOrMemberAccessExpressionOpt);

                CheckSurroundingContext(semanticDocument, SymbolKind.Field, cancellationToken);
                CheckSurroundingContext(semanticDocument, SymbolKind.Property, cancellationToken);

                return true;
            }

            private void CheckSurroundingContext(
                SemanticDocument semanticDocument, SymbolKind symbolKind, CancellationToken cancellationToken)
            {
                // See if we're being assigned to.  If so, look at the before/after statements
                // to see if either is an assignment.  If so, we can use that to try to determine
                // user patterns that can be used when generating the member.  For example,
                // if the sibling assignment is to a readonly field, then we want to offer to 
                // generate a readonly field vs a writable field.
                //
                // Also, because users often like to keep members/assignments in the same order
                // we can pick a good place for the new member based on the surrounding assignments.
                var syntaxFacts = semanticDocument.Document.GetLanguageService<ISyntaxFactsService>();
                var simpleName = this.SimpleNameOrMemberAccessExpressionOpt;

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

                            var previousAssignedSymbol = TryGetAssignedSymbol(semanticDocument, symbolKind, children, statementindex - 1, cancellationToken);
                            var nextAssignedSymbol = TryGetAssignedSymbol(semanticDocument, symbolKind, children, statementindex + 1, cancellationToken);

                            if (symbolKind == SymbolKind.Field)
                            {
                                this.OfferReadOnlyFieldFirst = FieldIsReadOnly(previousAssignedSymbol) ||
                                                               FieldIsReadOnly(nextAssignedSymbol);
                            }

                            this.AfterThisLocation = this.AfterThisLocation ?? previousAssignedSymbol?.Locations.FirstOrDefault();
                            this.BeforeThisLocation = this.BeforeThisLocation ?? nextAssignedSymbol?.Locations.FirstOrDefault();
                        }
                    }
                }
            }

            private ISymbol TryGetAssignedSymbol(
                SemanticDocument semanticDocument, SymbolKind symbolKind,
                ChildSyntaxList children, int index,
                CancellationToken cancellationToken)
            {
                var syntaxFacts = semanticDocument.Document.GetLanguageService<ISyntaxFactsService>();
                if (index >= 0 && index < children.Count)
                {
                    var sibling = children[index];
                    if (sibling.IsNode)
                    {
                        var siblingNode = sibling.AsNode();
                        if (syntaxFacts.IsSimpleAssignmentStatement(siblingNode))
                        {
                            syntaxFacts.GetPartsOfAssignmentStatement(
                                siblingNode, out var left, out var right);

                            var symbol = semanticDocument.SemanticModel.GetSymbolInfo(left, cancellationToken).Symbol;
                            if (symbol?.Kind == symbolKind &&
                                symbol.ContainingType.Equals(this.ContainingType))
                            {
                                return symbol;
                            }
                        }
                    }
                }

                return null;
            }

            private bool FieldIsReadOnly(ISymbol symbol)
                => symbol is IFieldSymbol field && field.IsReadOnly;

            private int GetStatementIndex(ChildSyntaxList children, SyntaxNode statement)
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

                throw ExceptionUtilities.Unreachable;
            }

            private void DetermineFieldType(
                SemanticDocument semanticDocument,
                CancellationToken cancellationToken)
            {
                var typeInference = semanticDocument.Document.GetLanguageService<ITypeInferenceService>();
                var inferredType = typeInference.InferType(
                    semanticDocument.SemanticModel, this.SimpleNameOrMemberAccessExpressionOpt, objectAsDefault: true,
                    nameOpt: this.IdentifierToken.ValueText, cancellationToken: cancellationToken);

                var compilation = semanticDocument.SemanticModel.Compilation;
                inferredType = inferredType.SpecialType == SpecialType.System_Void
                    ? compilation.ObjectType
                    : inferredType;

                if (this.IsInConditionalAccessExpression)
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
                var capturedMethodTypeParameters = inferredType.GetReferencedMethodTypeParameters();
                var mapping = capturedMethodTypeParameters.ToDictionary(tp => tp,
                    tp => compilation.ObjectType);

                this.TypeMemberType = inferredType.SubstituteTypes(mapping, compilation);
                var availableTypeParameters = this.TypeToGenerateIn.GetAllTypeParameters();
                this.TypeMemberType = TypeMemberType.RemoveUnavailableTypeParameters(
                    compilation, availableTypeParameters);

                var enclosingMethodSymbol = semanticDocument.SemanticModel.GetEnclosingSymbol<IMethodSymbol>(this.SimpleNameOrMemberAccessExpressionOpt.SpanStart, cancellationToken);
                if (enclosingMethodSymbol != null && enclosingMethodSymbol.TypeParameters != null && enclosingMethodSymbol.TypeParameters.Length != 0)
                {
                    var combinedTypeParameters = new List<ITypeParameterSymbol>();
                    combinedTypeParameters.AddRange(availableTypeParameters);
                    combinedTypeParameters.AddRange(enclosingMethodSymbol.TypeParameters);
                    this.LocalType = inferredType.RemoveUnavailableTypeParameters(
                    compilation, combinedTypeParameters);
                }
                else
                {
                    this.LocalType = this.TypeMemberType;
                }
            }

            private bool DetermineIsInConstructor(SemanticDocument semanticDocument)
            {
                if (!this.ContainingType.OriginalDefinition.Equals(this.TypeToGenerateIn.OriginalDefinition))
                {
                    return false;
                }

                var syntaxFacts = semanticDocument.Document.GetLanguageService<ISyntaxFactsService>();
                return syntaxFacts.IsInConstructor(this.SimpleNameOpt);
            }
        }
    }
}
