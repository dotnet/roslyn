// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExtractMethod;

/// <summary>
/// operation status paired with data
/// </summary>
internal sealed class OperationStatus<T>(OperationStatus status, T data)
{
    public OperationStatus Status { get; } = status;
    public T Data { get; } = data;

    public OperationStatus<T> With(OperationStatus status)
        => new(status, Data);

    public OperationStatus<TNew> With<TNew>(TNew data)
        => new(Status, data);
}
