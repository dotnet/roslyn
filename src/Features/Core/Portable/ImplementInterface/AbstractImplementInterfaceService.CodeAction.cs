// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ImplementInterface
{
    internal abstract partial class AbstractImplementInterfaceService
    {
        internal partial class ImplementInterfaceCodeAction : CodeAction
        {
            protected readonly bool Explicitly;
            protected readonly bool Abstractly;
            protected readonly ISymbol ThroughMember;
            protected readonly Document Document;
            protected readonly State State;
            protected readonly AbstractImplementInterfaceService Service;
            private readonly string _equivalenceKey;

            internal ImplementInterfaceCodeAction(
                AbstractImplementInterfaceService service,
                Document document,
                State state,
                bool explicitly,
                bool abstractly,
                ISymbol throughMember)
            {
                this.Service = service;
                this.Document = document;
                this.State = state;
                this.Abstractly = abstractly;
                this.Explicitly = explicitly;
                this.ThroughMember = throughMember;
                _equivalenceKey = ComputeEquivalenceKey(state, explicitly, abstractly, throughMember, this.GetType().FullName);
            }

            public static ImplementInterfaceCodeAction CreateImplementAbstractlyCodeAction(
                AbstractImplementInterfaceService service,
                Document document,
                State state)
            {
                return new ImplementInterfaceCodeAction(service, document, state, explicitly: false, abstractly: true, throughMember: null);
            }

            public static ImplementInterfaceCodeAction CreateImplementCodeAction(
                AbstractImplementInterfaceService service,
                Document document,
                State state)
            {
                return new ImplementInterfaceCodeAction(service, document, state, explicitly: false, abstractly: false, throughMember: null);
            }

            public static ImplementInterfaceCodeAction CreateImplementExplicitlyCodeAction(
                AbstractImplementInterfaceService service,
                Document document,
                State state)
            {
                return new ImplementInterfaceCodeAction(service, document, state, explicitly: true, abstractly: false, throughMember: null);
            }

            public static ImplementInterfaceCodeAction CreateImplementThroughMemberCodeAction(
                AbstractImplementInterfaceService service,
                Document document,
                State state,
                ISymbol throughMember)
            {
                return new ImplementInterfaceCodeAction(service, document, state, explicitly: false, abstractly: false, throughMember: throughMember);
            }

            public override string Title
            {
                get
                {
                    if (Explicitly)
                    {
                        return FeaturesResources.ImplementInterfaceExplicitly;
                    }
                    else if (Abstractly)
                    {
                        return FeaturesResources.ImplementInterfaceAbstractly;
                    }
                    else if (ThroughMember != null)
                    {
                        return string.Format(FeaturesResources.ImplementInterfaceThrough, GetDescription(ThroughMember));
                    }
                    else
                    {
                        return FeaturesResources.ImplementInterface;
                    }
                }
            }

            private static string ComputeEquivalenceKey(
                State state,
                bool explicitly,
                bool abstractly,
                ISymbol throughMember,
                string codeActionTypeName)
            {
                var interfaceType = state.InterfaceTypes.First();
                var typeName = interfaceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var assemblyName = interfaceType.ContainingAssembly.Name;

                return GetCodeActionEquivalenceKey(assemblyName, typeName, explicitly, abstractly, throughMember, codeActionTypeName);
            }

            // internal for testing purposes.
            internal static string GetCodeActionEquivalenceKey(
                string interfaceTypeAssemblyName,
                string interfaceTypeFullyQualifiedName,
                bool explicitly,
                bool abstractly,
                ISymbol throughMember,
                string codeActionTypeName)
            {
                if (throughMember != null)
                {
                    return null;
                }

                return explicitly.ToString() + ";" +
                    abstractly.ToString() + ";" +
                    interfaceTypeAssemblyName + ";" +
                    interfaceTypeFullyQualifiedName + ";" +
                    codeActionTypeName;
            }

            public override string EquivalenceKey
            {
                get
                {
                    return _equivalenceKey;
                }
            }

            private static string GetDescription(ISymbol throughMember)
            {
                return throughMember.TypeSwitch(
                    (IFieldSymbol field) => field.Name,
                    (IPropertySymbol property) => property.Name,
                    _ => Contract.FailWithReturn<string>());
            }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return GetUpdatedDocumentAsync(cancellationToken);
            }

            public Task<Document> GetUpdatedDocumentAsync(CancellationToken cancellationToken)
            {
                var unimplementedMembers = Explicitly ? State.UnimplementedExplicitMembers : State.UnimplementedMembers;
                return GetUpdatedDocumentAsync(Document, unimplementedMembers, State.ClassOrStructType, State.ClassOrStructDecl, cancellationToken);
            }

            public virtual async Task<Document> GetUpdatedDocumentAsync(
                    Document document,
                    IList<Tuple<INamedTypeSymbol, IList<ISymbol>>> unimplementedMembers,
                    INamedTypeSymbol classOrStructType,
                    SyntaxNode classOrStructDecl,
                    CancellationToken cancellationToken)
            {
                var result = document;
                var compilation = await result.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                var memberDefinitions = GenerateMembers(
                    compilation,
                    unimplementedMembers,
                    cancellationToken);

                result = await CodeGenerator.AddMemberDeclarationsAsync(
                    result.Project.Solution, classOrStructType, memberDefinitions,
                    new CodeGenerationOptions(contextLocation: classOrStructDecl.GetLocation()),
                    cancellationToken).ConfigureAwait(false);

                return result;
            }

            private IList<ISymbol> GenerateMembers(
                Compilation compilation,
                IList<Tuple<INamedTypeSymbol, IList<ISymbol>>> unimplementedMembers,
                CancellationToken cancellationToken)
            {
                // As we go along generating members we may end up with conflicts.  For example, say
                // you have "interface IFoo { string Bar { get; } }" and "interface IQuux { int Bar
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
                // signature otherwise.  i.e. if we chose to implement IFoo.Bar explicitly, then we
                // could implement IQuux.Bar implicitly (and vice versa).
                var implementedVisibleMembers = new List<ISymbol>();
                var implementedMembers = new List<ISymbol>();

                foreach (var tuple in unimplementedMembers)
                {
                    var interfaceType = tuple.Item1;
                    var unimplementedInterfaceMembers = tuple.Item2;

                    foreach (var unimplementedInterfaceMember in unimplementedInterfaceMembers)
                    {
                        var member = GenerateMember(compilation, unimplementedInterfaceMember, implementedVisibleMembers, cancellationToken);
                        if (member != null)
                        {
                            implementedMembers.Add(member);

                            if (!(member.ExplicitInterfaceImplementations().Any() && Service.HasHiddenExplicitImplementation))
                            {
                                implementedVisibleMembers.Add(member);
                            }
                        }
                    }
                }

                return implementedMembers;
            }

            private bool IsReservedName(string name)
            {
                return
                    IdentifiersMatch(State.ClassOrStructType.Name, name) ||
                    State.ClassOrStructType.TypeParameters.Any(t => IdentifiersMatch(t.Name, name));
            }

            private string DetermineMemberName(ISymbol member, List<ISymbol> implementedVisibleMembers)
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

            private ISymbol GenerateMember(
                Compilation compilation,
                ISymbol member,
                List<ISymbol> implementedVisibleMembers,
                CancellationToken cancellationToken)
            {
                // First check if we already generate a member that matches the member we want to
                // generate.  This can happen in C# when you have interfaces that have the same
                // method, and you are implementing implicitly.  For example:
                //
                // interface IFoo { void Foo(); }
                //
                // interface IBar : IFoo { new void Foo(); }
                //
                // class C : IBar
                //
                // In this case we only want to generate 'Foo' once.
                if (HasMatchingMember(implementedVisibleMembers, member))
                {
                    return null;
                }

                var memberName = DetermineMemberName(member, implementedVisibleMembers);

                // See if we need to generate an invisible member.  If we do, then reset the name
                // back to what then member wants it to be.
                var generateInvisibleMember = GenerateInvisibleMember(member, memberName);
                memberName = generateInvisibleMember ? member.Name : memberName;

                var generateAbstractly = !generateInvisibleMember && Abstractly;

                // Check if we need to add 'new' to the signature we're adding.  We only need to do this
                // if we're not generating something explicit and we have a naming conflict with
                // something in our base class hierarchy.
                var addNew = !generateInvisibleMember && HasNameConflict(member, memberName, State.ClassOrStructType.GetBaseTypes());

                // Check if we need to add 'unsafe' to the signature we're generating.
                var syntaxFacts = Document.GetLanguageService<ISyntaxFactsService>();
                var addUnsafe = member.IsUnsafe() && !syntaxFacts.IsUnsafeContext(State.Location);

                return GenerateMember(compilation, member, memberName, generateInvisibleMember, generateAbstractly, addNew, addUnsafe, cancellationToken);
            }

            private bool GenerateInvisibleMember(ISymbol member, string memberName)
            {
                if (Service.HasHiddenExplicitImplementation)
                {
                    // User asked for an explicit (i.e. invisible) member.
                    if (Explicitly)
                    {
                        return true;
                    }

                    // Have to create an invisible member if we have constraints we can't express
                    // with a visible member.
                    if (HasUnexpressibleConstraint(member))
                    {
                        return true;
                    }

                    // If we had a conflict with a member of the same name, then we have to generate
                    // as an invisible member.
                    if (member.Name != memberName)
                    {
                        return true;
                    }
                }

                // Can't generate an invisible member if the language doesn't support it.
                return false;
            }

            private bool HasUnexpressibleConstraint(ISymbol member)
            {
                // interface IFoo<T> { void Bar<U>() where U : T; }
                //
                // class A : IFoo<int> { }
                //
                // In this case we cannot generate an implement method for Bar.  That's because we'd
                // need to say "where U : int" and that's disallowed by the language.  So we must
                // generate something explicit here.
                if (member.Kind != SymbolKind.Method)
                {
                    return false;
                }

                var method = member as IMethodSymbol;

                return method.TypeParameters.Any(IsUnexpressibleTypeParameter);
            }

            private static bool IsUnexpressibleTypeParameter(ITypeParameterSymbol typeParameter)
            {
                var condition1 = typeParameter.ConstraintTypes.Count(t => t.TypeKind == TypeKind.Class) >= 2;
                var condition2 = typeParameter.ConstraintTypes.Any(ts => ts.IsUnexpressibleTypeParameterConstraint());
                var condition3 = typeParameter.HasReferenceTypeConstraint && typeParameter.ConstraintTypes.Any(ts => ts.IsReferenceType && ts.SpecialType != SpecialType.System_Object);

                return condition1 || condition2 || condition3;
            }

            private ISymbol GenerateMember(
                Compilation compilation,
                ISymbol member,
                string memberName,
                bool generateInvisibly,
                bool generateAbstractly,
                bool addNew,
                bool addUnsafe,
                CancellationToken cancellationToken)
            {
                var factory = this.Document.GetLanguageService<SyntaxGenerator>();
                var modifiers = new DeclarationModifiers(isAbstract: generateAbstractly, isNew: addNew, isUnsafe: addUnsafe);

                var useExplicitInterfaceSymbol = generateInvisibly || !Service.CanImplementImplicitly;
                var accessibility = member.Name == memberName ? Accessibility.Public : Accessibility.Private;

                if (member.Kind == SymbolKind.Method)
                {
                    var method = (IMethodSymbol)member;

                    return GenerateMethod(compilation, method, accessibility, modifiers, generateAbstractly, useExplicitInterfaceSymbol, memberName, cancellationToken);
                }
                else if (member.Kind == SymbolKind.Property)
                {
                    var property = (IPropertySymbol)member;

                    return GenerateProperty(compilation, property, accessibility, modifiers, generateAbstractly, useExplicitInterfaceSymbol, memberName, cancellationToken);
                }
                else if (member.Kind == SymbolKind.Event)
                {
                    var @event = (IEventSymbol)member;

                    var accessor = CodeGenerationSymbolFactory.CreateAccessorSymbol(
                        attributes: null,
                        accessibility: Accessibility.NotApplicable,
                        statements: factory.CreateThrowNotImplementedStatementBlock(compilation));

                    return CodeGenerationSymbolFactory.CreateEventSymbol(
                        @event,
                        accessibility: accessibility,
                        modifiers: modifiers,
                        explicitInterfaceSymbol: useExplicitInterfaceSymbol ? @event : null,
                        name: memberName,
                        addMethod: generateInvisibly ? accessor : null,
                        removeMethod: generateInvisibly ? accessor : null);
                }

                return null;
            }

            private SyntaxNode CreateThroughExpression(SyntaxGenerator factory)
            {
                var through = ThroughMember.IsStatic
                        ? GenerateName(factory, State.ClassOrStructType.IsGenericType)
                        : factory.ThisExpression();

                through = factory.MemberAccessExpression(
                    through, factory.IdentifierName(ThroughMember.Name));

                var throughMemberType = ThroughMember.GetMemberType();
                if ((State.InterfaceTypes != null) && (throughMemberType != null))
                {
                    // In the case of 'implement interface through field / property' , we need to know what
                    // interface we are implementing so that we can insert casts to this interface on every
                    // usage of the field in the generated code. Without these casts we would end up generating
                    // code that fails compilation in certain situations.
                    // 
                    // For example consider the following code.
                    //      class C : IReadOnlyList<int> { int[] field; }
                    // When applying the 'implement interface through field' code fix in the above example,
                    // we need to generate the following code to implement the Count property on IReadOnlyList<int>
                    //      class C : IReadOnlyList<int> { int[] field; int Count { get { ((IReadOnlyList<int>)field).Count; } ...}
                    // as opposed to the following code which will fail to compile (because the array field
                    // doesn't have a property named .Count) -
                    //      class C : IReadOnlyList<int> { int[] field; int Count { get { field.Count; } ...}
                    //
                    // The 'InterfaceTypes' property on the state object always contains only one item
                    // in the case of C# i.e. it will contain exactly the interface we are trying to implement.
                    // This is also the case most of the time in the case of VB, except in certain error conditions
                    // (recursive / circular cases) where the span of the squiggle for the corresponding 
                    // diagnostic (BC30149) changes and 'InterfaceTypes' ends up including all interfaces
                    // in the Implements clause. For the purposes of inserting the above cast, we ignore the
                    // uncommon case and optimize for the common one - in other words, we only apply the cast
                    // in cases where we can unambiguously figure out which interface we are trying to implement.
                    var interfaceBeingImplemented = State.InterfaceTypes.SingleOrDefault();
                    if ((interfaceBeingImplemented != null) && (!throughMemberType.Equals(interfaceBeingImplemented)))
                    {
                        through = factory.CastExpression(interfaceBeingImplemented,
                            through.WithAdditionalAnnotations(Simplifier.Annotation));

                        var facts = this.Document.GetLanguageService<ISyntaxFactsService>();
                        through = facts.Parenthesize(through);
                    }
                }

                return through.WithAdditionalAnnotations(Simplifier.Annotation);
            }

            private SyntaxNode GenerateName(SyntaxGenerator factory, bool isGenericType)
            {
                return isGenericType
                    ? factory.GenericName(State.ClassOrStructType.Name, State.ClassOrStructType.TypeArguments)
                    : factory.IdentifierName(State.ClassOrStructType.Name);
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
                                          .Any(m => HasNameConflict(member, memberName, m)));
            }

            private static bool HasNameConflict(
                ISymbol member,
                string memberName,
                ISymbol baseMember)
            {
                Contract.Requires(memberName == baseMember.Name);

                if (member.Kind == SymbolKind.Method && baseMember.Kind == SymbolKind.Method)
                {
                    // A method only conflicts with another method if they have the same parameter
                    // signature (return type is irrelevant). 
                    var method1 = (IMethodSymbol)member;
                    var method2 = (IMethodSymbol)baseMember;

                    if (method1.MethodKind == MethodKind.Ordinary &&
                        method2.MethodKind == MethodKind.Ordinary &&
                        method1.TypeParameters.Length == method2.TypeParameters.Length)
                    {
                        return method1.Parameters.Select(p => p.Type)
                                                 .SequenceEqual(method2.Parameters.Select(p => p.Type));
                    }
                }

                // Any non method members with the same name simple name conflict.
                return true;
            }

            private bool IdentifiersMatch(string identifier1, string identifier2)
            {
                return this.IsCaseSensitive
                    ? identifier1 == identifier2
                    : StringComparer.OrdinalIgnoreCase.Equals(identifier1, identifier2);
            }

            private bool IsCaseSensitive
            {
                get
                {
                    return this.Document.GetLanguageService<ISyntaxFactsService>().IsCaseSensitive;
                }
            }

            private bool HasMatchingMember(List<ISymbol> implementedVisibleMembers, ISymbol member)
            {
                // If this is a language that doesn't support implicit implementation then no
                // implemented members will ever match.  For example, if you have:
                //
                // Interface IFoo : sub Foo() : End Interface
                //
                // Interface IBar : Inherits IFoo : Shadows Sub Foo() : End Interface
                //
                // Class C : Implements IBar
                //
                // We'll first end up generating:
                //
                // Public Sub Foo() Implements IFoo.Foo
                //
                // However, that same method won't be viable for IBar.Foo (unlike C#) because it
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
                {
                    return false;
                }

                if (member1.DeclaredAccessibility != member2.DeclaredAccessibility ||
                    member1.IsStatic != member2.IsStatic)
                {
                    return false;
                }

                if (member1.ExplicitInterfaceImplementations().Any() || member2.ExplicitInterfaceImplementations().Any())
                {
                    return false;
                }

                return SignatureComparer.Instance.HaveSameSignatureAndConstraintsAndReturnTypeAndAccessors(
                    member1, member2, this.IsCaseSensitive);
            }
        }
    }
}
