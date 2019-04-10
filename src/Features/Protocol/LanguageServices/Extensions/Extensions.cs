// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using VSSymbolKind = Microsoft.VisualStudio.LanguageServer.Protocol.SymbolKind;
using VSLocation = Microsoft.VisualStudio.LanguageServer.Protocol.Location;

namespace Microsoft.CodeAnalysis.Protocol.LanguageServices.Extensions
{
    internal static class Extensions
    {
        public static Document GetDocument(this Solution solution, Uri fileName)
        {
            // TODO: we need to normalize this. but for now, we check both absolute and local path
            //       right now, based on who calls this, solution might has "/" or "\\" as directory
            //       separator
            var documentId = solution.GetDocumentIdsWithFilePath(fileName.AbsolutePath).FirstOrDefault() ??
                             solution.GetDocumentIdsWithFilePath(fileName.LocalPath).FirstOrDefault();
            if (documentId == null)
            {
                return null;
            }

            return solution.GetDocument(documentId);
        }

        public static async Task<int> GetPositionAsync(this Document document, LinePosition linePosition, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return text.Lines.GetPosition(linePosition);
        }

        public static string GetMarkdownLanguageName(this Document document)
        {
            switch (document.Project.Language)
            {
                case LanguageNames.CSharp:
                    return "csharp";
                case LanguageNames.VisualBasic:
                    return "vb";
                case LanguageNames.FSharp:
                    return "fsharp";
                case "TypeScript":
                    return "typescript";
                default:
                    return "csharp";
            }
        }

        public static DocumentHighlightKind ToDocumentHighlightKind(this HighlightSpanKind kind)
        {
            switch (kind)
            {
                case HighlightSpanKind.Reference:
                    return DocumentHighlightKind.Read;
                case HighlightSpanKind.WrittenReference:
                    return DocumentHighlightKind.Write;
                default:
                    return DocumentHighlightKind.Text;
            }
        }

        public static VSLocation ToLocation(this Range range, string uriString)
        {
            return new VSLocation()
            {
                Range = range,
                Uri = new Uri(uriString)
            };
        }

        public static VSSymbolKind GetKind(this string kind)
        {
            if (Enum.TryParse<VSSymbolKind>(kind, out var symbolKind))
            {
                return symbolKind;
            }

            switch (kind)
            {
                case "Stucture":
                    return VSSymbolKind.Struct;
                case "Delegate":
                    return VSSymbolKind.Function;
                default:
                    return VSSymbolKind.Object;
            }
        }
    }

#pragma warning disable SA1124, SA1201, SA1602, SA1516, CA1815
    #region Remove this once we have new sign bit

    #endregion
#pragma warning restore SA1124, SA1201, SA1602, SA1516,CA1815
}
