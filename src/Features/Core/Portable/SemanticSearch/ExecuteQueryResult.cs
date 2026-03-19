// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

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
internal readonly record struct QueryCompilationError(
    [property: DataMember(Order = 0)] string Id,
    [property: DataMember(Order = 1)] string Message,
    [property: DataMember(Order = 2)] TextSpan Span);

[DataContract]
internal readonly record struct CompiledQueryId(
    [property: DataMember(Order = 0)] int Id);
