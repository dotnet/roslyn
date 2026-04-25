// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Razor.Tooltip;
using Roslyn.Text.Adornments;
using Xunit;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal static class HoverAssertions
{
    public static void VerifyContents(this LspHover hover, Action<object?> verifier)
    {
        var vsHover = Assert.IsType<VSInternalHover>(hover);
        verifier(vsHover.RawContent);
    }

    public static Action<object?> Container(params ImmutableArray<Action<object?>> elements)
        => o =>
        {
            Assert.NotNull(o);
            var container = Assert.IsType<ContainerElement>(o);

            var allElements = container.Elements.ToArray();
            Assert.Equal(elements.Length, allElements.Length);

            for (var i = 0; i < elements.Length; i++)
            {
                elements[i](allElements[i]);
            }
        };

    public static Action<object?> Image
        => o =>
        {
            Assert.NotNull(o);
            Assert.IsType<ImageElement>(o);
        };

    public static Action<object?> ClassifiedText(params ImmutableArray<Action<ClassifiedTextRun>> runs)
        => o =>
        {
            Assert.NotNull(o);
            var classifiedText = Assert.IsType<ClassifiedTextElement>(o);

            var allRuns = classifiedText.Runs.ToArray();
            Assert.Equal(runs.Length, allRuns.Length);

            for (var i = 0; i < runs.Length; i++)
            {
                runs[i](allRuns[i]);
            }
        };

    public static Action<ClassifiedTextRun> Run(string text, string? classificationTypeName = null)
        => run =>
        {
            if (classificationTypeName is not null)
            {
                Assert.Equal(classificationTypeName, run.ClassificationTypeName);
            }

            Assert.Equal(text, run.Text);
        };

    public static Action<ClassifiedTextRun> ClassName(string text)
        => Run(text, ClassificationTypeNames.ClassName);

    public static Action<ClassifiedTextRun> Keyword(string text)
        => Run(text, ClassificationTypeNames.Keyword);

    public static Action<ClassifiedTextRun> Namespace(string text)
        => Run(text, ClassificationTypeNames.NamespaceName);

    public static Action<ClassifiedTextRun> LocalName(string text)
        => Run(text, ClassificationTypeNames.LocalName);

    public static Action<ClassifiedTextRun> PropertyName(string text)
        => Run(text, ClassificationTypeNames.PropertyName);

    public static Action<ClassifiedTextRun> Punctuation(string text)
        => Run(text, ClassificationTypeNames.Punctuation);

    public static Action<ClassifiedTextRun> Text(string text)
        => Run(text, ClassificationTypeNames.Text);

    public static Action<ClassifiedTextRun> Type(string text)
        => Run(text, ClassifiedTagHelperTooltipFactory.TypeClassificationName);

    public static Action<ClassifiedTextRun> WhiteSpace(string text)
        => Run(text, ClassificationTypeNames.WhiteSpace);

    public static Action<object?> HorizontalRule
        => o => { };
}
