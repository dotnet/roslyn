// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal partial class OperationStatus
    {
        public OperationStatus(OperationStatusFlag flag, string reason)
        {
            Contract.ThrowIfTrue(flag.Succeeded() && flag.HasBestEffort());

            Flag = flag;
            Reasons = reason == null ? SpecializedCollections.EmptyEnumerable<string>() : SpecializedCollections.SingletonEnumerable(reason);
        }

        private OperationStatus(OperationStatusFlag flag, IEnumerable<string> reasons)
        {
            Contract.ThrowIfNull(reasons);
            Contract.ThrowIfTrue(flag.Succeeded() && flag.HasBestEffort());

            Flag = flag;
            Reasons = reasons;
        }

        public OperationStatus With(OperationStatusFlag flag, string reason)
        {
            var newFlag = Flag | flag;

            newFlag = (this.Failed() || flag.Failed()) ? newFlag.RemoveFlag(OperationStatusFlag.Succeeded) : newFlag;
            newFlag = newFlag.Succeeded() ? newFlag.RemoveFlag(OperationStatusFlag.BestEffort) : newFlag;

            var reasons = reason == null ? Reasons : Reasons.Concat(reason);
            return new OperationStatus(newFlag, reasons);
        }

        public OperationStatus With(OperationStatus operationStatus)
        {
            var newFlag = Flag | operationStatus.Flag;

            newFlag = (this.Failed() || operationStatus.Failed()) ? newFlag.RemoveFlag(OperationStatusFlag.Succeeded) : newFlag;
            newFlag = newFlag.Succeeded() ? newFlag.RemoveFlag(OperationStatusFlag.BestEffort) : newFlag;

            var reasons = Reasons.Concat(operationStatus.Reasons);
            return new OperationStatus(newFlag, reasons);
        }

        public OperationStatus MakeFail()
        {
            return new OperationStatus(OperationStatusFlag.None, Reasons);
        }

        public OperationStatus MarkSuggestion()
        {
            return new OperationStatus(Flag | OperationStatusFlag.Suggestion, Reasons);
        }

        public OperationStatus<T> With<T>(T data)
        {
            return Create(this, data);
        }

        public OperationStatusFlag Flag { get; }
        public IEnumerable<string> Reasons { get; }
    }
}
