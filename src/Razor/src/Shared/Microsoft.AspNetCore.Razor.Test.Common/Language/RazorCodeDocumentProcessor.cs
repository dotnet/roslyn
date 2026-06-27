// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorCodeDocumentProcessor
{
    public RazorProjectEngine ProjectEngine { get; }
    public RazorCodeDocument CodeDocument { get; private set; }

    private RazorCodeDocumentProcessor(RazorProjectEngine projectEngine, RazorCodeDocument codeDocument)
    {
        ProjectEngine = projectEngine;
        CodeDocument = codeDocument;
    }

    public static RazorCodeDocumentProcessor From(RazorProjectEngine projectEngine, RazorCodeDocument codeDocument)
        => new(projectEngine, codeDocument);

    public RazorCodeDocumentProcessor ExecutePhasesThrough<T>()
        where T : IRazorEnginePhase
    {
        CodeDocument = ProjectEngine.ExecutePhasesThrough<T>(CodeDocument);

        return this;
    }

    /// <summary>
    /// Executes the engine phases up to and including <typeparamref name="TUntil"/>, but
    /// skips any phase whose runtime type is in <paramref name="skipPhases"/>. Useful for
    /// tests that need a non-contiguous slice of the pipeline -- e.g. lower IR then resolve
    /// tag helpers without running the document classifier in between.
    /// </summary>
    public RazorCodeDocumentProcessor ExecutePhasesThroughExcept<TUntil>(params Type[] skipPhases)
        where TUntil : IRazorEnginePhase
    {
        var hashSet = new System.Collections.Generic.HashSet<Type>(skipPhases);
        foreach (var phase in ProjectEngine.Engine.Phases)
        {
            if (!hashSet.Contains(phase.GetType()))
            {
                CodeDocument = phase.Execute(CodeDocument);
            }

            if (phase is TUntil)
            {
                break;
            }
        }

        return this;
    }

    public RazorCodeDocumentProcessor ExecutePass<T>()
        where T : IntermediateNodePassBase, new()
    {
        ProjectEngine.ExecutePass<T>(CodeDocument);

        return this;
    }

    public RazorCodeDocumentProcessor ExecutePass<T>(Func<T> passFactory)
        where T : IntermediateNodePassBase
    {
        ProjectEngine.ExecutePass<T>(CodeDocument, passFactory);

        return this;
    }

    public DocumentIntermediateNode GetDocumentNode()
    {
        var documentNode = CodeDocument.GetDocumentNode();
        Assert.NotNull(documentNode);

        return documentNode;
    }
}
