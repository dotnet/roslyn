// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor.Host
{
    internal interface IPreviewPaneService : IWorkspaceService
    {
        // TODO: we should move this API to use DiagnosticData not Diagnostic. but it required too much changes so for now,
        //       I created an issue https://github.com/dotnet/roslyn/issues/3111 and making this API to accept bunch of extra information.
        object GetPreviewPane(Diagnostic diagnostic, string language, string projectType, object previewContent);
    }
}
