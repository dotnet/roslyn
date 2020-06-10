' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.ImplementAbstractClass
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ImplementAbstractClass
    <ExportCodeFixProvider(LanguageNames.VisualBasic,
        Name:=PredefinedCodeFixProviderNames.ImplementAbstractClass), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.GenerateType)>
    Friend Class VisualBasicImplementAbstractClassCodeFixProvider
        Inherits AbstractImplementAbstractClassCodeFixProvider(Of ClassBlockSyntax)

        Friend Const BC30610 As String = "BC30610" ' Class 'goo' must either be declared 'MustInherit' or override the following inherited 'MustOverride' member(s): 

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
            MyBase.New(BC30610)
        End Sub

        Protected Overrides Function GetClassIdentifier(classNode As ClassBlockSyntax) As SyntaxToken
            Return classNode.ClassStatement.Identifier
        End Function
    End Class
End Namespace
