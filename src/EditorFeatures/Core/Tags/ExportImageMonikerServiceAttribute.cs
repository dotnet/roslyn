// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Editor.Tags
{
    /// <summary>
    /// Use this attribute to declare an <see cref="IImageMonikerService"/> implementation 
    /// so that it can be discovered by the host.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ExportImageMonikerServiceAttribute : ExportAttribute
    {
        /// <summary>
        /// The name of the <see cref="IImageMonikerService"/>.  
        /// </summary>
        public string Name { get; set; }

        public ExportImageMonikerServiceAttribute()
            : base(typeof(IImageMonikerService))
        {
        }
    }
}
