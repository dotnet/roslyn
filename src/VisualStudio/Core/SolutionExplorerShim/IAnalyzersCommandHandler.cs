// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Internal.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal interface IAnalyzersCommandHandler
    {
        IContextMenuController AnalyzerFolderContextMenuController { get; }
        IContextMenuController AnalyzerContextMenuController { get; }
        IContextMenuController DiagnosticContextMenuController { get; }
    }
}
