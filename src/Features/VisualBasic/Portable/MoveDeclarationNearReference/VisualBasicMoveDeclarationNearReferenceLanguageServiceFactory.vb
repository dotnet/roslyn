' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.MoveDeclarationNearReference

Namespace Microsoft.CodeAnalysis.VisualBasic.MoveDeclarationNearReference
    <ExportLanguageServiceFactory(GetType(IMoveDeclarationNearReferenceService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicMoveDeclarationNearReferenceLanguageServiceFactory
        Implements ILanguageServiceFactory

        Private ReadOnly _refactoringProvider As VisualBasicMoveDeclarationNearReferenceCodeRefactoringProvider

        <ImportingConstructor>
        Public Sub New(refactoringProvider As VisualBasicMoveDeclarationNearReferenceCodeRefactoringProvider)
            _refactoringProvider = refactoringProvider
        End Sub

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return _refactoringProvider
        End Function
    End Class
End Namespace
