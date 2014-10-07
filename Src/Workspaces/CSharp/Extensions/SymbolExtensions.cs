using System;
using System.Linq;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Utilities;

namespace Roslyn.Services.Editor.CSharp.Extensions
{
    internal static partial class SymbolExtensions
    {
        public static bool IsDeprecated(this Symbol symbol)
        {
            // TODO(cyrusn): Impelement this
            return false;
        }

        public static bool IsStaticType(this Symbol s)
        {
            return s.Kind == SymbolKind.NamedType && s.IsStatic;
        }

        public static bool IsNamespace(this Symbol s)
        {
            return s.Kind == SymbolKind.Namespace;
        }
    }
}