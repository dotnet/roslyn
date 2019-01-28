// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
{
    internal static class Helpers
    {
        internal static ClassifiedTextElement BuildClassifiedTextElement(ImmutableArray<TaggedText> taggedTexts)
        {
            return new ClassifiedTextElement(taggedTexts.Select(
                    part => new ClassifiedTextRun(part.Tag.ToClassificationTypeName(), part.Text)) ?? Enumerable.Empty<ClassifiedTextRun>());
        }
    }
}
