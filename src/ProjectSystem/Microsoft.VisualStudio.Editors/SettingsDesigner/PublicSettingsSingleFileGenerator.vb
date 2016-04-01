' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Reflection

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    '@ <summary>
    '@ Generator for strongly typed settings wrapper class
    '@ </summary>
    '@ <remarks></remarks>
    <Guid("940f36b5-a42e-435e-8ef4-20b9d4801d22")> _
    Public Class PublicSettingsSingleFileGenerator
        Inherits SettingsSingleFileGeneratorBase

        Public Const SingleFileGeneratorName As String = "PublicSettingsSingleFileGenerator"

        ''' <summary>
        ''' Returns the default visibility of this properties
        ''' </summary>
        ''' <value>MemberAttributes indicating what visibility to make the generated properties.</value>
        Friend Overrides ReadOnly Property SettingsClassVisibility() As System.Reflection.TypeAttributes
            Get
                Return TypeAttributes.Sealed Or TypeAttributes.Public
            End Get
        End Property

    End Class
End Namespace
