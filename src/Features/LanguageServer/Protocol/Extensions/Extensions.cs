// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal static class Extensions
    {
        public static Uri GetURI(this Document document)
        {
            return ProtocolConversions.GetUriFromFilePath(document.FilePath);
        }

        public static Document? GetDocumentFromURI(this Solution solution, Uri fileName, bool supportsRazorFeatures = false)
        {
            // TODO: we need to normalize this. but for now, we check both absolute and local path
            //       right now, based on who calls this, solution might has "/" or "\\" as directory
            //       separator
            var documentId = solution.GetDocumentIdsWithFilePath(fileName.AbsolutePath).FirstOrDefault() ??
                             solution.GetDocumentIdsWithFilePath(fileName.LocalPath).FirstOrDefault();

            var document = solution.GetDocument(documentId);

            if (supportsRazorFeatures)
            {
                var documentPropertiesService = document?.Services.GetService<DocumentPropertiesService>();
                // Razor generated documents always have designtimeonly set to true.
                // If it's not set, don't return the document as only razor documents should be returned in razor scenarios.
                // This workaround should be removed when https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1106064/
                // is fixed (so that the razor language server is only asked about razor buffers).
                if (documentPropertiesService?.DesignTimeOnly != true)
                {
                    return null;
                }
            }

            return document;

        }

        public static async Task<int> GetPositionFromLinePositionAsync(this Document document, LinePosition linePosition, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return text.Lines.GetPosition(linePosition);
        }

        public static bool HasVisualStudioLspCapability(this ClientCapabilities clientCapabilities)
        {
            if (clientCapabilities is VSClientCapabilities vsClientCapabilities)
            {
                return vsClientCapabilities.SupportsVisualStudioExtensions;
            }

            return false;
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
                    throw new ArgumentException(string.Format("Document project language {0} is not valid", document.Project.Language));
            }
        }

        public static ClassifiedTextElement GetClassifiedText(this DefinitionItem definition)
            => new ClassifiedTextElement(definition.DisplayParts.Select(part => new ClassifiedTextRun(part.Tag.ToClassificationTypeName(), part.Text)));
    }
}
