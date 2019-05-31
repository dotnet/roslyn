// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Host
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportWorkspaceEventListenerAttribute : ExportAttribute
    {
        public string[] WorkspaceKinds { get; }

        public ExportWorkspaceEventListenerAttribute(params string[] workspaceKinds)
            : base(typeof(IWorkspaceEventListener))
        {
            if (workspaceKinds?.Length == 0)
            {
                throw new ArgumentException(nameof(workspaceKinds));
            }

            this.WorkspaceKinds = workspaceKinds;
        }
    }
}
