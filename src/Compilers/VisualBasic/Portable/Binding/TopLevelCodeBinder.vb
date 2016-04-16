' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Provides context for binding top-level statements in a script. 
    ''' </summary>
    Friend NotInheritable Class TopLevelCodeBinder
        Inherits SubOrFunctionBodyBinder

        ''' <summary>
        ''' Create binder for binding the body of a method. 
        ''' </summary>
        Public Sub New(scriptInitializer As MethodSymbol, containingBinder As Binder)
            MyBase.New(scriptInitializer, scriptInitializer.Syntax, containingBinder)
            Debug.Assert(scriptInitializer.ContainingType.IsScriptClass)
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
