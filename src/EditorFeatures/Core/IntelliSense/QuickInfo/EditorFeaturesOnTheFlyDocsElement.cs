// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;

internal sealed class EditorFeaturesOnTheFlyDocsElement(Document document, OnTheFlyDocsElement onTheFlyDocsElement)
{
    internal Document Document { get; } = document;
    internal OnTheFlyDocsElement OnTheFlyDocsElement { get; } = onTheFlyDocsElement;
}
