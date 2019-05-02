// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Editor
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
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
