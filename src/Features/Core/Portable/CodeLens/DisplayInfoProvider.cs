// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeLens
{
    internal static class DisplayInfoProvider
    {
        private static bool IsValueParameter(ISymbol symbol)
        {
            if (symbol is IParameterSymbol)
            {
                var method = symbol.ContainingSymbol as IMethodSymbol;
                if (method != null)
                {
                    if (method.MethodKind == MethodKind.EventAdd ||
                        method.MethodKind == MethodKind.EventRemove ||
                        method.MethodKind == MethodKind.PropertySet)
                    {
                        return symbol.Name == "value";
                    }
                }
            }

            return false;
        }

        private static Glyph? GetGlyph(ISymbol symbol)
        {
            while (true)
            {
                if (symbol == null)
                {
                    return null;
                }

                Glyph publicIcon;

                switch (symbol.Kind)
                {
                    case SymbolKind.Alias:
                        symbol = ((IAliasSymbol)symbol).Target;
                        continue;

                    case SymbolKind.Assembly:
                        return Glyph.Assembly;

                    case SymbolKind.ArrayType:
                        symbol = ((IArrayTypeSymbol)symbol).ElementType;
                        continue;

                    case SymbolKind.DynamicType:
                        return Glyph.ClassPublic;

                    case SymbolKind.Event:
                        publicIcon = Glyph.EventPublic;
                        break;

                    case SymbolKind.Field:
                        var containingType = symbol.ContainingType;
                        if (containingType != null && containingType.TypeKind == TypeKind.Enum)
                        {
                            return Glyph.EnumMember;
                        }

                        publicIcon = ((IFieldSymbol)symbol).IsConst ? Glyph.ConstantPublic : Glyph.FieldPublic;
                        break;

                    case SymbolKind.Label:
                        return Glyph.Label;

                    case SymbolKind.Local:
                        return Glyph.Local;

                    case SymbolKind.NamedType:
                    case SymbolKind.ErrorType:
                        {
                            switch (((INamedTypeSymbol)symbol).TypeKind)
                            {
                                case TypeKind.Class:
                                    publicIcon = Glyph.ClassPublic;
                                    break;

                                case TypeKind.Delegate:
                                    publicIcon = Glyph.DelegatePublic;
                                    break;

                                case TypeKind.Enum:
                                    publicIcon = Glyph.EnumPublic;
                                    break;

                                case TypeKind.Interface:
                                    publicIcon = Glyph.InterfacePublic;
                                    break;

                                case TypeKind.Module:
                                    publicIcon = Glyph.ModulePublic;
                                    break;

                                case TypeKind.Struct:
                                    publicIcon = Glyph.StructurePublic;
                                    break;

                                case TypeKind.Error:
                                    return Glyph.Error;

                                default:
                                    return Glyph.Error;
                            }

                            break;
                        }

                    case SymbolKind.Method:
                        {
                            var methodSymbol = (IMethodSymbol)symbol;

                            if (methodSymbol.MethodKind == MethodKind.UserDefinedOperator || methodSymbol.MethodKind == MethodKind.Conversion)
                            {
                                return Glyph.Operator;
                            }
                            if (methodSymbol.IsExtensionMethod || methodSymbol.MethodKind == MethodKind.ReducedExtension)
                            {
                                publicIcon = Glyph.ExtensionMethodPublic;
                            }
                            else if (methodSymbol.MethodKind == MethodKind.PropertyGet || methodSymbol.MethodKind == MethodKind.PropertySet)
                            {
                                publicIcon = Glyph.PropertyPublic;
                            }
                            else
                            {
                                publicIcon = Glyph.MethodPublic;
                            }
                        }

                        break;

                    case SymbolKind.Namespace:
                        return Glyph.Namespace;

                    case SymbolKind.NetModule:
                        return Glyph.Assembly;

                    case SymbolKind.Parameter:
                        return IsValueParameter(symbol) ? Glyph.Keyword : Glyph.Parameter;

                    case SymbolKind.PointerType:
                        symbol = ((IPointerTypeSymbol)symbol).PointedAtType;
                        continue;

                    case SymbolKind.Property:
                        {
                            var propertySymbol = (IPropertySymbol)symbol;

                            publicIcon = propertySymbol.IsWithEvents ? Glyph.FieldPublic : Glyph.PropertyPublic;
                        }

                        break;

                    case SymbolKind.RangeVariable:
                        return Glyph.RangeVariable;

                    case SymbolKind.TypeParameter:
                        return Glyph.TypeParameter;

                    default:
                        return Glyph.Error;
                }

                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Private:
                        publicIcon += Glyph.ClassPrivate - Glyph.ClassPublic;
                        break;

                    case Accessibility.Protected:
                    case Accessibility.ProtectedAndInternal:
                    case Accessibility.ProtectedOrInternal:
                        publicIcon += Glyph.ClassProtected - Glyph.ClassPublic;
                        break;

                    case Accessibility.Internal:
                        publicIcon += Glyph.ClassInternal - Glyph.ClassPublic;
                        break;
                }

                return publicIcon;
            }
        }

        public static DisplayInfo GetDisplayInfoOfEnclosingSymbol(
            Document document,
            SemanticModel semanticModel,
            int position)
        {
            IDisplayInfoLanguageServices langServices = document.GetLanguageService<IDisplayInfoLanguageServices>();
            if (langServices == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Unsupported language '{0}'", semanticModel.Language), nameof(semanticModel));
            }

            SyntaxNode node = GetEnclosingCodeElementNode(document, position, langServices);
            string longName = langServices.GetDisplayName(semanticModel, node, DisplayFormat.Long);
            // while we have all these things, also grab the actual line of code for the reference
            Tuple<string, string, string, string, string, TextSpan> result = GetReferenceText(document, position);
            string referenceText = result.Item1;
            string lineBeforeReferenceText1 = result.Item2;
            string lineBeforeReferenceText2 = result.Item3;
            string lineAfterReferenceText1 = result.Item4;
            string lineAfterReferenceText2 = result.Item5;
            TextSpan referenceSpan = result.Item6;

            ISymbol symbol = semanticModel.GetDeclaredSymbol(node);
            var glyph = GetGlyph(symbol);

            return new DisplayInfo(longName,
                                   semanticModel.Language,
                                   glyph,
                                   referenceText,
                                   referenceSpan.Start,
                                   referenceSpan.Length,
                                   lineBeforeReferenceText1,
                                   lineBeforeReferenceText2,
                                   lineAfterReferenceText1,
                                   lineAfterReferenceText2);
        }

        /// <summary>
        /// given a document and the position of a reference, return the line of code (leading + trailing space trimmed) that referenced the reference.
        /// also returns as an out param the exact span of text that is the reference within the line of code (can be used for highlighting)
        /// </summary>
        /// <param name="document">the document containing the reference</param>
        /// <param name="position">the position of the reference</param>
        /// <returns>tuple of the the full line of code that used the reference, and the span of text that is the reference in the returned line of code</returns>
        private static Tuple<string, string, string, string, string, TextSpan> GetReferenceText(Document document, int position)
        {
            SyntaxToken token = FindTokenAtPosition(document, position);

            // get the full line of source text on the line that contains this position
            Task<SourceText> docText = document.GetTextAsync();
            SourceText text = docText.Result;
            // get the actual span of text for the line containing reference
            TextLine textLine = text.Lines.GetLineFromPosition(position);
            // turn the span from document relative to line relative
            int spanStart = token.Span.Start - textLine.Span.Start;
            string line = textLine.ToString();

            string beforeLine1 = string.Empty;
            if (textLine.LineNumber > 0)
            {
                TextLine beforeTextLine = text.Lines[textLine.LineNumber - 1];
                beforeLine1 = beforeTextLine.ToString();
            }

            string beforeLine2 = string.Empty;
            if (textLine.LineNumber - 1 > 0)
            {
                TextLine beforeTextLine = text.Lines[textLine.LineNumber - 2];
                beforeLine2 = beforeTextLine.ToString();
            }

            string afterLine1 = string.Empty;
            if (textLine.LineNumber < text.Lines.Count - 1)
            {
                TextLine afterTextLine = text.Lines[textLine.LineNumber + 1];
                afterLine1 = afterTextLine.ToString();
            }

            string afterLine2 = string.Empty;
            if (textLine.LineNumber + 1 < text.Lines.Count - 1)
            {
                TextLine afterTextLine = text.Lines[textLine.LineNumber + 2];
                afterLine2 = afterTextLine.ToString();
            }

            return new Tuple<string, string, string, string, string, TextSpan>(line.TrimEnd(),
                                                               beforeLine1.TrimEnd(),
                                                               beforeLine2.TrimEnd(),
                                                               afterLine1.TrimEnd(),
                                                               afterLine2.TrimEnd(),
                                                               new TextSpan(spanStart, token.Span.Length));
        }

        /// <summary>
        /// find the token at a given postion in the document
        /// </summary>
        /// <param name="document">the document containing the reference</param>
        /// <param name="position">the position of the reference</param>
        /// <returns>the syntax token at the specified location</returns>
        private static SyntaxToken FindTokenAtPosition(Document document, int position)
        {
            Task<SyntaxNode> taskGetSyntaxRoot = document.GetSyntaxRootAsync();
            SyntaxNode root = taskGetSyntaxRoot.Result;

            // IncludeTrivia when searching for token as roslyn returns locations from xml documentation as well
            SyntaxToken token;
            try
            {
                token = root.FindToken(position, true);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Could not find position '{0}' in document: {1}", position, document.FilePath), e);
            }

            return token;
        }

        private static SyntaxNode GetEnclosingCodeElementNode(Document document, int position, IDisplayInfoLanguageServices langServices)
        {
            SyntaxToken token = FindTokenAtPosition(document, position);

            SyntaxNode node = token.Parent;
            while (node != null)
            {
                if (langServices.IsDocumentationComment(node))
                {
                    IStructuredTriviaSyntax structuredTriviaSyntax = (IStructuredTriviaSyntax)node;
                    SyntaxTrivia parentTrivia = structuredTriviaSyntax.ParentTrivia;
                    node = parentTrivia.Token.Parent;
                }
                else if (langServices.IsDeclaration(node) ||
                         langServices.IsDirectiveOrImport(node) ||
                         langServices.IsGlobalAttribute(node))
                {
                    break;
                }
                else
                {
                    node = node.Parent;
                }
            }

            if (node == null)
            {
                node = token.Parent;
            }

            return langServices.GetDisplayNode(node);
        }
    }
}
