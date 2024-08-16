// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;

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
/// <param name="EmitTime">Time it took to emit the query compilation.</param>
/// <param name="ExecutionTime">Time it took to execute the query.</param>
[DataContract]
internal readonly record struct ExecuteQueryResult(
    [property: DataMember(Order = 0)] string? ErrorMessage,
    [property: DataMember(Order = 1)] string[]? ErrorMessageArgs = null,
    [property: DataMember(Order = 2)] TimeSpan EmitTime = default,
    [property: DataMember(Order = 3)] TimeSpan ExecutionTime = default);
