// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class ClassifiedSpanWriter(TextWriter writer, RazorSyntaxTree syntaxTree, bool validateSpanEditHandlers)
{
    public virtual void Visit()
    {
        var classifiedSpans = syntaxTree.GetClassifiedSpans();
        foreach (var span in classifiedSpans)
        {
            VisitClassifiedSpan(span);
            WriteNewLine();
        }
    }

    public virtual void VisitClassifiedSpan(ClassifiedSpanInternal span)
    {
        WriteClassifiedSpan(span);
    }

    protected void WriteClassifiedSpan(ClassifiedSpanInternal span)
    {
        Write($"{span.SpanKind} span at {span.Span}");
        if (validateSpanEditHandlers)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            Write($" (Accepts:{span.AcceptedCharacters})");
#pragma warning restore CS0618 // Type or member is obsolete
        }

        WriteSeparator();
        Write($"Parent: {span.BlockKind} block at {span.BlockSpan}");
    }

    protected void WriteSeparator()
    {
        Write(" - ");
    }

    protected void WriteNewLine()
    {
        writer.WriteLine();
    }

    protected void Write(object value)
    {
        writer.Write(value);
    }
}
