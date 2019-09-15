// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Editor
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ExcludeFromCodeCoverage]
    internal class ExportHighlighterAttribute : ExportAttribute
    {
        public string Language { get; }

        public ExportHighlighterAttribute(string language)
            : base(typeof(IHighlighter))
        {
            this.Language = language ?? throw new ArgumentNullException(nameof(language));
        }
    }
}
