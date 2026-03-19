// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.VsNavInfo;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library;

internal abstract class AbstractLibraryService : ILibraryService
{
    public Guid LibraryId { get; }
    public __SymbolToolLanguage PreferredLanguage { get; }

    public SymbolDisplayFormat TypeDisplayFormat { get; }
    public SymbolDisplayFormat MemberDisplayFormat { get; }

    public NavInfoFactory NavInfoFactory { get; }

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

        this.NavInfoFactory = new NavInfoFactory(this);
    }
}
