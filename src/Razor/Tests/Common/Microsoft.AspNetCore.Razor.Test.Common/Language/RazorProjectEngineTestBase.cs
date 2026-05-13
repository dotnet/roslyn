// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorProjectEngineTestBase
{
    private RazorProjectEngine? _projectEngine;

    /// <summary>
    ///  A default <see cref="RazorProjectEngine"/> instance that is configured with
    ///  <see cref="ConfigureProjectEngine(RazorProjectEngineBuilder)"/>.
    /// </summary>
    protected RazorProjectEngine ProjectEngine
    {
        get
        {
            return _projectEngine ?? InterlockedOperations.Initialize(ref _projectEngine, CreateProjectEngine());

            RazorProjectEngine CreateProjectEngine()
            {
                return RazorProjectEngine.Create(Configuration, RazorProjectFileSystem.Empty, ConfigureProjectEngine);
            }
        }
    }

    protected RazorConfiguration Configuration { get; }

    protected abstract RazorLanguageVersion Version { get; }

    protected RazorProjectEngineTestBase()
    {
        Configuration = new RazorConfiguration(Version, "test", Extensions: []);
    }

    /// <summary>
    ///  Override to configure the <see cref="RazorProjectEngine"/>s produced by this test class.
    /// </summary>
    ///
    /// <remarks>
    ///  This is called to configure the default <see cref="RazorProjectEngine"/> returned by <see cref="ProjectEngine"/>
    ///  and any <see cref="RazorProjectEngine"/> created by <see cref="CreateProjectEngine(Action{RazorProjectEngineBuilder})"/>.
    /// </remarks>
    protected virtual void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
    }

    /// <summary>
    ///  Override to configure the <see cref="RazorCodeDocumentProcessor"/>s produced by this test class.
    /// </summary>
    ///
    /// <remarks>
    ///  This can be used to ensure that the <see cref="RazorCodeDocument"/> is initially processed in particular way
    ///  by executing compiler phases or passes.
    /// </remarks>
    protected virtual void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
    }

    /// <summary>
    ///  Creates a new <see cref="RazorCodeDocumentProcessor"/> for the given <see cref="RazorCodeDocument"/> targeting
    ///  the default <see cref="RazorProjectEngine"/>.
    /// </summary>
    ///
    /// <remarks>
    ///  Override <see cref="ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor)"/> to configure the
    ///  <see cref="RazorCodeDocumentProcessor"/> that is returned. This can be used to ensure that
    ///  the <see cref="RazorCodeDocument"/> is initially processed in a particular way by executing compiler phases
    ///  or passes.
    /// </remarks>
    protected RazorCodeDocumentProcessor CreateCodeDocumentProcessor(RazorCodeDocument codeDocument)
        => CreateCodeDocumentProcessor(ProjectEngine, codeDocument);

    /// <summary>
    ///  Creates a new <see cref="RazorCodeDocumentProcessor"/> for the given <see cref="RazorCodeDocument"/> and
    ///  <see cref="RazorProjectEngine"/>.
    /// </summary>
    ///
    /// <remarks>
    ///  Override <see cref="ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor)"/> to configure the
    ///  <see cref="RazorCodeDocumentProcessor"/> that is returned. This can be used to ensure that
    ///  the <see cref="RazorCodeDocument"/> is processed in a particular way, such as executing compiler phases
    ///  or passes.
    /// </remarks>
    protected RazorCodeDocumentProcessor CreateCodeDocumentProcessor(RazorProjectEngine projectEngine, RazorCodeDocument codeDocument)
    {
        var processor = RazorCodeDocumentProcessor.From(projectEngine, codeDocument);
        ConfigureCodeDocumentProcessor(processor);

        return processor;
    }

    /// <summary>
    ///  Creates a new <see cref="RazorProjectEngine"/> configured with <see cref="ConfigureProjectEngine(RazorProjectEngineBuilder)"/>
    ///  and the delegate provided by <paramref name="configure"/>.
    /// </summary>
    /// 
    /// <param name="configure">
    ///  A delegate that provides additional configuration for the <see cref="RazorProjectEngine"/>. 
    /// </param>
    protected RazorProjectEngine CreateProjectEngine(Action<RazorProjectEngineBuilder> configure)
        => RazorProjectEngine.Create(Configuration, RazorProjectFileSystem.Empty, builder =>
        {
            // Ensure that tests are using the Roslyn tokenizer by default.
            builder.ConfigureParserOptions(builder =>
            {
                builder.UseRoslynTokenizer = true;
            });

            ConfigureProjectEngine(builder);
            configure.Invoke(builder);
        });

    /// <summary>
    ///  Finds the first descendant node of the specified type using a depth-first search.
    /// </summary>
    protected static T FindDescendant<T>(IntermediateNode root) where T : IntermediateNode
    {
        var stack = new System.Collections.Generic.Stack<IntermediateNode>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current is T match)
            {
                return match;
            }

            for (var i = current.Children.Count - 1; i >= 0; i--)
            {
                stack.Push(current.Children[i]);
            }
        }

        throw new InvalidOperationException("Not found: " + typeof(T).Name);
    }
}
