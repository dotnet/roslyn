// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using VSLocation = Microsoft.VisualStudio.LanguageServer.Protocol.Location;
using VSSymbolKind = Microsoft.VisualStudio.LanguageServer.Protocol.SymbolKind;

namespace Microsoft.CodeAnalysis.Protocol.LanguageServices.Extensions
{
    internal static class RoslynToProtocolExtensions
    {
        public static Uri ToUri(this Document document)
        {
            return new Uri(document.FilePath);
        }

        public static Position ToPosition(this LinePosition linePosition)
        {
            return new Position { Line = linePosition.Line, Character = linePosition.Character };
        }

        public static Range ToRange(this LinePositionSpan linePositionSpan)
        {
            return new Range { Start = linePositionSpan.Start.ToPosition(), End = linePositionSpan.End.ToPosition() };
        }

        public static Range ToRange(this TextSpan textSpan, SourceText text)
        {
            var linePosSpan = text.Lines.GetLinePositionSpan(textSpan);
            return linePosSpan.ToRange();
        }

        public static Task<VSLocation> ToLocationAsync(this DocumentSpan documentSpan, CancellationToken cancellationToken)
        {
            return documentSpan.Document.ToLocationAsync(documentSpan.SourceSpan, cancellationToken);
        }

        public static async Task<VSLocation> ToLocationAsync(this Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var location = new VSLocation
            {
                Uri = document.ToUri(),
                Range = span.ToRange(text),
            };

            return location;
        }

        public static VSSymbolKind ToSymbolKind(this Glyph glyph)
        {
            var glyphString = glyph.ToString().Replace("Public", string.Empty)
                                              .Replace("Protected", string.Empty)
                                              .Replace("Private", string.Empty)
                                              .Replace("Internal", string.Empty);

            if (Enum.TryParse<VSSymbolKind>(glyphString, out var symbolKind))
            {
                return symbolKind;
            }

            switch (glyph)
            {
                case Glyph.Assembly:
                case Glyph.BasicProject:
                case Glyph.CSharpProject:
                case Glyph.NuGet:
                    return VSSymbolKind.Package;
                case Glyph.BasicFile:
                case Glyph.CSharpFile:
                    return VSSymbolKind.File;
                case Glyph.DelegatePublic:
                case Glyph.DelegateProtected:
                case Glyph.DelegatePrivate:
                case Glyph.DelegateInternal:
                    return VSSymbolKind.Function;
                case Glyph.ExtensionMethodPublic:
                case Glyph.ExtensionMethodProtected:
                case Glyph.ExtensionMethodPrivate:
                case Glyph.ExtensionMethodInternal:
                    return VSSymbolKind.Method;
                case Glyph.Local:
                case Glyph.Parameter:
                case Glyph.RangeVariable:
                case Glyph.Reference:
                    return VSSymbolKind.Variable;
                case Glyph.StructurePublic:
                case Glyph.StructureProtected:
                case Glyph.StructurePrivate:
                case Glyph.StructureInternal:
                    return VSSymbolKind.Struct;
                default:
                    return VSSymbolKind.Object;
            }
        }
    }
}
