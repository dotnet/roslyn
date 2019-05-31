// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.EventListener
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportEventListenerAttribute : ExportAttribute
    {
        public string Service { get; }
        public string[] WorkspaceKinds { get; }

        public ExportEventListenerAttribute(string service, params string[] workspaceKinds)
            : base(typeof(IEventListener))
        {
            if (workspaceKinds?.Length == 0)
            {
                throw new ArgumentException(nameof(workspaceKinds));
            }

            this.Service = service ?? throw new ArgumentException("service");
            this.WorkspaceKinds = workspaceKinds;
        }
    }
}
