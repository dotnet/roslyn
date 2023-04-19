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
            get { return GetBooleanOption(SymbolSearchOptions.SuggestForTypesInReferenceAssemblies); }
            set { SetBooleanOption(SymbolSearchOptions.SuggestForTypesInReferenceAssemblies, value); }
        }

        public int AddImport_SuggestForTypesInNuGetPackages
        {
            get { return GetBooleanOption(SymbolSearchOptions.SuggestForTypesInNuGetPackages); }
            set { SetBooleanOption(SymbolSearchOptions.SuggestForTypesInNuGetPackages, value); }
        }
    }
}
