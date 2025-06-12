// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;

namespace Microsoft.CodeAnalysis.SemanticSearch;

/// <summary>
/// The result of Semantic Search query execution.
/// </summary>
/// <param name="ErrorMessage">An error message if the execution failed.</param>
/// <param name="ErrorMessageArgs">
/// Arguments to be substituted to <paramref name="ErrorMessage"/>.
/// Use when the values may contain PII that needs to be obscured in telemetry.
/// Otherwise, <paramref name="ErrorMessage"/> should contain the formatted message.
/// </param>
/// <param name="ExecutionTime">Time it took to execute the query.</param>
[DataContract]
internal readonly record struct ExecuteQueryResult(
    [property: DataMember(Order = 0)] string? ErrorMessage,
    [property: DataMember(Order = 1)] string[]? ErrorMessageArgs = null,
    [property: DataMember(Order = 2)] TimeSpan ExecutionTime = default);

/// <summary>
/// The result of Semantic Search query compilation.
/// </summary>
/// <param name="QueryId">Id of the compiled query if the compilation was successful.</param>
/// <param name="CompilationErrors">Compilation errors.</param>
/// <param name="EmitTime">Time it took to emit the query compilation.</param>
[DataContract]
internal readonly record struct CompileQueryResult(
    [property: DataMember(Order = 0)] CompiledQueryId QueryId,
    [property: DataMember(Order = 1)] ImmutableArray<QueryCompilationError> CompilationErrors,
    [property: DataMember(Order = 2)] TimeSpan EmitTime = default);

[DataContract]
internal readonly record struct CompiledQueryId
{
    private static int s_id;

    [DataMember(Order = 0)]
#pragma warning disable IDE0052 // Remove unread private members (https://github.com/dotnet/roslyn/issues/77907)
    private readonly int _id;
#pragma warning restore IDE0052

    [DataMember(Order = 1)]
#pragma warning disable IDE0052 // Remove unread private members (https://github.com/dotnet/roslyn/issues/77907)
    public readonly string Language;
#pragma warning restore IDE0052

    private CompiledQueryId(int id, string language)
    {
        _id = id;
        Language = language;
    }

    public static CompiledQueryId Create(string language)
        => new(Interlocked.Increment(ref s_id), language);
}
