// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

internal sealed class DocumentationIntermediateNode : ExtensionIntermediateNode
{
    public string Content { get; init; } = string.Empty;
    public bool IsValid { get; init; }

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public override void Accept(IntermediateNodeVisitor visitor)
        => AcceptExtensionNode(this, visitor);

    protected override IntermediateNode CloneNode()
        => new DocumentationIntermediateNode
        {
            Content = Content,
            IsValid = IsValid,
            IsSynthesizedHelper = IsSynthesizedHelper,
        };

    public override void WriteNode(CodeTarget target, CodeRenderingContext context)
    {
        if (!IsValid)
        {
            // The directive's diagnostics have already been reported. In particular, a
            // comment terminator in its content must not become executable C#.
            return;
        }

        if (Content.Length == 0)
        {
            return;
        }

        // Keep a leading '*' or '/' in the content separate from the comment opener.
        const string commentStart = "/**";
        const string commentEnd = "*/";
        var inlineCommentStartLength = commentStart.Length + 1;
        var writer = context.CodeWriter;
        if (context.Options.UseEnhancedLinePragma)
        {
            using (context.BuildEnhancedLinePragma(Source, characterOffset: inlineCommentStartLength))
            {
                writer.Write(commentStart).Write(" ").Write(Content).WriteLine(commentEnd);
            }
        }
        else if (Source is { LineIndex: > 0 } source && source.CharacterIndex < inlineCommentStartLength)
        {
            // For "@documentation\n{<summary>...</summary>}", the XML follows just the opening brace.
            // An inline "/** " shifts it three columns right, which ordinary pragmas cannot correct.
            // Put the opener on the preceding mapped line to preserve the XML's column.
            using (context.BuildLinePragma(source.WithLineIndex(source.LineIndex - 1)))
            {
                writer.WriteLine(commentStart);
                writer.WritePadding(offset: 0, source, context);
                context.AddSourceMappingFor(this);
                writer.Write(Content).WriteLine(commentEnd);
            }
        }
        else
        {
            using (context.BuildLinePragma(Source))
            {
                writer.WritePadding(offset: inlineCommentStartLength, Source, context);
                writer.Write(commentStart).Write(" ");
                context.AddSourceMappingFor(this);
                writer.Write(Content).WriteLine(commentEnd);
            }
        }
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(Content);
        formatter.WriteProperty(nameof(IsValid), IsValid.ToString());
    }
}
