// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Highlighting
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    [ExcludeFromCodeCoverage]
    internal class ExportHighlighterAttribute(string language) : ExportAttribute(typeof(IHighlighter))
    {
        public string Language { get; } = language ?? throw new ArgumentNullException(nameof(language));
    }
}
