' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Public Class VisualBasicCompilation

        Partial Friend Class DocumentationCommentCompiler
            Inherits VisualBasicSymbolVisitor

            Public Overrides Sub VisitNamespace(symbol As Symbols.NamespaceSymbol)
                Me._cancellationToken.ThrowIfCancellationRequested()

                If Not ShouldSkipSymbol(symbol) Then

                    If symbol.IsGlobalNamespace Then
                        Debug.Assert(Me._assemblyName IsNot Nothing)

                        WriteLine("<?xml version=""1.0""?>")
                        WriteLine("<doc>")
                        Indent()

                        If Not Me._compilation.Options.OutputKind.IsNetModule() Then
                            WriteLine("<assembly>")
                            Indent()
                            WriteLine("<name>")
                            WriteLine(Me._assemblyName)
                            WriteLine("</name>")
                            Unindent()
                            WriteLine("</assembly>")
                        End If

                        WriteLine("<members>")
                        Indent()
                    End If

                    Debug.Assert(Not Me._isForSingleSymbol, "Do not expect a doc comment query for a single namespace")
                    For Each member In symbol.GetMembers()
                        Me.Visit(member)
                    Next

                    If symbol.IsGlobalNamespace Then
                        Unindent()
                        WriteLine("</members>")
                        Unindent()
                        WriteLine("</doc>")
                    End If

                End If
            End Sub

        End Class

    End Class
End Namespace
