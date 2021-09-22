// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser
{
    internal static class Extensions
    {
        private static readonly SymbolDisplayFormat s_typeDisplayFormat = new(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance);

        private static readonly SymbolDisplayFormat s_memberDisplayFormat = new(
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
            // If the project name is unambiguous within the solution, use that name. Otherwise, use the unique name
            // provided by IVsSolution3.GetUniqueUINameOfProject. This covers all cases except for a single solution
            // with two or more multi-targeted projects with the same name and same targets.
            //
            // https://github.com/dotnet/roslyn/pull/43800
            // http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/949113

            if (IsUnambiguousProjectNameInSolution(project))
            {
                return project.Name;
            }
            else if (project.Solution.Workspace is VisualStudioWorkspace workspace
                && workspace.GetHierarchy(project.Id) is { } hierarchy
                && (IVsSolution3)ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution)) is { } solution)
            {
                if (ErrorHandler.Succeeded(solution.GetUniqueUINameOfProject(hierarchy, out var name)) && name != null)
                {
                    return name;
                }
            }

            return project.Name;

            // Local functions
            static bool IsUnambiguousProjectNameInSolution(Project project)
            {
                foreach (var other in project.Solution.Projects)
                {
                    if (other.Id == project.Id)
                        continue;

                    if (other.Name == project.Name)
                    {
                        // Another project with the same name was found in the solution. This project name is _not_
                        // unambiguous.
                        return false;
                    }
                }

                return true;
            }
        }

        public static bool IsVenus(this Project project)
        {
            if (project.Solution.Workspace is not VisualStudioWorkspaceImpl workspace)
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
                var builder = new StringBuilder(result);

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
            }

            return result;
        }
    }
}
