// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract partial class AnalyzerDriver
    {
        protected abstract IGroupedAnalyzerActions EmptyGroupedActions { get; }
        protected abstract IGroupedAnalyzerActions CreateGroupedActions(DiagnosticAnalyzer analyzer, in AnalyzerActions analyzerActions);

        protected interface IGroupedAnalyzerActions
        {
            bool IsEmpty { get; }
            AnalyzerActions AnalyzerActions { get; }
            IGroupedAnalyzerActions Append(IGroupedAnalyzerActions groupedAnalyzerActions);
        }
    }
}
