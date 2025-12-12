// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
