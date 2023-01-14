' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.SymbolSearch

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    Partial Public Class AutomationObject
        Public Property Option_SuggestImportsForTypesInReferenceAssemblies As Boolean
            Get
                Return GetBooleanOption(SymbolSearchOptionsStorage.SearchReferenceAssemblies)
            End Get
            Set(value As Boolean)
                SetBooleanOption(SymbolSearchOptionsStorage.SearchReferenceAssemblies, value)
            End Set
        End Property

        Public Property Option_SuggestImportsForTypesInNuGetPackages As Boolean
            Get
                Return GetBooleanOption(SymbolSearchOptionsStorage.SearchNuGetPackages)
            End Get
            Set(value As Boolean)
                SetBooleanOption(SymbolSearchOptionsStorage.SearchNuGetPackages, value)
            End Set
        End Property
    End Class
End Namespace
