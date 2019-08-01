// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
{
    internal static class Extensions
    {
        private static readonly SymbolDisplayFormat s_typeDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance);

        private static readonly SymbolDisplayFormat s_memberDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
            memberOptions: SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public static string GetMemberNavInfoNameOrEmpty(this ISymbol memberSymbol)
        {
            return memberSymbol != null
                ? memberSymbol.ToDisplayString(s_memberDisplayFormat)
                : string.Empty;
        }

        public static string GetNamespaceNavInfoNameOrEmpty(this INamespaceSymbol namespaceSymbol)
        {
            if (namespaceSymbol == null)
            {
                return string.Empty;
            }

            return !namespaceSymbol.IsGlobalNamespace
                ? namespaceSymbol.ToDisplayString()
                : string.Empty;
        }

        public static string GetTypeNavInfoNameOrEmpty(this ITypeSymbol typeSymbol)
        {
            return typeSymbol != null
                ? typeSymbol.ToDisplayString(s_typeDisplayFormat)
                : string.Empty;
        }

        public static string GetProjectDisplayName(this Project project)
        {
            if (project.Solution.Workspace is VisualStudioWorkspaceImpl workspace)
            {
                var hierarchy = workspace.GetHierarchy(project.Id);
                if (hierarchy != null)
                {
                    var solution = (IVsSolution3)ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution));
                    if (solution != null)
                    {
                        if (ErrorHandler.Succeeded(solution.GetUniqueUINameOfProject(hierarchy, out var name)) && name != null)
                        {
                            return name;
                        }
                    }
                }

                return project.Name;
            }

            return project.Name;
        }

        public static bool IsVenus(this Project project)
        {
            var workspace = project.Solution.Workspace as VisualStudioWorkspaceImpl;
            if (workspace == null)
            {
                return false;
            }

            foreach (var documentId in project.DocumentIds)
            {
                if (workspace.TryGetContainedDocument(documentId) != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a display name for the given project, walking its parent IVsHierarchy chain and
        /// pre-pending the names of parenting hierarchies (except the solution).
        /// </summary>
        public static string GetProjectNavInfoName(this Project project)
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

            if (hierarchy.TryGetParentHierarchy(out var parentHierarchy) && !(parentHierarchy is IVsSolution))
            {
                var builder = new StringBuilder(result);

                while (parentHierarchy != null && !(parentHierarchy is IVsSolution))
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
            }

            return result;
        }
    }
}
