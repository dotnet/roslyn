// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.NavInfos;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
{
    internal abstract partial class AbstractObjectBrowserLibraryManager
    {
        internal IVsNavInfo GetNavInfo(SymbolListItem symbolListItem, bool useExpandedHierarchy)
        {
            var project = GetProject(symbolListItem);
            if (project == null)
            {
                return null;
            }

            var compilation = symbolListItem.GetCompilation(this.Workspace);
            if (compilation == null)
            {
                return null;
            }

            var symbol = symbolListItem.ResolveSymbol(compilation);
            if (symbol == null)
            {
                return null;
            }

            if (symbolListItem is MemberListItem)
            {
                return GetMemberNavInfo(symbol, project, compilation, useExpandedHierarchy);
            }
            else if (symbolListItem is TypeListItem)
            {
                return GetTypeNavInfo((INamedTypeSymbol)symbol, project, compilation, useExpandedHierarchy);
            }
            else if (symbolListItem is NamespaceListItem)
            {
                return GetNamespaceNavInfo((INamespaceSymbol)symbol, project, compilation, useExpandedHierarchy);
            }

            return GetProjectNavInfo(project);
        }

        internal IVsNavInfo GetProjectNavInfo(ProjectId projectId)
        {
            var project = GetProject(projectId);
            if (project == null)
            {
                return null;
            }

            return GetProjectNavInfo(project);
        }

        internal IVsNavInfo GetProjectNavInfo(Project project)
        {
            return new NavInfo(
                this.LibraryGuid,
                this.SymbolToolLanguage,
                project.GetProjectNavInfoName());
        }

        internal IVsNavInfo GetReferenceNavInfo(MetadataReference reference)
        {
            var portableExecutableReference = reference as PortableExecutableReference;
            if (portableExecutableReference != null)
            {
                return new NavInfo(
                    this.LibraryGuid,
                    this.SymbolToolLanguage,
                    libraryName: portableExecutableReference.FilePath);
            }

            var compilationReference = reference as CompilationReference;
            if (compilationReference != null)
            {
                return new NavInfo(
                    this.LibraryGuid,
                    this.SymbolToolLanguage,
                    libraryName: compilationReference.Display);
            }

            return null;
        }

        internal IVsNavInfo GetAssemblyNavInfo(IAssemblySymbol assemblySymbol)
        {
            return new NavInfo(
                this.LibraryGuid,
                this.SymbolToolLanguage,
                libraryName: assemblySymbol.Identity.GetDisplayName());
        }

        internal IVsNavInfo GetMemberNavInfo(ISymbol memberSymbol, Project project, Compilation compilation, bool useExpandedHierarchy)
        {
            return CreateNavInfo(
                memberSymbol.ContainingAssembly,
                project,
                compilation,
                useExpandedHierarchy,
                namespaceName: memberSymbol.ContainingNamespace.GetNamespaceNavInfoNameOrEmpty(),
                className: memberSymbol.ContainingType.GetTypeNavInfoNameOrEmpty(),
                memberName: memberSymbol.GetMemberNavInfoNameOrEmpty());
        }

        internal IVsNavInfo GetTypeNavInfo(ITypeSymbol typeSymbol, Project project, Compilation compilation, bool useExpandedHierarchy)
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

            return CreateNavInfo(
                typeSymbol.ContainingAssembly,
                project,
                compilation,
                useExpandedHierarchy,
                namespaceName: typeSymbol.ContainingNamespace.GetNamespaceNavInfoNameOrEmpty(),
                className: typeSymbol.GetTypeNavInfoNameOrEmpty());
        }

        internal IVsNavInfo GetNamespaceNavInfo(INamespaceSymbol namespaceSymbol, Project project, Compilation compilation, bool useExpandedHierarchy)
        {
            return CreateNavInfo(
                namespaceSymbol.ContainingAssembly,
                project,
                compilation,
                useExpandedHierarchy,
                namespaceName: namespaceSymbol.GetNamespaceNavInfoNameOrEmpty());
        }

        private IVsNavInfo CreateNavInfo(
            IAssemblySymbol containingAssembly,
            Project project,
            Compilation compilation,
            bool useExpandedHierarchy,
            string namespaceName = null,
            string className = null,
            string memberName = null)
        {
            Debug.Assert(containingAssembly != null);
            Debug.Assert(compilation != null);

            var portableExecutableReference = compilation.GetMetadataReference(containingAssembly) as PortableExecutableReference;
            var assemblyName = portableExecutableReference != null
                ? portableExecutableReference.FilePath
                : containingAssembly.Identity.Name;

            var isReferencedAssembly = !containingAssembly.Identity.Equals(compilation.Assembly.Identity);

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

            var libraryName = isReferencedAssembly
                ? assemblyName
                : project.GetProjectNavInfoName();

            var metadataProjectItem = useExpandedHierarchy && isReferencedAssembly
                ? project.GetProjectNavInfoName()
                : null;

            return new NavInfo(
                this.LibraryGuid,
                this.SymbolToolLanguage,
                libraryName,
                metadataProjectItem,
                namespaceName,
                className,
                memberName);
        }
    }
}
