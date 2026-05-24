// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class RazorParser
{
    public RazorParser()
        : this(RazorParserOptions.Default)
    {
    }

    public RazorParser(RazorParserOptions options)
    {
        ArgHelper.ThrowIfNull(options);

        Options = options;
    }

    public RazorParserOptions Options { get; }

    public virtual RazorSyntaxTree Parse(RazorSourceDocument source, CancellationToken cancellationToken = default)
    {
        ArgHelper.ThrowIfNull(source);

        using var context = new ParserContext(source, Options, cancellationToken);
        using var codeParser = new CSharpCodeParser(Options.Directives, context);
        using var markupParser = new HtmlMarkupParser(context);

        codeParser.HtmlParser = markupParser;
        markupParser.CodeParser = codeParser;

        var root = markupParser.ParseDocument().CreateRed();
        var diagnostics = context.ErrorSink.GetErrorsAndClear();

        return new RazorSyntaxTree(root, source, diagnostics, Options);
    }
}
