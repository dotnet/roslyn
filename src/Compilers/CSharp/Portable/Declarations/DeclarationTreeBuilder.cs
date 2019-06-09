// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using CoreInternalSyntax = Microsoft.CodeAnalysis.Syntax.InternalSyntax;

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
            CoreInternalSyntax.SyntaxList<Syntax.InternalSyntax.MemberDeclarationSyntax> internalMembers)
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

        private static SingleNamespaceOrTypeDeclaration CreateImplicitClass(ImmutableHashSet<string> memberNames, SyntaxReference container, SingleTypeDeclaration.TypeDeclarationFlags declFlags)
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
                children: ImmutableArray<SingleTypeDeclaration>.Empty,
                diagnostics: ImmutableArray<Diagnostic>.Empty);
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
            ImmutableHashSet<string> memberNames,
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
                children: children,
                diagnostics: ImmutableArray<Diagnostic>.Empty);

            for (int i = fullName.Length - 2; i >= 0; i--)
            {
                decl = SingleNamespaceDeclaration.Create(
                    name: fullName[i],
                    hasUsings: false,
                    hasExternAliases: false,
                    syntaxReference: parentReference,
                    nameLocation: new SourceLocation(parentReference),
                    children: ImmutableArray.Create(decl),
                    diagnostics: ImmutableArray<Diagnostic>.Empty);
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
            QualifiedNameSyntax dotted;
            while ((dotted = name as QualifiedNameSyntax) != null)
            {
                var ns = SingleNamespaceDeclaration.Create(
                    name: dotted.Right.Identifier.ValueText,
                    hasUsings: hasUsings,
                    hasExternAliases: hasExterns,
                    syntaxReference: _syntaxTree.GetReference(currentNode),
                    nameLocation: new SourceLocation(dotted.Right),
                    children: children,
                    diagnostics: ImmutableArray<Diagnostic>.Empty);

                var nsDeclaration = new[] { ns };
                children = nsDeclaration.AsImmutableOrNull<SingleNamespaceOrTypeDeclaration>();
                currentNode = name = dotted.Left;
                hasUsings = false;
                hasExterns = false;
            }

            var diagnostics = DiagnosticBag.GetInstance();
            if (ContainsGeneric(node.Name))
            {
                // We're not allowed to have generics.
                diagnostics.Add(ErrorCode.ERR_UnexpectedGenericName, node.Name.GetLocation());
            }

            if (ContainsAlias(node.Name))
            {
                diagnostics.Add(ErrorCode.ERR_UnexpectedAliasedName, node.Name.GetLocation());
            }

            if (node.AttributeLists.Count > 0)
            {
                diagnostics.Add(ErrorCode.ERR_BadModifiersOnNamespace, node.AttributeLists[0].GetLocation());
            }

            if (node.Modifiers.Count > 0)
            {
                diagnostics.Add(ErrorCode.ERR_BadModifiersOnNamespace, node.Modifiers[0].GetLocation());
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
                children: children,
                diagnostics: diagnostics.ToReadOnlyAndFree());
        }

        private static bool ContainsAlias(NameSyntax name)
        {
            switch (name.Kind())
            {
                case SyntaxKind.GenericName:
                    return false;
                case SyntaxKind.AliasQualifiedName:
                    return true;
                case SyntaxKind.QualifiedName:
                    var qualifiedName = (QualifiedNameSyntax)name;
                    return ContainsAlias(qualifiedName.Left);
            }

            return false;
        }

        private static bool ContainsGeneric(NameSyntax name)
        {
            switch (name.Kind())
            {
                case SyntaxKind.GenericName:
                    return true;
                case SyntaxKind.AliasQualifiedName:
                    return ContainsGeneric(((AliasQualifiedNameSyntax)name).Name);
                case SyntaxKind.QualifiedName:
                    var qualifiedName = (QualifiedNameSyntax)name;
                    return ContainsGeneric(qualifiedName.Left) || ContainsGeneric(qualifiedName.Right);
            }

            return false;
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

            var diagnostics = DiagnosticBag.GetInstance();
            if (node.Arity == 0)
            {
                Symbol.ReportErrorIfHasConstraints(node.ConstraintClauses, diagnostics);
            }

            var memberNames = GetNonTypeMemberNames(((Syntax.InternalSyntax.TypeDeclarationSyntax)(node.Green)).Members,
                                                    ref declFlags);

            var modifiers = node.Modifiers.ToDeclarationModifiers(diagnostics: diagnostics);

            return new SingleTypeDeclaration(
                kind: kind,
                name: node.Identifier.ValueText,
                modifiers: modifiers,
                arity: node.Arity,
                declFlags: declFlags,
                syntaxReference: _syntaxTree.GetReference(node),
                nameLocation: new SourceLocation(node.Identifier),
                memberNames: memberNames,
                children: VisitTypeChildren(node),
                diagnostics: diagnostics.ToReadOnlyAndFree());
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
            var declFlags = node.AttributeLists.Any()
                ? SingleTypeDeclaration.TypeDeclarationFlags.HasAnyAttributes
                : SingleTypeDeclaration.TypeDeclarationFlags.None;

            var diagnostics = DiagnosticBag.GetInstance();
            if (node.Arity == 0)
            {
                Symbol.ReportErrorIfHasConstraints(node.ConstraintClauses, diagnostics);
            }

            declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.HasAnyNontypeMembers;

            var modifiers = node.Modifiers.ToDeclarationModifiers(diagnostics: diagnostics);

            return new SingleTypeDeclaration(
                kind: DeclarationKind.Delegate,
                name: node.Identifier.ValueText,
                modifiers: modifiers,
                declFlags: declFlags,
                arity: node.Arity,
                syntaxReference: _syntaxTree.GetReference(node),
                nameLocation: new SourceLocation(node.Identifier),
                memberNames: ImmutableHashSet<string>.Empty,
                children: ImmutableArray<SingleTypeDeclaration>.Empty,
                diagnostics: diagnostics.ToReadOnlyAndFree());
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

            ImmutableHashSet<string> memberNames = GetEnumMemberNames(members, ref declFlags);

            var diagnostics = DiagnosticBag.GetInstance();
            var modifiers = node.Modifiers.ToDeclarationModifiers(diagnostics: diagnostics);

            return new SingleTypeDeclaration(
                kind: DeclarationKind.Enum,
                name: node.Identifier.ValueText,
                arity: 0,
                modifiers: modifiers,
                declFlags: declFlags,
                syntaxReference: _syntaxTree.GetReference(node),
                nameLocation: new SourceLocation(node.Identifier),
                memberNames: memberNames,
                children: ImmutableArray<SingleTypeDeclaration>.Empty,
                diagnostics: diagnostics.ToReadOnlyAndFree());
        }

        private static readonly ObjectPool<ImmutableHashSet<string>.Builder> s_memberNameBuilderPool =
            new ObjectPool<ImmutableHashSet<string>.Builder>(() => ImmutableHashSet.CreateBuilder<string>());

        private static ImmutableHashSet<string> ToImmutableAndFree(ImmutableHashSet<string>.Builder builder)
        {
            var result = builder.ToImmutable();
            builder.Clear();
            s_memberNameBuilderPool.Free(builder);
            return result;
        }

        private static ImmutableHashSet<string> GetEnumMemberNames(SeparatedSyntaxList<EnumMemberDeclarationSyntax> members, ref SingleTypeDeclaration.TypeDeclarationFlags declFlags)
        {
            var cnt = members.Count;

            var memberNamesBuilder = s_memberNameBuilderPool.Allocate();
            if (cnt != 0)
            {
                declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.HasAnyNontypeMembers;
            }

            bool anyMemberHasAttributes = false;
            foreach (var member in members)
            {
                memberNamesBuilder.Add(member.Identifier.ValueText);
                if (!anyMemberHasAttributes && member.AttributeLists.Any())
                {
                    anyMemberHasAttributes = true;
                }
            }

            if (anyMemberHasAttributes)
            {
                declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.AnyMemberHasAttributes;
            }

            return ToImmutableAndFree(memberNamesBuilder);
        }

        private static ImmutableHashSet<string> GetNonTypeMemberNames(
            CoreInternalSyntax.SyntaxList<Syntax.InternalSyntax.MemberDeclarationSyntax> members, ref SingleTypeDeclaration.TypeDeclarationFlags declFlags)
        {
            bool anyMethodHadExtensionSyntax = false;
            bool anyMemberHasAttributes = false;
            bool anyNonTypeMembers = false;

            var memberNameBuilder = s_memberNameBuilderPool.Allocate();

            foreach (var member in members)
            {
                AddNonTypeMemberNames(member, memberNameBuilder, ref anyNonTypeMembers);

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

            return ToImmutableAndFree(memberNameBuilder);
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

        private static void AddNonTypeMemberNames(
            Syntax.InternalSyntax.CSharpSyntaxNode member, ImmutableHashSet<string>.Builder set, ref bool anyNonTypeMembers)
        {
            switch (member.Kind)
            {
                case SyntaxKind.FieldDeclaration:
                    anyNonTypeMembers = true;
                    CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList<Syntax.InternalSyntax.VariableDeclaratorSyntax> fieldDeclarators =
                        ((Syntax.InternalSyntax.FieldDeclarationSyntax)member).Declaration.Variables;
                    int numFieldDeclarators = fieldDeclarators.Count;
                    for (int i = 0; i < numFieldDeclarators; i++)
                    {
                        set.Add(fieldDeclarators[i].Identifier.ValueText);
                    }
                    break;

                case SyntaxKind.EventFieldDeclaration:
                    anyNonTypeMembers = true;
                    CoreInternalSyntax.SeparatedSyntaxList<Syntax.InternalSyntax.VariableDeclaratorSyntax> eventDeclarators =
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
                    set.Add(((Syntax.InternalSyntax.ConstructorDeclarationSyntax)member).Modifiers.Any((int)SyntaxKind.StaticKeyword)
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
