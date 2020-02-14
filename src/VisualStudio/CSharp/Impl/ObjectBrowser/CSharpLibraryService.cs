﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ObjectBrowser
{
    [ExportLanguageService(typeof(ILibraryService), LanguageNames.CSharp), Shared]
    internal class CSharpLibraryService : AbstractLibraryService
    {
        private static readonly SymbolDisplayFormat s_typeDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance);

        private static readonly SymbolDisplayFormat s_memberDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
            memberOptions: SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        [ImportingConstructor]
        public CSharpLibraryService()
            : base(Guids.CSharpLibraryId, __SymbolToolLanguage.SymbolToolLanguage_CSharp, s_typeDisplayFormat, s_memberDisplayFormat)
        {
        }
    }
}
