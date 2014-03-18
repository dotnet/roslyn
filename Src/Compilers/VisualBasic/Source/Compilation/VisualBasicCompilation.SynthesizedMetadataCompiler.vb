' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Instrumentation
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Public Class VisualBasicCompilation

        ''' <summary>
        ''' When compiling in metadata-only mode, MethodCompiler is not run. This is problematic because 
        ''' method body compiler adds synthesized explicit implementations to the list of compiler 
        ''' generated definitions. In lieu of running MethodBodyCompiler, this class performs a quick 
        ''' traversal of the symbol table and performs processing of synthesized symbols if necessary
        ''' </summary>
        Friend Class SynthesizedMetadataCompiler
            Inherits VisualBasicSymbolVisitor

            Private ReadOnly _moduleBeingBuilt As PEModuleBuilder
            Private ReadOnly _cancellationToken As CancellationToken

            Private Sub New(moduleBeingBuilt As PEModuleBuilder,
                            cancellationToken As CancellationToken)

                Me._moduleBeingBuilt = moduleBeingBuilt
                Me._cancellationToken = cancellationToken
            End Sub

            ''' <summary>
            ''' Traverse the symbol table and properly add/process synthesized extra metadata if needed
            ''' </summary>
            Friend Shared Sub ProcessSynthesizedMembers(compilation As VisualBasicCompilation,
                                                        moduleBeingBuilt As PEModuleBuilder,
                                                        Optional cancellationToken As CancellationToken = Nothing)

                Debug.Assert(moduleBeingBuilt IsNot Nothing)
                Using Logger.LogBlock(FunctionId.CSharp_Compiler_CompileSynthesizedMethodMetadata, message:=compilation.AssemblyName, cancellationToken:=cancellationToken)
                    Dim compiler = New SynthesizedMetadataCompiler(moduleBeingBuilt:=moduleBeingBuilt, cancellationToken:=cancellationToken)
                    compilation.SourceModule.GlobalNamespace.Accept(compiler)
                End Using
            End Sub

            Public Overrides Sub VisitNamespace(symbol As NamespaceSymbol)
                Me._cancellationToken.ThrowIfCancellationRequested()
                For Each member In symbol.GetMembers()
                    member.Accept(Me)
                Next
            End Sub

            Public Overrides Sub VisitNamedType(symbol As NamedTypeSymbol)
                Me._cancellationToken.ThrowIfCancellationRequested()

                For Each member In symbol.GetMembers()
                    member.Accept(Me)
                Next
            End Sub

            Public Overrides Sub VisitMethod(symbol As MethodSymbol)
                Me._cancellationToken.ThrowIfCancellationRequested()

                If symbol.IsAsync Then
                    Me._moduleBeingBuilt.AddCompilerGeneratedDefinition(symbol.ContainingType, symbol.GetAsyncStateMachineType())
                End If
            End Sub

        End Class
    End Class
End Namespace
