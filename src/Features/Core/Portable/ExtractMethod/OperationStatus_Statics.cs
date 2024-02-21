// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal partial class OperationStatus
    {
        public static readonly OperationStatus SucceededStatus = new(succeeded: true, reason: null);
        public static readonly OperationStatus FailedWithUnknownReason = new(succeeded: false, reason: FeaturesResources.Unknown_error_occurred);
        public static readonly OperationStatus OverlapsHiddenPosition = new(succeeded: false, FeaturesResources.generated_code_is_overlapping_with_hidden_portion_of_the_code);
        public static readonly OperationStatus NoValidLocationToInsertMethodCall = new(succeeded: false, FeaturesResources.No_valid_location_to_insert_method_call);

        public static readonly OperationStatus NoActiveStatement = new(succeeded: false, FeaturesResources.The_selection_contains_no_active_statement);
        public static readonly OperationStatus ErrorOrUnknownType = new(succeeded: true, FeaturesResources.The_selection_contains_an_error_or_unknown_type);
        public static readonly OperationStatus UnsafeAddressTaken = new(succeeded: true, FeaturesResources.The_address_of_a_variable_is_used_inside_the_selected_code);
        public static readonly OperationStatus LocalFunctionCallWithoutDeclaration = new(succeeded: false, FeaturesResources.The_selection_contains_a_local_function_call_without_its_declaration);

        /// <summary>
        /// create operation status with the given data
        /// </summary>
        public static OperationStatus<T> Create<T>(OperationStatus status, T data)
            => new(status, data);
    }
}
