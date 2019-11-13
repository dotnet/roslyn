// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal partial class OperationStatus
    {
        public static readonly OperationStatus Succeeded = new OperationStatus(OperationStatusFlag.Succeeded, reason: null);
        public static readonly OperationStatus FailedWithUnknownReason = new OperationStatus(OperationStatusFlag.None, reason: FeaturesResources.Unknown_error_occurred);
        public static readonly OperationStatus OverlapsHiddenPosition = new OperationStatus(OperationStatusFlag.None, FeaturesResources.generated_code_is_overlapping_with_hidden_portion_of_the_code);

        public static readonly OperationStatus NoActiveStatement = new OperationStatus(OperationStatusFlag.BestEffort, FeaturesResources.The_selection_contains_no_active_statement);
        public static readonly OperationStatus ErrorOrUnknownType = new OperationStatus(OperationStatusFlag.BestEffort, FeaturesResources.The_selection_contains_an_error_or_unknown_type);
        public static readonly OperationStatus UnsafeAddressTaken = new OperationStatus(OperationStatusFlag.BestEffort, FeaturesResources.The_address_of_a_variable_is_used_inside_the_selected_code);
        public static readonly OperationStatus LocalFunctionCallWithoutDeclaration = new OperationStatus(OperationStatusFlag.BestEffort, FeaturesResources.The_selection_contains_a_local_function_call_without_its_declaration);

        /// <summary>
        /// create operation status with the given data
        /// </summary>
        public static OperationStatus<T> Create<T>(OperationStatus status, T data)
        {
            return new OperationStatus<T>(status, data);
        }
    }
}
