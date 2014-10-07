using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.Extensions
{
    internal static class ILocationExtensions
    {
        public static CommonSyntaxToken FindToken(this CommonLocation location)
        {
            return location.SourceTree.Root.FindToken(location.SourceSpan.Start);
        }
    }
}