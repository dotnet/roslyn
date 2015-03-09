' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' When compiling in metadata-only mode, <see cref="MethodCompiler"/> is not run. This is problematic because 
    ''' <see cref="MethodCompiler"/> adds synthesized explicit implementations to the list of synthesized definitions. 
    ''' In lieu of running <see cref="MethodCompiler"/>, this class performs a quick 
    ''' traversal of the symbol table and performs processing of synthesized symbols if necessary.
    ''' </summary>
    Friend Class SynthesizedMetadataCompiler
        Inherits VisualBasicSymbolVisitor

        Private ReadOnly _moduleBeingBuilt As PEModuleBuilder
        Private ReadOnly _cancellationToken As CancellationToken

        Private Sub New(moduleBeingBuilt As PEModuleBuilder, cancellationToken As CancellationToken)
            Me._moduleBeingBuilt = moduleBeingBuilt
            Me._cancellationToken = cancellationToken
        End Sub

        ''' <summary>
        ''' Traverse the symbol table and properly add/process synthesized extra metadata if needed.
        ''' </summary>
        Friend Shared Sub ProcessSynthesizedMembers(compilation As VisualBasicCompilation,
                                                        moduleBeingBuilt As PEModuleBuilder,
                                                        Optional cancellationToken As CancellationToken = Nothing)

            Debug.Assert(moduleBeingBuilt IsNot Nothing)
            Dim compiler = New SynthesizedMetadataCompiler(moduleBeingBuilt:=moduleBeingBuilt, cancellationToken:=cancellationToken)
            compilation.SourceModule.GlobalNamespace.Accept(compiler)
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
                Select Case member.Kind
                    Case SymbolKind.NamedType
                        member.Accept(Me)
                End Select
            Next
        End Sub

#If DEBUG Then
        Public Overrides Sub VisitProperty(symbol As PropertySymbol)
            Throw ExceptionUtilities.Unreachable
        End Sub

        Public Overrides Sub VisitMethod(symbol As MethodSymbol)
            Throw ExceptionUtilities.Unreachable
        End Sub
#End If

    End Class
End Namespace
