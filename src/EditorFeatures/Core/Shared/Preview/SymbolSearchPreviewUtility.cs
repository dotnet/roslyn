using Microsoft.CodeAnalysis.Experiments;

namespace Microsoft.CodeAnalysis.Editor.Shared.Preview
{
    internal class SymbolSearchPreviewUtility
    {
        internal static bool EditorHandlesSymbolSearch(Workspace workspace)
        {
            if (workspace == null)
            {
                return false;
            }
            var experimentationService = workspace.Services.GetService<IExperimentationService>();
            return experimentationService.IsExperimentEnabled(WellKnownExperimentNames.EditorHandlesSymbolSearch);
        }
    }
}
