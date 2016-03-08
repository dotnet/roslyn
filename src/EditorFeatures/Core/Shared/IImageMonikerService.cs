using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.CodeAnalysis.Editor.Shared
{
    internal interface IImageMonikerService : IWorkspaceService
    {
        ImageMoniker GetImageMoniker(Glyph glyph);
    }
}
