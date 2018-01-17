using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Core.Imaging;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IImageIdService : IWorkspaceService
    {
        ImageId GetImageId(Glyph glyph);
    }
}
