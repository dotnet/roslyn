// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// Use this attribute to declare a <see cref="IWorkspaceService"/> implementation for inclusion in a MEF-based workspace.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ExportWorkspaceServiceAttribute : ExportAttribute
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
        /// Declares a <see cref="IWorkspaceService"/> implementation for inclusion in a MEF-based workspace.
        /// </summary>
        /// <param name="serviceType">The type that will be used to retrieve the service from a <see cref="HostWorkspaceServices"/>.</param>
        /// <param name="layer">The layer that the service is specified for; <see cref="ServiceLayer.Default" />, etc.</param>
        public ExportWorkspaceServiceAttribute(Type serviceType, string layer = ServiceLayer.Default)
            : base(typeof(IWorkspaceService))
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
