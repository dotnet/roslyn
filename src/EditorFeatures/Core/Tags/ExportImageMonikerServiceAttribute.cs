// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Editor.Tags
{
    /// <summary>
    /// Use this attribute to declare an <see cref="IImageMonikerService"/> implementation 
    /// so that it can be discovered by the host.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
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
