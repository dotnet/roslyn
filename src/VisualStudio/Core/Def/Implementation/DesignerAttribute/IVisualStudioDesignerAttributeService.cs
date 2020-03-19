// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    /// <summary>
    /// In process service responsible for listening to OOP notifications whether or not a file is
    /// designable  and then notifying the respective project systems about that information.
    /// </summary>
    internal interface IVisualStudioDesignerAttributeService
    {
        /// <summary>
        /// Called by a host to let this service know that it should start background
        /// analysis of the workspace to determine which classes are designable.
        /// </summary>
        void Start(CancellationToken cancellationToken);
    }
}
