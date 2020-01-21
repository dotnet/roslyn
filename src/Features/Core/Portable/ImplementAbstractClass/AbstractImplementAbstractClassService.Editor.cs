// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ImplementAbstractClass
{
    internal partial class AbstractImplementAbstractClassService<TClassSyntax>
    {
        private partial class Editor
        {
            private readonly Document _document;
            private readonly SemanticModel _model;
            private readonly State _state;

            public Editor(
                Document document,
                SemanticModel model,
                State state)
            {
                _document = document;
                _model = model;
                _state = state;
            }

            public async Task<Document> GetEditAsync(CancellationToken cancellationToken)
            {
                var unimplementedMembers = _state.UnimplementedMembers;

                var options = await _document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var propertyGenerationBehavior = options.GetOption(ImplementTypeOptions.PropertyGenerationBehavior);

                var memberDefinitions = GenerateMembers(
                    unimplementedMembers, propertyGenerationBehavior, cancellationToken);

                var insertionBehavior = options.GetOption(ImplementTypeOptions.InsertionBehavior);
                var groupMembers = insertionBehavior == ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind;

                return await CodeGenerator.AddMemberDeclarationsAsync(
                    _document.Project.Solution,
                    _state.ClassType,
                    memberDefinitions,
                    new CodeGenerationOptions(
                        _state.Location.GetLocation(),
                        autoInsertionLocation: groupMembers,
                        sortMembers: groupMembers),
                    cancellationToken).ConfigureAwait(false);
            }

            private ImmutableArray<ISymbol> GenerateMembers(
                ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> unimplementedMembers,
                ImplementTypePropertyGenerationBehavior propertyGenerationBehavior,
                CancellationToken cancellationToken)
            {
                return unimplementedMembers.SelectMany(t => t.members)
                                           .Select(m => GenerateMember(m, propertyGenerationBehavior, cancellationToken))
                                           .WhereNotNull()
                                           .ToImmutableArray();
            }

            private ISymbol GenerateMember(
                ISymbol member,
                ImplementTypePropertyGenerationBehavior propertyGenerationBehavior,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Check if we need to add 'unsafe' to the signature we're generating.
                var syntaxFacts = _document.GetLanguageService<ISyntaxFactsService>();
                var addUnsafe = member.IsUnsafe() && !syntaxFacts.IsUnsafeContext(_state.Location);

                return GenerateMember(member, addUnsafe, propertyGenerationBehavior, cancellationToken);
            }

            private ISymbol GenerateMember(
                ISymbol member,
                bool addUnsafe,
                ImplementTypePropertyGenerationBehavior propertyGenerationBehavior,
                CancellationToken cancellationToken)
            {
                var modifiers = new DeclarationModifiers(isOverride: true, isUnsafe: addUnsafe);
                var accessibility = member.ComputeResultantAccessibility(_state.ClassType);

                return member switch
                {
                    IMethodSymbol method => GenerateMethod(method, modifiers, accessibility, cancellationToken),
                    IPropertySymbol property => GenerateProperty(property, modifiers, accessibility, propertyGenerationBehavior),
                    IEventSymbol @event => CodeGenerationSymbolFactory.CreateEventSymbol(
                        @event, accessibility: accessibility, modifiers: modifiers),
                    _ => null,
                };
            }

            private ISymbol GenerateMethod(
                IMethodSymbol method, DeclarationModifiers modifiers, Accessibility accessibility, CancellationToken cancellationToken)
            {
                var syntaxFacts = _document.GetLanguageService<ISyntaxFactsService>();
                var syntaxFactory = _document.GetLanguageService<SyntaxGenerator>();
                var throwingBody = syntaxFactory.CreateThrowNotImplementedStatementBlock(
                    _model.Compilation);

                method = method.EnsureNonConflictingNames(_state.ClassType, syntaxFacts);

                return CodeGenerationSymbolFactory.CreateMethodSymbol(
                    method,
                    accessibility: accessibility,
                    modifiers: modifiers,
                    statements: throwingBody);
            }

            private IPropertySymbol GenerateProperty(
                IPropertySymbol property,
                DeclarationModifiers modifiers,
                Accessibility accessibility,
                ImplementTypePropertyGenerationBehavior propertyGenerationBehavior)
            {
                if (property.GetMethod == null)
                {
                    // Can't generate an auto-prop for a setter-only property.
                    propertyGenerationBehavior = ImplementTypePropertyGenerationBehavior.PreferThrowingProperties;
                }

                var syntaxFactory = _document.GetLanguageService<SyntaxGenerator>();

                var accessorBody = propertyGenerationBehavior == ImplementTypePropertyGenerationBehavior.PreferAutoProperties
                    ? default
                    : syntaxFactory.CreateThrowNotImplementedStatementBlock(_model.Compilation);

                var getMethod = ShouldGenerateAccessor(property.GetMethod)
                    ? CodeGenerationSymbolFactory.CreateAccessorSymbol(
                        property.GetMethod,
                        attributes: default,
                        accessibility: property.GetMethod.ComputeResultantAccessibility(_state.ClassType),
                        statements: accessorBody)
                    : null;

                var setMethod = ShouldGenerateAccessor(property.SetMethod)
                    ? CodeGenerationSymbolFactory.CreateAccessorSymbol(
                        property.SetMethod,
                        attributes: default,
                        accessibility: property.SetMethod.ComputeResultantAccessibility(_state.ClassType),
                        statements: accessorBody)
                    : null;

                return CodeGenerationSymbolFactory.CreatePropertySymbol(
                    property,
                    accessibility: accessibility,
                    modifiers: modifiers,
                    getMethod: getMethod,
                    setMethod: setMethod);
            }

            private bool ShouldGenerateAccessor(IMethodSymbol method)
                => method != null && _state.ClassType.FindImplementationForAbstractMember(method) == null;
        }
    }
}
