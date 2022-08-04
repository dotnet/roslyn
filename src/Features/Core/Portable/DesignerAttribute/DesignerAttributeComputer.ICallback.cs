// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.DesignerAttribute
{
    internal sealed partial class DesignerAttributeComputer
    {
        /// <summary>
        /// Callback that this <see cref="DesignerAttributeComputer"/> will use to notify a listener of important information.
        /// </summary>
        public interface ICallback
        {
            /// <summary>
            /// Called if this <paramref name="projectId"/> is no longer part of the solution.  All data related to is can now be removed.
            /// </summary>
            ValueTask ReportProjectRemovedAsync(ProjectId projectId, CancellationToken cancellationToken);
            
            /// <summary>
            /// Called to notify a listener about any <em>changed</em> designer attribute data discovered while scanning.
            /// </summary>
            ValueTask ReportDesignerAttributeDataAsync(ImmutableArray<DesignerAttributeData> data, CancellationToken cancellationToken);
        }
    }
}
