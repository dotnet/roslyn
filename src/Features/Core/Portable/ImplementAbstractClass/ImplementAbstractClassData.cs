// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
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
    internal class ImplementAbstractClassData
    {
        private readonly Document _document;
        private readonly SyntaxNode _classNode;
        private readonly ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> _unimplementedMembers;

        public readonly INamedTypeSymbol ClassType;
        public readonly INamedTypeSymbol AbstractClassType;

        public ImplementAbstractClassData(
            Document document, SyntaxNode classNode, INamedTypeSymbol classType, INamedTypeSymbol abstractClassType,
            ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> unimplementedMembers)
        {
            _document = document;
            _classNode = classNode;
            ClassType = classType;
            AbstractClassType = abstractClassType;
            _unimplementedMembers = unimplementedMembers;
        }

        public static async Task<ImplementAbstractClassData?> TryGetDataAsync(
            Document document, SyntaxNode classNode, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (!(semanticModel.GetDeclaredSymbol(classNode) is INamedTypeSymbol classType))
                return null;

            if (classType.IsAbstract)
                return null;

            var abstractClassType = classType.BaseType;
            if (abstractClassType == null || !abstractClassType.IsAbstractClass())
                return null;

            if (!CodeGenerator.CanAdd(document.Project.Solution, classType, cancellationToken))
                return null;

            var unimplementedMembers = classType.GetAllUnimplementedMembers(
                SpecializedCollections.SingletonEnumerable(abstractClassType), cancellationToken);
            if (unimplementedMembers.IsEmpty)
                return null;

            return new ImplementAbstractClassData(document, classNode, classType, abstractClassType, unimplementedMembers);
        }

        public static async Task<Document?> TryImplementAbstractClassAsync(Document document, SyntaxNode classNode, CancellationToken cancellationToken)
        {
            var data = await TryGetDataAsync(document, classNode, cancellationToken).ConfigureAwait(false);
            if (data == null)
                return null;

            return await data.ImplementAbstractClassAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<Document> ImplementAbstractClassAsync(CancellationToken cancellationToken)
        {
            var compilation = await _document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            var options = await _document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var propertyGenerationBehavior = options.GetOption(ImplementTypeOptions.PropertyGenerationBehavior);

            var memberDefinitions = GenerateMembers(compilation, propertyGenerationBehavior, cancellationToken);

            var insertionBehavior = options.GetOption(ImplementTypeOptions.InsertionBehavior);
            var groupMembers = insertionBehavior == ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind;

            return await CodeGenerator.AddMemberDeclarationsAsync(
                _document.Project.Solution,
                this.ClassType,
                memberDefinitions,
                new CodeGenerationOptions(
                    contextLocation: _classNode.GetLocation(),
                    autoInsertionLocation: groupMembers,
                    sortMembers: groupMembers),
                cancellationToken).ConfigureAwait(false);
        }

        private ImmutableArray<ISymbol> GenerateMembers(
            Compilation compilation,
            ImplementTypePropertyGenerationBehavior propertyGenerationBehavior,
            CancellationToken cancellationToken)
        {
            return _unimplementedMembers
                .SelectMany(t => t.members)
                .Select(m => GenerateMember(compilation, m, propertyGenerationBehavior, cancellationToken))
                .WhereNotNull()
                .ToImmutableArray();
        }

        private ISymbol? GenerateMember(
            Compilation compilation, ISymbol member,
            ImplementTypePropertyGenerationBehavior propertyGenerationBehavior,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check if we need to add 'unsafe' to the signature we're generating.
            var syntaxFacts = _document.GetRequiredLanguageService<ISyntaxFactsService>();
            var addUnsafe = member.IsUnsafe() && !syntaxFacts.IsUnsafeContext(_classNode);

            return GenerateMember(compilation, member, addUnsafe, propertyGenerationBehavior);
        }

        private ISymbol? GenerateMember(
            Compilation compilation, ISymbol member, bool addUnsafe,
            ImplementTypePropertyGenerationBehavior propertyGenerationBehavior)
        {
            var modifiers = new DeclarationModifiers(isOverride: true, isUnsafe: addUnsafe);
            var accessibility = member.ComputeResultantAccessibility(this.ClassType);

            return member switch
            {
                IMethodSymbol method => GenerateMethod(compilation, method, modifiers, accessibility),
                IPropertySymbol property => GenerateProperty(compilation, property, modifiers, accessibility, propertyGenerationBehavior),
                IEventSymbol @event => GenerateEvent(@event, accessibility, modifiers),
                _ => null,
            };
        }

        private ISymbol GenerateMethod(
            Compilation compilation, IMethodSymbol method,
            DeclarationModifiers modifiers, Accessibility accessibility)
        {
            var syntaxFacts = _document.GetRequiredLanguageService<ISyntaxFactsService>();
            var generator = _document.GetRequiredLanguageService<SyntaxGenerator>();
            var body = generator.CreateThrowNotImplementedStatement(compilation);

            method = method.EnsureNonConflictingNames(this.ClassType, syntaxFacts);

            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                method,
                accessibility: accessibility,
                modifiers: modifiers,
                statements: ImmutableArray.Create(body));
        }

        private IPropertySymbol GenerateProperty(
            Compilation compilation,
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

            var generator = _document.GetRequiredLanguageService<SyntaxGenerator>();
            var preferAutoProperties = propertyGenerationBehavior == ImplementTypePropertyGenerationBehavior.PreferAutoProperties;

            var getMethod = ShouldGenerateAccessor(property.GetMethod)
                ? CodeGenerationSymbolFactory.CreateAccessorSymbol(
                    property.GetMethod,
                    attributes: default,
                    accessibility: property.GetMethod.ComputeResultantAccessibility(this.ClassType),
                    statements: generator.GetGetAccessorStatements(
                        compilation, property, throughMember: null, preferAutoProperties))
                : null;

            var setMethod = ShouldGenerateAccessor(property.SetMethod)
                ? CodeGenerationSymbolFactory.CreateAccessorSymbol(
                    property.SetMethod,
                    attributes: default,
                    accessibility: property.SetMethod.ComputeResultantAccessibility(this.ClassType),
                    statements: generator.GetSetAccessorStatements(
                        compilation, property, throughMember: null, preferAutoProperties))
                : null;

            return CodeGenerationSymbolFactory.CreatePropertySymbol(
                property,
                accessibility: accessibility,
                modifiers: modifiers,
                getMethod: getMethod,
                setMethod: setMethod);
        }

        private IEventSymbol GenerateEvent(
            IEventSymbol @event, Accessibility accessibility, DeclarationModifiers modifiers)
        {
            var generator = _document.GetRequiredLanguageService<SyntaxGenerator>();
            return CodeGenerationSymbolFactory.CreateEventSymbol(
                @event, accessibility: accessibility, modifiers: modifiers);
        }

        private bool ShouldGenerateAccessor(IMethodSymbol? method)
            => method != null && this.ClassType.FindImplementationForAbstractMember(method) == null;
    }
}
