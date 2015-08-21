// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal partial class OperationStatus
    {
        public static readonly OperationStatus Succeeded = new OperationStatus(OperationStatusFlag.Succeeded, reason: null);
        public static readonly OperationStatus FailedWithUnknownReason = new OperationStatus(OperationStatusFlag.None, reason: FeaturesResources.ExtractMethodFailedWithUnknownReasons);
        public static readonly OperationStatus OverlapsHiddenPosition = new OperationStatus(OperationStatusFlag.None, FeaturesResources.GeneratedCodeIsOverlapping);

        public static readonly OperationStatus NoActiveStatement = new OperationStatus(OperationStatusFlag.BestEffort, FeaturesResources.NoActiveStatement);
        public static readonly OperationStatus ErrorOrUnknownType = new OperationStatus(OperationStatusFlag.BestEffort, FeaturesResources.ErrorOrUnknownType);
        public static readonly OperationStatus UnsafeAddressTaken = new OperationStatus(OperationStatusFlag.BestEffort, FeaturesResources.TheAddressOfAVariableIsUsed);

        /// <summary>
        /// create operation status with the given data
        /// </summary>
        public static OperationStatus<T> Create<T>(OperationStatus status, T data)
        {
            return new OperationStatus<T>(status, data);
        }
    }
}
