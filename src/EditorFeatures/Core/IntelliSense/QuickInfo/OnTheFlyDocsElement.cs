// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.IntelliSense.QuickInfo;

internal sealed class OnTheFlyDocsElement
{
    internal Document Document { get; }
    internal ISymbol Symbol { get; }
    internal string DescriptionText { get; }

    public OnTheFlyDocsElement(Document document, ISymbol symbol, string descriptionText)
    {
        Document = document;
        Symbol = symbol;
        DescriptionText = descriptionText;
    }
}
