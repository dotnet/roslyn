﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor.GoToBase
{
    internal interface IGoToBaseService : ILanguageService
    {
        /// <summary>
        /// Finds the base members overridden or implemented by the symbol at the specific position in the document,
        /// pushing the results into the context instance.
        /// </summary>
        Task FindBasesAsync(Document document, int position, IFindUsagesContext context);
    }
}
