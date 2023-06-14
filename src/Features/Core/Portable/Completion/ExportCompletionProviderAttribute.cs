// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// Use this attribute to export a <see cref="CompletionProvider"/> so that it will
    /// be found and used by the per language associated <see cref="CompletionService"/>.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ExportCompletionProviderAttribute(string name, string language) : ExportAttribute(typeof(CompletionProvider))
    {
        public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
        public string Language { get; } = language ?? throw new ArgumentNullException(nameof(language));
        public string[]? Roles { get; set; }
    }
}
