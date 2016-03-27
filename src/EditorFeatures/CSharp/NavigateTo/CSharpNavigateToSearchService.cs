// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.NavigateTo
{
    [ExportLanguageService(typeof(INavigateToSearchService), LanguageNames.CSharp), Shared]
    internal class CSharpNavigateToSearchService : AbstractNavigateToSearchService
    {
        [ImportingConstructor]
        public CSharpNavigateToSearchService([ImportMany] IEnumerable<INavigateToSearchResultProvider> resultProviders) : base(resultProviders)
        {
        }
    }
}
