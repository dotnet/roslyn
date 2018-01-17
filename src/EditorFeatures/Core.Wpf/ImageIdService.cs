using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Core.Imaging;

namespace Microsoft.CodeAnalysis.Editor
{
    [Shared]
    [ExportWorkspaceService(typeof(IImageIdService), ServiceLayer.Host)]
    internal class ImageIdService : IImageIdService
    {
        ImageId IImageIdService.GetImageId(Glyph glyph)
        {
            var moniker = glyph.GetImageMoniker();
            return new ImageId(moniker.Guid, moniker.Id);
        }
    }
}
