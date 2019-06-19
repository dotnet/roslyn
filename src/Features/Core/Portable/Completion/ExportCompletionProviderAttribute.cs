// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    public sealed class ExportCompletionProviderAttribute : ExportAttribute
    {
        public string Name { get; }
        public string Language { get; }
        public string[] Roles { get; set; }

        public ExportCompletionProviderAttribute(string name, string language)
            : base(typeof(CompletionProvider))
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Language = language ?? throw new ArgumentNullException(nameof(language));
        }
    }
}
