// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Metalama.Compiler
{
    /// <summary>
    /// Represents a managed resource.
    /// </summary>
    public sealed class ManagedResource
    {
        /// <summary>
        /// Gets the underlying <see cref="ResourceDescription"/>.
        /// </summary>
        public ResourceDescription Resource { get; }

        /// <summary>
        /// Gets a value indicating whether the resource should be included in reference assemblies.
        /// </summary>
        public bool IncludeInRefAssembly { get; }

#if !METALAMA_COMPILER_INTERFACE

        /// <summary>
        /// Initializes a new instance of <see cref="ManagedResource"/> that represents an existing <see cref="ResourceDescription"/>.
        /// </summary>
        internal ManagedResource(ResourceDescription resource, bool includeInRefAssembly = false)
        {
            this.Resource = resource;
            this.Name = resource.ResourceName;
            this.IsEmbedded = resource.IsEmbedded;
            this.IsPublic = resource.IsPublic;
            this.DataProvider = resource.DataProvider;
            this.IncludeInRefAssembly = includeInRefAssembly;
        }
#endif

        /// <summary>
        /// Initializes a new instance of <see cref="ManagedResource"/> that represents a new public resource
        /// embedded in the module.
        /// </summary>
        /// <param name="name">Name of the manage resource.</param>
        /// <param name="data">The managed resource data.</param>
        /// <param name="includeInRefAssembly">A value indicating whether the resource should be included in reference assemblies.</param>
        public ManagedResource(string name, byte[] data, bool includeInRefAssembly = false)
        {
            this.Resource = new ResourceDescription(name, () => new MemoryStream(data), true);
            this.Name = name;
            this.IsPublic = true;
            this.IsEmbedded = true;
            this.DataProvider = () => new MemoryStream(data);
            this.IncludeInRefAssembly = includeInRefAssembly;
        }

        /// <summary>
        /// Gets the resource name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a value indicating whether the resource is public.
        /// </summary>
        public bool IsPublic { get; }

        /// <summary>
        /// Gets a value indicating whether the resource is embedded in the module.
        /// </summary>
        public bool? IsEmbedded { get; }

        /// <summary>
        /// Gets a delegate returning the content of the resource.
        /// </summary>
        public Func<Stream>? DataProvider { get; }
    }
}
