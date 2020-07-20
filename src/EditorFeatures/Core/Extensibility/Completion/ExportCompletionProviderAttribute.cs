// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Editor
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportCompletionProviderMef1Attribute : ExportAttribute
    {
        public string Name { get; }
        public string Language { get; }

        public ExportCompletionProviderMef1Attribute(string name, string language)
            : base(typeof(CompletionProvider))
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Language = language ?? throw new ArgumentNullException(nameof(language));
        }
    }
}
