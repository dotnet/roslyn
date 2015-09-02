// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateVariable
{
    internal abstract partial class AbstractGenerateVariableService<TService, TSimpleNameSyntax, TExpressionSyntax>
    {
        private partial class GenerateVariableCodeAction : CodeAction
        {
            private readonly TService _service;
            private readonly State _state;
            private readonly bool _generateProperty;
            private readonly bool _isReadonly;
            private readonly bool _isConstant;
            private readonly Document _document;
            private readonly string _equivalenceKey;

            public GenerateVariableCodeAction(
                TService service,
                Document document,
                State state,
                bool generateProperty,
                bool isReadonly,
                bool isConstant)
            {
                _service = service;
                _document = document;
                _state = state;
                _generateProperty = generateProperty;
                _isReadonly = isReadonly;
                _isConstant = isConstant;
                _equivalenceKey = Title;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var syntaxTree = await _document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var generateUnsafe = _state.TypeMemberType.IsUnsafe() &&
                                     !_state.IsContainedInUnsafeType;

                if (_generateProperty)
                {
                    var getAccessor = CodeGenerationSymbolFactory.CreateAccessorSymbol(
                        attributes: null,
                        accessibility: DetermineMaximalAccessibility(_state),
                        statements: null);
                    var setAccessor = _isReadonly ? null : CodeGenerationSymbolFactory.CreateAccessorSymbol(
                        attributes: null,
                        accessibility: DetermineMinimalAccessibility(_state),
                        statements: null);

                    var result = await CodeGenerator.AddPropertyDeclarationAsync(
                        _document.Project.Solution,
                        _state.TypeToGenerateIn,
                        CodeGenerationSymbolFactory.CreatePropertySymbol(
                            attributes: null,
                            accessibility: DetermineMaximalAccessibility(_state),
                            modifiers: new DeclarationModifiers(isStatic: _state.IsStatic, isUnsafe: generateUnsafe),
                            type: _state.TypeMemberType,
                            explicitInterfaceSymbol: null,
                            name: _state.IdentifierToken.ValueText,
                            isIndexer: _state.IsIndexer,
                            parameters: _state.Parameters,
                            getMethod: getAccessor,
                            setMethod: setAccessor),
                        new CodeGenerationOptions(contextLocation: _state.IdentifierToken.GetLocation()),
                        cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    return result;
                }
                else
                {
                    var result = await CodeGenerator.AddFieldDeclarationAsync(
                        _document.Project.Solution,
                        _state.TypeToGenerateIn,
                        CodeGenerationSymbolFactory.CreateFieldSymbol(
                            attributes: null,
                            accessibility: DetermineMinimalAccessibility(_state),
                            modifiers: _isConstant ?
                                new DeclarationModifiers(isConst: true, isUnsafe: generateUnsafe) :
                                new DeclarationModifiers(isStatic: _state.IsStatic, isReadOnly: _isReadonly, isUnsafe: generateUnsafe),
                            type: _state.TypeMemberType,
                            name: _state.IdentifierToken.ValueText),
                        new CodeGenerationOptions(contextLocation: _state.IdentifierToken.GetLocation()),
                        cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    return result;
                }
            }

            private Accessibility DetermineMaximalAccessibility(State state)
            {
                if (state.TypeToGenerateIn.TypeKind == TypeKind.Interface)
                {
                    return Accessibility.NotApplicable;
                }

                var accessibility = Accessibility.Public;

                // Ensure that we're not overly exposing a type.
                var containingTypeAccessibility = state.TypeToGenerateIn.DetermineMinimalAccessibility();
                var effectiveAccessibility = AccessibilityUtilities.Minimum(
                    containingTypeAccessibility, accessibility);

                var returnTypeAccessibility = state.TypeMemberType.DetermineMinimalAccessibility();

                if (AccessibilityUtilities.Minimum(effectiveAccessibility, returnTypeAccessibility) !=
                    effectiveAccessibility)
                {
                    return returnTypeAccessibility;
                }

                return accessibility;
            }

            private Accessibility DetermineMinimalAccessibility(State state)
            {
                if (state.TypeToGenerateIn.TypeKind == TypeKind.Interface)
                {
                    return Accessibility.NotApplicable;
                }

                // Otherwise, figure out what accessibility modifier to use and optionally mark
                // it as static.
                var syntaxFacts = _document.GetLanguageService<ISyntaxFactsService>();
                if (syntaxFacts.IsAttributeNamedArgumentIdentifier(state.SimpleNameOrMemberAccessExpressionOpt))
                {
                    return Accessibility.Public;
                }
                else if (state.ContainingType.IsContainedWithin(state.TypeToGenerateIn))
                {
                    return Accessibility.Private;
                }
                else if (DerivesFrom(state, state.ContainingType) && state.IsStatic)
                {
                    // NOTE(cyrusn): We only generate protected in the case of statics.  Consider
                    // the case where we're generating into one of our base types.  i.e.:
                    //
                    // class B : A { void Foo() { A a; a.Foo(); }
                    //
                    // In this case we can *not* mark the method as protected.  'B' can only
                    // access protected members of 'A' through an instance of 'B' (or a subclass
                    // of B).  It can not access protected members through an instance of the
                    // superclass.  In this case we need to make the method public or internal.
                    //
                    // However, this does not apply if the method will be static.  i.e.
                    // 
                    // class B : A { void Foo() { A.Foo(); }
                    //
                    // B can access the protected statics of A, and so we generate 'Foo' as
                    // protected.
                    return Accessibility.Protected;
                }
                else if (state.ContainingType.ContainingAssembly.IsSameAssemblyOrHasFriendAccessTo(state.TypeToGenerateIn.ContainingAssembly))
                {
                    return Accessibility.Internal;
                }
                else
                {
                    // TODO: Code coverage - we need a unit-test that generates across projects
                    return Accessibility.Public;
                }
            }

            private bool DerivesFrom(State state, INamedTypeSymbol containingType)
            {
                return containingType.GetBaseTypes().Select(t => t.OriginalDefinition)
                                                    .Contains(state.TypeToGenerateIn);
            }

            public override string Title
            {
                get
                {
                    var text = _isConstant
                        ? FeaturesResources.GenerateConstantIn
                        : _generateProperty
                            ? _isReadonly ? FeaturesResources.GenerateReadonlyProperty : FeaturesResources.GeneratePropertyIn
                            : _isReadonly ? FeaturesResources.GenerateReadonlyField : FeaturesResources.GenerateFieldIn;

                    return string.Format(
                        text,
                        _state.IdentifierToken.ValueText,
                        _state.TypeToGenerateIn.Name);
                }
            }

            public override string EquivalenceKey
            {
                get
                {
                    return _equivalenceKey;
                }
            }
        }
    }
}
