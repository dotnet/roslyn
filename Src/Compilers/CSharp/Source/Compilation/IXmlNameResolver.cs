using System.IO;

namespace Roslyn.Compilers.CSharp
{
    public interface IXmlNameResolver
    {
        // Given an syntax tree and a <include> directive in XML documentation from that tree,
        // return a stream for reading the contents of that included file.
        Stream GetXmlInclude(SyntaxTree tree, string xmlIncludeFile);
    }
}