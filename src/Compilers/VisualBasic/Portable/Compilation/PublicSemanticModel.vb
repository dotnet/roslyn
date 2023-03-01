' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Instances of this <see cref="SemanticModel"/> can be exposed to external consumers.
    ''' Other types of <see cref="VBSemanticModel"/> are not designed for direct exposure 
    ''' and their implementation might not be able to handle external requests properly.
    ''' </summary>
    Friend MustInherit Class PublicSemanticModel
        Inherits VBSemanticModel

        Friend NotOverridable Overrides ReadOnly Property ContainingPublicModelOrSelf As SemanticModel
            Get
                Return Me
            End Get
        End Property

    End Class
End Namespace
