// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Adornments;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal static class ProtocolConversions
    {
        public static LinePosition PositionToLinePosition(LSP.Position position)
        {
            return new LinePosition(position.Line, position.Character);
        }

        public static LinePositionSpan RangeToLinePositionSpan(LSP.Range range)
        {
            return new LinePositionSpan(PositionToLinePosition(range.Start), PositionToLinePosition(range.End));
        }

        public static TextSpan RangeToTextSpan(LSP.Range range, SourceText text)
        {
            var linePositionSpan = RangeToLinePositionSpan(range);
            return text.Lines.GetTextSpan(linePositionSpan);
        }

        public static LSP.TextEdit TextChangeToTextEdit(TextChange textChange, SourceText text)
        {
            return new LSP.TextEdit
            {
                NewText = textChange.NewText,
                Range = TextSpanToRange(textChange.Span, text)
            };
        }

        public static LSP.Position LinePositionToPosition(LinePosition linePosition)
        {
            return new LSP.Position { Line = linePosition.Line, Character = linePosition.Character };
        }

        public static LSP.Range LinePositionToRange(LinePositionSpan linePositionSpan)
        {
            return new LSP.Range { Start = LinePositionToPosition(linePositionSpan.Start), End = LinePositionToPosition(linePositionSpan.End) };
        }

        public static LSP.Range TextSpanToRange(TextSpan textSpan, SourceText text)
        {
            var linePosSpan = text.Lines.GetLinePositionSpan(textSpan);
            return LinePositionToRange(linePosSpan);
        }

        public static Task<LSP.Location> DocumentSpanToLocationAsync(DocumentSpan documentSpan, CancellationToken cancellationToken)
        {
            return TextSpanToLocationAsync(documentSpan.Document, documentSpan.SourceSpan, cancellationToken);
        }

        public static async Task<LSP.LocationWithText> DocumentSpanToLocationWithTextAsync(DocumentSpan documentSpan, ClassifiedTextElement text, CancellationToken cancellationToken)
        {
            var sourceText = await documentSpan.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var locationWithText = new LSP.LocationWithText
            {
                Uri = documentSpan.Document.GetURI(),
                Range = TextSpanToRange(documentSpan.SourceSpan, sourceText),
                Text = text
            };

            return locationWithText;
        }

        public static LSP.Location RangeToLocation(LSP.Range range, string uriString)
        {
            return new LSP.Location()
            {
                Range = range,
                Uri = new Uri(uriString)
            };
        }

        public static async Task<LSP.Location> TextSpanToLocationAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            return TextSpanToLocation(span, text, document.GetURI());
        }

        public static LSP.Location TextSpanToLocation(TextSpan span, SourceText text, Uri documentUri)
        {
            var location = new LSP.Location
            {
                Uri = documentUri,
                Range = TextSpanToRange(span, text),
            };

            return location;
        }

        public static LSP.Location TextSpanToLocationWithText(TextSpan span, SourceText text, Uri documentUri)
        {
            var location = new LSP.LocationWithText
            {
                Uri = documentUri,
                Range = TextSpanToRange(span, text),
                Text = text
            };

            return location;
        }

        public static LSP.DiagnosticSeverity DiagnosticSeverityToLspDiagnositcSeverity(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Hidden:
                    return LSP.DiagnosticSeverity.Hint;
                case DiagnosticSeverity.Info:
                    return LSP.DiagnosticSeverity.Information;
                case DiagnosticSeverity.Warning:
                    return LSP.DiagnosticSeverity.Warning;
                case DiagnosticSeverity.Error:
                    return LSP.DiagnosticSeverity.Error;
                default:
                    return LSP.DiagnosticSeverity.Hint;
            }
        }

        public static LSP.SymbolKind NavigateToKindToSymbolKind(string kind)
        {
            if (Enum.TryParse<LSP.SymbolKind>(kind, out var symbolKind))
            {
                return symbolKind;
            }

            // TODO - Define conversion from NavigateToItemKind to LSP Symbol kind
            switch (kind)
            {
                case NavigateToItemKind.EnumItem:
                    return LSP.SymbolKind.EnumMember;
                case NavigateToItemKind.Structure:
                    return LSP.SymbolKind.Struct;
                case NavigateToItemKind.Delegate:
                    return LSP.SymbolKind.Function;
                default:
                    return LSP.SymbolKind.Object;
            }
        }

        public static LSP.DocumentHighlightKind HighlightSpanKindToDocumentHighlightKind(HighlightSpanKind kind)
        {
            switch (kind)
            {
                case HighlightSpanKind.Reference:
                    return LSP.DocumentHighlightKind.Read;
                case HighlightSpanKind.WrittenReference:
                    return LSP.DocumentHighlightKind.Write;
                default:
                    return LSP.DocumentHighlightKind.Text;
            }
        }

        public static LSP.SymbolKind GlyphToSymbolKind(Glyph glyph)
        {
            // Glyph kinds have accessibility modifiers in their name, e.g. ClassPrivate.
            // Remove the accessibility modifier and try to convert to LSP symbol kind.
            var glyphString = glyph.ToString().Replace(nameof(Accessibility.Public), string.Empty)
                                              .Replace(nameof(Accessibility.Protected), string.Empty)
                                              .Replace(nameof(Accessibility.Private), string.Empty)
                                              .Replace(nameof(Accessibility.Internal), string.Empty);

            if (Enum.TryParse<LSP.SymbolKind>(glyphString, out var symbolKind))
            {
                return symbolKind;
            }

            switch (glyph)
            {
                case Glyph.Assembly:
                case Glyph.BasicProject:
                case Glyph.CSharpProject:
                case Glyph.NuGet:
                    return LSP.SymbolKind.Package;
                case Glyph.BasicFile:
                case Glyph.CSharpFile:
                    return LSP.SymbolKind.File;
                case Glyph.DelegatePublic:
                case Glyph.DelegateProtected:
                case Glyph.DelegatePrivate:
                case Glyph.DelegateInternal:
                    return LSP.SymbolKind.Function;
                case Glyph.ExtensionMethodPublic:
                case Glyph.ExtensionMethodProtected:
                case Glyph.ExtensionMethodPrivate:
                case Glyph.ExtensionMethodInternal:
                    return LSP.SymbolKind.Method;
                case Glyph.Local:
                case Glyph.Parameter:
                case Glyph.RangeVariable:
                case Glyph.Reference:
                    return LSP.SymbolKind.Variable;
                case Glyph.StructurePublic:
                case Glyph.StructureProtected:
                case Glyph.StructurePrivate:
                case Glyph.StructureInternal:
                    return LSP.SymbolKind.Struct;
                default:
                    return LSP.SymbolKind.Object;
            }
        }
    }
}
