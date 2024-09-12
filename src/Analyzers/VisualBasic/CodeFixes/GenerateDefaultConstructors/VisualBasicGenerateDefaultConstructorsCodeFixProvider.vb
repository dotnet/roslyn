' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.GenerateDefaultConstructors
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateDefaultConstructors
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.GenerateDefaultConstructors), [Shared]>
    Friend NotInheritable Class VisualBasicGenerateDefaultConstructorsCodeFixProvider
        Inherits AbstractGenerateDefaultConstructorCodeFixProvider

        Private Const BC30387 As String = NameOf(BC30387) ' Class 'C' must declare a 'Sub New' because its base class 'B' does not have an accessible 'Sub New' that can be called with no arguments.	
        Private Const BC40056 As String = NameOf(BC40056) ' Namespace or type specified in the Imports 'TestProj' doesn't contain any public member or cannot be found. Make sure the namespace or the type is defined and contains at least one public member. Make sure the imported element name doesn't use any aliases.

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property FixableDiagnosticIds As Immutable.ImmutableArray(Of String) =
            ImmutableArray.Create(BC30387, BC40056)

        Protected Overrides Function TryGetTypeName(typeDeclaration As SyntaxNode) As SyntaxToken?
            Return TryCast(typeDeclaration, TypeBlockSyntax)?.BlockStatement.Identifier
        End Function
    End Class
End Namespace
