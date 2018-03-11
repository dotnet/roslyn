﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal interface IProgressionLanguageService : ILanguageService
    {
        IEnumerable<SyntaxNode> GetTopLevelNodesFromDocument(SyntaxNode root, CancellationToken cancellationToken);
        string GetDescriptionForSymbol(ISymbol symbol, bool includeContainingSymbol);
        string GetLabelForSymbol(ISymbol symbol, bool includeContainingSymbol);
    }
}
