' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                _cancellationToken.ThrowIfCancellationRequested()

                If Not ShouldSkipSymbol(symbol) Then

                    If symbol.IsGlobalNamespace Then
                        Debug.Assert(_assemblyName IsNot Nothing)

                        WriteLine("<?xml version=""1.0""?>")
                        WriteLine("<doc>")
                        Indent()

                        If Not _compilation.Options.OutputKind.IsNetModule() Then
                            WriteLine("<assembly>")
                            Indent()
                            WriteLine("<name>")
                            WriteLine(_assemblyName)
                            WriteLine("</name>")
                            Unindent()
                            WriteLine("</assembly>")
                        End If

                        WriteLine("<members>")
                        Indent()
                    End If

                    Debug.Assert(Not _isForSingleSymbol, "Do not expect a doc comment query for a single namespace")
                    For Each member In symbol.GetMembers()
                        Visit(member)
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
