' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Xml.Serialization

Namespace Microsoft.VisualStudio.Editors.MyApplication


    ''' <summary>
    ''' Utility class to (de)serialize the contents of a DesignTimeSetting object 
    ''' given a stream reader/writer
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class MyApplicationSerializer

        ''' <summary>
        '''  Deserialize XML stream of MyApplication data
        ''' </summary>
        ''' <param name="Reader">Text reader on stream containing object info</param>
        ''' <remarks></remarks>
        Public Shared Function Deserialize(ByVal Reader As TextReader) As MyApplicationData
            Dim serializer As XmlSerializer = New MyApplicationDataSerializer()
            'XmlSerializer(GetType(MyApplicationData))
            Dim xmlReader As System.Xml.XmlReader = System.Xml.XmlReader.Create(Reader)
            Return DirectCast(serializer.Deserialize(xmlReader), MyApplicationData)
        End Function

        ''' <summary>
        '''  Serialize MyApplication instance
        ''' </summary>
        ''' <param name="data">Instance to serialize</param>
        ''' <param name="Writer">Text writer on stream to serialize MyApplicationData to</param>
        ''' <remarks></remarks>
        Public Shared Sub Serialize(ByVal data As MyApplicationData, ByVal Writer As TextWriter)
            Dim serializer As XmlSerializer = new MyApplicationDataSerializer()
            'New XmlSerializer(GetType(MyApplicationData))
            serializer.Serialize(Writer, data)
        End Sub

    End Class
End Namespace
