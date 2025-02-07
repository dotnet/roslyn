// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

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
