// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public static Project GetRequiredProject(this Solution solution, ProjectId projectId)
        {
            var project = solution.GetProject(projectId);
            if (project == null)
            {
                throw new InvalidOperationException(string.Format(WorkspacesResources.Project_of_ID_0_is_required_to_accomplish_the_task_but_is_not_available_from_the_solution, projectId));
            }

            return project;
        }
    }
}
