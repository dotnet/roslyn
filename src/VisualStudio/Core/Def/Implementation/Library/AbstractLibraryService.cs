// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.VsNavInfo;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library
{
    internal abstract class AbstractLibraryService : ILibraryService
    {
        public Guid LibraryId { get; }
        public __SymbolToolLanguage PreferredLanguage { get; }

        public SymbolDisplayFormat TypeDisplayFormat { get; }
        public SymbolDisplayFormat MemberDisplayFormat { get; }

        public NavInfoFactory NavInfo { get; }

        protected AbstractLibraryService(
            Guid libraryId,
            __SymbolToolLanguage preferredLanguage,
            SymbolDisplayFormat typeDisplayFormat,
            SymbolDisplayFormat memberDisplayFormat)
        {
            this.LibraryId = libraryId;
            this.PreferredLanguage = preferredLanguage;
            this.TypeDisplayFormat = typeDisplayFormat;
            this.MemberDisplayFormat = memberDisplayFormat;

            this.NavInfo = new NavInfoFactory(this);
        }
    }
}
