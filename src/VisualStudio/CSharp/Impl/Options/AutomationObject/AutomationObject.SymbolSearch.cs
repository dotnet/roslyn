// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    public partial class AutomationObject
    {
        public int AddImport_SuggestForTypesInReferenceAssemblies
        {
            get { return GetBooleanOption(SymbolSearchOptionsStorage.SearchReferenceAssemblies); }
            set { SetBooleanOption(SymbolSearchOptionsStorage.SearchReferenceAssemblies, value); }
        }

        public int AddImport_SuggestForTypesInNuGetPackages
        {
            get { return GetBooleanOption(SymbolSearchOptionsStorage.SearchNuGetPackages); }
            set { SetBooleanOption(SymbolSearchOptionsStorage.SearchNuGetPackages, value); }
        }
    }
}
