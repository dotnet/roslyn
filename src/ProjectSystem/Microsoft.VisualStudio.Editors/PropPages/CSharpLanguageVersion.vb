'-----------------------------------------------------------------------------------------------------------
'
'  Copyright (c) Microsoft Corporation.  All rights reserved.
'
'-----------------------------------------------------------------------------------------------------------

Imports System

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' Represents a csharp language version and can be placed into a control
    ''' </summary>
    Class CSharpLanguageVersion

        Private Const LanguageVersion_Default As String = "default"
        Private Const LanguageVersion_ISO1 As String = "ISO-1"
        Private Const LanguageVersion_ISO2 As String = "ISO-2"
        Private Const LanguageVersion_3 As String = "3"
        Private Const LanguageVersion_DisplayNameFor3 As String = "C# 3.0"
        Private Const LanguageVersion_4 As String = "4"
        Private Const LanguageVersion_DisplayNameFor4 As String = "C# 4.0"
        Private Const LanguageVersion_5 As String = "5"
        Private Const LanguageVersion_DisplayNameFor5 As String = "C# 5.0"
        Private Const LanguageVersion_6 As String = "6"
        Private Const LanguageVersion_DisplayNameFor6 As String = "C# 6.0"

        ''' <summary>
        ''' Stores the property value corresponding to the language version
        ''' </summary>
        Private m_Value As String

        ''' <summary>
        ''' Stores the display name of the language version
        ''' </summary>
        Private m_DisplayName As String

        ''' <summary>
        ''' Constructor that uses the provided value and display name
        ''' </summary>
        Private Sub New(ByVal value As String, ByVal displayName As String)

            m_Value = value
            m_DisplayName = displayName

        End Sub

        ''' <summary>
        ''' Gets the value of the language version
        ''' </summary>
        Public ReadOnly Property Value() As String
            Get
                Return m_Value
            End Get
        End Property

        ''' <summary>
        ''' Use the display name for the string display
        ''' </summary>
        Public Overrides Function ToString() As String
            Return m_DisplayName
        End Function

        ''' <summary>
        ''' Return the 'default' language version object
        ''' </summary>
        Public Shared ReadOnly Property [Default]() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(LanguageVersion_Default, LanguageVersion_Default)
                Return value
            End Get
        End Property

        ''' <summary>
        ''' Return the ISO-1 language version object
        ''' </summary>
        Public Shared ReadOnly Property ISO1() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(LanguageVersion_ISO1, LanguageVersion_ISO1)
                Return value
            End Get
        End Property

        ''' <summary>
        ''' Return the ISO-2 language version object
        ''' </summary>
        Public Shared ReadOnly Property ISO2() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(LanguageVersion_ISO2, LanguageVersion_ISO2)
                Return value
            End Get
        End Property

        ''' <summary>
        ''' Return the C# 3.0 language version object
        ''' </summary>
        Public Shared ReadOnly Property Version3() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(LanguageVersion_3, LanguageVersion_DisplayNameFor3)
                Return value
            End Get
        End Property

        ''' <summary>
        ''' Return the C# 4.0 language version object
        ''' </summary>
        Public Shared ReadOnly Property Version4() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(LanguageVersion_4, LanguageVersion_DisplayNameFor4)
                Return value
            End Get
        End Property

        ''' <summary>
        ''' Return the C# 5.0 language version object
        ''' </summary>
        Public Shared ReadOnly Property Version5() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(LanguageVersion_5, LanguageVersion_DisplayNameFor5)
                Return value
            End Get
        End Property

        ''' <summary>
        ''' Return the C# 6.0 language version object
        ''' </summary>
        Public Shared ReadOnly Property Version6() As CSharpLanguageVersion
            Get
                Static value As New CSharpLanguageVersion(LanguageVersion_6, LanguageVersion_DisplayNameFor6)
                Return value
            End Get
        End Property

    End Class

End Namespace
