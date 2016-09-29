using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.Shared
{
    internal interface IImageMonikerService : IWorkspaceService
    {
        ImageMoniker GetImageMoniker(Glyph glyph);
    }
}
