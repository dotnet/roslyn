Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Reflection
Imports Microsoft.VisualStudio.Editors.MyExtensibility.MyExtensibilityUtil

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    Partial Class MyExtensibilitySettings

        ''' ;AssemblyAutoOption
        ''' <summary>
        ''' Simple structure / class containing the assembly auto-add / auto-remove option.
        ''' </summary>
        Private Class AssemblyAutoOption

            Public Sub New(ByVal autoAdd As AssemblyOption, ByVal autoRemove As AssemblyOption)
                m_AutoAdd = autoAdd
                m_AutoRemove = autoRemove
            End Sub

            Public Property AutoAdd() As AssemblyOption
                Get
                    Return m_AutoAdd
                End Get
                Set(ByVal value As AssemblyOption)
                    m_AutoAdd = value
                End Set
            End Property

            Public Property AutoRemove() As AssemblyOption
                Get
                    Return m_AutoRemove
                End Get
                Set(ByVal value As AssemblyOption)
                    m_AutoRemove = value
                End Set
            End Property

            Private m_AutoAdd As AssemblyOption = AssemblyOption.Prompt
            Private m_AutoRemove As AssemblyOption = AssemblyOption.Prompt
        End Class
    End Class
End Namespace
