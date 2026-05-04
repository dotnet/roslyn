// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Roslyn.LanguageServer.Protocol;

namespace Roslyn.Text.Adornments;

[JsonConverter(typeof(ClassifiedTextElementConverter))]
internal sealed class ClassifiedTextElement
{
    public const string TextClassificationTypeName = "text";

    public IEnumerable<ClassifiedTextRun> Runs { get; }

    public ClassifiedTextElement(params ClassifiedTextRun[] runs)
    {
        Runs = runs?.ToImmutableList() ?? throw new ArgumentNullException("runs");
    }

    public ClassifiedTextElement(IEnumerable<ClassifiedTextRun> runs)
    {
        Runs = runs?.ToImmutableList() ?? throw new ArgumentNullException("runs");
    }

    public static ClassifiedTextElement CreateHyperlink(string text, string tooltip, Action navigationAction)
    {
        //Requires.NotNull(text, "text");
        //Requires.NotNull(navigationAction, "navigationAction");
        return new ClassifiedTextElement(new ClassifiedTextRun("text", text, navigationAction: navigationAction, tooltip: tooltip));
    }

    public static ClassifiedTextElement CreatePlainText(string text)
    {
        //Requires.NotNull(text, "text");
        return new ClassifiedTextElement(new ClassifiedTextRun("text", text));
    }
}
