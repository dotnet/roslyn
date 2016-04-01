' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict On
Option Explicit On

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    Friend Partial Class MyExtensibilitySettings

        ''' ;AssemblyAutoOption
        ''' <summary>
        ''' Simple structure / class containing the assembly auto-add / auto-remove option.
        ''' </summary>
        Private Class AssemblyAutoOption

            Public Sub New(ByVal autoAdd As AssemblyOption, ByVal autoRemove As AssemblyOption)
                _autoAdd = autoAdd
                _autoRemove = autoRemove
            End Sub

            Public Property AutoAdd() As AssemblyOption
                Get
                    Return _autoAdd
                End Get
                Set(ByVal value As AssemblyOption)
                    _autoAdd = value
                End Set
            End Property

            Public Property AutoRemove() As AssemblyOption
                Get
                    Return _autoRemove
                End Get
                Set(ByVal value As AssemblyOption)
                    _autoRemove = value
                End Set
            End Property

            Private _autoAdd As AssemblyOption = AssemblyOption.Prompt
            Private _autoRemove As AssemblyOption = AssemblyOption.Prompt
        End Class
    End Class
End Namespace
