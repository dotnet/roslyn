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