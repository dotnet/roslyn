// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.WorkspaceServices
{
    /// <summary>
    /// Specifies the exact type of the service exported by the IWorkspaceService.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportWorkspaceServiceAttribute : ExportAttribute
    {
        public ExportWorkspaceServiceAttribute(Type serviceType, string workspaceKind)
            : base(typeof(IWorkspaceService))
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }

            if (workspaceKind == null)
            {
                throw new ArgumentNullException("workspaceKind");
            }

            this.ServiceTypeAssemblyQualifiedName = serviceType.AssemblyQualifiedName;
            this.WorkspaceKind = workspaceKind;
        }

        public string ServiceTypeAssemblyQualifiedName { get; private set; }
        public string WorkspaceKind { get; private set; }
    }
}