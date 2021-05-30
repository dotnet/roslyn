﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// Use this attribute to declare a <see cref="IWorkspaceServiceFactory"/> implementation for inclusion in a MEF-based workspace.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    public class ExportWorkspaceServiceFactoryAttribute : ExportAttribute
    {
        /// <summary>
        /// The assembly qualified name of the service's type.
        /// </summary>
        public string ServiceType { get; }

        /// <summary>
        /// The layer that the service is specified for; ServiceLayer.Default, etc.
        /// </summary>
        public string Layer { get; }

        /// <summary>
        /// Declares a <see cref="IWorkspaceServiceFactory"/> implementation for inclusion in a MEF-based workspace.
        /// </summary>
        /// <param name="serviceType">The type that will be used to retrieve the service from a <see cref="HostWorkspaceServices"/>.</param>
        /// <param name="layer">The layer or workspace kind that the service is specified for; <see cref="ServiceLayer.Default" />, <see cref="WorkspaceKind.MiscellaneousFiles" />etc.</param>
        public ExportWorkspaceServiceFactoryAttribute(Type serviceType, string layer = ServiceLayer.Default)
            : base(typeof(IWorkspaceServiceFactory))
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            this.ServiceType = serviceType.AssemblyQualifiedName;
            this.Layer = layer ?? throw new ArgumentNullException(nameof(layer));
        }
    }
}
