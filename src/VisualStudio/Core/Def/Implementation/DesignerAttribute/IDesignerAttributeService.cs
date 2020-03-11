// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    internal interface IDesignerAttributeService : IWorkspaceService
    {
        /// <summary>
        /// Called by a host to let this service know that it should start background
        /// analysis of the workspace to determine which classes are designable.
        /// </summary>
        void Start(CancellationToken cancellationToken);
    }
}
