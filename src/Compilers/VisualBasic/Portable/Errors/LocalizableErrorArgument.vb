' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' The LocalizableErrorArgument class contains members that allows formatting and serialization of error arguments.
    ''' Message IDs may refer to strings that need to be localized.   This struct makes an IFormattable wrapper around a MessageID
    ''' </summary>
    Public Structure LocalizableErrorArgument
        Implements IFormattable

        Private ReadOnly _id As ERRID

        Friend Sub New(id As ERRID)
            Me._id = id
        End Sub

        ''' <summary>
        '''Creates a string representing the unformatted LocalizableErrorArgument instance.
        ''' </summary>
        Public Overrides Function ToString() As String
            Return ToString_IFormattable(Nothing, Nothing)
        End Function

        ''' <summary>
        ''' Creates a string representing the formatted LocalizableErrorArgument instance.
        ''' </summary>
        ''' <param name="format">A string to use for formatting.</param>
        ''' <param name="formatProvider">An object that supplies culture-specific format information about format.</param>
        Public Function ToString_IFormattable(format As String, formatProvider As IFormatProvider) As String Implements IFormattable.ToString
            Return ErrorFactory.IdToString(_id, DirectCast(formatProvider, CultureInfo))
        End Function
    End Structure
End Namespace
