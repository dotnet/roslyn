// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.FlightEnabledAnalysis
{
    /// <summary>
    /// Global abstract value for analysis.
    /// </summary>
    internal interface IGlobalAbstractValue : IEquatable<IGlobalAbstractValue>
    {
        /// <summary>
        /// Return negated value if the analysis value is a predicated value.
        /// Otherwise, return the current instance itself.
        /// </summary>
        /// <returns></returns>
        IGlobalAbstractValue GetNegatedValue();

        /// <summary>
        /// String representation of the abstract value.
        /// </summary>
        /// <returns></returns>
        string ToString();
    }
}
