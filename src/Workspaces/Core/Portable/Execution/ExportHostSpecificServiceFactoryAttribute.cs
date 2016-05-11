// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Execution;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// Use this attribute to declare a <see cref="IHostSpecificServiceFactory"/> implementation for inclusion in a MEF-based workspace.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportHostSpecificServiceFactoryAttribute : ExportAttribute
    {
        /// <summary>
        /// The assembly qualified name of the service's type.
        /// </summary>
        public string ServiceType { get; }

        /// <summary>
        /// The host that the service is specified for; HostKinds.InProc, etc
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// The layer that the service is specified for; ServiceLayer.Default, etc.
        /// </summary>
        public string Layer { get; }

        /// <summary>
        /// Declares a <see cref="IHostSpecificServiceFactory"/> implementation for inclusion in a MEF-based workspace.
        /// </summary>
        /// <param name="type">The type that will be used to retrieve the service from a <see cref="IExecutionHostingService"/>.</param>
        /// <param name="host">The host that the service is specified for; HostKinds.InProc, etc.</param>
        /// <param name="layer">The layer that the service is specified for; ServiceLayer.Default, etc.</param>
        public ExportHostSpecificServiceFactoryAttribute(Type type, string host, string layer = ServiceLayer.Default)
            : base(typeof(IHostSpecificServiceFactory))
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.ServiceType = type.AssemblyQualifiedName;
            this.Host = host;
            this.Layer = layer;
        }
    }
}
