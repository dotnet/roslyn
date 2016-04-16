' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ObjectBrowser
    <ExportLanguageService(GetType(ILibraryService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicLibraryService
        Inherits AbstractLibraryService

        Private Shared ReadOnly s_typeDisplayFormat As New SymbolDisplayFormat(
            typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance)

        Private Shared ReadOnly s_memberDisplayFormat As SymbolDisplayFormat =
            New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:=SymbolDisplayMemberOptions.IncludeExplicitInterface Or SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeType,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Public Sub New()
            MyBase.New(Guids.VisualBasicLibraryId, __SymbolToolLanguage.SymbolToolLanguage_VB, s_typeDisplayFormat, s_memberDisplayFormat)
        End Sub
    End Class
End Namespace
