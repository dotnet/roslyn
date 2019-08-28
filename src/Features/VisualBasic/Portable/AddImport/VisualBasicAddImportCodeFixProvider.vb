' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.AddImport
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Packaging
Imports Microsoft.CodeAnalysis.SymbolSearch

Namespace Microsoft.CodeAnalysis.VisualBasic.AddImport
    Friend Module AddImportDiagnosticIds
        ''' <summary>
        ''' Type xxx is not defined
        ''' </summary>
        Friend Const BC30002 = "BC30002"

        ''' <summary>
        ''' Error 'x' is not declared
        ''' </summary>
        Friend Const BC30451 = "BC30451"

        ''' <summary>
        ''' xxx is not a member of yyy
        ''' </summary>
        Friend Const BC30456 = "BC30456"

        ''' <summary>
        ''' 'X' has no parameters and its return type cannot be indexed
        ''' </summary>
        Friend Const BC32016 = "BC32016"

        ''' <summary>
        ''' Too few type arguments
        ''' </summary>
        Friend Const BC32042 = "BC32042"

        ''' <summary>
        ''' Expression of type xxx is not queryable
        ''' </summary>
        Friend Const BC36593 = "BC36593"

        ''' <summary>
        ''' 'A' has no type parameters and so cannot have type arguments.
        ''' </summary>
        Friend Const BC32045 = "BC32045"

        ''' <summary>
        ''' 'A' is not accessible in this context because it is 'Friend'.
        ''' </summary>
        Friend Const BC30389 = "BC30389"

        ''' <summary>
        ''' 'A' cannot be used as an attribute because it does not inherit from 'System.Attribute'.
        ''' </summary>
        Friend Const BC31504 = "BC31504"

        ''' <summary>
        ''' Name 'A' is either not declared or not in the current scope.
        ''' </summary>
        Friend Const BC36610 = "BC36610"

        ''' <summary>
        ''' Cannot initialize the type 'A' with a collection initializer because it does not have an accessible 'Add' method
        ''' </summary>
        Friend Const BC36719 = "BC36719"

        ''' <summary>
        ''' Option Strict On disallows implicit conversions from 'Integer' to 'String'.
        ''' </summary>
        Friend Const BC30512 = "BC30512"

        ''' <summary>
        ''' 'A' is not accessible in this context because it is 'Private'.
        ''' </summary>
        Friend Const BC30390 = "BC30390"

        ''' <summary>
        ''' XML comment has a tag With a 'cref' attribute that could not be resolved. XML comment will be ignored.
        ''' </summary>
        Friend Const BC42309 = "BC42309"

        ''' <summary>
        ''' Type expected.
        ''' </summary>
        Friend Const BC30182 = "BC30182"

        Public ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30002, BC30451, BC30456, BC32042, BC36593, BC32045, BC30389, BC31504, BC32016, BC36610,
                                             BC36719, BC30512, BC30390, BC42309, BC30182, IDEDiagnosticIds.UnboundIdentifierId)
            End Get
        End Property
    End Module

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.AddImport), [Shared]>
    Friend Class VisualBasicAddImportCodeFixProvider
        Inherits AbstractAddImportCodeFixProvider

        <ImportingConstructor>
        Public Sub New()
        End Sub

        ''' <summary>	
        ''' For testing purposes so that tests can pass in mocks for these values.	
        ''' </summary>	
        Friend Sub New(installerService As IPackageInstallerService,
                       searchService As ISymbolSearchService)
            MyBase.New(installerService, searchService)
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = AddImportDiagnosticIds.FixableDiagnosticIds
    End Class
End Namespace
