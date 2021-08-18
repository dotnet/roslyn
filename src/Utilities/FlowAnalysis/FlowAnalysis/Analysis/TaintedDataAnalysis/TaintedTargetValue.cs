// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal static class TaintedTargetValue
    {
        /// <summary>
        /// Taint return value.
        /// </summary>
        public const string Return = ".Return";

        /// <summary>
        /// Taint the instance value.
        /// </summary>
        public const string This = ".This";
    }
}
