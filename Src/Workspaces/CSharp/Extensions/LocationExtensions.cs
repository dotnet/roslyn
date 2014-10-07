using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class LocationExtensions
    {
        public static SyntaxToken FindToken(this Location location, CancellationToken cancellationToken)
        {
            return location.SourceTree.GetRoot(cancellationToken).FindToken(location.SourceSpan.Start);
        }
    }
}