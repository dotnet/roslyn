// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    public enum PointsToAnalysisKind
    {
        // NOTE: Below fields names are used in the .editorconfig specification
        //       for PointsToAnalysisKind option. Hence the names should *not* be modified,
        //       as that would be a breaking change for .editorconfig specification.

        /// <summary>
        /// Analysis is disabled.
        /// </summary>
        None,

        /// <summary>
        /// Partial analysis that tracks <see cref="PointsToAbstractValue"/> for <see cref="AnalysisEntity"/>
        /// except fields and properties.
        /// </summary>
        PartialWithoutTrackingFieldsAndProperties,

        /// <summary>
        /// Complete analysis that tracks <see cref="PointsToAbstractValue"/> for all <see cref="AnalysisEntity"/>.
        /// </summary>
        Complete,
    }
}
