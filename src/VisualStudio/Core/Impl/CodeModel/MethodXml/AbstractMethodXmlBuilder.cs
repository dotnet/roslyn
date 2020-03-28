﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.MethodXml
{
    internal abstract partial class AbstractMethodXmlBuilder
    {
        private const string ArgumentElementName = "Argument";
        private const string ArrayElementName = "Array";
        private const string ArrayElementAccessElementName = "ArrayElementAccess";
        private const string ArrayTypeElementName = "ArrayType";
        private const string AssignmentElementName = "Assignment";
        private const string BaseReferenceElementName = "BaseReference";
        private const string BinaryOperationElementName = "BinaryOperation";
        private const string BlockElementName = "Block";
        private const string BooleanElementName = "Boolean";
        private const string BoundElementName = "Bound";
        private const string CastElementName = "Cast";
        private const string CharElementName = "Char";
        private const string CommentElementName = "Comment";
        private const string ExpressionElementName = "Expression";
        private const string ExpressionStatementElementName = "ExpressionStatement";
        private const string LiteralElementName = "Literal";
        private const string LocalElementName = "Local";
        private const string MethodCallElementName = "MethodCall";
        private const string NameElementName = "Name";
        private const string NameRefElementName = "NameRef";
        private const string NewArrayElementName = "NewArray";
        private const string NewClassElementName = "NewClass";
        private const string NewDelegateElementName = "NewDelegate";
        private const string NullElementName = "Null";
        private const string NumberElementName = "Number";
        private const string ParenthesesElementName = "Parentheses";
        private const string QuoteElementName = "Quote";
        private const string StringElementName = "String";
        private const string ThisReferenceElementName = "ThisReference";
        private const string TypeElementName = "Type";

        private const string BinaryOperatorAttributeName = "binaryoperator";
        private const string DirectCastAttributeName = "directcast";
        private const string FullNameAttributeName = "fullname";
        private const string ImplicitAttributeName = "implicit";
        private const string LineAttributeName = "line";
        private const string NameAttributeName = "name";
        private const string RankAttributeName = "rank";
        private const string TryCastAttributeName = "trycast";
        private const string TypeAttributeName = "type";
        private const string VariableKindAttributeName = "variablekind";

        private static readonly char[] s_encodedChars = new[] { '<', '>', '&' };
        private static readonly string[] s_encodings = new[] { "&lt;", "&gt;", "&amp;" };

        private readonly StringBuilder _builder;
        protected readonly IMethodSymbol Symbol;
        protected readonly SemanticModel SemanticModel;
        protected readonly SourceText Text;

        protected AbstractMethodXmlBuilder(IMethodSymbol symbol, SemanticModel semanticModel)
        {
            _builder = new StringBuilder();

            this.Symbol = symbol;
            this.SemanticModel = semanticModel;
            this.Text = semanticModel.SyntaxTree.GetText();
        }

        public override string ToString()
        {
            return _builder.ToString();
        }

        private void AppendEncoded(string text)
        {
            var length = text.Length;

            var startIndex = 0;
            int index;

            for (index = 0; index < length; index++)
            {
                var encodingIndex = Array.IndexOf(s_encodedChars, text[index]);
                if (encodingIndex >= 0)
                {
                    if (index > startIndex)
                    {
                        _builder.Append(text, startIndex, index - startIndex);
                    }

                    _builder.Append(s_encodings[encodingIndex]);

                    startIndex = index + 1;
                }
            }

            if (index > startIndex)
            {
                _builder.Append(text, startIndex, index - startIndex);
            }
        }

        private void AppendOpenTag(string name, AttributeInfo[] attributes)
        {
            _builder.Append('<');
            _builder.Append(name);

            foreach (var attribute in attributes.Where(a => !a.IsEmpty))
            {
                _builder.Append(' ');
                _builder.Append(attribute.Name);
                _builder.Append("=\"");
                AppendEncoded(attribute.Value);
                _builder.Append('"');
            }

            _builder.Append('>');
        }

        private void AppendCloseTag(string name)
        {
            _builder.Append("</");
            _builder.Append(name);
            _builder.Append('>');
        }

        private void AppendLeafTag(string name)
        {
            _builder.Append('<');
            _builder.Append(name);
            _builder.Append("/>");
        }

        private static string GetBinaryOperatorKindText(BinaryOperatorKind kind)
            => kind switch
            {
                BinaryOperatorKind.Plus => "plus",
                BinaryOperatorKind.BitwiseOr => "bitor",
                BinaryOperatorKind.BitwiseAnd => "bitand",
                BinaryOperatorKind.Concatenate => "concatenate",
                BinaryOperatorKind.AddDelegate => "adddelegate",
                _ => throw new InvalidOperationException("Invalid BinaryOperatorKind: " + kind.ToString()),
            };

        private static string GetVariableKindText(VariableKind kind)
            => kind switch
            {
                VariableKind.Property => "property",
                VariableKind.Method => "method",
                VariableKind.Field => "field",
                VariableKind.Local => "local",
                VariableKind.Unknown => "unknown",
                _ => throw new InvalidOperationException("Invalid SymbolKind: " + kind.ToString()),
            };

        private IDisposable Tag(string name, params AttributeInfo[] attributes)
        {
            return new AutoTag(this, name, attributes);
        }

        private AttributeInfo BinaryOperatorAttribute(BinaryOperatorKind kind)
        {
            if (kind == BinaryOperatorKind.None)
            {
                return AttributeInfo.Empty;
            }

            return new AttributeInfo(BinaryOperatorAttributeName, GetBinaryOperatorKindText(kind));
        }

        private AttributeInfo FullNameAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return AttributeInfo.Empty;
            }

            return new AttributeInfo(FullNameAttributeName, name);
        }

        private AttributeInfo ImplicitAttribute(bool? @implicit)
        {
            if (@implicit == null)
            {
                return AttributeInfo.Empty;
            }

            return new AttributeInfo(ImplicitAttributeName, @implicit.Value ? "yes" : "no");
        }

        private AttributeInfo LineNumberAttribute(int lineNumber)
        {
            return new AttributeInfo(LineAttributeName, lineNumber.ToString());
        }

        private AttributeInfo NameAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return AttributeInfo.Empty;
            }

            return new AttributeInfo(NameAttributeName, name);
        }

        private AttributeInfo RankAttribute(int rank)
        {
            return new AttributeInfo(RankAttributeName, rank.ToString());
        }

        private AttributeInfo SpecialCastKindAttribute(SpecialCastKind? specialCastKind = null)
            => specialCastKind switch
            {
                SpecialCastKind.DirectCast => new AttributeInfo(DirectCastAttributeName, "yes"),
                SpecialCastKind.TryCast => new AttributeInfo(TryCastAttributeName, "yes"),
                _ => AttributeInfo.Empty,
            };

        private AttributeInfo TypeAttribute(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return AttributeInfo.Empty;
            }

            return new AttributeInfo(TypeAttributeName, typeName);
        }

        private AttributeInfo VariableKindAttribute(VariableKind kind)
        {
            if (kind == VariableKind.None)
            {
                return AttributeInfo.Empty;
            }

            return new AttributeInfo(VariableKindAttributeName, GetVariableKindText(kind));
        }

        protected IDisposable ArgumentTag()
        {
            return Tag(ArgumentElementName);
        }

        protected IDisposable ArrayElementAccessTag()
        {
            return Tag(ArrayElementAccessElementName);
        }

        protected IDisposable ArrayTag()
        {
            return Tag(ArrayElementName);
        }

        protected IDisposable ArrayTypeTag(int rank)
        {
            return Tag(ArrayTypeElementName, RankAttribute(rank));
        }

        protected IDisposable AssignmentTag(BinaryOperatorKind kind = BinaryOperatorKind.None)
        {
            return Tag(AssignmentElementName, BinaryOperatorAttribute(kind));
        }

        protected void BaseReferenceTag()
        {
            AppendLeafTag(BaseReferenceElementName);
        }

        protected IDisposable BinaryOperationTag(BinaryOperatorKind kind)
        {
            return Tag(BinaryOperationElementName, BinaryOperatorAttribute(kind));
        }

        protected IDisposable BlockTag()
        {
            return Tag(BlockElementName);
        }

        protected IDisposable BooleanTag()
        {
            return Tag(BooleanElementName);
        }

        protected IDisposable BoundTag()
        {
            return Tag(BoundElementName);
        }

        protected IDisposable CastTag(SpecialCastKind? specialCastKind = null)
        {
            return Tag(CastElementName, SpecialCastKindAttribute(specialCastKind));
        }

        protected IDisposable CharTag()
        {
            return Tag(CharElementName);
        }

        protected IDisposable CommentTag()
        {
            return Tag(CommentElementName);
        }

        protected IDisposable ExpressionTag()
        {
            return Tag(ExpressionElementName);
        }

        protected IDisposable ExpressionStatementTag(int lineNumber)
        {
            return Tag(ExpressionStatementElementName, LineNumberAttribute(lineNumber));
        }

        protected IDisposable LiteralTag()
        {
            return Tag(LiteralElementName);
        }

        protected IDisposable LocalTag(int lineNumber)
        {
            return Tag(LocalElementName, LineNumberAttribute(lineNumber));
        }

        protected IDisposable MethodCallTag()
        {
            return Tag(MethodCallElementName);
        }

        protected IDisposable NameTag()
        {
            return Tag(NameElementName);
        }

        protected IDisposable NameRefTag(VariableKind kind, string name = null, string fullName = null)
        {
            return Tag(NameRefElementName, VariableKindAttribute(kind), NameAttribute(name), FullNameAttribute(fullName));
        }

        protected IDisposable NewArrayTag()
        {
            return Tag(NewArrayElementName);
        }

        protected IDisposable NewClassTag()
        {
            return Tag(NewClassElementName);
        }

        protected IDisposable NewDelegateTag(string name)
        {
            return Tag(NewDelegateElementName, NameAttribute(name));
        }

        protected void NullTag()
        {
            AppendLeafTag(NullElementName);
        }

        protected IDisposable NumberTag(string typeName = null)
        {
            return Tag(NumberElementName, TypeAttribute(typeName));
        }

        protected IDisposable ParenthesesTag()
        {
            return Tag(ParenthesesElementName);
        }

        protected IDisposable QuoteTag(int lineNumber)
        {
            return Tag(QuoteElementName, LineNumberAttribute(lineNumber));
        }

        protected IDisposable StringTag()
        {
            return Tag(StringElementName);
        }

        protected void ThisReferenceTag()
        {
            AppendLeafTag(ThisReferenceElementName);
        }

        protected IDisposable TypeTag(bool? @implicit = null)
        {
            return Tag(TypeElementName, ImplicitAttribute(@implicit));
        }

        protected void LineBreak()
        {
            _builder.AppendLine();
        }

        protected void EncodedText(string text)
        {
            AppendEncoded(text);
        }

        protected int GetMark()
        {
            return _builder.Length;
        }

        protected void Rewind(int mark)
        {
            _builder.Length = mark;
        }

        protected virtual VariableKind GetVariableKind(ISymbol symbol)
        {
            if (symbol == null)
            {
                return VariableKind.Unknown;
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Event:
                case SymbolKind.Field:
                    return VariableKind.Field;
                case SymbolKind.Local:
                case SymbolKind.Parameter:
                    return VariableKind.Local;
                case SymbolKind.Method:
                    return VariableKind.Method;
                case SymbolKind.Property:
                    return VariableKind.Property;
                default:
                    throw new InvalidOperationException("Invalid symbol kind: " + symbol.Kind.ToString());
            }
        }

        protected string GetTypeName(ITypeSymbol typeSymbol)
        {
            return MetadataNameHelpers.GetMetadataName(typeSymbol);
        }

        protected int GetLineNumber(SyntaxNode node)
        {
            return Text.Lines.IndexOf(node.SpanStart);
        }

        protected void GenerateUnknown(SyntaxNode node)
        {
            using (QuoteTag(GetLineNumber(node)))
            {
                EncodedText(node.ToString());
            }
        }

        protected void GenerateName(string name)
        {
            using (NameTag())
            {
                EncodedText(name);
            }
        }

        protected void GenerateType(ITypeSymbol type, bool? @implicit = null, bool assemblyQualify = false)
        {
            if (type.TypeKind == TypeKind.Array)
            {
                var arrayType = (IArrayTypeSymbol)type;
                using var tag = ArrayTypeTag(arrayType.Rank);

                GenerateType(arrayType.ElementType, @implicit, assemblyQualify);
            }
            else
            {
                using (TypeTag(@implicit))
                {
                    var typeName = assemblyQualify
                        ? GetTypeName(type) + ", " + type.ContainingAssembly.ToDisplayString()
                        : GetTypeName(type);

                    EncodedText(typeName);
                }
            }
        }

        protected void GenerateType(SpecialType specialType)
        {
            GenerateType(SemanticModel.Compilation.GetSpecialType(specialType));
        }

        protected void GenerateNullLiteral()
        {
            using (LiteralTag())
            {
                NullTag();
            }
        }

        protected void GenerateNumber(object value, ITypeSymbol type)
        {
            using (NumberTag(GetTypeName(type)))
            {
                if (value is double d)
                {
                    // Note: use G17 for doubles to ensure that we roundtrip properly on 64-bit
                    EncodedText(d.ToString("G17", CultureInfo.InvariantCulture));
                }
                else if (value is float f)
                {
                    EncodedText(f.ToString("R", CultureInfo.InvariantCulture));
                }
                else
                {
                    EncodedText(Convert.ToString(value, CultureInfo.InvariantCulture));
                }
            }
        }

        protected void GenerateNumber(object value, SpecialType specialType)
        {
            GenerateNumber(value, SemanticModel.Compilation.GetSpecialType(specialType));
        }

        protected void GenerateChar(char value)
        {
            using (CharTag())
            {
                EncodedText(value.ToString());
            }
        }

        protected void GenerateString(string value)
        {
            using (StringTag())
            {
                EncodedText(value);
            }
        }

        protected void GenerateBoolean(bool value)
        {
            using (BooleanTag())
            {
                EncodedText(value.ToString().ToLower());
            }
        }

        protected void GenerateThisReference()
        {
            ThisReferenceTag();
        }

        protected void GenerateBaseReference()
        {
            BaseReferenceTag();
        }
    }
}
