// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Internal.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal interface IAnalyzersCommandHandler
{
    IContextMenuController AnalyzerFolderContextMenuController { get; }
    IContextMenuController AnalyzerContextMenuController { get; }
    IContextMenuController DiagnosticContextMenuController { get; }
}
