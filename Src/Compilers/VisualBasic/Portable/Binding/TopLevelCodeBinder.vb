' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Provides context for binding top-level statements in a script. 
    ''' </summary>
    Friend NotInheritable Class TopLevelCodeBinder
        Inherits SubOrFunctionBodyBinder

        ''' <summary>
        ''' Create binder for binding the body of a method. 
        ''' </summary>
        Public Sub New(scriptConstructor As MethodSymbol, containingBinder As Binder)
            MyBase.New(scriptConstructor, scriptConstructor.Syntax, containingBinder)
            Debug.Assert(scriptConstructor.ContainingType.IsScriptClass AndAlso scriptConstructor.MethodKind = MethodKind.Constructor)
        End Sub

        Public Overrides Function GetLocalForFunctionValue() As LocalSymbol
            Return Nothing
        End Function

        Public Overrides ReadOnly Property IsInQuery As Boolean
            Get
                Return False
            End Get
        End Property
    End Class

End Namespace