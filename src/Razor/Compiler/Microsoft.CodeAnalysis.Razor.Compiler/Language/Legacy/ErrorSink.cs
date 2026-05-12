// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

/// <summary>
/// Used to manage <see cref="RazorDiagnostic">RazorDiagnostics</see> encountered during the Razor parsing phase.
/// </summary>
internal sealed class ErrorSink : IDisposable
{
    private ImmutableArray<RazorDiagnostic>.Builder? _errors;

    public void Dispose()
    {
        var errors = _errors;

        if (errors is not null)
        {
            ArrayBuilderPool<RazorDiagnostic>.Default.Return(errors);
            _errors = null;
        }
    }

    public ImmutableArray<RazorDiagnostic> GetErrorsAndClear()
    {
        var errors = _errors;
        if (errors is null)
        {
            return [];
        }

        var result = errors.ToImmutableAndClear();
        ArrayBuilderPool<RazorDiagnostic>.Default.Return(errors);
        _errors = null;

        return result;
    }

    /// <summary>
    /// Tracks the given <paramref name="error"/>.
    /// </summary>
    /// <param name="error">The <see cref="RazorDiagnostic"/> to track.</param>
    public void OnError(RazorDiagnostic error)
    {
        var errors = _errors ??= ArrayBuilderPool<RazorDiagnostic>.Default.Get();
        errors.Add(error);
    }
}
