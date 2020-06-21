// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.FlightEnabledAnalysis
{
    internal enum FlightEnabledAbstractValueKind
    {
        /// <summary>
        /// Unset value.
        /// This is needed along with Empty to ensure the following merge results:
        /// Unset + Known = Known
        /// Empty + Known = Empty
        /// </summary>
        Unset,

        /// <summary>
        /// One or more known set of flights enabled.
        /// </summary>
        Known,

        /// <summary>
        /// No flights enabled.
        /// </summary>
        Empty,

        /// <summary>
        /// Unknown set of flights enabled.
        /// </summary>
        Unknown
    }
}