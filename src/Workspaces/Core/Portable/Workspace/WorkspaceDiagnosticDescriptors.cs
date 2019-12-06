// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    internal sealed class WorkspaceDiagnosticDescriptors
    {
        internal readonly static DiagnosticDescriptor ErrorReadingFileContent;

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
