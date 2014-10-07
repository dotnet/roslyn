using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services.Shared.Utilities;

namespace Roslyn.Services.Editor.CSharp.Extensions
{
    internal static partial class SymbolContentBuilderExtensions
    {
        public static void AppendMinimalSymbol(this SymbolContentBuilder builder, Symbol symbol, Location location, SemanticModel semanticModel, SymbolDisplayFormat format = null)
        {
            var parts = symbol.ToMinimalDisplayParts(location, semanticModel, format);
            builder.AddParts(parts);
        }

        public static void AppendSymbol(this SymbolContentBuilder builder, Symbol symbol, SymbolDisplayFormat format = null)
        {
            var parts = symbol.ToDisplayParts(format);
            builder.AddParts(parts);
        }
    }
}