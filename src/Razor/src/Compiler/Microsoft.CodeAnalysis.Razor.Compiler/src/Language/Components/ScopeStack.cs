// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Components;

/// <summary>
/// Keeps track of the nesting of elements/containers while writing out the C# source code
/// for a component. This allows us to detect mismatched start/end tags, as well as inject
/// additional C# source to capture component descendants in a lambda.
/// </summary>
internal sealed partial class ScopeStack
{
    private Entry _current;
    private Stack<Entry>? _stack;

    public BuilderVariableName BuilderVariableName => _current.BuilderVariableName;
    public RenderModeVariableName RenderModeVariableName => _current.RenderModeVariableName;
    public FormNameVariableName FormNameVariableName => _current.FormNameVariableName;

    public int Depth => _stack?.Count ?? 0;

    public ScopeStack()
    {
        _current = Entry.CreateFirst();
    }

    public readonly ref struct Scope(ScopeStack instance)
    {
        public void Dispose()
        {
            instance.CloseScope();
        }
    }

    public Scope OpenComponentScope(CodeRenderingContext context, string? parameterName)
    {
        // Writes code that looks like:
        //
        // ((__builder) => { ... })
        // OR
        // ((context) => (__builder) => { ... })

        if (parameterName != null)
        {
            context.CodeWriter.WriteLambdaHeader(parameterName);
        }

        return OpenScope(context);
    }

    public Scope OpenTemplateScope(CodeRenderingContext context)
        => OpenScope(context);

    private Scope OpenScope(CodeRenderingContext context)
    {
        _stack ??= new();
        _stack.Push(_current);

        _current = _current.Next(context);

        return new(this);
    }

    private void CloseScope()
    {
        Debug.Assert(_stack is not null && _stack.Count > 0);

        _current.Dispose();
        _current = _stack.Pop();
    }

    public void IncrementRenderMode() => _current.IncrementRenderMode();

    public void IncrementFormName() => _current.IncrementFormName();
}
