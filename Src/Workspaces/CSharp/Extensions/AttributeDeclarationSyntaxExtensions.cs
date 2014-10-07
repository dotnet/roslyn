using Roslyn.Compilers.CSharp;

namespace Roslyn.Services.CSharp.Extensions
{
    internal static class AttributeDeclarationSyntaxExtensions
    {
        public static bool IsAssemblyAttribute(this AttributeListSyntax attribute)
        {
            return
                attribute != null &&
                attribute.Target != null &&
                attribute.Target.Identifier.Kind == SyntaxKind.AssemblyKeyword;
        }
    }
}