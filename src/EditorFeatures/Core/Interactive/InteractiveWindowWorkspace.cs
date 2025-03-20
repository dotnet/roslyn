// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.InteractiveWindow;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal sealed class InteractiveWindowWorkspace : InteractiveWorkspace
    {
        public IInteractiveWindow? Window { get; set; }

        public InteractiveWindowWorkspace(HostServices hostServices)
            : base(hostServices)
        {
        }
    }
}
