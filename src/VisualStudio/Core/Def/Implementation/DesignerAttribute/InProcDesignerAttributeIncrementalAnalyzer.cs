// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DesignerAttribute;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    internal sealed class InProcDesignerAttributeIncrementalAnalyzer : AbstractDesignerAttributeIncrementalAnalyzer
    {
        private readonly IDesignerAttributeListener _listener;

        public InProcDesignerAttributeIncrementalAnalyzer(IDesignerAttributeListener listener)
        {
            _listener = listener;
        }

        protected override ValueTask ReportProjectRemovedAsync(ProjectId projectId, CancellationToken cancellationToken)
            => _listener.OnProjectRemovedAsync(projectId, cancellationToken);

        protected override ValueTask ReportDesignerAttributeDataAsync(ImmutableArray<DesignerAttributeData> data, CancellationToken cancellationToken)
            => _listener.ReportDesignerAttributeDataAsync(data, cancellationToken);
    }
}
