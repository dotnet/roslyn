using System;
using System.Text;

namespace Roslyn.Compilers.CSharp.Descriptions
{
    public static class SymbolDescriptions
    {
        public static ReadOnlyArray<SymbolDescriptionPart> GetDescription(this Symbol symbol, SymbolDescriptionFormat format, IFormatProvider formatProvider)
        {
            return GetDescriptionInContext(symbol, format, formatProvider, location: null, binding: null);
        }

        public static ReadOnlyArray<SymbolDescriptionPart> GetDescriptionInContext(this Symbol symbol, SymbolDescriptionFormat format, IFormatProvider formatProvider, Location location, SyntaxBinding binding)
        {
            var builder = ArrayBuilder<SymbolDescriptionPart>.GetInstance();

            var visitor = new SymbolDescriptionVisitor(format, location, binding, formatProvider);
            visitor.Visit(symbol, builder);

            return builder.ToReadOnlyAndFree();
        }

        public static string GetDescriptionString(this Symbol symbol, SymbolDescriptionFormat format, IFormatProvider formatProvider)
        {
            return GetDescriptionString(GetDescriptionInContext(symbol, format, formatProvider, location: null, binding: null));
        }

        public static string GetDescriptionStringInContext(this Symbol symbol, SymbolDescriptionFormat format, IFormatProvider formatProvider, Location location, SyntaxBinding binding)
        {
            return GetDescriptionString(GetDescriptionInContext(symbol, format, formatProvider, location, binding));
        }

        //TODO: overloads/defaults

        public static string GetDescriptionString(ReadOnlyArray<SymbolDescriptionPart> actual)
        {
            var actualBuilder = new StringBuilder();

            foreach (var part in actual)
            {
                actualBuilder.Append(part.Text);
            }

            return actualBuilder.ToString();
        }
    }

    public struct SymbolDescriptionPart
    {
        public SymbolDescriptionPartKind Kind { get; internal set; }

        public string Text { get; internal set; }
    }

    public enum SymbolDescriptionPartKind
    {
        Punctuation,
        Operator,
        Keyword,
        AssemblyName,
        ModuleName,
        ClassName,
        StructureName,
        InterfaceName,
        EnumName,
        DelegateName,
        TypeParameterName,
        MethodName,
        PropertyName,
        FieldName,
        NamespaceName,
        Identifier,
        Literal,
        Space,
    }
}