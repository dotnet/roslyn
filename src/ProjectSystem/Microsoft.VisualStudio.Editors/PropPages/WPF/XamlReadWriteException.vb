' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.Serialization

Namespace Microsoft.VisualStudio.Editors.PropertyPages.WPF

    <Serializable()> _
    Friend Class XamlReadWriteException
        Inherits PropertyPageException

        Public Sub New(ByVal message As String)
            MyBase.New(message)
        End Sub

        ''' <summary>
        ''' Deserialization constructor.  Required for serialization/remotability support
        '''   (not that we expect this to be needed).
        ''' </summary>
        ''' <param name="Info"></param>
        ''' <param name="Context"></param>
        ''' <remarks>
        '''See .NET Framework Developer's Guide, "Custom Serialization" for more information
        ''' </remarks>
        Protected Sub New(ByVal Info As SerializationInfo, ByVal Context As StreamingContext)
            MyBase.New(Info, Context)
        End Sub

    End Class

End Namespace
