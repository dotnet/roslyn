using Roslyn.Compilers.Common;

namespace Roslyn.Services.Host
{
    /// <summary>
    /// A cache of syntax trees we've seen in the workspace. Used to prevent having to reparse the
    /// most recently seen parse trees over and over again.
    /// </summary>
    [ExportWorkspaceServiceFactory(typeof(IRetainerFactory<CommonSyntaxTree>), WorkspaceKind.Any)]
    internal class SyntaxTreeRetainerFactoryFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(IWorkspaceServiceProvider workspaceServices)
        {
            return new SyntaxTreeRetainerFactory();
        }

        public class SyntaxTreeRetainerFactory : CostBasedRetainerFactory<CommonSyntaxTree>
        {
            private const long DefaultSize = 1 << 22; // 4M chars * 2bytes/char = 8 MB
            private const int DefaultTreeCount = 8;

            public SyntaxTreeRetainerFactory()
                : this(DefaultSize, DefaultTreeCount)
            {
            }

            public SyntaxTreeRetainerFactory(long maxTreeTextSize = DefaultSize, int minTreeCount = DefaultTreeCount)
                : base(itemCost: t => t.Length, maxCost: maxTreeTextSize, minCount: minTreeCount)
            {
            }
        }
    }
}