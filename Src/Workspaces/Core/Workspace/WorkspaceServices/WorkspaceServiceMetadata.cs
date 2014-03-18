// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.WorkspaceServices
{
    internal class WorkspaceServiceMetadata
    {
        public string ServiceTypeAssemblyQualifiedName { get; private set; }
        public string WorkspaceKind { get; private set; }

        public WorkspaceServiceMetadata(Type serviceType, string workspaceKind) : this(serviceType.AssemblyQualifiedName, workspaceKind)
        {
        }

        public WorkspaceServiceMetadata(IDictionary<string, object> data)
        {
            this.ServiceTypeAssemblyQualifiedName = (string)data.GetValueOrDefault("ServiceTypeAssemblyQualifiedName");
            this.WorkspaceKind = (string)data.GetValueOrDefault("WorkspaceKind");
        }

        public WorkspaceServiceMetadata(string serviceTypeAssemblyQualifiedName, string workspaceKind)
        {
            this.ServiceTypeAssemblyQualifiedName = serviceTypeAssemblyQualifiedName;
            this.WorkspaceKind = workspaceKind;
        }
    }
}