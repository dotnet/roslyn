' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Returns True for <see cref="Binder.IsSemanticModelBinder"/>
    ''' </summary>
    Friend Class SemanticModelBinder
        Inherits Binder

        Protected Sub New(containingBinder As Binder)
            MyBase.New(containingBinder)
        End Sub

        Public Shared Function Mark(binder As Binder) As Binder
            Return If(
                binder.IsSemanticModelBinder,
                binder,
                New SemanticModelBinder(binder))
        End Function

        Public NotOverridable Overrides ReadOnly Property IsSemanticModelBinder As Boolean
            Get
                Return True
            End Get
        End Property
    End Class

End Namespace