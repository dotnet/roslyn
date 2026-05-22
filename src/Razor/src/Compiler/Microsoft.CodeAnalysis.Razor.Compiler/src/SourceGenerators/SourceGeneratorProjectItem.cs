// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    internal class SourceGeneratorProjectItem : RazorProjectItem, IEquatable<SourceGeneratorProjectItem>
    {
        private readonly RazorFileKind _fileKind;
        private readonly RazorSourceDocument? _source;

        public SourceGeneratorProjectItem(
            string basePath, 
            string filePath, 
            string relativePhysicalPath,
            RazorFileKind fileKind, 
            AdditionalText additionalText, 
            string? cssScope)
        {
            BasePath = basePath;
            FilePath = filePath;
            RelativePhysicalPath = relativePhysicalPath;
            _fileKind = fileKind;
            AdditionalText = additionalText;
            CssScope = cssScope;

            var text = AdditionalText.GetText();
            if (text is not null)
            {
                _source = RazorSourceDocument.Create(text, RazorSourceDocumentProperties.Create(AdditionalText.Path, relativePhysicalPath));
            }
        }

        public AdditionalText AdditionalText { get; }

        public override string BasePath { get; }

        public override string FilePath { get; }

        public override bool Exists => true;

        public override string PhysicalPath => AdditionalText.Path;

        public override string RelativePhysicalPath { get; }

        public override RazorFileKind FileKind => _fileKind;

        public override string? CssScope { get; }

        public override Stream Read()
            => throw new NotSupportedException("This API should not be invoked. We should instead be relying on " +
                "the RazorSourceDocument associated with this item instead.");

        internal override RazorSourceDocument? GetSource() => _source;

        public bool Equals(SourceGeneratorProjectItem? other)
        {
            if (other is null ||
                CssScope != other.CssScope ||
                PhysicalPath != other.PhysicalPath)
            {
                return false;
            }

            if (ReferenceEquals(AdditionalText, other.AdditionalText))
            {
                return true;
            }

            // In the compiler server when the generator driver cache is enabled the
            // additional files are always different instances even if their content is the same.
            // It's technically possible for these hashes to collide, but other things would
            // also break in those cases, so for now we're okay with this.
            var thisHash = AdditionalText.GetText()?.GetContentHash() ?? [];
            var otherHash = other.AdditionalText.GetText()?.GetContentHash() ?? [];
            return thisHash.SequenceEqual(otherHash);
        }

        public override int GetHashCode() => AdditionalText.GetHashCode();

        public override bool Equals(object? obj) => obj is SourceGeneratorProjectItem projectItem && Equals(projectItem);
    }
}
