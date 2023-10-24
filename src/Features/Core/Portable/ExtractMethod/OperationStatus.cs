// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal partial class OperationStatus
{
    public OperationStatus(OperationStatusFlag flag, string reason)
    {
        Flag = flag;
        Reasons = reason == null ? ImmutableArray<string>.Empty : ImmutableArray.Create(reason);
    }

    private OperationStatus(OperationStatusFlag flag, ImmutableArray<string> reasons)
    {
        Contract.ThrowIfTrue(reasons.IsDefault);

        Flag = flag;
        Reasons = reasons;
    }

    public OperationStatus With(OperationStatusFlag flag, string reason)
    {
        var newFlag = Flag | flag;

        newFlag = (this.Failed() || flag.Failed()) ? newFlag.RemoveFlag(OperationStatusFlag.Succeeded) : newFlag;

        var reasons = reason == null ? Reasons : Reasons.Concat(reason);
        return new OperationStatus(newFlag, reasons);
    }

    public OperationStatus With(OperationStatus operationStatus)
    {
        var newFlag = Flag | operationStatus.Flag;

        newFlag = (this.Failed() || operationStatus.Failed()) ? newFlag.RemoveFlag(OperationStatusFlag.Succeeded) : newFlag;

        var reasons = Reasons.Concat(operationStatus.Reasons);
        return new OperationStatus(newFlag, reasons);
    }

    public OperationStatus MakeFail()
        => new(OperationStatusFlag.Failed, Reasons);

    public OperationStatus<T> With<T>(T data)
        => Create(this, data);

    public OperationStatusFlag Flag { get; }
    public ImmutableArray<string> Reasons { get; }
}
