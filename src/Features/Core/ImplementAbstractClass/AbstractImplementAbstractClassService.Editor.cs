// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ImplementAbstractClass
{
    internal partial class AbstractImplementAbstractClassService
    {
        private partial class Editor
        {
            private readonly Document document;
            private readonly SemanticModel model;
            private readonly State state;

            public Editor(
                Document document,
                SemanticModel model,
                State state)
            {
                this.document = document;
                this.model = model;
                this.state = state;
            }

            public async Task<Document> GetEditAsync(CancellationToken cancellationToken)
            {
                var unimplementedMembers = state.UnimplementedMembers;

                var memberDefinitions = GenerateMembers(
                    unimplementedMembers,
                    cancellationToken);

                var result = await CodeGenerator.AddMemberDeclarationsAsync(
                    document.Project.Solution,
                    state.ClassType,
                    memberDefinitions,
                    new CodeGenerationOptions(state.Location.GetLocation()),
                    cancellationToken)
                    .ConfigureAwait(false);

                return result;
            }

            private IList<ISymbol> GenerateMembers(
                IList<Tuple<INamedTypeSymbol, IList<ISymbol>>> unimplementedMembers,
                CancellationToken cancellationToken)
            {
                return
                    unimplementedMembers.SelectMany(t => t.Item2)
                                        .Select(m => GenerateMember(m, cancellationToken))
                                        .WhereNotNull()
                                        .ToList();
            }

            private ISymbol GenerateMember(
                ISymbol member,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Check if we need to add 'unsafe' to the signature we're generating.
                var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                var addUnsafe = member.IsUnsafe() && !syntaxFacts.IsUnsafeContext(this.state.Location);

                return GenerateMember(member, addUnsafe, cancellationToken);
            }

            private ISymbol GenerateMember(
                ISymbol member,
                bool addUnsafe,
                CancellationToken cancellationToken)
            {
                var modifiers = new DeclarationModifiers(isOverride: true, isUnsafe: addUnsafe);
                var accessibility = member.ComputeResultantAccessibility(state.ClassType);

                if (member.Kind == SymbolKind.Method)
                {
                    return GenerateMethod((IMethodSymbol)member, modifiers, accessibility, cancellationToken);
                }
                else if (member.Kind == SymbolKind.Property)
                {
                    return GenerateProperty((IPropertySymbol)member, modifiers, accessibility, cancellationToken);
                }
                else if (member.Kind == SymbolKind.Event)
                {
                    var @event = (IEventSymbol)member;
                    return CodeGenerationSymbolFactory.CreateEventSymbol(
                        @event,
                        accessibility: accessibility,
                        modifiers: modifiers);
                }

                return null;
            }

            private ISymbol GenerateMethod(
                IMethodSymbol method, DeclarationModifiers modifiers, Accessibility accessibility, CancellationToken cancellationToken)
            {
                var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
                var syntaxFactory = document.Project.LanguageServices.GetService<SyntaxGenerator>();
                var throwingBody = syntaxFactory.CreateThrowNotImplementedStatementBlock(
                    this.model.Compilation);

                method = method.EnsureNonConflictingNames(this.state.ClassType, syntaxFacts, cancellationToken);

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
                CancellationToken cancellationToken)
            {
                var syntaxFactory = document.Project.LanguageServices.GetService<SyntaxGenerator>();
                var throwingBody = syntaxFactory.CreateThrowNotImplementedStatementBlock(
                    this.model.Compilation);

                var getMethod = ShouldGenerateAccessor(property.GetMethod)
                    ? CodeGenerationSymbolFactory.CreateAccessorSymbol(
                        property.GetMethod,
                        attributes: null,
                        accessibility: property.GetMethod.ComputeResultantAccessibility(state.ClassType),
                        statements: throwingBody)
                    : null;

                var setMethod = ShouldGenerateAccessor(property.SetMethod)
                    ? CodeGenerationSymbolFactory.CreateAccessorSymbol(
                        property.SetMethod,
                        attributes: null,
                        accessibility: property.SetMethod.ComputeResultantAccessibility(state.ClassType),
                        statements: throwingBody)
                    : null;

                return CodeGenerationSymbolFactory.CreatePropertySymbol(
                    property,
                    accessibility: accessibility,
                    modifiers: modifiers,
                    getMethod: getMethod,
                    setMethod: setMethod);
            }

            private bool ShouldGenerateAccessor(IMethodSymbol method)
            {
                return
                    method != null &&
                    method.IsAccessibleWithin(state.ClassType) &&
                    state.ClassType.FindImplementationForAbstractMember(method) == null;
            }
        }
    }
}
