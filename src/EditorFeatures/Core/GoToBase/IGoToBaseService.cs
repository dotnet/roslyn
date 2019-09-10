// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
