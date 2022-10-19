// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
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
            private readonly State _state;
            private readonly bool _generateProperty;
            private readonly bool _isReadonly;
            private readonly bool _isConstant;
            private readonly RefKind _refKind;
            private readonly SemanticDocument _semanticDocument;
            private readonly string _equivalenceKey;

            public GenerateVariableCodeAction(
                SemanticDocument document,
                State state,
                bool generateProperty,
                bool isReadonly,
                bool isConstant,
                RefKind refKind)
            {
                _semanticDocument = document;
                _state = state;
                _generateProperty = generateProperty;
                _isReadonly = isReadonly;
                _isConstant = isConstant;
                _refKind = refKind;
                _equivalenceKey = Title;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var solution = _semanticDocument.Project.Solution;
                var generateUnsafe = _state.TypeMemberType.RequiresUnsafeModifier() &&
                                     !_state.IsContainedInUnsafeType;

                var context = new CodeGenerationContext(
                    afterThisLocation: _state.AfterThisLocation,
                    beforeThisLocation: _state.BeforeThisLocation,
                    contextLocation: _state.IdentifierToken.GetLocation());

                if (_generateProperty)
                {
                    var getAccessor = CreateAccessor(_state.DetermineMaximalAccessibility());
                    var setAccessor = _isReadonly || _refKind != RefKind.None
                        ? null
                        : CreateAccessor(DetermineMinimalAccessibility(_state));

                    var propertySymbol = CodeGenerationSymbolFactory.CreatePropertySymbol(
                        attributes: default,
                        accessibility: _state.DetermineMaximalAccessibility(),
                        modifiers: new DeclarationModifiers(isStatic: _state.IsStatic, isUnsafe: generateUnsafe),
                        type: _state.TypeMemberType,
                        refKind: _refKind,
                        explicitInterfaceImplementations: default,
                        name: _state.IdentifierToken.ValueText,
                        isIndexer: _state.IsIndexer,
                        parameters: _state.Parameters,
                        getMethod: getAccessor,
                        setMethod: setAccessor);

                    return await CodeGenerator.AddPropertyDeclarationAsync(
                        solution, _state.TypeToGenerateIn, propertySymbol, context, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var fieldSymbol = CodeGenerationSymbolFactory.CreateFieldSymbol(
                        attributes: default,
                        accessibility: DetermineMinimalAccessibility(_state),
                        modifiers: _isConstant
                            ? new DeclarationModifiers(isConst: true, isUnsafe: generateUnsafe)
                            : new DeclarationModifiers(isStatic: _state.IsStatic, isReadOnly: _isReadonly, isUnsafe: generateUnsafe),
                        type: _state.TypeMemberType,
                        name: _state.IdentifierToken.ValueText);

                    return await CodeGenerator.AddFieldDeclarationAsync(
                        solution, _state.TypeToGenerateIn, fieldSymbol, context, cancellationToken).ConfigureAwait(false);
                }
            }

            private IMethodSymbol CreateAccessor(Accessibility accessibility)
            {
                return CodeGenerationSymbolFactory.CreateAccessorSymbol(
                    attributes: default,
                    accessibility: accessibility,
                    statements: GenerateStatements());
            }

            private ImmutableArray<SyntaxNode> GenerateStatements()
            {
                var syntaxFactory = _semanticDocument.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language).GetService<SyntaxGenerator>();

                var throwStatement = CodeGenerationHelpers.GenerateThrowStatement(
                    syntaxFactory, _semanticDocument, "System.NotImplementedException");

                return _state.TypeToGenerateIn.TypeKind != TypeKind.Interface && _refKind != RefKind.None
                    ? ImmutableArray.Create(throwStatement)
                    : default;
            }

            private Accessibility DetermineMinimalAccessibility(State state)
            {
                if (state.TypeToGenerateIn.TypeKind == TypeKind.Interface)
                {
                    return Accessibility.NotApplicable;
                }

                // Otherwise, figure out what accessibility modifier to use and optionally mark
                // it as static.
                var syntaxFacts = _semanticDocument.Document.GetLanguageService<ISyntaxFactsService>();
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
                    // class B : A { void Goo() { A a; a.Goo(); }
                    //
                    // In this case we can *not* mark the method as protected.  'B' can only
                    // access protected members of 'A' through an instance of 'B' (or a subclass
                    // of B).  It can not access protected members through an instance of the
                    // superclass.  In this case we need to make the method public or internal.
                    //
                    // However, this does not apply if the method will be static.  i.e.
                    // 
                    // class B : A { void Goo() { A.Goo(); }
                    //
                    // B can access the protected statics of A, and so we generate 'Goo' as
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

            private static bool DerivesFrom(State state, INamedTypeSymbol containingType)
            {
                return containingType.GetBaseTypes().Select(t => t.OriginalDefinition)
                                                    .Contains(state.TypeToGenerateIn);
            }

            public override string Title
            {
                get
                {
                    var text = _isConstant
                        ? FeaturesResources.Generate_constant_0
                        : _generateProperty
                            ? _isReadonly ? FeaturesResources.Generate_read_only_property_0 : FeaturesResources.Generate_property_0
                            : _isReadonly ? FeaturesResources.Generate_read_only_field_0 : FeaturesResources.Generate_field_0;

                    return string.Format(
                        text,
                        _state.IdentifierToken.ValueText);
                }
            }

            public override string EquivalenceKey => _equivalenceKey;
        }
    }
}
