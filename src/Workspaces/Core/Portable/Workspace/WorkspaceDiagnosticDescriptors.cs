// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis
{
    internal sealed class WorkspaceDiagnosticDescriptors
    {
        internal static readonly DiagnosticDescriptor ErrorReadingFileContent;

        internal const string ErrorReadingFileContentId = "IDE1100";

        static WorkspaceDiagnosticDescriptors()
        {
            ErrorReadingFileContent = new DiagnosticDescriptor(
                id: ErrorReadingFileContentId,
                title: new LocalizableResourceString(nameof(WorkspacesResources.Workspace_error), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)),
                messageFormat: new LocalizableResourceString(nameof(WorkspacesResources.Error_reading_content_of_source_file_0_1), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)),
                category: WorkspacesResources.Workspace_error,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                customTags: new[] { WellKnownDiagnosticTags.NotConfigurable });
        }
    }
}
