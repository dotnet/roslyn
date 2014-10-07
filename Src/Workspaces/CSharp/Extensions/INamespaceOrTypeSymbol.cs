using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.CSharp.Extensions
{
    internal static class INamespaceOrTypeSymbolExtensions
    {
        public static bool IsBuiltInType(this INamespaceOrTypeSymbol symbol)
        {
            var typeSymbol = symbol as ITypeSymbol;
            if (typeSymbol != null)
            {
                switch (typeSymbol.SpecialType)
                {
                    case SpecialType.System_Object:
                    case SpecialType.System_Void:
                    case SpecialType.System_Boolean:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Decimal:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_Char:
                    case SpecialType.System_String:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                        return true;
                }
            }

            return false;
        }
    }
}