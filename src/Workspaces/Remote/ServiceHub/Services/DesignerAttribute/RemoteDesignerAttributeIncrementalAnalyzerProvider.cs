// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.Remote
{
    internal class RemoteDesignerAttributeIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        private readonly RemoteEndPoint _endPoint;

        public RemoteDesignerAttributeIncrementalAnalyzerProvider(RemoteEndPoint endPoint)
        {
            _endPoint = endPoint;
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
            => new RemoteDesignerAttributeIncrementalAnalyzer(_endPoint, workspace);
    }
}
