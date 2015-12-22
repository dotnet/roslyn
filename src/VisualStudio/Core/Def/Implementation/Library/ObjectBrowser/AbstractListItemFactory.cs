// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
{
    internal abstract class AbstractListItemFactory
    {
        private static readonly SymbolDisplayFormat s_searchFormat =
            new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);

        private static readonly SymbolDisplayFormat s_simplePredefinedTypeDisplay =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);

        private static readonly SymbolDisplayFormat s_simpleNormalTypeDisplay =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static readonly SymbolDisplayFormat s_simplePredefinedTypeFullName =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        private static readonly SymbolDisplayFormat s_simpleNormalTypeFullName =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static readonly SymbolDisplayFormat s_predefinedTypeDisplay =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        private static readonly SymbolDisplayFormat s_normalTypeDisplay =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        protected static string GetSimpleDisplayText(INamedTypeSymbol namedTypeSymbol)
        {
            return namedTypeSymbol.SpecialType.ToPredefinedType() != PredefinedType.None
                ? namedTypeSymbol.ToDisplayString(s_simplePredefinedTypeDisplay)
                : namedTypeSymbol.ToDisplayString(s_simpleNormalTypeDisplay);
        }

        protected abstract string GetMemberDisplayString(ISymbol memberSymbol);
        protected abstract string GetMemberAndTypeDisplayString(ISymbol memberSymbol);

        protected MemberListItem CreateFullyQualifiedMemberListItem(ISymbol memberSymbol, ProjectId projectId, bool hidden)
        {
            var displayText = GetMemberAndTypeDisplayString(memberSymbol);
            var fullNameText = displayText;
            var searchText = memberSymbol.ToDisplayString(s_searchFormat);

            return new MemberListItem(projectId, memberSymbol, displayText, fullNameText, searchText, hidden, isInherited: true);
        }

        protected MemberListItem CreateInheritedMemberListItem(ISymbol memberSymbol, ProjectId projectId, bool hidden)
        {
            return CreateMemberListItem(memberSymbol, projectId, hidden, isInherited: true);
        }

        protected MemberListItem CreateSimpleMemberListItem(ISymbol memberSymbol, ProjectId projectId, bool hidden)
        {
            return CreateMemberListItem(memberSymbol, projectId, hidden, isInherited: false);
        }

        private MemberListItem CreateMemberListItem(ISymbol memberSymbol, ProjectId projectId, bool hidden, bool isInherited)
        {
            var displayText = GetMemberDisplayString(memberSymbol);

            var containingType = memberSymbol.ContainingType;
            var fullNameText = containingType != null
                ? GetSimpleDisplayText(containingType) + "." + displayText
                : displayText;

            var searchText = memberSymbol.ToDisplayString(s_searchFormat);

            return new MemberListItem(projectId, memberSymbol, displayText, fullNameText, searchText, hidden, isInherited: isInherited);
        }

        protected TypeListItem CreateSimpleTypeListItem(INamedTypeSymbol namedTypeSymbol, ProjectId projectId, bool hidden)
        {
            var displayText = GetSimpleDisplayText(namedTypeSymbol);

            var fullNameText = namedTypeSymbol.SpecialType.ToPredefinedType() != PredefinedType.None
                ? namedTypeSymbol.ToDisplayString(s_simplePredefinedTypeFullName)
                : namedTypeSymbol.ToDisplayString(s_simpleNormalTypeFullName);

            var searchText = namedTypeSymbol.ToDisplayString(s_searchFormat);

            return new TypeListItem(projectId, namedTypeSymbol, displayText, fullNameText, searchText, hidden);
        }

        protected TypeListItem CreateFullyQualifiedTypeListItem(INamedTypeSymbol namedTypeSymbol, ProjectId projectId, bool hidden)
        {
            var displayText = namedTypeSymbol.SpecialType.ToPredefinedType() != PredefinedType.None
                ? namedTypeSymbol.ToDisplayString(s_predefinedTypeDisplay)
                : namedTypeSymbol.ToDisplayString(s_normalTypeDisplay);

            var fullNameText = displayText;

            var searchText = namedTypeSymbol.ToDisplayString(s_searchFormat);

            return new TypeListItem(projectId, namedTypeSymbol, displayText, fullNameText, searchText, hidden);
        }

        protected NamespaceListItem CreateNamespaceListItem(INamespaceSymbol namespaceSymbol, ProjectId projectId)
        {
            var text = namespaceSymbol.ToDisplayString();

            return new NamespaceListItem(projectId, namespaceSymbol, displayText: text, fullNameText: text, searchText: text);
        }

        private static bool IncludeSymbol(ISymbol symbol)
        {
            return symbol.IsErrorType()
                || symbol.Locations.Any(l => l.IsInSource || l.IsInMetadata);
        }

        private static bool IncludeMemberSymbol(ISymbol symbol, IAssemblySymbol assemblySymbol)
        {
            if (symbol.Kind == SymbolKind.NamedType)
            {
                return false;
            }

            if (symbol.IsImplicitlyDeclared || symbol.IsAccessor())
            {
                return false;
            }

            if (symbol.Locations.Any(l => l.IsInSource))
            {
                return true;
            }

            if (symbol.Locations.Any(l => l.IsInMetadata))
            {
                // We want to display protected members because we don't really have through
                // type to pass along for protected checks. Besides, protected members are 
                // interesting for the user to know about since they could create a sub class 
                // and access them.
                return symbol.DeclaredAccessibility == Accessibility.Protected
                    || symbol.DeclaredAccessibility == Accessibility.ProtectedOrInternal
                    || symbol.IsAccessibleWithin(assemblySymbol);
            }

            return false;
        }

        private ImmutableArray<ObjectListItem> CreateListItemsFromSymbols<TSymbol>(
            ImmutableArray<TSymbol> symbols,
            Compilation compilation,
            ProjectId projectId,
            Func<TSymbol, ProjectId, bool, ObjectListItem> listItemCreator)
            where TSymbol : class, ISymbol
        {
            var builder = ImmutableArray.CreateBuilder<ObjectListItem>(symbols.Length);

            AddListItemsFromSymbols(symbols, compilation, projectId, listItemCreator, builder);

            return builder.ToImmutable();
        }

        private static void AddListItemsFromSymbols<TSymbol>(
            IEnumerable<TSymbol> symbols,
            Compilation compilation,
            ProjectId projectId,
            Func<TSymbol, ProjectId, bool, ObjectListItem> listItemCreator,
            ImmutableArray<ObjectListItem>.Builder builder)
            where TSymbol : class, ISymbol
        {
            var editorBrowsableAttributeConstructor = EditorBrowsableHelpers.GetSpecialEditorBrowsableAttributeConstructor(compilation);
            var typeLibFuncAttributeConstructors = EditorBrowsableHelpers.GetSpecialTypeLibFuncAttributeConstructors(compilation);
            var typeLibTypeAttributeConstructors = EditorBrowsableHelpers.GetSpecialTypeLibTypeAttributeConstructors(compilation);
            var typeLibVarAttributeConstructors = EditorBrowsableHelpers.GetSpecialTypeLibVarAttributeConstructors(compilation);

            foreach (var symbol in symbols)
            {
                if (!IncludeSymbol(symbol))
                {
                    continue;
                }

                var hideAdvancedMembers = false;
                var isHidden = !symbol.IsEditorBrowsable(
                    hideAdvancedMembers,
                    compilation,
                    editorBrowsableAttributeConstructor,
                    typeLibFuncAttributeConstructors,
                    typeLibTypeAttributeConstructors,
                    typeLibVarAttributeConstructors);

                builder.Add(listItemCreator(symbol, projectId, isHidden));
            }
        }

        private ImmutableArray<ObjectListItem> GetBaseTypeListItems(INamedTypeSymbol namedTypeSymbol, Compilation compilation, ProjectId projectId)
        {
            // Special case: System.Object doesn't have a base type
            if (namedTypeSymbol.SpecialType == SpecialType.System_Object)
            {
                return ImmutableArray<ObjectListItem>.Empty;
            }

            var symbolBuilder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

            // Add base if not an interface
            if (namedTypeSymbol.TypeKind != TypeKind.Interface)
            {
                symbolBuilder.Add(namedTypeSymbol.BaseType);
            }

            foreach (var interfaceSymbol in namedTypeSymbol.Interfaces)
            {
                symbolBuilder.Add(interfaceSymbol);
            }

            return CreateListItemsFromSymbols(symbolBuilder.ToImmutable(), compilation, projectId, CreateSimpleTypeListItem);
        }

        public ImmutableArray<ObjectListItem> GetBaseTypeListItems(ObjectListItem parentListItem, Compilation compilation)
        {
            Debug.Assert(parentListItem != null);
            Debug.Assert(parentListItem is TypeListItem);
            Debug.Assert(compilation != null);

            var parentTypeItem = parentListItem as TypeListItem;
            if (parentTypeItem == null)
            {
                return ImmutableArray<ObjectListItem>.Empty;
            }

            var typeSymbol = parentTypeItem.ResolveTypedSymbol(compilation);

            return GetBaseTypeListItems(typeSymbol, compilation, parentListItem.ProjectId);
        }

        public ImmutableArray<ObjectListItem> GetFolderListItems(ObjectListItem parentListItem, Compilation compilation)
        {
            Debug.Assert(parentListItem != null);
            Debug.Assert(parentListItem is TypeListItem ||
                         parentListItem is ProjectListItem);
            Debug.Assert(compilation != null);

            // Hierarchies are parented by either a project or a type. In the case that it's a project, we show a folder
            // for "Project References". For types, we show a "Base Types" folder.

            var builder = ImmutableArray.CreateBuilder<ObjectListItem>();

            var parentProjectItem = parentListItem as ProjectListItem;
            if (parentProjectItem != null)
            {
                builder.Add(new FolderListItem(parentListItem.ProjectId, ServicesVSResources.Library_ProjectReferences));
            }

            var parentTypeItem = parentListItem as TypeListItem;
            if (parentTypeItem != null)
            {
                var typeSymbol = parentTypeItem.ResolveTypedSymbol(compilation);

                var addBaseTypes = false;

                if (typeSymbol.TypeKind == TypeKind.Interface)
                {
                    if (typeSymbol.Interfaces.Length > 0)
                    {
                        addBaseTypes = true;
                    }
                }
                else if (typeSymbol.TypeKind != TypeKind.Module &&
                         typeSymbol.SpecialType != SpecialType.System_Object)
                {
                    addBaseTypes = true;
                }

                if (addBaseTypes)
                {
                    builder.Add(new FolderListItem(parentListItem.ProjectId, ServicesVSResources.Library_BaseTypes));
                }
            }

            return builder.ToImmutable();
        }

        private ImmutableArray<ObjectListItem> GetMemberListItems(
            INamedTypeSymbol namedTypeSymbol,
            Compilation compilation,
            ProjectId projectId,
            bool fullyQualified = false)
        {
            var builder = ImmutableArray.CreateBuilder<ObjectListItem>();

            var immediateMembers = GetMemberSymbols(namedTypeSymbol, compilation);

            if (fullyQualified)
            {
                AddListItemsFromSymbols(immediateMembers, compilation, projectId, CreateFullyQualifiedMemberListItem, builder);
            }
            else
            {
                AddListItemsFromSymbols(immediateMembers, compilation, projectId, CreateSimpleMemberListItem, builder);

                var inheritedMembers = GetInheritedMemberSymbols(namedTypeSymbol, compilation);

                AddListItemsFromSymbols(inheritedMembers, compilation, projectId, CreateInheritedMemberListItem, builder);
            }

            return builder.ToImmutable();
        }

        private ImmutableArray<ISymbol> GetMemberSymbols(INamedTypeSymbol namedTypeSymbol, Compilation compilation)
        {
            var members = namedTypeSymbol.GetMembers();
            var symbolBuilder = ImmutableArray.CreateBuilder<ISymbol>(members.Length);

            foreach (var member in members)
            {
                if (IncludeMemberSymbol(member, compilation.Assembly))
                {
                    symbolBuilder.Add(member);
                }
            }

            return symbolBuilder.ToImmutable();
        }

        private ImmutableArray<ISymbol> GetInheritedMemberSymbols(INamedTypeSymbol namedTypeSymbol, Compilation compilation)
        {
            var symbolBuilder = ImmutableArray.CreateBuilder<ISymbol>();

            HashSet<ISymbol> overriddenMembers = null;
            AddOverriddenMembers(namedTypeSymbol, ref overriddenMembers);

            foreach (var baseType in namedTypeSymbol.GetBaseTypes())
            {
                AddOverriddenMembers(baseType, ref overriddenMembers);

                foreach (var member in baseType.GetMembers())
                {
                    if (member.Kind == SymbolKind.Method)
                    {
                        var methodSymbol = (IMethodSymbol)member;
                        if (methodSymbol.MethodKind == MethodKind.Destructor ||
                            methodSymbol.MethodKind == MethodKind.Constructor ||
                            methodSymbol.IsImplicitlyDeclared)
                        {
                            continue;
                        }
                    }

                    if (!member.IsAccessibleWithin(namedTypeSymbol))
                    {
                        continue;
                    }

                    if (overriddenMembers != null && overriddenMembers.Contains(member))
                    {
                        continue;
                    }

                    if (IncludeMemberSymbol(member, compilation.Assembly))
                    {
                        symbolBuilder.Add(member);
                    }
                }
            }

            return symbolBuilder.ToImmutable();
        }

        private void AddOverriddenMembers(INamedTypeSymbol namedTypeSymbol, ref HashSet<ISymbol> overriddenMembers)
        {
            foreach (var member in namedTypeSymbol.GetMembers())
            {
                if (member.IsOverride)
                {
                    var overriddenMember = member.OverriddenMember();
                    while (overriddenMember != null)
                    {
                        if (overriddenMembers == null)
                        {
                            overriddenMembers = new HashSet<ISymbol>();
                        }

                        overriddenMembers.Add(overriddenMember);
                        overriddenMember = overriddenMember.OverriddenMember();
                    }
                }
            }
        }

        public ImmutableArray<ObjectListItem> GetMemberListItems(ObjectListItem parentListItem, Compilation compilation)
        {
            Debug.Assert(parentListItem != null);
            Debug.Assert(parentListItem is TypeListItem);
            Debug.Assert(compilation != null);

            var parentTypeItem = parentListItem as TypeListItem;
            if (parentTypeItem == null)
            {
                return ImmutableArray<ObjectListItem>.Empty;
            }

            var typeSymbol = parentTypeItem.ResolveTypedSymbol(compilation);

            return GetMemberListItems(typeSymbol, compilation, parentListItem.ProjectId);
        }

        public void CollectNamespaceListItems(IAssemblySymbol assemblySymbol, ProjectId projectId, ImmutableArray<ObjectListItem>.Builder builder, string searchString)
        {
            Debug.Assert(assemblySymbol != null);

            var stack = new Stack<INamespaceSymbol>();
            stack.Push(assemblySymbol.GlobalNamespace);

            while (stack.Count > 0)
            {
                var namespaceSymbol = stack.Pop();

                // Only add non-global namespaces that contain accessible type symbols.
                if (!namespaceSymbol.IsGlobalNamespace &&
                    ContainsAccessibleTypeMember(namespaceSymbol, assemblySymbol))
                {
                    var namespaceListItem = CreateNamespaceListItem(namespaceSymbol, projectId);

                    if (searchString == null)
                    {
                        builder.Add(namespaceListItem);
                    }
                    else if (namespaceListItem.SearchText.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        builder.Add(namespaceListItem);
                    }
                }

                // Visit any nested namespaces
                foreach (var namespaceMember in namespaceSymbol.GetNamespaceMembers())
                {
                    stack.Push(namespaceMember);
                }
            }
        }

        public ImmutableArray<ObjectListItem> GetNamespaceListItems(ObjectListItem parentListItem, Compilation compilation)
        {
            Debug.Assert(parentListItem != null);
            Debug.Assert(parentListItem is ProjectListItem ||
                         parentListItem is ReferenceListItem);
            Debug.Assert(compilation != null);

            var assemblySymbol = parentListItem is ReferenceListItem
                ? ((ReferenceListItem)parentListItem).GetAssembly(compilation)
                : compilation.Assembly;

            var builder = ImmutableArray.CreateBuilder<ObjectListItem>();

            CollectNamespaceListItems(assemblySymbol, parentListItem.ProjectId, builder, searchString: null);

            return builder.ToImmutable();
        }

        private class AssemblySymbolComparer : IEqualityComparer<Tuple<ProjectId, IAssemblySymbol>>
        {
            public bool Equals(Tuple<ProjectId, IAssemblySymbol> x, Tuple<ProjectId, IAssemblySymbol> y)
            {
                return x.Item2.Identity.Equals(y.Item2.Identity);
            }

            public int GetHashCode(Tuple<ProjectId, IAssemblySymbol> obj)
            {
                return obj.Item2.Identity.GetHashCode();
            }
        }

        public ImmutableHashSet<Tuple<ProjectId, IAssemblySymbol>> GetAssemblySet(Solution solution, string languageName, CancellationToken cancellationToken)
        {
            var set = ImmutableHashSet.CreateBuilder(new AssemblySymbolComparer());

            foreach (var projectId in solution.ProjectIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var project = solution.GetProject(projectId);
                if (project.Language != languageName)
                {
                    continue;
                }

                if (project.IsVenus())
                {
                    continue;
                }

                var compilation = project
                    .GetCompilationAsync(cancellationToken)
                    .WaitAndGetResult(cancellationToken);

                if (compilation != null &&
                    compilation.Assembly != null)
                {
                    set.Add(Tuple.Create(projectId, compilation.Assembly));

                    foreach (var reference in project.MetadataReferences)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var referenceAssembly = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                        if (referenceAssembly != null)
                        {
                            set.Add(Tuple.Create(projectId, referenceAssembly));
                        }
                    }
                }
            }

            return set.ToImmutable();
        }

        public ImmutableHashSet<Tuple<ProjectId, IAssemblySymbol>> GetAssemblySet(Project project, bool lookInReferences, CancellationToken cancellationToken)
        {
            var set = ImmutableHashSet.CreateBuilder(new AssemblySymbolComparer());

            var compilation = project
                .GetCompilationAsync(cancellationToken)
                .WaitAndGetResult(cancellationToken);

            if (compilation != null &&
                compilation.Assembly != null)
            {
                set.Add(Tuple.Create(project.Id, compilation.Assembly));

                if (lookInReferences)
                {
                    foreach (var reference in project.MetadataReferences)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var referenceAssembly = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                        if (referenceAssembly != null)
                        {
                            set.Add(Tuple.Create(project.Id, referenceAssembly));
                        }
                    }
                }
            }

            return set.ToImmutable();
        }

        private bool ContainsAccessibleTypeMember(INamespaceOrTypeSymbol namespaceOrTypeSymbol, IAssemblySymbol assemblySymbol)
        {
            foreach (var typeMember in namespaceOrTypeSymbol.GetTypeMembers())
            {
                if (IncludeSymbol(typeMember) && typeMember.IsAccessibleWithin(assemblySymbol))
                {
                    return true;
                }
            }

            return false;
        }

        private ImmutableArray<INamedTypeSymbol> GetAccessibleTypeMembers(INamespaceOrTypeSymbol namespaceOrTypeSymbol, IAssemblySymbol assemblySymbol)
        {
            var typeMembers = namespaceOrTypeSymbol.GetTypeMembers();
            var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>(typeMembers.Length);

            foreach (var typeMember in typeMembers)
            {
                if (IncludeSymbol(typeMember) && typeMember.IsAccessibleWithin(assemblySymbol))
                {
                    builder.Add(typeMember);
                }
            }

            return builder.ToImmutable();
        }

        public ImmutableArray<ObjectListItem> GetProjectListItems(Solution solution, string languageName, uint listFlags, CancellationToken cancellationToken)
        {
            var projectIds = solution.ProjectIds;
            if (!projectIds.Any())
            {
                return ImmutableArray<ObjectListItem>.Empty;
            }

            var projectListItemBuilder = ImmutableArray.CreateBuilder<ObjectListItem>();
            var referenceListItemBuilder = ImmutableArray.CreateBuilder<ObjectListItem>();
            HashSet<AssemblyIdentity> assemblyIdentitySet = null;

            foreach (var projectId in projectIds)
            {
                var project = solution.GetProject(projectId);
                if (project.Language != languageName)
                {
                    continue;
                }

                if (project.IsVenus())
                {
                    continue;
                }

                projectListItemBuilder.Add(new ProjectListItem(project));

                if (Helpers.IsObjectBrowser(listFlags))
                {
                    if (assemblyIdentitySet == null)
                    {
                        assemblyIdentitySet = new HashSet<AssemblyIdentity>();
                    }

                    foreach (var reference in project.MetadataReferences)
                    {
                        var portableExecutableReference = reference as PortableExecutableReference;
                        if (portableExecutableReference != null)
                        {
                            var assemblyIdentity = AssemblyIdentityUtils.TryGetAssemblyIdentity(portableExecutableReference.FilePath);
                            if (assemblyIdentity != null && !assemblyIdentitySet.Contains(assemblyIdentity))
                            {
                                assemblyIdentitySet.Add(assemblyIdentity);

                                var referenceListItem = new ReferenceListItem(projectId, assemblyIdentity.Name, reference);
                                referenceListItemBuilder.Add(referenceListItem);
                            }
                        }
                    }
                }
            }

            projectListItemBuilder.AddRange(referenceListItemBuilder);

            return projectListItemBuilder.ToImmutable();
        }

        public ImmutableArray<ObjectListItem> GetReferenceListItems(ObjectListItem parentListItem, Compilation compilation)
        {
            Debug.Assert(parentListItem != null);
            Debug.Assert(parentListItem is ProjectListItem);
            Debug.Assert(compilation != null);

            if (!compilation.References.Any())
            {
                return ImmutableArray<ObjectListItem>.Empty;
            }

            var builder = ArrayBuilder<ObjectListItem>.GetInstance();

            foreach (var reference in compilation.References)
            {
                var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;

                if (assemblySymbol != null)
                {
                    builder.Add(new ReferenceListItem(parentListItem.ProjectId, assemblySymbol.Name, reference));
                }
            }

            return builder.ToImmutableAndFree();
        }

        private ImmutableArray<INamedTypeSymbol> GetAccessibleTypes(INamespaceSymbol namespaceSymbol, Compilation compilation)
        {
            var typeMembers = GetAccessibleTypeMembers(namespaceSymbol, compilation.Assembly);
            var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>(typeMembers.Length);
            var stack = new Stack<INamedTypeSymbol>();

            foreach (var typeMember in typeMembers)
            {
                stack.Push(typeMember);

                while (stack.Count > 0)
                {
                    var typeSymbol = stack.Pop();

                    builder.Add(typeSymbol);

                    foreach (var nestedTypeMember in GetAccessibleTypeMembers(typeSymbol, compilation.Assembly))
                    {
                        stack.Push(nestedTypeMember);
                    }
                }
            }

            return builder.ToImmutable();
        }

        private ImmutableArray<ObjectListItem> GetTypeListItems(
            INamespaceSymbol namespaceSymbol,
            Compilation compilation,
            ProjectId projectId,
            string searchString,
            bool fullyQualified = false)
        {
            var types = GetAccessibleTypes(namespaceSymbol, compilation);

            var listItems = fullyQualified
                ? CreateListItemsFromSymbols(types, compilation, projectId, CreateFullyQualifiedTypeListItem)
                : CreateListItemsFromSymbols(types, compilation, projectId, CreateSimpleTypeListItem);

            if (searchString == null)
            {
                return listItems;
            }

            var finalBuilder = ImmutableArray.CreateBuilder<ObjectListItem>();

            foreach (var listItem in listItems)
            {
                if (listItem.DisplayText.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    finalBuilder.Add(listItem);
                }
            }

            return finalBuilder.ToImmutable();
        }

        public ImmutableArray<ObjectListItem> GetTypeListItems(ObjectListItem parentListItem, Compilation compilation)
        {
            Debug.Assert(parentListItem != null);
            Debug.Assert(parentListItem is NamespaceListItem ||
                         parentListItem is ProjectListItem ||
                         parentListItem is ReferenceListItem);
            Debug.Assert(compilation != null);

            INamespaceSymbol namespaceSymbol;
            if (parentListItem is NamespaceListItem)
            {
                namespaceSymbol = ((NamespaceListItem)parentListItem).ResolveTypedSymbol(compilation);
            }
            else if (parentListItem is ReferenceListItem)
            {
                namespaceSymbol = ((ReferenceListItem)parentListItem).GetAssembly(compilation).GlobalNamespace;
            }
            else
            {
                namespaceSymbol = compilation.Assembly.GlobalNamespace;
            }

            return GetTypeListItems(namespaceSymbol, compilation, parentListItem.ProjectId, searchString: null);
        }

        public void CollectTypeListItems(IAssemblySymbol assemblySymbol, Compilation compilation, ProjectId projectId, ImmutableArray<ObjectListItem>.Builder builder, string searchString)
        {
            Debug.Assert(assemblySymbol != null);
            Debug.Assert(compilation != null);

            var stack = new Stack<INamespaceSymbol>();
            stack.Push(assemblySymbol.GlobalNamespace);

            while (stack.Count > 0)
            {
                var namespaceSymbol = stack.Pop();
                var typeListItems = GetTypeListItems(namespaceSymbol, compilation, projectId, searchString, fullyQualified: true);

                foreach (var typeListItem in typeListItems)
                {
                    if (searchString == null)
                    {
                        builder.Add(typeListItem);
                    }
                    else if (typeListItem.SearchText.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        builder.Add(typeListItem);
                    }
                }

                // Visit any nested namespaces
                foreach (var namespaceMember in namespaceSymbol.GetNamespaceMembers())
                {
                    stack.Push(namespaceMember);
                }
            }
        }

        public void CollectMemberListItems(IAssemblySymbol assemblySymbol, Compilation compilation, ProjectId projectId, ImmutableArray<ObjectListItem>.Builder builder, string searchString)
        {
            Debug.Assert(assemblySymbol != null);
            Debug.Assert(compilation != null);

            var namespaceStack = new Stack<INamespaceSymbol>();
            namespaceStack.Push(assemblySymbol.GlobalNamespace);

            while (namespaceStack.Count > 0)
            {
                var namespaceSymbol = namespaceStack.Pop();
                var types = GetAccessibleTypes(namespaceSymbol, compilation);

                foreach (var type in types)
                {
                    var memberListItems = GetMemberListItems(type, compilation, projectId, fullyQualified: true);
                    foreach (var memberListItem in memberListItems)
                    {
                        if (searchString == null)
                        {
                            builder.Add(memberListItem);
                        }
                        else if (memberListItem.SearchText.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            builder.Add(memberListItem);
                        }
                    }
                }

                // Visit any nested namespaces
                foreach (var namespaceMember in namespaceSymbol.GetNamespaceMembers())
                {
                    namespaceStack.Push(namespaceMember);
                }
            }
        }
    }
}
