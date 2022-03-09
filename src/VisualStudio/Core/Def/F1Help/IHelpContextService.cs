﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.F1Help
{
    internal interface IHelpContextService : ILanguageService
    {
        string Language { get; }
        string Product { get; }

        Task<string> GetHelpTermAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);

        string FormatSymbol(ISymbol symbol);
    }
}
