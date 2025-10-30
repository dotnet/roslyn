// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal sealed partial class OperationStatus
{
    public OperationStatus(bool succeeded, string? reason)
    {
        Succeeded = succeeded;
        Reasons = reason == null ? [] : [reason];
    }

    private OperationStatus(bool succeeded, ImmutableArray<string> reasons)
    {
        Contract.ThrowIfTrue(reasons.IsDefault);

        Succeeded = succeeded;
        Reasons = reasons;
    }

    public OperationStatus With(bool succeeded, string reason)
    {
        var newSucceeded = Succeeded && succeeded;

        var reasons = reason == null ? Reasons : Reasons.Concat(reason);
        return new OperationStatus(newSucceeded, reasons);
    }

    public OperationStatus With(OperationStatus operationStatus)
        => new(
            Succeeded && operationStatus.Succeeded,
            [.. Reasons, .. operationStatus.Reasons]);

    public OperationStatus MakeFail()
        => new(succeeded: false, Reasons);

    public OperationStatus<T> With<T>(T data)
        => Create(this, data);

    public bool Succeeded { get; }
    public ImmutableArray<string> Reasons { get; }

    public bool Failed => !Succeeded;
}
