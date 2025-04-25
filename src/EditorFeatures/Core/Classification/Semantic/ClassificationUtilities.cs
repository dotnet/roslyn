// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Classification;

internal static class ClassificationUtilities
{
    public static TagSpan<IClassificationTag> Convert(IClassificationTypeMap typeMap, ITextSnapshot snapshot, ClassifiedSpan classifiedSpan)
    {
        return new TagSpan<IClassificationTag>(
            classifiedSpan.TextSpan.ToSnapshotSpan(snapshot),
            new ClassificationTag(typeMap.GetClassificationType(classifiedSpan.ClassificationType)));
    }
}
