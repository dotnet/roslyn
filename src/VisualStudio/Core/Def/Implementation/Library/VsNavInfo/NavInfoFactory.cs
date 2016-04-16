// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.VsNavInfo
{
    internal class NavInfoFactory
    {
        internal AbstractLibraryService LibraryService { get; }

        public NavInfoFactory(AbstractLibraryService libraryService)
        {
            LibraryService = libraryService;
        }

        public IVsNavInfo CreateForProject(Project project)
        {
            return new NavInfo(this, libraryName: GetLibraryName(project));
        }

        public IVsNavInfo CreateForReference(MetadataReference reference)
        {
            var portableExecutableReference = reference as PortableExecutableReference;
            if (portableExecutableReference != null)
            {
                return new NavInfo(this, libraryName: portableExecutableReference.FilePath);
            }

            return new NavInfo(this, libraryName: reference.Display);
        }

        public IVsNavInfo CreateForSymbol(ISymbol symbol, Project project, Compilation compilation, bool useExpandedHierarchy = false)
        {
            var assemblySymbol = symbol as IAssemblySymbol;
            if (assemblySymbol != null)
            {
                return CreateForAssembly(assemblySymbol);
            }

            var aliasSymbol = symbol as IAliasSymbol;
            if (aliasSymbol != null)
            {
                symbol = aliasSymbol.Target;
            }

            var namespaceSymbol = symbol as INamespaceSymbol;
            if (namespaceSymbol != null)
            {
                return CreateForNamespace(namespaceSymbol, project, compilation, useExpandedHierarchy);
            }

            var typeSymbol = symbol as ITypeSymbol;
            if (typeSymbol != null)
            {
                return CreateForType(typeSymbol, project, compilation, useExpandedHierarchy);
            }

            if (symbol.Kind == SymbolKind.Event ||
                symbol.Kind == SymbolKind.Field ||
                symbol.Kind == SymbolKind.Method ||
                symbol.Kind == SymbolKind.Property)
            {
                return CreateForMember(symbol, project, compilation, useExpandedHierarchy);
            }

            return null;
        }

        public IVsNavInfo CreateForAssembly(IAssemblySymbol assemblySymbol)
        {
            return new NavInfo(this, libraryName: assemblySymbol.Identity.GetDisplayName());
        }

        public IVsNavInfo CreateForNamespace(INamespaceSymbol namespaceSymbol, Project project, Compilation compilation, bool useExpandedHierarchy = false)
        {
            return Create(
                namespaceSymbol.ContainingAssembly,
                project,
                compilation,
                useExpandedHierarchy,
                namespaceName: GetNamespaceName(namespaceSymbol));
        }

        public IVsNavInfo CreateForType(ITypeSymbol typeSymbol, Project project, Compilation compilation, bool useExpandedHierarchy = false)
        {
            while (typeSymbol != null)
            {
                if (typeSymbol.SpecialType == SpecialType.System_Nullable_T)
                {
                    typeSymbol = ((INamedTypeSymbol)typeSymbol).TypeArguments[0];
                }
                else if (typeSymbol.TypeKind == TypeKind.Pointer)
                {
                    typeSymbol = ((IPointerTypeSymbol)typeSymbol).PointedAtType;
                }
                else if (typeSymbol.TypeKind == TypeKind.Array)
                {
                    typeSymbol = ((IArrayTypeSymbol)typeSymbol).ElementType;
                }
                else
                {
                    break;
                }
            }

            typeSymbol = typeSymbol.OriginalDefinition;

            if (typeSymbol.TypeKind == TypeKind.Error ||
                typeSymbol.TypeKind == TypeKind.Unknown ||
                typeSymbol.TypeKind == TypeKind.Dynamic ||
                typeSymbol.TypeKind == TypeKind.TypeParameter)
            {
                return null;
            }

            return Create(
                typeSymbol.ContainingAssembly,
                project,
                compilation,
                useExpandedHierarchy,
                namespaceName: GetNamespaceName(typeSymbol.ContainingNamespace),
                className: GetClassName(typeSymbol));
        }

        public IVsNavInfo CreateForMember(ISymbol memberSymbol, Project project, Compilation compilation, bool useExpandedHierarchy = false)
        {
            memberSymbol = memberSymbol.OriginalDefinition;

            return Create(
                memberSymbol.ContainingAssembly,
                project,
                compilation,
                useExpandedHierarchy,
                namespaceName: GetNamespaceName(memberSymbol.ContainingNamespace),
                className: GetClassName(memberSymbol.ContainingType),
                memberName: GetMemberName(memberSymbol));
        }

        private IVsNavInfo Create(IAssemblySymbol containingAssembly, Project project, Compilation compilation, bool useExpandedHierarchy = false,
            string namespaceName = null, string className = null, string memberName = null)
        {
            // useExpandedHierarchy is true when references are nested inside the project by the
            // hierarchy. In Class View, they are nested in the Project References node. In Object Browser,
            // they are not.
            //
            // In the case that references are nested inside of the project, we need to create the nav info
            // differently:
            //
            //     project -> containing assembly -> namespace -> type
            //
            // Otherwise, we create it like so:
            //
            //     containing assembly -> namespace -> type

            string libraryName;
            string referenceOwnerName = null;

            var isCompilationAssembly = containingAssembly.Identity.Equals(compilation.Assembly.Identity);
            if (isCompilationAssembly)
            {
                libraryName = GetLibraryName(project);
            }
            else
            {
                var portableExecutableReference = compilation.GetMetadataReference(containingAssembly) as PortableExecutableReference;

                libraryName = portableExecutableReference != null
                    ? portableExecutableReference.FilePath
                    : containingAssembly.Identity.Name;

                if (useExpandedHierarchy)
                {
                    referenceOwnerName = GetLibraryName(project);
                }
            }

            return Create(libraryName, referenceOwnerName, namespaceName, className, memberName);
        }

        public IVsNavInfo Create(string libraryName, string referenceOwnerName, string namespaceName, string className, string memberName)
        {
            return new NavInfo(this, libraryName, referenceOwnerName, namespaceName, className, memberName);
        }

        /// <summary>
        /// Returns a display name for the given project, walking its parent IVsHierarchy chain and
        /// pre-pending the names of parenting hierarchies (except the solution).
        /// </summary>
        private static string GetLibraryName(Project project)
        {
            var result = project.Name;

            var workspace = project.Solution.Workspace as VisualStudioWorkspace;
            if (workspace == null)
            {
                return result;
            }

            var hierarchy = workspace.GetHierarchy(project.Id);
            if (hierarchy == null)
            {
                return result;
            }

            if (!hierarchy.TryGetName(out result))
            {
                return result;
            }

            IVsHierarchy parentHierarchy;
            if (hierarchy.TryGetParentHierarchy(out parentHierarchy) && !(parentHierarchy is IVsSolution))
            {
                var builder = SharedPools.Default<StringBuilder>().AllocateAndClear();

                while (parentHierarchy != null && !(parentHierarchy is IVsSolution))
                {
                    string parentName;
                    if (parentHierarchy.TryGetName(out parentName))
                    {
                        builder.Insert(0, parentName + "\\");
                    }

                    if (!parentHierarchy.TryGetParentHierarchy(out parentHierarchy))
                    {
                        break;
                    }
                }

                result = builder.ToString();

                SharedPools.Default<StringBuilder>().ClearAndFree(builder);
            }

            return result;
        }

        private static string GetNamespaceName(INamespaceSymbol namespaceSymbol)
        {
            if (namespaceSymbol == null)
            {
                return string.Empty;
            }

            return !namespaceSymbol.IsGlobalNamespace
                ? namespaceSymbol.ToDisplayString()
                : string.Empty;
        }

        private string GetClassName(ITypeSymbol typeSymbol)
        {
            return typeSymbol != null
                ? typeSymbol.ToDisplayString(LibraryService.TypeDisplayFormat)
                : string.Empty;
        }

        private string GetMemberName(ISymbol memberSymbol)
        {
            return memberSymbol != null
                ? memberSymbol.ToDisplayString(LibraryService.MemberDisplayFormat)
                : string.Empty;
        }
    }
}
