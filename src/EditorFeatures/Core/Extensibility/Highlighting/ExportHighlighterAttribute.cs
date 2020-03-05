﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Editor
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
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
