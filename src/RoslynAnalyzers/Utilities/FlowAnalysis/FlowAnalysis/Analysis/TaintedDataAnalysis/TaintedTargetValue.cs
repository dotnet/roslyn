// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
