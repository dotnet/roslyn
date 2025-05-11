// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.QuickInfo.Presentation;

internal sealed class QuickInfoOnTheFlyDocsElement(Document document, OnTheFlyDocsInfo info) : QuickInfoElement
{
    public Document Document { get; } = document;
    public OnTheFlyDocsInfo Info { get; } = info;
}
