' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

#Disable Warning CA1200 ' Avoid using cref tags with a prefix
    ''' <summary>
    ''' Causes all diagnostics related to <see cref="ObsoleteAttribute"/>
    ''' and <see cref="T:Windows.Foundation.MetadataDeprecatedAttribute"/> 
    ''' to be suppressed.
    ''' </summary>
    Friend NotInheritable Class SuppressDiagnosticsBinder
#Enable Warning CA1200 ' Avoid using cref tags with a prefix
        Inherits Binder

        Public Sub New(containingBinder As Binder)
            MyBase.New(containingBinder)
        End Sub

        Friend Overrides ReadOnly Property SuppressObsoleteDiagnostics As Boolean
            Get
                Return True
            End Get
        End Property
    End Class

End Namespace
