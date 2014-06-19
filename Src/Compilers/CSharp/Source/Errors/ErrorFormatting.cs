// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System;

namespace Roslyn.Compilers.CSharp
{
    // This class implements the formatting rules used in previous versions of the C# compiler 
    // to display types and symbols in error messages. 
    //
    // For example, a method should always be displayed as
    //
    // N.C.M<int,double?>(N.S[*,*], void*)
    //
    // rather than, say
    //
    // void N.C.M<System.Int32, System.Nullable<System.Double>>>(N.S[,] someArray, void* somePointer)
    //

    internal static class ErrorFormatting
    {
        public static object GetErrorReportingName(SymbolKind kind)
        {
            switch (kind)
            {
                case SymbolKind.Namespace:
                    return MessageID.IDS_SK_NAMESPACE.Localize();
                default:
                    // TODO: what is the right way to get these strings?
                    return kind.ToString().ToLower();
            }
        }

        public static object GetErrorReportingName(Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Assembly:
                    return GetErrorReportingName(symbol as AssemblySymbol);
                case SymbolKind.Namespace:
                    return GetErrorReportingName(symbol as NamespaceSymbol);
                case SymbolKind.Parameter:
                    return symbol.Name;
                case SymbolKind.Local:
                    return symbol.GetFullName();
                case SymbolKind.Field:
                    return GetErrorReportingName(symbol as FieldSymbol);
                case SymbolKind.Method:
                    return GetErrorReportingName(symbol as MethodSymbol);
                case SymbolKind.ArrayType:
                case SymbolKind.DynamicType:
                case SymbolKind.NamedType:
                case SymbolKind.ErrorType:
                case SymbolKind.TypeParameter:
                case SymbolKind.PointerType:
                    return GetErrorReportingName(symbol as NamedTypeSymbol);
                default:
                    throw new NotImplementedException();
            }
        }

        public static string GetErrorReportingName(AssemblySymbol assembly)
        {
            return assembly.AssemblyName.FullName;
        }

        public static string GetErrorReportingName(BoundExpression expression)
        {
            switch(expression.Kind)
            {
                case BoundKind.Literal:
                    if (expression.ConstantValue.IsNull)
                        return "<null>";
                    break;
                case BoundKind.Lambda:
                case BoundKind.UnboundLambda:
                    return "lambda expression"; // UNDONE: or "anonymous method"
                case BoundKind.MethodGroup:
                    return "method group";
            }
            return GetErrorReportingName(expression.GetExpressionType());
        }

        public static string GetErrorReportingName(FieldSymbol field)
        {
            return GetErrorReportingName(field.ContainingType) + "." + field.Name;
        }

        public static string GetErrorReportingName(MethodSymbol method)
        {
            var builder = new StringBuilder();

            string name;
            switch (method.MethodKind)
            {
                case MethodKind.Constructor:
                case MethodKind.StaticConstructor:
                    name = method.ContainingSymbol.Name;
                    break;
                case MethodKind.Destructor:
                    name = "~" + method.ContainingSymbol.Name;
                    break;
                default:
                    name = method.Name;
                    break;
            }

            builder.AppendFormat("{0}.{1}", method.ContainingSymbol.GetFullName(), name);

            if (method.IsGeneric)
            {
                builder.Append("<");
                method.TypeArguments.Select(typeArg=>GetErrorReportingName(typeArg)).Comma(",", builder);
                builder.Append(">");
            }

            builder.AppendFormat("(");
            method.Parameters.Select(p=>GetErrorReportingName(p.Type, p.RefKind)).Comma(", ", builder);
            builder.Append(")");

            return builder.ToString();
        }

        public static object GetErrorReportingName(NamespaceSymbol @namespace)
        {
            return @namespace.IsGlobalNamespace ?
                MessageID.IDS_GlobalNamespace.Localize() :
                (object) @namespace.GetFullName();
        }

        public static string GetErrorReportingName(TypeSymbol type, RefKind refKind = RefKind.None)
        {
            string prefix = "";
            switch (refKind)
            {
                case RefKind.Ref: prefix = "ref "; break;
                case RefKind.Out: prefix = "out "; break;
            }

            switch (type.GetSpecialTypeSafe())
            {
                case SpecialType.System_Void: 
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Byte: 
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                case SpecialType.System_Char:
                case SpecialType.System_Boolean:
                case SpecialType.System_String:
                case SpecialType.System_Object:
                    return prefix + SemanticFacts.GetLanguageName(type.SpecialType);

                case SpecialType.None:
                    if (type != null && type.IsNullableType() && !ReferenceEquals(type, type.OriginalDefinition))
                    {
                        TypeSymbol underlyingType = type.GetNullableUnderlyingType();

                        switch (underlyingType.GetSpecialTypeSafe())
                        {
                            case SpecialType.System_Boolean:
                            case SpecialType.System_SByte:
                            case SpecialType.System_Int16:
                            case SpecialType.System_Int32:
                            case SpecialType.System_Int64:
                            case SpecialType.System_Byte:
                            case SpecialType.System_UInt16:
                            case SpecialType.System_UInt32:
                            case SpecialType.System_UInt64:
                            case SpecialType.System_Single:
                            case SpecialType.System_Double:
                            case SpecialType.System_Decimal:
                            case SpecialType.System_Char:
                                return prefix + SemanticFacts.GetLanguageName(underlyingType.SpecialType) + "?";
                        }

                        return prefix + GetErrorReportingName(underlyingType) + "?";
                    }

                    break;
            }

            var dynamicType = type as DynamicTypeSymbol;
            if (dynamicType != null)
            {
                return prefix + "dynamic";
            }

            var arrayType = type as ArrayTypeSymbol;
            if (arrayType != null)
            {
                string suffix = "";
                while (true)
                {
                    var elementType = arrayType.ElementType;
                    suffix += GetSuffix(arrayType.Rank);
                    arrayType = elementType as ArrayTypeSymbol;
                    if (arrayType == null)
                    {
                        return prefix + GetErrorReportingName(elementType) + suffix;
                    }
                }
            }

            var pointerType = type as PointerTypeSymbol;
            if (pointerType != null)
            {
                return prefix + GetErrorReportingName(pointerType.BaseType) + "*";
            }

            var namedType = type as NamedTypeSymbol;
            if (namedType != null)
            {
                string result = "";
                if (namedType.ContainingType != null)
                {
                    result = GetErrorReportingName(namedType.ContainingType) + ".";
                }
                else if (namedType.ContainingNamespace != null && !namedType.ContainingNamespace.IsGlobalNamespace)
                {
                    result = namedType.ContainingNamespace.GetFullName() + ".";
                }
                result += type.Name;
                if (namedType.TypeArguments.Count != 0)
                {
                    result += "<";
                    result += namedType.TypeArguments.Select(a => GetErrorReportingName(a)).Comma(",");
                    result += ">";
                }
                return prefix + result;
            }

            var typeParameter = type as TypeParameterSymbol;
            if (typeParameter != null)
            {
                return prefix + type.Name;
            }

            Debug.Fail("What case did we miss in type name error reporter?");
            return prefix + type.GetFullName();
        }

        private static string GetSuffix(int rank)
        {
            string suffix = "[";
            for (int i = 0; i < rank - 1; ++i)
            {
                if (i == 0)
                {
                    suffix += "*";
                }
                suffix += ",*";
            }
            return suffix + "]";
        }

        private static void Comma(this IEnumerable<string> items, string separator, StringBuilder builder)
        {
            bool first = true;
            foreach (var item in items)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(separator);
                }
                builder.Append(item);
            }
        }

        private static string Comma(this IEnumerable<string> items, string separator)
        {
            return string.Join(separator, items.ToArray());
        }
    }
}
