// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ParameterValidationAnalysis
{
    /// <summary>
    /// Abstract validation value for <see cref="AbstractLocation"/>/<see cref="IOperation"/> tracked by <see cref="ParameterValidationAnalysis"/>.
    /// </summary>
    internal enum ParameterValidationAbstractValue
    {
        /// <summary>
        /// Analysis is not applicable for this location.
        /// </summary>
        NotApplicable,

        /// <summary>
        /// Location has not been validated.
        /// </summary>
        NotValidated,

        /// <summary>
        /// Location has been validated.
        /// </summary>
        Validated,

        /// <summary>
        /// Location may have been validated.
        /// </summary>
        MayBeValidated
    }
}
