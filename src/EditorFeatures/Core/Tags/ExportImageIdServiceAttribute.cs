// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;

namespace Microsoft.CodeAnalysis.Editor.Tags;

/// <summary>
/// Use this attribute to declare an <see cref="IImageIdService"/> implementation 
/// so that it can be discovered by the host.
/// </summary>
[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class)]
internal sealed class ExportImageIdServiceAttribute : ExportAttribute
{
    /// <summary>
    /// The name of the <see cref="IImageIdService"/>.  
    /// </summary>
    public string Name { get; set; }

    public ExportImageIdServiceAttribute()
        : base(typeof(IImageIdService))
    {
    }
}
