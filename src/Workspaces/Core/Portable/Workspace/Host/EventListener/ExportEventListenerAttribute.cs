// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;

namespace Microsoft.CodeAnalysis.Host
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportEventListenerAttribute : ExportAttribute
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
                throw new ArgumentException(nameof(workspaceKinds));
            }

            this.Service = service ?? throw new ArgumentException(nameof(service));
            this.WorkspaceKinds = workspaceKinds;
        }
    }
}
