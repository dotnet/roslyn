// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    /// <summary>
    /// Use this attribute to export a <see cref="IMetadataAsSourceFileProvider"/> so that it will
    /// be found and used by the <see cref="IMetadataAsSourceFileService"/>.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExportMetadataAsSourceFileProviderAttribute(string name) : ExportAttribute(typeof(IMetadataAsSourceFileProvider))
    {
        public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
    }
}
