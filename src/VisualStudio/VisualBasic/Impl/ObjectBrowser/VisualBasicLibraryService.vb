' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(Guids.VisualBasicLibraryId, __SymbolToolLanguage.SymbolToolLanguage_VB, s_typeDisplayFormat, s_memberDisplayFormat)
        End Sub
    End Class
End Namespace
