// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Cci
{
    /// <summary>
    /// A reference to an IResource instance.
    /// </summary>
    internal interface IResourceReference
    {
        /// <summary>
        /// A collection of metadata custom attributes that are associated with this resource.
        /// </summary>
        IEnumerable<ICustomAttribute> Attributes { get; }

        /// <summary>
        /// Specifies whether other code from other assemblies may access this resource.
        /// </summary>
        bool IsPublic { get; }

        /// <summary>
        /// The name of the resource.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The referenced resource.
        /// </summary>
        IResource Resource { get; }
    }

    /// <summary>
    /// A named data resource that is stored as part of CLR metadata.
    /// </summary>
    internal interface IResource : IResourceReference
    {
        /// <summary>
        /// Write the resource data to embed.
        /// </summary>
        void WriteData(Microsoft.Cci.BinaryWriter resourceWriter);

        /// <summary>
        /// The external file that contains the resource.
        /// </summary>
        IFileReference ExternalFile
        {
            get;

            // ^ requires this.IsInExternalFile;
        }

        /// <summary>
        /// The Offset to the data in the external file.
        /// </summary>
        uint Offset { get; }
    }
}