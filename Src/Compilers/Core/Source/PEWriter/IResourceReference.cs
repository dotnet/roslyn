using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

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
        /// A symbolic reference to the IAssembly that defines the resource.
        /// </summary>
        IAssemblyReference DefiningAssembly { get; }

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
}