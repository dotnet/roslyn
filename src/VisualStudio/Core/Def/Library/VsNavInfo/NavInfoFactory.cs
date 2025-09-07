// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.VsNavInfo;

internal sealed class NavInfoFactory
{
    internal AbstractLibraryService LibraryService { get; }

    public NavInfoFactory(AbstractLibraryService libraryService)
        => LibraryService = libraryService;

    public IVsNavInfo CreateForProject(Project project)
        => new NavInfo(this, libraryName: GetLibraryName(project));

    public IVsNavInfo CreateForReference(MetadataReference reference)
    {
        if (reference is PortableExecutableReference portableExecutableReference)
        {
            return new NavInfo(this, libraryName: portableExecutableReference.FilePath);
        }

        return new NavInfo(this, libraryName: reference.Display);
    }

    public IVsNavInfo CreateForSymbol(ISymbol symbol, Project project, Compilation compilation, bool useExpandedHierarchy = false)
    {
        switch (symbol)
        {
            case IAssemblySymbol assemblySymbol:
                return CreateForAssembly(assemblySymbol);
            case IAliasSymbol aliasSymbol:
                symbol = aliasSymbol.Target;
                break;
            case INamespaceSymbol namespaceSymbol:
                return CreateForNamespace(namespaceSymbol, project, compilation, useExpandedHierarchy);
            case ITypeSymbol typeSymbol:
                return CreateForType(typeSymbol, project, compilation, useExpandedHierarchy);
        }

        if (symbol.Kind is SymbolKind.Event or
            SymbolKind.Field or
            SymbolKind.Method or
            SymbolKind.Property)
        {
            return CreateForMember(symbol, project, compilation, useExpandedHierarchy);
        }

        return null;
    }

    public IVsNavInfo CreateForAssembly(IAssemblySymbol assemblySymbol)
        => new NavInfo(this, libraryName: assemblySymbol.Identity.GetDisplayName());

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

        if (typeSymbol.TypeKind is TypeKind.Error or
            TypeKind.Unknown or
            TypeKind.Dynamic or
            TypeKind.TypeParameter)
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
            libraryName = compilation.GetMetadataReference(containingAssembly) is PortableExecutableReference portableExecutableReference
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
        => new NavInfo(this, libraryName, referenceOwnerName, namespaceName, className, memberName);

    /// <summary>
    /// Returns a display name for the given project, walking its parent IVsHierarchy chain and
    /// pre-pending the names of parenting hierarchies (except the solution).
    /// </summary>
    private static string GetLibraryName(Project project)
    {
        var result = project.Name;

        if (project.Solution.Workspace is not VisualStudioWorkspace workspace)
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

        if (hierarchy.TryGetParentHierarchy(out var parentHierarchy) && !(parentHierarchy is IVsSolution))
        {
            var builder = SharedPools.Default<StringBuilder>().AllocateAndClear();
            builder.Append(result);

            while (parentHierarchy is not null and not IVsSolution)
            {
                if (parentHierarchy.TryGetName(out var parentName))
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
