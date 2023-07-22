// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using CoreInternalSyntax = Microsoft.CodeAnalysis.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    using BoxedMemberNames = StrongBox<ImmutableSegmentedHashSet<string>>;

    internal sealed class DeclarationTreeBuilder : CSharpSyntaxVisitor<SingleNamespaceOrTypeDeclaration>
    {
        /// <summary>
        /// Cache of node to the last set of member names computed for it.  Helpful for avoiding excess hashset
        /// allocations for common scenarios where compilations are created with the same trees used to create prior
        /// compilations.
        /// </summary>
        /// <remarks>
        /// We store as green nodes for two purposes.  First, it allows the data to be stored, without holding onto the
        /// whole tree.  It allows files with multiple types in them to reuse the member names for untouched types when
        /// others in the file are edited.
        /// </remarks>
        private static readonly ConditionalWeakTable<GreenNode, BoxedMemberNames> s_nodeToMemberNames
            = new ConditionalWeakTable<GreenNode, BoxedMemberNames>();

        private static readonly BoxedMemberNames s_emptyMemberNames = new BoxedMemberNames(ImmutableSegmentedHashSet<string>.Empty);

        private readonly SyntaxTree _syntaxTree;
        private readonly string _scriptClassName;
        private readonly bool _isSubmission;

        /// <summary>
        /// Stored in lexical order for the types in this tree that had member names the last time we created decls for
        /// the tree.
        /// </summary>
        private readonly OneOrMany<WeakReference<BoxedMemberNames>> _previousMemberNames;

        /// <summary>
        /// Any special attributes we may be referencing through a using alias in the file.
        /// For example <c>using X = System.Runtime.CompilerServices.TypeForwardedToAttribute</c>.
        /// </summary>
        private QuickAttributes _nonGlobalAliasedQuickAttributes;

        /// <summary>
        /// The index of the current type we're processing in lexicographic order with respect to all other types in the
        /// file.  For example:
        /// <code>
        /// class A // Index 0
        /// {
        ///     class B // Index 1
        ///     {
        ///     }
        /// }
        /// 
        /// class C // Index 2
        /// {
        /// }
        /// </code>
        /// </summary>
        private int _currentTypeIndex;

        private DeclarationTreeBuilder(
            SyntaxTree syntaxTree,
            string scriptClassName,
            bool isSubmission,
            OneOrMany<WeakReference<BoxedMemberNames>> previousMemberNames)
        {
            _syntaxTree = syntaxTree;
            _scriptClassName = scriptClassName;
            _isSubmission = isSubmission;
            _previousMemberNames = previousMemberNames;
        }

        public static RootSingleNamespaceDeclaration ForTree(
            SyntaxTree syntaxTree,
            string scriptClassName,
            bool isSubmission,
            OneOrMany<WeakReference<BoxedMemberNames>>? previousMemberNames = null)
        {
            var builder = new DeclarationTreeBuilder(
                syntaxTree, scriptClassName, isSubmission,
                previousMemberNames ?? OneOrMany<WeakReference<BoxedMemberNames>>.Empty);
            return (RootSingleNamespaceDeclaration)builder.Visit(syntaxTree.GetRoot());
        }

        public static bool CachesComputedMemberNames(SingleTypeDeclaration typeDeclaration)
        {
            return typeDeclaration.Kind switch
            {
                // A type declaration can't ever be a namespace.
                DeclarationKind.Namespace => throw ExceptionUtilities.Unreachable(),

                // Delegates also do not cache any members names as the member names are always a known fixed set.
                DeclarationKind.Delegate => false,

                DeclarationKind.Class or
                DeclarationKind.Interface or
                DeclarationKind.Struct or
                DeclarationKind.Enum or
                DeclarationKind.Script or
                DeclarationKind.Submission or
                DeclarationKind.ImplicitClass or
                DeclarationKind.Record or
                DeclarationKind.RecordStruct => true,

                _ => throw ExceptionUtilities.UnexpectedValue(typeDeclaration.Kind)
            };
        }

        private ImmutableArray<SingleNamespaceOrTypeDeclaration> VisitNamespaceChildren(
            CSharpSyntaxNode node,
            SyntaxList<MemberDeclarationSyntax> members,
            CoreInternalSyntax.SyntaxList<Syntax.InternalSyntax.MemberDeclarationSyntax> internalMembers)
        {
            Debug.Assert(
                node.Kind() is SyntaxKind.NamespaceDeclaration or SyntaxKind.FileScopedNamespaceDeclaration ||
                (node.Kind() == SyntaxKind.CompilationUnit && _syntaxTree.Options.Kind == SourceCodeKind.Regular));

            if (members.Count == 0)
            {
                return ImmutableArray<SingleNamespaceOrTypeDeclaration>.Empty;
            }

            // We look for members that are not allowed in a namespace. 
            // If there are any we create an implicit class to wrap them.
            bool hasGlobalMembers = false;
            bool acceptSimpleProgram = node.Kind() == SyntaxKind.CompilationUnit && _syntaxTree.Options.Kind == SourceCodeKind.Regular;
            bool hasAwaitExpressions = false;
            bool isIterator = false;
            bool hasReturnWithExpression = false;
            GlobalStatementSyntax firstGlobalStatement = null;
            bool hasNonEmptyGlobalStatement = false;

            var childrenBuilder = ArrayBuilder<SingleNamespaceOrTypeDeclaration>.GetInstance();
            foreach (var member in members)
            {
                SingleNamespaceOrTypeDeclaration namespaceOrType = Visit(member);
                if (namespaceOrType != null)
                {
                    childrenBuilder.Add(namespaceOrType);
                }
                else if (acceptSimpleProgram && member.IsKind(SyntaxKind.GlobalStatement))
                {
                    var global = (GlobalStatementSyntax)member;
                    firstGlobalStatement ??= global;
                    var topLevelStatement = global.Statement;

                    if (!topLevelStatement.IsKind(SyntaxKind.EmptyStatement))
                    {
                        hasNonEmptyGlobalStatement = true;
                    }

                    if (!hasAwaitExpressions)
                    {
                        hasAwaitExpressions = SyntaxFacts.HasAwaitOperations(topLevelStatement);
                    }

                    if (!isIterator)
                    {
                        isIterator = SyntaxFacts.HasYieldOperations(topLevelStatement);
                    }

                    if (!hasReturnWithExpression)
                    {
                        hasReturnWithExpression = SyntaxFacts.HasReturnWithExpression(topLevelStatement);
                    }
                }
                else if (!hasGlobalMembers && member.Kind() != SyntaxKind.IncompleteMember)
                {
                    hasGlobalMembers = true;
                }
            }

            // wrap all global statements in a compilation unit into a simple program type:
            if (firstGlobalStatement is object)
            {
                var diagnostics = ImmutableArray<Diagnostic>.Empty;

                if (!hasNonEmptyGlobalStatement)
                {
                    var bag = DiagnosticBag.GetInstance();
                    bag.Add(ErrorCode.ERR_SimpleProgramIsEmpty, ((EmptyStatementSyntax)firstGlobalStatement.Statement).SemicolonToken.GetLocation());
                    diagnostics = bag.ToReadOnlyAndFree();
                }

                childrenBuilder.Add(CreateSimpleProgram(firstGlobalStatement, hasAwaitExpressions, isIterator, hasReturnWithExpression, diagnostics));
            }

            // wrap all members that are defined in a namespace or compilation unit into an implicit type:
            if (hasGlobalMembers)
            {
                //The implicit class is not static and has no extensions
                SingleTypeDeclaration.TypeDeclarationFlags declFlags = SingleTypeDeclaration.TypeDeclarationFlags.None;
                var memberNames = GetNonTypeMemberNames(node, internalMembers, ref declFlags, skipGlobalStatements: acceptSimpleProgram);
                var container = _syntaxTree.GetReference(node);

                childrenBuilder.Add(CreateImplicitClass(memberNames, container, declFlags));
            }

            return childrenBuilder.ToImmutableAndFree();
        }

        private static SingleNamespaceOrTypeDeclaration CreateImplicitClass(BoxedMemberNames memberNames, SyntaxReference container, SingleTypeDeclaration.TypeDeclarationFlags declFlags)
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
                diagnostics: ImmutableArray<Diagnostic>.Empty,
                quickAttributes: QuickAttributes.None);
        }

        private static SingleNamespaceOrTypeDeclaration CreateSimpleProgram(GlobalStatementSyntax firstGlobalStatement, bool hasAwaitExpressions, bool isIterator, bool hasReturnWithExpression, ImmutableArray<Diagnostic> diagnostics)
        {
            return new SingleTypeDeclaration(
                kind: DeclarationKind.Class,
                name: WellKnownMemberNames.TopLevelStatementsEntryPointTypeName,
                arity: 0,
                modifiers: DeclarationModifiers.Partial,
                declFlags: (hasAwaitExpressions ? SingleTypeDeclaration.TypeDeclarationFlags.HasAwaitExpressions : SingleTypeDeclaration.TypeDeclarationFlags.None) |
                           (isIterator ? SingleTypeDeclaration.TypeDeclarationFlags.IsIterator : SingleTypeDeclaration.TypeDeclarationFlags.None) |
                           (hasReturnWithExpression ? SingleTypeDeclaration.TypeDeclarationFlags.HasReturnWithExpression : SingleTypeDeclaration.TypeDeclarationFlags.None) |
                           SingleTypeDeclaration.TypeDeclarationFlags.IsSimpleProgram,
                syntaxReference: firstGlobalStatement.SyntaxTree.GetReference(firstGlobalStatement.Parent),
                nameLocation: new SourceLocation(firstGlobalStatement.GetFirstToken()),
                memberNames: s_emptyMemberNames,
                children: ImmutableArray<SingleTypeDeclaration>.Empty,
                diagnostics: diagnostics,
                quickAttributes: QuickAttributes.None);
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
            var membernames = GetNonTypeMemberNames(compilationUnit, ((Syntax.InternalSyntax.CompilationUnitSyntax)(compilationUnit.Green)).Members, ref declFlags);
            rootChildren.Add(
                CreateScriptClass(
                    compilationUnit,
                    scriptChildren.ToImmutableAndFree(),
                    membernames,
                    declFlags));

            return CreateRootSingleNamespaceDeclaration(compilationUnit, rootChildren.ToImmutableAndFree(), isForScript: true);
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
            BoxedMemberNames memberNames,
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
                diagnostics: ImmutableArray<Diagnostic>.Empty,
                quickAttributes: QuickAttributes.None);

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

        private static QuickAttributes GetQuickAttributes(
            SyntaxList<UsingDirectiveSyntax> usings, bool global)
        {
            var result = QuickAttributes.None;

            foreach (var directive in usings)
            {
                if (directive.Alias == null)
                {
                    continue;
                }

                var isGlobal = directive.GlobalKeyword.Kind() != SyntaxKind.None;
                if (isGlobal != global)
                {
                    continue;
                }

                if (directive.Name is not NameSyntax name)
                {
                    continue;
                }

                result |= QuickAttributeHelpers.GetQuickAttributes(name.GetUnqualifiedName().Identifier.ValueText, inAttribute: false);
            }

            return result;
        }

        public override SingleNamespaceOrTypeDeclaration VisitCompilationUnit(CompilationUnitSyntax compilationUnit)
        {
            if (_syntaxTree.Options.Kind != SourceCodeKind.Regular)
            {
                return CreateScriptRootDeclaration(compilationUnit);
            }

            _nonGlobalAliasedQuickAttributes = GetNonGlobalAliasedQuickAttributes(compilationUnit);

            var children = VisitNamespaceChildren(compilationUnit, compilationUnit.Members, ((Syntax.InternalSyntax.CompilationUnitSyntax)(compilationUnit.Green)).Members);

            return CreateRootSingleNamespaceDeclaration(compilationUnit, children, isForScript: false);
        }

        private static QuickAttributes GetNonGlobalAliasedQuickAttributes(CompilationUnitSyntax compilationUnit)
        {
            var result = GetQuickAttributes(compilationUnit.Usings, global: false);
            foreach (var member in compilationUnit.Members)
            {
                if (member is BaseNamespaceDeclarationSyntax @namespace)
                {
                    result |= GetNonGlobalAliasedQuickAttributes(@namespace);
                }
            }

            return result;
        }

        private static QuickAttributes GetNonGlobalAliasedQuickAttributes(BaseNamespaceDeclarationSyntax @namespace)
        {
            var result = GetQuickAttributes(@namespace.Usings, global: false);
            foreach (var member in @namespace.Members)
            {
                if (member is BaseNamespaceDeclarationSyntax child)
                {
                    result |= GetNonGlobalAliasedQuickAttributes(child);
                }
            }

            return result;
        }

        private RootSingleNamespaceDeclaration CreateRootSingleNamespaceDeclaration(CompilationUnitSyntax compilationUnit, ImmutableArray<SingleNamespaceOrTypeDeclaration> children, bool isForScript)
        {
            bool hasUsings = false;
            bool hasGlobalUsings = false;
            bool reportedGlobalUsingOutOfOrder = false;

            var diagnostics = DiagnosticBag.GetInstance();

            foreach (var directive in compilationUnit.Usings)
            {
                if (directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword))
                {
                    hasGlobalUsings = true;

                    if (hasUsings && !reportedGlobalUsingOutOfOrder)
                    {
                        reportedGlobalUsingOutOfOrder = true;
                        diagnostics.Add(ErrorCode.ERR_GlobalUsingOutOfOrder, directive.GlobalKeyword.GetLocation());
                    }
                }
                else
                {
                    hasUsings = true;
                }
            }

            var globalAliasedQuickAttributes = GetQuickAttributes(compilationUnit.Usings, global: true);

            CheckFeatureAvailabilityForUsings(diagnostics, compilationUnit.Usings);
            CheckFeatureAvailabilityForExterns(diagnostics, compilationUnit.Externs);

            return new RootSingleNamespaceDeclaration(
                hasGlobalUsings: hasGlobalUsings,
                hasUsings: hasUsings,
                hasExternAliases: compilationUnit.Externs.Any(),
                treeNode: _syntaxTree.GetReference(compilationUnit),
                children: children,
                referenceDirectives: isForScript ? GetReferenceDirectives(compilationUnit) : ImmutableArray<ReferenceDirective>.Empty,
                hasAssemblyAttributes: compilationUnit.AttributeLists.Any(),
                diagnostics: diagnostics.ToReadOnlyAndFree(),
                globalAliasedQuickAttributes);
        }

        private static void CheckFeatureAvailabilityForUsings(DiagnosticBag diagnostics, SyntaxList<UsingDirectiveSyntax> usings)
        {
            foreach (var usingDirective in usings)
            {
                if (usingDirective.StaticKeyword != default)
                    MessageID.IDS_FeatureUsingStatic.CheckFeatureAvailability(diagnostics, usingDirective, usingDirective.StaticKeyword.GetLocation());

                if (usingDirective.GlobalKeyword != default)
                    MessageID.IDS_FeatureGlobalUsing.CheckFeatureAvailability(diagnostics, usingDirective, usingDirective.GlobalKeyword.GetLocation());
            }
        }

        private static void CheckFeatureAvailabilityForExterns(DiagnosticBag diagnostics, SyntaxList<ExternAliasDirectiveSyntax> externs)
        {
            foreach (var externAlias in externs)
                MessageID.IDS_FeatureExternAlias.CheckFeatureAvailability(diagnostics, externAlias, externAlias.ExternKeyword.GetLocation());
        }

        public override SingleNamespaceOrTypeDeclaration VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
            => this.VisitBaseNamespaceDeclaration(node);

        public override SingleNamespaceOrTypeDeclaration VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            => this.VisitBaseNamespaceDeclaration(node);

        private SingleNamespaceDeclaration VisitBaseNamespaceDeclaration(BaseNamespaceDeclarationSyntax node)
        {
            var children = VisitNamespaceChildren(node, node.Members, ((Syntax.InternalSyntax.BaseNamespaceDeclarationSyntax)node.Green).Members);

            bool hasUsings = node.Usings.Any();
            bool hasExterns = node.Externs.Any();
            NameSyntax name = node.Name;
            CSharpSyntaxNode currentNode = node;
            while (name is QualifiedNameSyntax dotted)
            {
                var ns = SingleNamespaceDeclaration.Create(
                    name: dotted.Right.Identifier.ValueText,
                    hasUsings: hasUsings,
                    hasExternAliases: hasExterns,
                    syntaxReference: _syntaxTree.GetReference(currentNode),
                    nameLocation: new SourceLocation(dotted.Right),
                    children: children,
                    diagnostics: ImmutableArray<Diagnostic>.Empty);

                children = ImmutableArray.Create<SingleNamespaceOrTypeDeclaration>(ns);
                currentNode = name = dotted.Left;
                hasUsings = false;
                hasExterns = false;
            }

            var diagnostics = DiagnosticBag.GetInstance();

            if (node is FileScopedNamespaceDeclarationSyntax)
            {
                MessageID.IDS_FeatureFileScopedNamespace.CheckFeatureAvailability(diagnostics, node, node.NamespaceKeyword.GetLocation());

                if (node.Parent is FileScopedNamespaceDeclarationSyntax)
                {
                    // Happens when user writes:
                    //      namespace A.B;
                    //      namespace X.Y;
                    diagnostics.Add(ErrorCode.ERR_MultipleFileScopedNamespace, node.Name.GetLocation());
                }
                else if (node.Parent is NamespaceDeclarationSyntax)
                {
                    // Happens with:
                    //
                    //      namespace A.B
                    //      {
                    //          namespace X.Y;
                    diagnostics.Add(ErrorCode.ERR_FileScopedAndNormalNamespace, node.Name.GetLocation());
                }
                else
                {
                    // Happens with cases like:
                    //
                    //      namespace A.B { }
                    //      namespace X.Y;
                    //
                    // or even
                    //
                    //      class C { }
                    //      namespace X.Y;

                    Debug.Assert(node.Parent is CompilationUnitSyntax);
                    var compilationUnit = (CompilationUnitSyntax)node.Parent;
                    if (node != compilationUnit.Members[0])
                    {
                        diagnostics.Add(ErrorCode.ERR_FileScopedNamespaceNotBeforeAllMembers, node.Name.GetLocation());
                    }
                }
            }
            else
            {
                Debug.Assert(node is NamespaceDeclarationSyntax);

                //      namespace X.Y;
                //      namespace A.B { }
                if (node.Parent is FileScopedNamespaceDeclarationSyntax)
                {
                    diagnostics.Add(ErrorCode.ERR_FileScopedAndNormalNamespace, node.Name.GetLocation());
                }
            }

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

            foreach (var directive in node.Usings)
            {
                if (directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword))
                {
                    diagnostics.Add(ErrorCode.ERR_GlobalUsingInNamespace, directive.GlobalKeyword.GetLocation());
                    break;
                }
            }

            CheckFeatureAvailabilityForUsings(diagnostics, node.Usings);
            CheckFeatureAvailabilityForExterns(diagnostics, node.Externs);

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

        public override SingleNamespaceOrTypeDeclaration VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            var declarationKind = node.Kind() switch
            {
                SyntaxKind.RecordDeclaration => DeclarationKind.Record,
                SyntaxKind.RecordStructDeclaration => DeclarationKind.RecordStruct,
                _ => throw ExceptionUtilities.UnexpectedValue(node.Kind())
            };

            return VisitTypeDeclaration(node, declarationKind);
        }

        private SingleTypeDeclaration VisitTypeDeclaration(TypeDeclarationSyntax node, DeclarationKind kind)
        {
            var declFlags = node.AttributeLists.Any()
                ? SingleTypeDeclaration.TypeDeclarationFlags.HasAnyAttributes
                : SingleTypeDeclaration.TypeDeclarationFlags.None;

            if (node.BaseList != null)
            {
                declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.HasBaseDeclarations;
            }

            var diagnostics = DiagnosticBag.GetInstance();
            if (node.Arity == 0)
            {
                Symbol.ReportErrorIfHasConstraints(node.ConstraintClauses, diagnostics);
            }

            var hasPrimaryCtor = node.ParameterList != null && node is RecordDeclarationSyntax or ClassDeclarationSyntax or StructDeclarationSyntax;
            if (hasPrimaryCtor)
            {
                declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.HasAnyNontypeMembers;
                declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.HasPrimaryConstructor;

                foreach (var attributeListSyntax in node.AttributeLists)
                {
                    if (attributeListSyntax.Target?.Identifier.ToAttributeLocation() == AttributeLocation.Method)
                    {
                        declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.AnyMemberHasAttributes;
                        break;
                    }
                }
            }

            var memberNames = GetNonTypeMemberNames(
                node, ((Syntax.InternalSyntax.TypeDeclarationSyntax)(node.Green)).Members,
                ref declFlags, hasPrimaryCtor: hasPrimaryCtor);

            // If we have `record class` or `record struct` check that this is supported in the language. Note: we don't
            // have to do any check for the simple `record` case as the parser itself would never produce such a node
            // unless the language version was sufficient (since it actually will not produce the node at all on
            // previous versions).
            if (node is RecordDeclarationSyntax record)
            {
                if (record.ClassOrStructKeyword.Kind() != SyntaxKind.None)
                {
                    MessageID.IDS_FeatureRecordStructs.CheckFeatureAvailability(diagnostics, record, record.ClassOrStructKeyword.GetLocation());
                }
            }
            else if (node.Kind() is SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.InterfaceDeclaration)
            {
                if (node.ParameterList != null)
                {
                    if (node.Kind() is SyntaxKind.InterfaceDeclaration)
                    {
                        diagnostics.Add(ErrorCode.ERR_UnexpectedParameterList, node.ParameterList.GetLocation());
                    }
                    else
                    {
                        MessageID.IDS_FeaturePrimaryConstructors.CheckFeatureAvailability(diagnostics, node.ParameterList);
                    }
                }
                else if (node.OpenBraceToken == default && node.CloseBraceToken == default && node.SemicolonToken != default)
                {
                    MessageID.IDS_FeaturePrimaryConstructors.CheckFeatureAvailability(diagnostics, node, node.SemicolonToken.GetLocation());
                }
            }

            var modifiers = node.Modifiers.ToDeclarationModifiers(isForTypeDeclaration: true, diagnostics: diagnostics);
            var quickAttributes = GetQuickAttributes(node.AttributeLists);

            foreach (var modifier in node.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.StaticKeyword) && kind == DeclarationKind.Class)
                {
                    MessageID.IDS_FeatureStaticClasses.CheckFeatureAvailability(diagnostics, node, modifier.GetLocation());
                }
                else if (modifier.IsKind(SyntaxKind.ReadOnlyKeyword) && kind is DeclarationKind.Struct or DeclarationKind.RecordStruct)
                {
                    MessageID.IDS_FeatureReadOnlyStructs.CheckFeatureAvailability(diagnostics, node, modifier.GetLocation());
                }
                else if (modifier.IsKind(SyntaxKind.RefKeyword) && kind is DeclarationKind.Struct or DeclarationKind.RecordStruct)
                {
                    MessageID.IDS_FeatureRefStructs.CheckFeatureAvailability(diagnostics, node, modifier.GetLocation());
                }
            }

            return new SingleTypeDeclaration(
                kind: kind,
                name: node.Identifier.ValueText,
                arity: node.Arity,
                modifiers: modifiers,
                declFlags: declFlags,
                syntaxReference: _syntaxTree.GetReference(node),
                nameLocation: new SourceLocation(node.Identifier),
                memberNames: memberNames,
                children: VisitTypeChildren(node),
                diagnostics: diagnostics.ToReadOnlyAndFree(),
                _nonGlobalAliasedQuickAttributes | quickAttributes);
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
                children.AddIfNotNull(typeDecl);
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

            var modifiers = node.Modifiers.ToDeclarationModifiers(isForTypeDeclaration: true, diagnostics: diagnostics);
            var quickAttributes = DeclarationTreeBuilder.GetQuickAttributes(node.AttributeLists);

            return new SingleTypeDeclaration(
                kind: DeclarationKind.Delegate,
                name: node.Identifier.ValueText,
                arity: node.Arity,
                modifiers: modifiers,
                declFlags: declFlags,
                syntaxReference: _syntaxTree.GetReference(node),
                nameLocation: new SourceLocation(node.Identifier),
                memberNames: s_emptyMemberNames,
                children: ImmutableArray<SingleTypeDeclaration>.Empty,
                diagnostics: diagnostics.ToReadOnlyAndFree(),
                _nonGlobalAliasedQuickAttributes | quickAttributes);
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

            var memberNames = GetEnumMemberNames(node, ref declFlags);

            var diagnostics = DiagnosticBag.GetInstance();
            var modifiers = node.Modifiers.ToDeclarationModifiers(isForTypeDeclaration: true, diagnostics: diagnostics);
            var quickAttributes = DeclarationTreeBuilder.GetQuickAttributes(node.AttributeLists);

            if (node.OpenBraceToken == default && node.CloseBraceToken == default && node.SemicolonToken != default)
            {
                MessageID.IDS_FeaturePrimaryConstructors.CheckFeatureAvailability(diagnostics, node, node.SemicolonToken.GetLocation());
            }

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
                diagnostics: diagnostics.ToReadOnlyAndFree(),
                _nonGlobalAliasedQuickAttributes | quickAttributes);
        }

        private static QuickAttributes GetQuickAttributes(SyntaxList<AttributeListSyntax> attributeLists)
        {
            var result = QuickAttributes.None;
            foreach (var attributeList in attributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    result |= QuickAttributeHelpers.GetQuickAttributes(attribute.Name.GetUnqualifiedName().Identifier.ValueText, inAttribute: true);
                }
            }

            return result;
        }

        private BoxedMemberNames GetEnumMemberNames(
            EnumDeclarationSyntax enumDeclaration,
            ref SingleTypeDeclaration.TypeDeclarationFlags declFlags)
        {
            var members = enumDeclaration.Members;
            var cnt = members.Count;

            if (cnt != 0)
            {
                declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.HasAnyNontypeMembers;
            }

            bool anyMemberHasAttributes = members.Any(static m => m.AttributeLists.Any());

            if (anyMemberHasAttributes)
            {
                declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.AnyMemberHasAttributes;
            }

            return GetOrComputeMemberNames(
                enumDeclaration,
                static (memberNamesBuilder, members) =>
                {
                    foreach (var member in members)
                        memberNamesBuilder.Add(member.Identifier.ValueText);
                },
                members);
        }

        private BoxedMemberNames GetNonTypeMemberNames(
            CSharpSyntaxNode parent,
            CoreInternalSyntax.SyntaxList<Syntax.InternalSyntax.MemberDeclarationSyntax> members,
            ref SingleTypeDeclaration.TypeDeclarationFlags declFlags,
            bool skipGlobalStatements = false,
            bool hasPrimaryCtor = false)
        {
            bool anyMethodHadExtensionSyntax = false;
            bool anyMemberHasAttributes = false;
            bool anyNonTypeMembers = false;
            bool anyRequiredMembers = false;

            foreach (var member in members)
            {
                if (!anyNonTypeMembers && HasAnyNonTypeMemberNames(member, skipGlobalStatements))
                {
                    anyNonTypeMembers = true;
                }

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

                if (!anyRequiredMembers && checkPropertyOrFieldMemberForRequiredModifier(member))
                {
                    anyRequiredMembers = true;
                }

                // Break early if we've hit all sorts of members.
                if (anyNonTypeMembers && anyMethodHadExtensionSyntax && anyMemberHasAttributes && anyRequiredMembers)
                {
                    break;
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

            if (anyRequiredMembers)
            {
                declFlags |= SingleTypeDeclaration.TypeDeclarationFlags.HasRequiredMembers;
            }

            return GetOrComputeMemberNames(
                parent,
                static (memberNamesBuilder, tuple) =>
                {
                    if (tuple.hasPrimaryCtor)
                        memberNamesBuilder.Add(WellKnownMemberNames.InstanceConstructorName);

                    foreach (var member in tuple.members)
                        AddNonTypeMemberNames(member, memberNamesBuilder);
                },
                (members, hasPrimaryCtor));

            static bool checkPropertyOrFieldMemberForRequiredModifier(Syntax.InternalSyntax.CSharpSyntaxNode member)
            {
                var modifiers = member switch
                {
                    Syntax.InternalSyntax.FieldDeclarationSyntax fieldDeclaration => fieldDeclaration.Modifiers,
                    Syntax.InternalSyntax.PropertyDeclarationSyntax propertyDeclaration => propertyDeclaration.Modifiers,
                    _ => default
                };

                return modifiers.Any((int)SyntaxKind.RequiredKeyword);
            }
        }

        private BoxedMemberNames GetOrComputeMemberNames<TData>(
            SyntaxNode parent,
            Action<HashSet<string>, TData> addMemberNames,
            TData data)
        {
            // Compute the member names, then always ensure we move our current type index pointer forward.
            var result = getOrComputeMemberNamesWorker();
            _currentTypeIndex++;
            return result;

            BoxedMemberNames getOrComputeMemberNamesWorker()
            {
                // Lookup in the cache first.
                var greenNode = parent.Green;
                if (!s_nodeToMemberNames.TryGetValue(greenNode, out BoxedMemberNames memberNames))
                {
                    // If not there, make a fresh set, and add all the member names to it.
                    var memberNamesBuilder = PooledHashSet<string>.GetInstance();
                    addMemberNames(memberNamesBuilder, data);

                    // Try to obtain the prior member names computed for the corresponding type decl (in lexicographic order)
                    // from the prior version of this tree.
                    var previousMemberNames = _currentTypeIndex < _previousMemberNames.Count && _previousMemberNames[_currentTypeIndex].TryGetTarget(out var previousNames)
                        ? previousNames
                        : s_emptyMemberNames;

                    // If the members names are the same as the prior computed ones, then use that instead.
                    memberNames = previousMemberNames.Value.Count == memberNamesBuilder.Count && previousMemberNames.Value.SetEquals(memberNamesBuilder)
                        ? previousMemberNames
                        : memberNamesBuilder.Count == 0
                            ? s_emptyMemberNames
                            : new BoxedMemberNames(ImmutableSegmentedHashSet.CreateRange(memberNamesBuilder));
                    memberNamesBuilder.Free();

                    // Store the names in the cache to be found in the next compilation update. But don't bother caching
                    // when there are no member names (common for most compilation units).  If another thread beat us to
                    // this, then use their values instead.
                    if (memberNames.Value.Count > 0)
                    {
                        // Avoid unnecessary allocation (as CWT on NET6 and prior has no non-allocating way to try to
                        // get an existing value, and only add if that particular KVP is not already present).
                        using PooledDelegates.Releaser _ = PooledDelegates.GetPooledCreateValueCallback(
                            static (GreenNode _, BoxedMemberNames memberNames) => memberNames, memberNames, out var pooledCallback);
                        memberNames = s_nodeToMemberNames.GetValue(greenNode, pooledCallback);
                    }
                }

                return memberNames;
            }
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
                case SyntaxKind.RecordDeclaration:
                case SyntaxKind.RecordStructDeclaration:
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
            Syntax.InternalSyntax.CSharpSyntaxNode member, HashSet<string> set)
        {
            switch (member.Kind)
            {
                case SyntaxKind.FieldDeclaration:
                    CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList<Syntax.InternalSyntax.VariableDeclaratorSyntax> fieldDeclarators =
                        ((Syntax.InternalSyntax.FieldDeclarationSyntax)member).Declaration.Variables;
                    int numFieldDeclarators = fieldDeclarators.Count;
                    for (int i = 0; i < numFieldDeclarators; i++)
                    {
                        set.Add(fieldDeclarators[i].Identifier.ValueText);
                    }
                    break;

                case SyntaxKind.EventFieldDeclaration:
                    CoreInternalSyntax.SeparatedSyntaxList<Syntax.InternalSyntax.VariableDeclaratorSyntax> eventDeclarators =
                        ((Syntax.InternalSyntax.EventFieldDeclarationSyntax)member).Declaration.Variables;
                    int numEventDeclarators = eventDeclarators.Count;
                    for (int i = 0; i < numEventDeclarators; i++)
                    {
                        set.Add(eventDeclarators[i].Identifier.ValueText);
                    }
                    break;

                case SyntaxKind.MethodDeclaration:
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
                    // Handle in the same way as explicit method implementations
                    var propertyDecl = (Syntax.InternalSyntax.PropertyDeclarationSyntax)member;
                    if (propertyDecl.ExplicitInterfaceSpecifier == null)
                    {
                        set.Add(propertyDecl.Identifier.ValueText);
                    }
                    break;

                case SyntaxKind.EventDeclaration:
                    // Handle in the same way as explicit method implementations
                    var eventDecl = (Syntax.InternalSyntax.EventDeclarationSyntax)member;
                    if (eventDecl.ExplicitInterfaceSpecifier == null)
                    {
                        set.Add(eventDecl.Identifier.ValueText);
                    }
                    break;

                case SyntaxKind.ConstructorDeclaration:
                    set.Add(((Syntax.InternalSyntax.ConstructorDeclarationSyntax)member).Modifiers.Any((int)SyntaxKind.StaticKeyword)
                        ? WellKnownMemberNames.StaticConstructorName
                        : WellKnownMemberNames.InstanceConstructorName);

                    break;

                case SyntaxKind.DestructorDeclaration:
                    set.Add(WellKnownMemberNames.DestructorName);
                    break;

                case SyntaxKind.IndexerDeclaration:
                    set.Add(WellKnownMemberNames.Indexer);
                    break;

                case SyntaxKind.OperatorDeclaration:
                    {
                        // Handle in the same way as explicit method implementations
                        var opDecl = (Syntax.InternalSyntax.OperatorDeclarationSyntax)member;

                        if (opDecl.ExplicitInterfaceSpecifier == null)
                        {
                            var name = OperatorFacts.OperatorNameFromDeclaration(opDecl);
                            set.Add(name);
                        }
                    }
                    break;

                case SyntaxKind.ConversionOperatorDeclaration:
                    {
                        // Handle in the same way as explicit method implementations
                        var opDecl = (Syntax.InternalSyntax.ConversionOperatorDeclarationSyntax)member;

                        if (opDecl.ExplicitInterfaceSpecifier == null)
                        {
                            var name = OperatorFacts.OperatorNameFromDeclaration(opDecl);
                            set.Add(name);
                        }
                    }
                    break;
            }
        }

        private static bool HasAnyNonTypeMemberNames(
            Syntax.InternalSyntax.CSharpSyntaxNode member, bool skipGlobalStatements)
        {
            switch (member.Kind)
            {
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.IndexerDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                    return true;

                case SyntaxKind.GlobalStatement:
                    return !skipGlobalStatements;
            }

            return false;
        }
    }
}
