// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;

namespace Microsoft.CodeAnalysis.CSharp.NavigateTo
{
    [ExportLanguageService(typeof(INavigateToSearchService_RemoveInterfaceAboveAndRenameThisAfterInternalsVisibleToUsersUpdate), LanguageNames.CSharp), Shared]
    internal class CSharpNavigateToSearchService : AbstractNavigateToSearchService
    {
        [ImportingConstructor]
        public CSharpNavigateToSearchService()
        {
        }
    }
}
