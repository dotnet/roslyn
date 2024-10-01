// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;

namespace Microsoft.CodeAnalysis.Host
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExportEventListenerAttribute : ExportAttribute
    {
        public string Service { get; }
        public IReadOnlyCollection<string> WorkspaceKinds { get; }

        /// <summary>
        /// MEF export attribute for <see cref="IEventListener"/>
        /// </summary>
        /// <param name="service">
        /// one of values from <see cref="WellKnownEventListeners"/> indicating which service this event listener is for
        /// </param>
        /// <param name="workspaceKinds">indicate which workspace kind this event listener is for</param>
        public ExportEventListenerAttribute(string service, params string[] workspaceKinds)
            : base(typeof(IEventListener))
        {
            if (workspaceKinds?.Length == 0)
            {
                throw new ArgumentNullException(nameof(workspaceKinds));
            }

            this.Service = service ?? throw new ArgumentNullException(nameof(service));
            this.WorkspaceKinds = workspaceKinds;
        }
    }
}
