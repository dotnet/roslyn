// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class ProjectInstance(
    MSB.Execution.ProjectInstance? projectInstance,
    DiagnosticLog log) : IProjectInstance
{
    public ImmutableArray<DiagnosticLogItem> GetDiagnosticLogItems()
        => [.. log];

    public string GetPropertyValue(string propertyName)
    {
        if (projectInstance is null)
        {
            return string.Empty;
        }

        return projectInstance.GetPropertyValue(propertyName);
    }
}
