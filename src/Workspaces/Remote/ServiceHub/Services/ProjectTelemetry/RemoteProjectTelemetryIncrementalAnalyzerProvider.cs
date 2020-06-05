// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <remarks>Note: this is explicitly <b>not</b> exported.  We don't want the <see
    /// cref="RemoteWorkspace"/> to automatically load this.  Instead, VS waits until it is ready
    /// and then calls into OOP to tell it to start analyzing the solution.  At that point we'll get
    /// created and added to the solution crawler.
    /// </remarks>
    internal class RemoteProjectTelemetryIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        private readonly RemoteEndPoint _endPoint;

        public RemoteProjectTelemetryIncrementalAnalyzerProvider(RemoteEndPoint endPoint)
        {
            _endPoint = endPoint;
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
            => new RemoteProjectTelemetryIncrementalAnalyzer(_endPoint);
    }
}
