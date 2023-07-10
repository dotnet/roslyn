// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.Snippets
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExportSnippetProviderAttribute(string name, string language) : ExportAttribute(typeof(ISnippetProvider))
    {
        public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
        public string Language { get; } = language ?? throw new ArgumentNullException(nameof(language));
    }
}
