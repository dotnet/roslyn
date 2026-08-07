// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(GoToDefinitionHandler)), Shared]
[Method(LSP.Methods.TextDocumentDefinitionName)]
internal sealed class GoToDefinitionHandler : AbstractGoToDefinitionHandler
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public GoToDefinitionHandler(IMetadataAsSourceFileService metadataAsSourceFileService, IGlobalOptionService globalOptions)
        : base(metadataAsSourceFileService, globalOptions)
    {
    }

    public override async Task<LSP.Location[]?> HandleRequestAsync(LSP.TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
    {
        if (context.Document is { } document &&
            await TryGetFileLevelDirectiveLocationAsync(document, request.Position, cancellationToken).ConfigureAwait(false) is { } location)
        {
            return [location];
        }

        return await GetDefinitionAsync(request, forSymbolType: false, context, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<LSP.Location?> TryGetFileLevelDirectiveLocationAsync(
        Document document, LSP.Position position, CancellationToken cancellationToken)
    {
        if (document.Project.Language != LanguageNames.CSharp ||
            document.FilePath is null ||
            document.Project.ParseOptions?.Features.ContainsKey("FileBasedProgram") != true)
        {
            return null;
        }

        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var absolutePosition = sourceText.Lines.GetPosition(ProtocolConversions.PositionToLinePosition(position));
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root.FindToken(absolutePosition, findInsideTrivia: true);
        if (token.Parent is not IgnoredDirectiveTriviaSyntax { Content.RawKind: (int)SyntaxKind.StringLiteralToken } directive)
            return null;

        var content = directive.Content;
        var contentText = content.Text;
        var directiveNameStart = SkipWhitespace(contentText, 0);
        var directiveNameEnd = SkipNonWhitespace(contentText, directiveNameStart);
        var pathStart = SkipWhitespace(contentText, directiveNameEnd);
        var pathEnd = TrimTrailingWhitespace(contentText, pathStart);
        if (pathStart == pathEnd)
            return null;

        var directiveName = contentText.AsSpan(directiveNameStart, directiveNameEnd - directiveNameStart);
        var isProjectDirective = directiveName.SequenceEqual("project".AsSpan());
        if (!isProjectDirective &&
            !directiveName.SequenceEqual("ref".AsSpan()) &&
            !directiveName.SequenceEqual("include".AsSpan()))
        {
            return null;
        }

        var pathSpan = TextSpan.FromBounds(content.SpanStart + pathStart, content.SpanStart + pathEnd);
        if (absolutePosition < pathSpan.Start || absolutePosition > pathSpan.End)
            return null;

        var sourceDirectory = Path.GetDirectoryName(document.FilePath);
        if (sourceDirectory is null)
            return null;

        try
        {
            var directivePath = contentText.Substring(pathStart, pathEnd - pathStart).Replace('\\', '/');
            var combinedPath = Path.Combine(sourceDirectory, directivePath);
            if (!PathUtilities.IsAbsolute(combinedPath))
                return null;

            var normalizedPath = Path.GetFullPath(combinedPath);

            if (isProjectDirective && Directory.Exists(normalizedPath))
            {
                var projectFiles = new DirectoryInfo(normalizedPath).GetFiles("*proj");
                if (projectFiles.Length != 1)
                    return null;

                normalizedPath = projectFiles[0].FullName;
            }

            if (!File.Exists(normalizedPath))
                return null;

            return new()
            {
                DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(normalizedPath),
                Range = new()
                {
                    Start = new(),
                    End = new(),
                },
            };
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (SecurityException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }

        static int SkipWhitespace(string text, int start)
        {
            while (start < text.Length && char.IsWhiteSpace(text[start]))
                start++;

            return start;
        }

        static int SkipNonWhitespace(string text, int start)
        {
            while (start < text.Length && !char.IsWhiteSpace(text[start]))
                start++;

            return start;
        }

        static int TrimTrailingWhitespace(string text, int start)
        {
            var end = text.Length;
            while (end > start && char.IsWhiteSpace(text[end - 1]))
                end--;

            return end;
        }
    }
}
