#if false
using System;
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Utilities;

namespace Roslyn.Services.Shared.Utilities
{
    internal class SymbolContentBuilder
    {
        public void AddParts(ReadOnlyArray<SymbolDisplayPart> parts, IFormatProvider formatProvider = null)
        {
            AddParts(parts.AsList(), formatProvider);
        }

        public void AddParts(IEnumerable<SymbolDisplayPart> parts, IFormatProvider formatProvider = null)
        {
            foreach (var part in parts)
            {
                AddPart(part, formatProvider);
            }
        }

        private void AddPart(SymbolDisplayPart part, IFormatProvider formatProvider)
        {
            switch (part.Kind)
            {
                case SymbolDisplayPartKind.Arity:
                    AddArity(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.AliasName:
                    AddAliasName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.AssemblyName:
                    AddAssemblyName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.ClassName:
                    AddClassName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.DelegateName:
                    AddDelegateName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.EnumName:
                    AddEnumName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.ErrorTypeName:
                    AddErrorTypeName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.EventName:
                    AddEventName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.FieldName:
                    AddFieldName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.InterfaceName:
                    AddInterfaceName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.Keyword:
                    AddKeyword(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.LabelName:
                    AddLabelName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.LineBreak:
                    AddLineBreak(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.NumericLiteral:
                    AddNumericLiteral(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.StringLiteral:
                    AddStringLiteral(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.LocalName:
                    AddLocalName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.MethodName:
                    AddMethodName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.ModuleName:
                    AddModuleName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.NamespaceName:
                    AddNamespaceName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.Operator:
                    AddOperator(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.ParameterName:
                    AddParameterName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.PropertyName:
                    AddPropertyName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.Punctuation:
                    AddPunctuation(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.RangeVariableName:
                    AddRangeVariableName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.Space:
                    AddSpace(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.StructName:
                    AddStructName(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.Text:
                    AddText(part.GetText(formatProvider));
                    break;
                case SymbolDisplayPartKind.TypeParameterName:
                    AddTypeParameterName(part.GetText(formatProvider));
                    break;
                default:
                    throw new ArgumentException("Unknown part kind".NeedsLocalization());
            }
        }

        protected virtual void DefaultAdd(string text)
        {
            throw new NotImplementedException();
        }

        public virtual void AddArity(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddAliasName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddAssemblyName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddClassName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddDelegateName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddEnumName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddErrorTypeName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddEventName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddFieldName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddIdentifier(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddInterfaceName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddKeyword(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddLabelName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddLineBreak(string text = "\r\n")
        {
            DefaultAdd(text);
        }

        public virtual void AddNumericLiteral(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddStringLiteral(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddLocalName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddMethodName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddModuleName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddNamespaceName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddOperator(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddParameterName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddPropertyName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddPunctuation(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddRangeVariableName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddStructName(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddSpace(string text = " ")
        {
            DefaultAdd(text);
        }

        public virtual void AddText(string text)
        {
            DefaultAdd(text);
        }

        public virtual void AddTypeParameterName(string text)
        {
            DefaultAdd(text);
        }
    }
}
#endif