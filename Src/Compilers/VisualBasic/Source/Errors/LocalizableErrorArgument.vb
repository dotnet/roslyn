' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Globalization
Imports System.Runtime.Serialization
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' The LocalizableErrorArgument class contains members members that allows formatting and serialization of error arguments.
    ''' Message IDs may refer to strings that need to be localized.   This struct makes an IFormattable wrapper around a MessageID
    ''' </summary>
    ''' <remarks></remarks>
    <Serializable>
    Public Structure LocalizableErrorArgument
        Implements IFormattable, ISerializable

        Private ReadOnly id As ERRID

        Friend Sub New(id As ERRID)
            Me.id = id
        End Sub

        Private Sub New(info As SerializationInfo, context As StreamingContext)
            Me.New(DirectCast(info.GetInt32("id"), ERRID))
        End Sub

        ''' <summary>
        '''Creates a string representing the unformatted LocalizableErrorArgument instance.
        ''' </summary>
        ''' <returns></returns>
        Public Overrides Function ToString() As String
            Return ToString(Nothing)
        End Function

        ''' <summary>
        ''' Creates a string representing the formatted LocalizableErrorArgument instance.
        ''' </summary>
        ''' <param name="format">A string to use for formatting.</param>
        ''' <param name="formatProvider">An object that supplies culture-specific format information about format.</param>
        ''' <returns></returns>
        Public Function ToString_IFormattable(format As String, formatProvider As IFormatProvider) As String Implements IFormattable.ToString
            Return ErrorFactory.IdToString(id, DirectCast(formatProvider, CultureInfo))
        End Function

        ''' <summary>
        ''' Implements the System.Runtime.Serialization.ISerializable interface and returns the data needed to serialize the LocalizableErrorArgument instance.
        ''' </summary>
        ''' <param name="info">A SerializationInfo object that contains the information required to serialize the LocalizableErrorArgument instance. <see cref="System.Runtime.Serialization.SerializationInfo"/> </param>
        ''' <param name="context">A StreamingContext object that contains the source and destination of the serialized stream associated with the LocalizableErrorArgument instance. <see cref="System.Runtime.Serialization.StreamingContext"/>   </param>
        Public Sub GetObjectData(info As SerializationInfo, context As StreamingContext) Implements ISerializable.GetObjectData
            info.AddValue("id", CInt(id))
        End Sub
    End Structure
End Namespace