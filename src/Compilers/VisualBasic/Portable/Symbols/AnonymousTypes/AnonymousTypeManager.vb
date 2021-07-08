' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary> 
    ''' Manages anonymous types and delegates created on module level. All requests 
    ''' for anonymous type/delegate symbols go via the instance of this class.
    ''' 
    ''' Manager also is in charge of creating implementation types which are used in 
    ''' emit phase to substitute anonymous type/delegate public symbols.
    ''' </summary>
    Partial Friend NotInheritable Class AnonymousTypeManager
        Inherits CommonAnonymousTypeManager

        ''' <summary> Source module </summary>
        Public ReadOnly Property ContainingModule As SourceModuleSymbol
            Get
                Return DirectCast(Compilation.SourceModule, SourceModuleSymbol)
            End Get
        End Property

        ''' <summary> Owning compilationSource module </summary>
        Public ReadOnly Compilation As VisualBasicCompilation

        Public Sub New(compilation As VisualBasicCompilation)
            Me.Compilation = compilation
        End Sub

        ''' <summary> 
        ''' Given anonymous type descriptor provided construct an anonymous type symbol
        ''' </summary>
        Public Function ConstructAnonymousTypeSymbol(typeDescr As AnonymousTypeDescriptor) As AnonymousTypePublicSymbol
            Return New AnonymousTypePublicSymbol(Me, typeDescr)
        End Function

        ''' <summary> 
        ''' Given anonymous delegate descriptor provided, construct an anonymous delegate symbol
        ''' </summary>
        Public Function ConstructAnonymousDelegateSymbol(delegateDescriptor As AnonymousTypeDescriptor) As AnonymousDelegatePublicSymbol
            Return New AnonymousDelegatePublicSymbol(Me, delegateDescriptor)
        End Function

    End Class

End Namespace
