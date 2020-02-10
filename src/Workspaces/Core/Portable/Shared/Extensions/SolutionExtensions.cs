// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SolutionExtensions
    {
        public static void WriteTo(this IObjectWritable @object, ObjectWriter writer)
        {
            @object.WriteTo(writer);
        }

        public static Document GetRequiredDocument(this Solution solution, SyntaxTree syntaxTree)
            => solution.GetDocument(syntaxTree) ?? throw new InvalidOperationException();

        public static Project GetRequiredProject(this Solution solution, ProjectId projectId)
        {
            var project = solution.GetProject(projectId);
            if (project == null)
            {
                throw new InvalidOperationException(string.Format(WorkspacesResources.Project_of_ID_0_is_required_to_accomplish_the_task_but_is_not_available_from_the_solution, projectId));
            }

            return project;
        }

        public static Document GetRequiredDocument(this Solution solution, DocumentId documentId)
        {
            var document = solution.GetDocument(documentId);
            if (document == null)
            {
                throw new InvalidOperationException(WorkspacesResources.The_solution_does_not_contain_the_specified_document);
            }

            return document;
        }
    }
}
