using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.CSharp.NavigationBar;
using Microsoft.CodeAnalysis.Editor.VisualBasic.NavigationBar;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;

namespace Roslyn.Assets
{
    public static partial class SolutionProvider
    {
        public static readonly ImmutableArray<Assembly> ExternalHostAssemblies =
            MefHostServices.DefaultAssemblies
                // This adds the exported MEF services from Workspaces.Desktop
                .Add(typeof(TemporaryStorageServiceFactory.TemporaryStorageService).Assembly)
                // This adds the exported MEF services from the RemoteWorkspaces assembly.
                .Add(typeof(RoslynServices).Assembly)
                .Add(typeof(IMetadataAsSourceFileService).Assembly)
                .Add(typeof(CSharpNavigationBarItemService).Assembly)
                .Add(typeof(VisualBasicNavigationBarItemService).Assembly);

        public static ISolutionProvider FromAsset(ISolutionAsset asset)
        {
            return new AssetSolutionProvider(asset);
        }

        public static ISolutionProvider FromStream(Stream stream)
        {
            return new StreamSolutionProvider(stream);
        }
    }
}
