' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.AnalyzerPowerPack.Design
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis

Namespace Design

    ''' <summary>
    ''' CA1033: Interface methods should be callable by child types
    ''' <para>
    ''' Consider a base type that explicitly implements a public interface method.
    ''' A type that derives from the base type can access the inherited interface method only through a reference to the current instance ('Me' in VB) that is cast to the interface.
    ''' If the derived type re-implements (explicitly) the inherited interface method, the base implementation can no longer be accessed.
    ''' The call through the current instance reference will invoke the derived implementation; this causes recursion and an eventual stack overflow.
    ''' </para>
    ''' </summary>
    ''' <remarks>
    ''' This rule does not report a violation for an explicit implementation of IDisposable.Dispose when an externally visible Close() or System.IDisposable.Dispose(Boolean) method is provided.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicInterfaceMethodsShouldBeCallableByChildTypesAnalyzer
        Inherits InterfaceMethodsShouldBeCallableByChildTypesAnalyzer(Of InvocationExpressionSyntax)

        Protected Overrides Function ShouldExcludeCodeBlock(codeBlock As SyntaxNode) As Boolean
            Dim body = TryCast(codeBlock, MethodBlockBaseSyntax)
            If body IsNot Nothing Then
                If body.Statements.Count = 0 OrElse
                    body.Statements.Count = 1 AndAlso TypeOf body.Statements(0) Is ThrowStatementSyntax Then
                    ' Empty body OR body that just throws.
                    Return True
                End If
            End If

            Return False
        End Function
    End Class
End Namespace
