// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ImplementInterface;

using static ImplementHelpers;

internal abstract partial class AbstractImplementInterfaceService
{
    private partial class ImplementInterfaceGenerator : IImplementInterfaceGenerator
    {
        protected readonly bool Explicitly;
        protected readonly bool Abstractly;
        private readonly bool _onlyRemaining;
        protected readonly ISymbol? ThroughMember;
        protected readonly Document Document;
        protected readonly ImplementTypeGenerationOptions Options;
        protected readonly IImplementInterfaceInfo State;
        protected readonly AbstractImplementInterfaceService Service;

        internal ImplementInterfaceGenerator(
            AbstractImplementInterfaceService service,
            Document document,
            ImplementTypeGenerationOptions options,
            IImplementInterfaceInfo state,
            bool explicitly,
            bool abstractly,
            bool onlyRemaining,
            ISymbol? throughMember)
        {
            Service = service;
            Document = document;
            State = state;
            Options = options;
            Abstractly = abstractly;
            _onlyRemaining = onlyRemaining;
            Explicitly = explicitly;
            ThroughMember = throughMember;
        }

        public Task<Document> ImplementInterfaceAsync(CancellationToken cancellationToken)
            => GetUpdatedDocumentAsync(cancellationToken);

        public Task<Document> GetUpdatedDocumentAsync(CancellationToken cancellationToken)
        {
            var unimplementedMembers = Explicitly
                ? _onlyRemaining
                    ? State.MembersWithoutExplicitOrImplicitImplementation
                    : State.MembersWithoutExplicitImplementation
                : State.MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented;
            return GetUpdatedDocumentAsync(Document, unimplementedMembers, State.ClassOrStructType, State.ClassOrStructDecl, cancellationToken);
        }

        protected virtual Task<Document> GetUpdatedDocumentAsync(
            Document document,
            ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> unimplementedMembers,
            INamedTypeSymbol classOrStructType,
            SyntaxNode classOrStructDecl,
            CancellationToken cancellationToken)
        {
            return GetUpdatedDocumentAsync(
                document, unimplementedMembers, classOrStructType, classOrStructDecl,
                [], cancellationToken);
        }

        protected async Task<Document> GetUpdatedDocumentAsync(
            Document document,
            ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> unimplementedMembers,
            INamedTypeSymbol classOrStructType,
            SyntaxNode classOrStructDecl,
            ImmutableArray<ISymbol> extraMembers,
            CancellationToken cancellationToken)
        {
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            var isComImport = unimplementedMembers.Any(static t => t.type.IsComImport);

            var memberDefinitions = GenerateMembers(
                compilation, tree.Options, unimplementedMembers, Options.ImplementTypeOptions.PropertyGenerationBehavior);

            // Only group the members in the destination if the user wants that *and* 
            // it's not a ComImport interface.  Member ordering in ComImport interfaces 
            // matters, so we don't want to much with them.
            var groupMembers = !isComImport &&
                Options.ImplementTypeOptions.InsertionBehavior == ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind;

            return await CodeGenerator.AddMemberDeclarationsAsync(
                new CodeGenerationSolutionContext(
                    document.Project.Solution,
                    new CodeGenerationContext(
                        contextLocation: classOrStructDecl.GetLocation(),
                        autoInsertionLocation: groupMembers,
                        sortMembers: groupMembers)),
                classOrStructType,
                memberDefinitions.Concat(extraMembers),
                cancellationToken).ConfigureAwait(false);
        }

        private ImmutableArray<ISymbol> GenerateMembers(
            Compilation compilation,
            ParseOptions options,
            ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> unimplementedMembers,
            ImplementTypePropertyGenerationBehavior propertyGenerationBehavior)
        {
            // As we go along generating members we may end up with conflicts.  For example, say
            // you have "interface IGoo { string Bar { get; } }" and "interface IQuux { int Bar
            // { get; } }" and we need to implement both 'Bar' methods.  The second will have to
            // be explicitly implemented as it will conflict with the first.  So we need to keep
            // track of what we've actually implemented so that we can check further interface
            // members against both the actual type and that list.
            //
            // Similarly, if you have two interfaces with the same member, then we don't want to
            // implement that member twice.  
            //
            // Note: if we implement a method explicitly then we do *not* add it to this list.
            // That's because later members won't conflict with it even if they have the same
            // signature otherwise.  i.e. if we chose to implement IGoo.Bar explicitly, then we
            // could implement IQuux.Bar implicitly (and vice versa).
            using var _1 = ArrayBuilder<ISymbol>.GetInstance(out var implementedVisibleMembers);
            using var _2 = ArrayBuilder<ISymbol>.GetInstance(out var implementedMembers);

            foreach (var (_, unimplementedInterfaceMembers) in unimplementedMembers)
            {
                foreach (var unimplementedInterfaceMember in unimplementedInterfaceMembers)
                {
                    var members = GenerateMembers(
                        compilation, options,
                        unimplementedInterfaceMember, implementedVisibleMembers,
                        propertyGenerationBehavior);
                    foreach (var member in members)
                    {
                        if (member is null)
                            continue;

                        implementedMembers.Add(member);

                        if (!(member.ExplicitInterfaceImplementations().Any() && Service.HasHiddenExplicitImplementation))
                            implementedVisibleMembers.Add(member);
                    }
                }
            }

            return implementedMembers.ToImmutableAndClear();
        }

        private bool IsReservedName(string name)
        {
            return
                IdentifiersMatch(State.ClassOrStructType.Name, name) ||
                State.ClassOrStructType.TypeParameters.Any(static (t, arg) => arg.self.IdentifiersMatch(t.Name, arg.name), (self: this, name));
        }

        private string DetermineMemberName(ISymbol member, ArrayBuilder<ISymbol> implementedVisibleMembers)
        {
            if (HasConflictingMember(member, implementedVisibleMembers))
            {
                var memberNames = State.ClassOrStructType.GetAccessibleMembersInThisAndBaseTypes<ISymbol>(State.ClassOrStructType).Select(m => m.Name);

                return NameGenerator.GenerateUniqueName(
                    string.Format("{0}_{1}", member.ContainingType.Name, member.Name),
                    n => !memberNames.Contains(n) &&
                        !implementedVisibleMembers.Any(m => IdentifiersMatch(m.Name, n)) &&
                        !IsReservedName(n));
            }

            return member.Name;
        }

        private IEnumerable<ISymbol?> GenerateMembers(
            Compilation compilation,
            ParseOptions options,
            ISymbol member,
            ArrayBuilder<ISymbol> implementedVisibleMembers,
            ImplementTypePropertyGenerationBehavior propertyGenerationBehavior)
        {
            // First check if we already generate a member that matches the member we want to
            // generate.  This can happen in C# when you have interfaces that have the same
            // method, and you are implementing implicitly.  For example:
            //
            // interface IGoo { void Goo(); }
            //
            // interface IBar : IGoo { new void Goo(); }
            //
            // class C : IBar
            //
            // In this case we only want to generate 'Goo' once.
            if (HasMatchingMember(implementedVisibleMembers, member))
                return [];

            var memberName = DetermineMemberName(member, implementedVisibleMembers);

            // See if we need to generate an invisible member.  If we do, then reset the name
            // back to what then member wants it to be.
            var generateInvisibleMember = ShouldGenerateInvisibleMember(options, member, memberName);
            memberName = generateInvisibleMember ? member.Name : memberName;

            // The language doesn't allow static abstract implementations of interface methods. i.e,
            // Only interface member is declared abstract static, but implementation should be only static.
            var generateAbstractly = !member.IsStatic && !generateInvisibleMember && Abstractly;

            // Check if we need to add 'new' to the signature we're adding.  We only need to do this
            // if we're not generating something explicit and we have a naming conflict with
            // something in our base class hierarchy.
            var addNew = !generateInvisibleMember && HasNameConflict(member, memberName, State.ClassOrStructType.GetBaseTypes());

            // Check if we need to add 'unsafe' to the signature we're generating.
            var syntaxFacts = Document.GetRequiredLanguageService<ISyntaxFactsService>();
            var addUnsafe = member.RequiresUnsafeModifier() && !syntaxFacts.IsUnsafeContext(State.InterfaceNode);

            return GenerateMembers(
                compilation, member, memberName, generateInvisibleMember, generateAbstractly,
                addNew, addUnsafe, propertyGenerationBehavior);
        }

        private bool ShouldGenerateInvisibleMember(ParseOptions options, ISymbol member, string memberName)
        {
            if (Service.HasHiddenExplicitImplementation)
            {
                // User asked for an explicit (i.e. invisible) member.
                if (Explicitly)
                    return true;

                // Have to create an invisible member if we have constraints we can't express
                // with a visible member.
                if (HasUnexpressibleConstraint(options, member))
                    return true;

                // If we had a conflict with a member of the same name, then we have to generate
                // as an invisible member.
                if (member.Name != memberName)
                    return true;

                // If the member is less accessible than type, for which we are implementing it,
                // then only explicit implementation is valid.
                if (IsLessAccessibleThan(member, State.ClassOrStructType))
                    return true;
            }

            // Can't generate an invisible member if the language doesn't support it.
            return false;
        }

        private bool HasUnexpressibleConstraint(ParseOptions options, ISymbol member)
        {
            // interface IGoo<T> { void Bar<U>() where U : T; }
            //
            // class A : IGoo<int> { }
            //
            // In this case we cannot generate an implement method for Bar.  That's because we'd
            // need to say "where U : int" and that's disallowed by the language.  So we must
            // generate something explicit here.
            if (member is not IMethodSymbol method)
                return false;

            var allowDelegateAndEnumConstraints = this.Service.AllowDelegateAndEnumConstraints(options);
            return method.TypeParameters.Any(t => IsUnexpressibleTypeParameter(t, allowDelegateAndEnumConstraints));
        }

        private static bool IsUnexpressibleTypeParameter(
            ITypeParameterSymbol typeParameter,
            bool allowDelegateAndEnumConstraints)
        {
            var condition1 = typeParameter.ConstraintTypes.Count(t => t.TypeKind == TypeKind.Class) >= 2;
            var condition2 = typeParameter.ConstraintTypes.Any(static (ts, allowDelegateAndEnumConstraints) => ts.IsUnexpressibleTypeParameterConstraint(allowDelegateAndEnumConstraints), allowDelegateAndEnumConstraints);
            var condition3 = typeParameter.HasReferenceTypeConstraint && typeParameter.ConstraintTypes.Any(static ts => ts.IsReferenceType && ts.SpecialType != SpecialType.System_Object);

            return condition1 || condition2 || condition3;
        }

        private IEnumerable<ISymbol?> GenerateMembers(
            Compilation compilation,
            ISymbol member,
            string memberName,
            bool generateInvisibly,
            bool generateAbstractly,
            bool addNew,
            bool addUnsafe,
            ImplementTypePropertyGenerationBehavior propertyGenerationBehavior)
        {
            var factory = Document.GetRequiredLanguageService<SyntaxGenerator>();
            var modifiers = new DeclarationModifiers(isStatic: member.IsStatic, isAbstract: generateAbstractly, isNew: addNew, isUnsafe: addUnsafe);

            var useExplicitInterfaceSymbol = generateInvisibly || !Service.CanImplementImplicitly;
            var accessibility = member.Name == memberName || generateAbstractly
                ? Accessibility.Public
                : Accessibility.Private;

            if (member is IMethodSymbol method)
            {
                yield return GenerateMethod(compilation, method, accessibility, modifiers, generateAbstractly, useExplicitInterfaceSymbol, memberName);
            }
            else if (member is IPropertySymbol property)
            {
                foreach (var generated in GeneratePropertyMembers(compilation, property, accessibility, modifiers, generateAbstractly, useExplicitInterfaceSymbol, memberName, propertyGenerationBehavior))
                    yield return generated;
            }
            else if (member is IEventSymbol @event)
            {
                yield return GenerateEvent(compilation, memberName, generateInvisibly, factory, modifiers, useExplicitInterfaceSymbol, accessibility, @event);
            }
        }

        private ISymbol GenerateEvent(Compilation compilation, string memberName, bool generateInvisibly, SyntaxGenerator factory, DeclarationModifiers modifiers, bool useExplicitInterfaceSymbol, Accessibility accessibility, IEventSymbol @event)
        {
            var accessor = CodeGenerationSymbolFactory.CreateAccessorSymbol(
                attributes: default,
                accessibility: Accessibility.NotApplicable,
                statements: factory.CreateThrowNotImplementedStatementBlock(compilation));

            return CodeGenerationSymbolFactory.CreateEventSymbol(
                @event,
                accessibility: accessibility,
                modifiers: modifiers,
                explicitInterfaceImplementations: useExplicitInterfaceSymbol ? [@event] : default,
                name: memberName,
                addMethod: GetAddOrRemoveMethod(@event, generateInvisibly, accessor, memberName, factory.AddEventHandler),
                removeMethod: GetAddOrRemoveMethod(@event, generateInvisibly, accessor, memberName, factory.RemoveEventHandler));
        }

        private IMethodSymbol? GetAddOrRemoveMethod(
            IEventSymbol @event, bool generateInvisibly, IMethodSymbol accessor, string memberName,
            Func<SyntaxNode, SyntaxNode, SyntaxNode> createAddOrRemoveHandler)
        {
            if (ThroughMember != null)
            {
                var generator = Document.GetRequiredLanguageService<SyntaxGenerator>();
                var throughExpression = generator.CreateDelegateThroughExpression(@event, ThroughMember);
                var statement = generator.ExpressionStatement(createAddOrRemoveHandler(
                    generator.MemberAccessExpression(throughExpression, memberName), generator.IdentifierName("value")));

                return CodeGenerationSymbolFactory.CreateAccessorSymbol(
                       attributes: default,
                       accessibility: Accessibility.NotApplicable,
                       statements: [statement]);
            }

            return generateInvisibly ? accessor : null;
        }

        private bool HasNameConflict(
            ISymbol member,
            string memberName,
            IEnumerable<INamedTypeSymbol> baseTypes)
        {
            // There's a naming conflict if any member in the base types chain is accessible to
            // us, has our name.  Note: a simple name won't conflict with a generic name (and
            // vice versa).  A method only conflicts with another method if they have the same
            // parameter signature (return type is irrelevant). 
            return
                baseTypes.Any(ts => ts.GetMembers(memberName)
                                      .Where(m => m.IsAccessibleWithin(State.ClassOrStructType))
                                      .Any(m => HasNameConflict(member, m)));
        }

        private static bool HasNameConflict(ISymbol member, ISymbol baseMember)
        {
            if (member is IMethodSymbol method1 && baseMember is IMethodSymbol method2)
            {
                // A method only conflicts with another method if they have the same parameter
                // signature (return type is irrelevant). 
                return method1.MethodKind == MethodKind.Ordinary &&
                       method2.MethodKind == MethodKind.Ordinary &&
                       method1.TypeParameters.Length == method2.TypeParameters.Length &&
                       method1.Parameters.SequenceEqual(method2.Parameters, SymbolEquivalenceComparer.Instance.ParameterEquivalenceComparer);
            }

            // Any non method members with the same name simple name conflict.
            return true;
        }

        private bool IdentifiersMatch(string identifier1, string identifier2)
            => IsCaseSensitive
                ? identifier1 == identifier2
                : StringComparer.OrdinalIgnoreCase.Equals(identifier1, identifier2);

        private bool IsCaseSensitive => Document.GetRequiredLanguageService<ISyntaxFactsService>().IsCaseSensitive;

        private bool HasMatchingMember(ArrayBuilder<ISymbol> implementedVisibleMembers, ISymbol member)
        {
            // If this is a language that doesn't support implicit implementation then no
            // implemented members will ever match.  For example, if you have:
            //
            // Interface IGoo : sub Goo() : End Interface
            //
            // Interface IBar : Inherits IGoo : Shadows Sub Goo() : End Interface
            //
            // Class C : Implements IBar
            //
            // We'll first end up generating:
            //
            // Public Sub Goo() Implements IGoo.Goo
            //
            // However, that same method won't be viable for IBar.Goo (unlike C#) because it
            // explicitly specifies its interface).
            if (!Service.CanImplementImplicitly)
            {
                return false;
            }

            return implementedVisibleMembers.Any(m => MembersMatch(m, member));
        }

        private bool MembersMatch(ISymbol member1, ISymbol member2)
        {
            if (member1.Kind != member2.Kind)
                return false;

            if (member1.DeclaredAccessibility != member2.DeclaredAccessibility ||
                member1.IsStatic != member2.IsStatic)
            {
                return false;
            }

            if (member1.ExplicitInterfaceImplementations().Any() || member2.ExplicitInterfaceImplementations().Any())
                return false;

            return SignatureComparer.Instance.HaveSameSignatureAndConstraintsAndReturnTypeAndAccessors(
                member1, member2, IsCaseSensitive);
        }
    }
}
