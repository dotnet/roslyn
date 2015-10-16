// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServices.Implementation.Library;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ObjectBrowser
{
    internal class CSharpLibraryService : AbstractLibraryService
    {
        public CSharpLibraryService()
            : base(Guids.CSharpLibraryId, __SymbolToolLanguage.SymbolToolLanguage_CSharp)
        {
        }
    }
}
