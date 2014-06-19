using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp
{
    internal static partial class SymbolExtensions
    {
        private static void AppendGenericName(Symbol s, StringBuilder result)
        {
            if (s.Kind == SymbolKind.Assembly || s.Kind == SymbolKind.NetModule)
            {
                return;
            }

            result.Append(s.Name);
            if (s is NamespaceSymbol || s is TypeParameterSymbol || s is DynamicTypeSymbol)
            {
                return;
            }

            var nt = s as NamedTypeSymbol;
            if (nt != null)
            {
                if (nt.Arity != 0)
                {
                    result.Append("<");
                    bool needComma = false;
                    foreach (var typeArg in nt.TypeArguments)
                    {
                        if (needComma)
                        {
                            result.Append(", ");
                        }

                        AppendQualifiedGenericName(typeArg, result);
                        needComma = true;
                    }

                    result.Append(">");
                }

                // We do not attach the error message to an error symbol's name, as the name is used
                // to test for type equality of error symbols.

                ////if (s is ErrorTypeSymbol)
                ////{
                ////    result.Append("<<error " + (s as ErrorTypeSymbol).ErrorInfo + ">>");
                ////}

                return;
            }

            var a = s as ArrayTypeSymbol;
            if (a != null)
            {
                // Note this code produces the dimentions of ragged arrays in the opposite order from the C# language syntax.
                AppendQualifiedGenericName(a.ElementType, result);
                result.Append("[");
                result.Append(',', a.Rank - 1);
                result.Append("]");
                return;
            }

            throw new NotImplementedException();
        }

        private static void AppendQualifiedGenericName(Symbol s, StringBuilder result)
        {
            if (!(s is TypeParameterSymbol) && s.ContainingSymbol != null && s.ContainingSymbol.Name.Length != 0)
            {
                AppendQualifiedGenericName(s.ContainingSymbol, result);
                result.Append(".");
            }

            AppendGenericName(s, result);
        }

        public static string QualifiedGenericName(this Symbol s)
        {
            var result = new StringBuilder();
            AppendQualifiedGenericName(s, result);
            return result.ToString();
        }
    }
}
