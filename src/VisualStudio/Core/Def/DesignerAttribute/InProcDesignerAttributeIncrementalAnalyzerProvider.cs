// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    /// <remarks>Note: this is explicitly <b>not</b> exported.  We don't want the Workspace
    /// to automatically load this.  Instead, VS waits until it is ready
    /// and then calls into the service to tell it to start analyzing the solution.  At that point we'll get
    /// created and added to the solution crawler.
    /// </remarks>
    internal sealed class InProcDesignerAttributeIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        private readonly IDesignerAttributeListener _listener;

        public InProcDesignerAttributeIncrementalAnalyzerProvider(IDesignerAttributeListener listener)
        {
            _listener = listener;
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
            => new InProcDesignerAttributeIncrementalAnalyzer(_listener);
    }
}
