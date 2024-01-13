// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// MEF metadata class used for finding <see cref="IWorkspaceService"/> and <see cref="IWorkspaceServiceFactory"/> exports.
    /// </summary>
    internal class WorkspaceServiceMetadata
    {
        public string ServiceType { get; }
        public string Layer { get; }

        public WorkspaceServiceMetadata(Type serviceType, string layer)
            : this(serviceType.AssemblyQualifiedName, layer)
        {
        }

        public WorkspaceServiceMetadata(IDictionary<string, object> data)
        {
            this.ServiceType = (string)data.GetValueOrDefault("ServiceType");
            this.Layer = (string)data.GetValueOrDefault("Layer");
        }

        public WorkspaceServiceMetadata(string serviceType, string layer)
        {
            this.ServiceType = serviceType;
            this.Layer = layer;
        }
    }
}
