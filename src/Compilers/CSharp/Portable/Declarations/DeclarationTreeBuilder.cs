// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class DeclarationTreeBuilder : CSharpSyntaxVisitor<SingleNamespaceOrTypeDeclaration>
    {
        private readonly SyntaxTree _syntaxTree;
        private readonly string _scriptClassName;
        private readonly bool _isSubmission;

        private DeclarationTreeBuilder(SyntaxTree syntaxTree, string scriptClassName, bool isSubmission)
        {
            _syntaxTree = syntaxTree;
            _scriptClassName = scriptClassName;
            _isSubmission = isSubmission;
        }

        public static RootSingleNamespaceDeclaration ForTree(
            SyntaxTree syntaxTree,
            string scriptClassName,
            bool isSubmission)
        {
            var builder = new DeclarationTreeBuilder(syntaxTree, scriptClassName, isSubmission);
            return (RootSingleNamespaceDeclaration)builder.Visit(syntaxTree.GetRoot());
        }

        private ImmutableArray<SingleNamespaceOrTypeDeclaration> VisitNamespaceChildren(
            CSharpSyntaxNode node,
            SyntaxList<MemberDeclarationSyntax> members,
            Syntax.InternalSyntax.SyntaxList<Syntax.InternalSyntax.MemberDeclarationSyntax> internalMembers)
        {
            Debug.Assert(node.Kind() == SyntaxKind.NamespaceDeclaration || (node.Kind() == SyntaxKind.CompilationUnit && _syntaxTree.Options.Kind == SourceCodeKind.Regular));

            if (members.Count == 0)
            {
                return ImmutableArray<SingleNamespaceOrTypeDeclaration>.Empty;
            }

            // We look for members that are not allowed in a namespace. 
            // If there are any we create an implicit class to wrap them.
            bool hasGlobalMembers = false;

            var childrenBuilder = ArrayBuilder<SingleNamespaceOrTypeDeclaration>.GetInstance();
            foreach (var member in members)
            {
                SingleNamespaceOrTypeDeclaration namespaceOrType = Visit(member);
                if (namespaceOrType != null)
                {
                    childrenBuilder.Add(namespaceOrType);
                }
                else
                {
                    hasGlobalMembers = hasGlobalMembers || member.Kind() != SyntaxKind.IncompleteMember;
                }
            }

            // wrap all members that are defined in a namespace or compilation unit into an implicit type:
            if (hasGlobalMembers)
            {
                //The implicit class is not static and has no extensions
                SingleTypeDeclaration.TypeDeclarationFlags declFlags = SingleTypeDeclaration.TypeDeclarationFlags.None;
                var memberNames = GetNonTypeMemberNames(internalMembers, ref declFlags);
                var container = _syntaxTree.GetReference(node);

                childrenBuilder.Add(CreateImplicitClass(memberNames, container, declFlags));
            }

            return childrenBuilder.ToImmutableAndFree();
        }

        private static SingleNamespaceOrTypeDeclaration CreateImplicitClass(ICollection<string> memberNames, SyntaxReference container, SingleTypeDeclaration.TypeDeclarationFlags declFlags)
        {
            return new SingleTypeDeclaration(
                kind: DeclarationKind.ImplicitClass,
                name: TypeSymbol.ImplicitTypeName,
                arity: 0,
                modifiers: DeclarationModifiers.Internal | DeclarationModifiers.Partial | DeclarationModifiers.Sealed,
                declFlags: declFlags,
                syntaxReference: container,
                nameLocation: new SourceLocation(container),
                memberNames: memberNames,
                children: ImmutableArray<SingleTypeDeclaration>.Empty);
        }

        /// <summary>
        /// Creates a root declaration that contains a Script class declaration (possibly in a namespace) and namespace declarations.
        /// Top-level declarations in script code are nested in Script class.
        /// </summary>
        private RootSingleNamespaceDeclaration CreateScriptRootDeclaration(CompilationUnitSyntax compilationUnit)
        {
            Debug.Assert(_syntaxTree.Options.Kind != SourceCodeKind.Regular);

            var members = compilationUnit.Members;
            var rootChildren = ArrayBuilder<SingleNamespaceOrTypeDeclaration>.GetInstance();
            var scriptChildren = ArrayBuilder<SingleTypeDeclaration>.GetInstance();

            foreach (var member in members)
            {
                var decl = Visit(member);
                if (decl != null)
                {
                    // Although namespaces are not allowed in script code process them 
                    // here as if they were to improve error reporting.
                    if (decl.Kind == DeclarationKind.Namespace)
                    {
                        rootChildren.Add(decl);
                    }
                    else
                    {
                        scriptChildren.Add((SingleTypeDeclaration)decl);
                    }
                }
            }

            //Script class is not static and contains no extensions.
            SingleTypeDeclaration.TypeDeclarationFlags declFlags = SingleTypeDeclaration.TypeDeclarationFlags.None;
            var membernames = GetNonTypeMemberNames(((Syntax.InternalSyntax.CompilationUnitSyntax)(compilationUnit.Green)).Members, ref declFlags);
            rootChildren.Add(
                CreateScriptClass(
                    compilationUnit,
                    scriptChildren.ToImmutableAndFree(),
                    membernames,
                    declFlags));

            return new RootSingleNamespaceDeclaration(
                hasUsings: compilationUnit.Usings.Any(),
                hasExternAliases: compilationUnit.Externs.Any(),
                treeNode: _syntaxTree.GetReference(compilationUnit),
                children: rootChildren.ToImmutableAndFree(),
                referenceDirectives: GetReferenceDirectives(compilationUnit),
                hasAssemblyAttributes: compilationUnit.AttributeLists.Any());
        }

        private static ImmutableArray<ReferenceDirective> GetReferenceDirectives(CompilationUnitSyntax compilationUnit)
        {
            IList<ReferenceDirectiveTriviaSyntax> directiveNodes = compilationUnit.GetReferenceDirectives(
                d => !d.File.ContainsDiagnostics && !string.IsNullOrEmpty(d.File.ValueText));
            if (directiveNodes.Count == 0)
            {
                return ImmutableArray<ReferenceDirective>.Empty;
            }

            var directives = ArrayBuilder<ReferenceDirective>.GetInstance(directiveNodes.Count);
            foreach (var directiveNode in directiveNodes)
            {
                directives.Add(new ReferenceDirective(directiveNode.File.ValueText, new SourceLocation(directiveNode)));
            }
            return directives.ToImmutableAndFree();
        }

        private SingleNamespaceOrTypeDeclaration CreateScriptClass(
            CompilationUnitSyntax parent,
            ImmutableArray<SingleTypeDeclaration> children,
            ICollection<string> memberNames,
            SingleTypeDeclaration.TypeDeclarationFlags declFlags)
        {
            Debug.Assert(parent.Kind() == SyntaxKind.CompilationUnit && _syntaxTree.Options.Kind != SourceCodeKind.Regular);

            // script type is represented by the parent node:
            var parentReference = _syntaxTree.GetReference(parent);
            var fullName = _scriptClassName.Split('.');

            // Note: The symbol representing the merged declarations uses parentReference to enumerate non-type members.
            SingleNamespaceOrTypeDeclaration decl = new SingleTypeDeclaration(
                kind: _isSubmission ? DeclarationKind.Submission : DeclarationKind.Script,
                name: fullName.Last(),
                arity: 0,
                modifiers: DeclarationModifiers.Internal | DeclarationModifiers.Partial | DeclarationModifiers.Sealed,
                declFlags: declFlags,
                syntaxReference: parentReference,
                nameLocation: new SourceLocation(parentReference),
                memberNames: memberNames,
                children: children);

            for (int i = fullName.Length - 2; i >= 0; i--)
            {
                decl = SingleNamespaceDeclaration.Create(
                    name: fullName[i],
                    hasUsings: false,
                    hasExternAliases: false,
                    syntaxReference: parentReference,
                    nameLocation: new SourceLocation(parentReference),
                    children: ImmutableArray.Create<SingleNamespaceOrTypeDeclaration>(decl));
            }

            return decl;
        }

        public override SingleNamespaceOrTypeDeclaration VisitCompilationUnit(CompilationUnitSyntax compilationUnit)
        {
            if (_syntaxTree.Options.Kind != SourceCodeKind.Regular)
            {
                return CreateScriptRootDeclaration(compilationUnit);
            }

            var children = VisitNamespaceChildren(compilationUnit, compilationUnit.Members, ((Syntax.InternalSyntax.CompilationUnitSyntax)(compilationUnit.Green)).Members);

            return new RootSingleNamespaceDeclaration(
                hasUsings: compilationUnit.Usings.Any(),
                hasExternAliases: compilationUnit.Externs.Any(),
                treeNode: _syntaxTree.GetReference(compilationUnit),
                children: children,
                referenceDirectives: ImmutableArray<ReferenceDirective>.Empty,
                hasAssemblyAttributes: compilationUnit.AttributeLists.Any());
        }

        public override SingleNamespaceOrTypeDeclaration VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            var children = VisitNamespaceChildren(node, node.Members, node.Green.Members);

            bool hasUsings = node.Usings.Any();
            bool hasExterns = node.Externs.Any();
            NameSyntax name = node.Name;
            CSharpSyntaxNode currentNode = node;
            while (name is QualifiedNameSyntax)
            {
                var dotted = name as QualifiedNameSyntax;
                var ns = SingleNamespaceDeclaration.Create(
                    name: dotted.Right.Identifier.ValueText,
                    hasUsings: hasUsings,
                    hasExternAliases: hasExterns,
                    syntaxReference: _syntaxTree.GetReference(currentNode),
                    nameLocation: new SourceLocation(dotted.Right),
                    children: children);

                var nsDeclaration = new[] { ns };
                children = nsDeclaration.AsImmutableOrNull<SingleNamespaceOrTypeDeclaration>();
                currentNode = name = dotted.Left;
                hasUsings = false;
                hasExterns = false;
            }

            // NOTE: *Something* has to happen for alias-qualified names.  It turns out that we
            // just grab the part after the colons (via GetUnqualifiedName, below).  This logic
            // must be kept in sync with NamespaceSymbol.GetNestedNamespace.
            return SingleNamespaceDeclaration.Create(
                name: name.GetUnqualifiedName().Identifier.ValueText,
                hasUsings: hasUsings,
                hasExternAliases: hasExterns,
                syntaxReference: _syntaxTree.GetReference(currentNode),
                nameLocation: new SourceLocation(name),
                children: children);
        }

        public override SingleNamespaceOrTypeDeclaration VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            return VisitTypeDeclaration(node, DeclarationKind.Class);
        }

        public override SingleNamespaceOrTypeDeclaration VisitStructDeclaration(StructDeclarationSyntax node)
        {
            return VisitTypeDeclaration(node, DeclarationKind.Struct);
        }

        public override SingleNamespaceOrTypeDeclaration VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            return VisitTypeDeclaration(node, DeclarationKind.Interface);
        }

        private SingleNamespaceOrTypeDeclaration VisitTypeDeclaration(TypeDeclarationSyntax node, DeclarationKind kind)
        {
            SingleTypeDeclaration.TypeDeclarationFlags declFlags = node.AttributeLists.Any() ?
                SingleTypeDeclaration.TypeDeclarationFlags.HasAnyAttributes :
                SingleTypeDeclaration.TypeDeclarationFlags.None;

            if (node.BaseList != null)
            {
                declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.HasBaseDeclarations;
            }

            var memberNames = GetNonTypeMemberNames(((Syntax.InternalSyntax.TypeDeclarationSyntax)(node.Green)).Members,
                                                    ref declFlags);

            return new SingleTypeDeclaration(
                kind: kind,
                name: node.Identifier.ValueText,
                modifiers: node.Modifiers.ToDeclarationModifiers(),
                arity: node.Arity,
                declFlags: declFlags,
                syntaxReference: _syntaxTree.GetReference(node),
                nameLocation: new SourceLocation(node.Identifier),
                memberNames: memberNames,
                children: VisitTypeChildren(node));
        }

        private ImmutableArray<SingleTypeDeclaration> VisitTypeChildren(TypeDeclarationSyntax node)
        {
            if (node.Members.Count == 0)
            {
                return ImmutableArray<SingleTypeDeclaration>.Empty;
            }

            var children = ArrayBuilder<SingleTypeDeclaration>.GetInstance();
            foreach (var member in node.Members)
            {
                var typeDecl = Visit(member) as SingleTypeDeclaration;
                if (typeDecl != null)
                {
                    children.Add(typeDecl);
                }
            }

            return children.ToImmutableAndFree();
        }

        public override SingleNamespaceOrTypeDeclaration VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            SingleTypeDeclaration.TypeDeclarationFlags declFlags = node.AttributeLists.Any() ?
                    SingleTypeDeclaration.TypeDeclarationFlags.HasAnyAttributes :
                    SingleTypeDeclaration.TypeDeclarationFlags.None;

            declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.HasAnyNontypeMembers;

            return new SingleTypeDeclaration(
                kind: DeclarationKind.Delegate,
                name: node.Identifier.ValueText,
                modifiers: node.Modifiers.ToDeclarationModifiers(),
                declFlags: declFlags,
                arity: node.Arity,
                syntaxReference: _syntaxTree.GetReference(node),
                nameLocation: new SourceLocation(node.Identifier),
                memberNames: SpecializedCollections.EmptyCollection<string>(),
                children: ImmutableArray<SingleTypeDeclaration>.Empty);
        }

        public override SingleNamespaceOrTypeDeclaration VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var members = node.Members;

            SingleTypeDeclaration.TypeDeclarationFlags declFlags = node.AttributeLists.Any() ?
                SingleTypeDeclaration.TypeDeclarationFlags.HasAnyAttributes :
                SingleTypeDeclaration.TypeDeclarationFlags.None;

            if (node.BaseList != null)
            {
                declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.HasBaseDeclarations;
            }

            string[] memberNames = GetEnumMemberNames(members, ref declFlags);

            return new SingleTypeDeclaration(
                kind: DeclarationKind.Enum,
                name: node.Identifier.ValueText,
                arity: 0,
                modifiers: node.Modifiers.ToDeclarationModifiers(),
                declFlags: declFlags,
                syntaxReference: _syntaxTree.GetReference(node),
                nameLocation: new SourceLocation(node.Identifier),
                memberNames: memberNames,
                children: ImmutableArray<SingleTypeDeclaration>.Empty);
        }

        private static string[] GetEnumMemberNames(SeparatedSyntaxList<EnumMemberDeclarationSyntax> members, ref SingleTypeDeclaration.TypeDeclarationFlags declFlags)
        {
            var cnt = members.Count;

            string[] memberNames = new string[cnt];
            if (cnt != 0)
            {
                declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.HasAnyNontypeMembers;
            }

            int i = 0;
            bool anyMemberHasAttributes = false;
            foreach (var member in members)
            {
                memberNames[i++] = member.Identifier.ValueText;
                if (!anyMemberHasAttributes && member.AttributeLists.Any())
                {
                    anyMemberHasAttributes = true;
                }
            }

            if (anyMemberHasAttributes)
            {
                declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.AnyMemberHasAttributes;
            }

            return memberNames;
        }

        private static string[] GetNonTypeMemberNames(Syntax.InternalSyntax.SyntaxList<Syntax.InternalSyntax.MemberDeclarationSyntax> members, ref SingleTypeDeclaration.TypeDeclarationFlags declFlags)
        {
            bool anyMethodHadExtensionSyntax = false;
            bool anyMemberHasAttributes = false;
            bool anyNonTypeMembers = false;

            var set = PooledHashSet<string>.GetInstance();

            foreach (var member in members)
            {
                AddNonTypeMemberNames(member, set, ref anyNonTypeMembers);

                // Check to see if any method contains a 'this' modifier on its first parameter.
                // This data is used to determine if a type needs to have its members materialized
                // as part of extension method lookup.
                if (!anyMethodHadExtensionSyntax && CheckMethodMemberForExtensionSyntax(member))
                {
                    anyMethodHadExtensionSyntax = true;
                }

                if (!anyMemberHasAttributes && CheckMemberForAttributes(member))
                {
                    anyMemberHasAttributes = true;
                }
            }

            if (anyMethodHadExtensionSyntax)
            {
                declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.AnyMemberHasExtensionMethodSyntax;
            }

            if (anyMemberHasAttributes)
            {
                declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.AnyMemberHasAttributes;
            }

            if (anyNonTypeMembers)
            {
                declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.HasAnyNontypeMembers;
            }

            // PERF: The member names collection tends to be long-lived. Use a string array since
            // that uses less memory than a HashSet<string>.
            string[] result;
            if (set.Count == 0)
            {
                result = SpecializedCollections.EmptyArray<string>();
            }
            else
            {
                result = new string[set.Count];
                set.CopyTo(result);
            }

            set.Free();
            return result;
        }

        private static bool CheckMethodMemberForExtensionSyntax(Syntax.InternalSyntax.CSharpSyntaxNode member)
        {
            if (member.Kind == SyntaxKind.MethodDeclaration)
            {
                var methodDecl = (Syntax.InternalSyntax.MethodDeclarationSyntax)member;

                var paramList = methodDecl.parameterList;
                if (paramList != null)
                {
                    var parameters = paramList.Parameters;

                    if (parameters.Count != 0)
                    {
                        var firstParameter = parameters[0];
                        foreach (var modifier in firstParameter.Modifiers)
                        {
                            if (modifier.Kind == SyntaxKind.ThisKeyword)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private static bool CheckMemberForAttributes(Syntax.InternalSyntax.CSharpSyntaxNode member)
        {
            switch (member.Kind)
            {
                case SyntaxKind.CompilationUnit:
                    return (((Syntax.InternalSyntax.CompilationUnitSyntax)member).AttributeLists).Any();

                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.EnumDeclaration:
                    return (((Syntax.InternalSyntax.BaseTypeDeclarationSyntax)member).AttributeLists).Any();

                case SyntaxKind.DelegateDeclaration:
                    return (((Syntax.InternalSyntax.DelegateDeclarationSyntax)member).AttributeLists).Any();

                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                    return (((Syntax.InternalSyntax.BaseFieldDeclarationSyntax)member).AttributeLists).Any();

                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                    return (((Syntax.InternalSyntax.BaseMethodDeclarationSyntax)member).AttributeLists).Any();

                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.IndexerDeclaration:
                    var baseProp = (Syntax.InternalSyntax.BasePropertyDeclarationSyntax)member;
                    bool hasAttributes = baseProp.AttributeLists.Any();

                    if (!hasAttributes && baseProp.AccessorList != null)
                    {
                        foreach (var accessor in baseProp.AccessorList.Accessors)
                        {
                            hasAttributes |= accessor.AttributeLists.Any();
                        }
                    }

                    return hasAttributes;
            }

            return false;
        }

        private static void AddNonTypeMemberNames(Syntax.InternalSyntax.CSharpSyntaxNode member, HashSet<string> set, ref bool anyNonTypeMembers)
        {
            switch (member.Kind)
            {
                case SyntaxKind.FieldDeclaration:
                    anyNonTypeMembers = true;
                    Syntax.InternalSyntax.SeparatedSyntaxList<Syntax.InternalSyntax.VariableDeclaratorSyntax> fieldDeclarators =
                        ((Syntax.InternalSyntax.FieldDeclarationSyntax)member).Declaration.Variables;
                    int numFieldDeclarators = fieldDeclarators.Count;
                    for (int i = 0; i < numFieldDeclarators; i++)
                    {
                        set.Add(fieldDeclarators[i].Identifier.ValueText);
                    }
                    break;

                case SyntaxKind.EventFieldDeclaration:
                    anyNonTypeMembers = true;
                    Syntax.InternalSyntax.SeparatedSyntaxList<Syntax.InternalSyntax.VariableDeclaratorSyntax> eventDeclarators =
                        ((Syntax.InternalSyntax.EventFieldDeclarationSyntax)member).Declaration.Variables;
                    int numEventDeclarators = eventDeclarators.Count;
                    for (int i = 0; i < numEventDeclarators; i++)
                    {
                        set.Add(eventDeclarators[i].Identifier.ValueText);
                    }
                    break;

                case SyntaxKind.MethodDeclaration:
                    anyNonTypeMembers = true;
                    // Member names are exposed via NamedTypeSymbol.MemberNames and are used primarily
                    // as an acid test to determine whether a more in-depth search of a type is worthwhile.
                    // We decided that it was reasonable to exclude explicit interface implementations
                    // from the list of member names.
                    var methodDecl = (Syntax.InternalSyntax.MethodDeclarationSyntax)member;
                    if (methodDecl.ExplicitInterfaceSpecifier == null)
                    {
                        set.Add(methodDecl.Identifier.ValueText);
                    }
                    break;

                case SyntaxKind.PropertyDeclaration:
                    anyNonTypeMembers = true;
                    // Handle in the same way as explicit method implementations
                    var propertyDecl = (Syntax.InternalSyntax.PropertyDeclarationSyntax)member;
                    if (propertyDecl.ExplicitInterfaceSpecifier == null)
                    {
                        set.Add(propertyDecl.Identifier.ValueText);
                    }
                    break;

                case SyntaxKind.EventDeclaration:
                    anyNonTypeMembers = true;
                    // Handle in the same way as explicit method implementations
                    var eventDecl = (Syntax.InternalSyntax.EventDeclarationSyntax)member;
                    if (eventDecl.ExplicitInterfaceSpecifier == null)
                    {
                        set.Add(eventDecl.Identifier.ValueText);
                    }
                    break;

                case SyntaxKind.ConstructorDeclaration:
                    anyNonTypeMembers = true;
                    set.Add(((Syntax.InternalSyntax.ConstructorDeclarationSyntax)member).Modifiers.Any(SyntaxKind.StaticKeyword)
                        ? WellKnownMemberNames.StaticConstructorName
                        : WellKnownMemberNames.InstanceConstructorName);
                    break;

                case SyntaxKind.DestructorDeclaration:
                    anyNonTypeMembers = true;
                    set.Add(WellKnownMemberNames.DestructorName);
                    break;

                case SyntaxKind.IndexerDeclaration:
                    anyNonTypeMembers = true;
                    set.Add(WellKnownMemberNames.Indexer);
                    break;

                case SyntaxKind.OperatorDeclaration:
                    anyNonTypeMembers = true;
                    var opDecl = (Syntax.InternalSyntax.OperatorDeclarationSyntax)member;
                    var name = OperatorFacts.OperatorNameFromDeclaration(opDecl);
                    set.Add(name);
                    break;

                case SyntaxKind.ConversionOperatorDeclaration:
                    anyNonTypeMembers = true;
                    set.Add(((Syntax.InternalSyntax.ConversionOperatorDeclarationSyntax)member).ImplicitOrExplicitKeyword.Kind == SyntaxKind.ImplicitKeyword
                        ? WellKnownMemberNames.ImplicitConversionName
                        : WellKnownMemberNames.ExplicitConversionName);
                    break;

                case SyntaxKind.GlobalStatement:
                    anyNonTypeMembers = true;
                    break;
            }
        }
    }
}
