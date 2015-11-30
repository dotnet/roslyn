' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Provides context for binding statements in speculative code.
    ''' </summary>
    Friend NotInheritable Class SpeculativeStatementBinder
        Inherits ExecutableCodeBinder

        ''' <summary>
        ''' Create binder for binding statements in speculative code. 
        ''' </summary>
        Public Sub New(root As VisualBasicSyntaxNode, containingBinder As Binder)
            MyBase.New(root, containingBinder)
        End Sub

        Public Overrides ReadOnly Property IsSemanticModelBinder As Boolean
            Get
                Return True
            End Get
        End Property
    End Class

End Namespace
