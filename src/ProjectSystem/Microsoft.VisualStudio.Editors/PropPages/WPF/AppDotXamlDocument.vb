'------------------------------------------------------------------------------
' <copyright company='Microsoft Corporation'>           
'    Copyright (c) Microsoft Corporation. All Rights Reserved.                
'    Information Contained Herein is Proprietary and Confidential.            
' </copyright>                                                                
'------------------------------------------------------------------------------

Imports Microsoft.VisualBasic
Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Xml

Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.TextManager.Interop
Imports Microsoft.VisualStudio.Editors.Common.ArgumentValidation

Namespace Microsoft.VisualStudio.Editors.PropertyPages.WPF

    ''' <summary>
    ''' When reading and writing the Application.xaml file, we have a requirement that
    '''   we do not change the user's formatting, comments, etc.  Thus we can't simply
    '''   read in the XML, modify it, and write it back out.  Instead, we have to
    '''   modify just the parts we need to directly in the text buffer.  This class
    '''   handles that surprisingly complex job.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class AppDotXamlDocument
        Implements IDebugLockCheck
        Implements IDisposable
        Implements IReplaceText

        'REFERENCES:
        '  XAML Overviews: http://windowssdk.msdn.microsoft.com/en-us/library/ms744825.aspx
        '  Property element syntax: http://windowssdk.msdn.microsoft.com/en-us/library/ms788723(VS.80).aspx#PESyntax

#Region "Interface IReplaceText"

        ''' <summary>
        ''' A simple interface to allow XamlProperty to ask the AppDotXamlDocument to 
        '''   make text replacements.
        ''' </summary>
        ''' <remarks></remarks>
        Friend Interface IReplaceText

            ''' <summary>
            ''' Replace the text at the given location in the buffer with new text.
            ''' </summary>
            ''' <param name="sourceStart"></param>
            ''' <param name="sourceEnd"></param>
            ''' <param name="newText"></param>
            ''' <remarks></remarks>
            Sub ReplaceText(ByVal sourceStart As Location, ByVal sourceEnd As Location, ByVal newText As String)

        End Interface

#End Region

#Region "Nested class 'Location'"

        ''' <summary>
        ''' Represents a position in a text buffer
        ''' </summary>
        ''' <remarks></remarks>
        Friend Class Location
            Public LineIndex As Integer 'Zero-based line #
            Public CharIndex As Integer 'Zero-based character on line

            Public Sub New(ByVal lineIndex As Integer, ByVal charOnLineIndex As Integer)
                If lineIndex < 0 Then
                    Throw CreateArgumentException("lineIndex")
                End If
                If charOnLineIndex < 0 Then
                    Throw CreateArgumentException("charOnLineIndex")
                End If

                Me.LineIndex = lineIndex
                Me.CharIndex = charOnLineIndex
            End Sub


            ''' <summary>
            ''' Creates a location corresponding to the current location of the
            ''' XmlReader
            ''' </summary>
            ''' <param name="reader"></param>
            ''' <remarks></remarks>
            Public Sub New(ByVal reader As XmlReader)
                Me.New(CType(reader, IXmlLineInfo).LineNumber - 1, CType(reader, IXmlLineInfo).LinePosition - 1)
            End Sub

            Public Function Shift(ByVal charIndexToAdd As Integer) As Location
                Return New Location(Me.LineIndex, Me.CharIndex + charIndexToAdd)
            End Function

        End Class

#End Region

#Region "Nested class BufferLock"

        ''' <summary>
        ''' Used by the document to verify BufferLock is used when it's needed
        ''' </summary>
        ''' <remarks></remarks>
        Interface IDebugLockCheck
            Sub OnBufferLock()
            Sub OnBufferUnlock()
        End Interface

        ''' <summary>
        ''' We need to make sure the buffer doesn't change while we're looking up properties
        '''  and changing them.  Our XmlReader() needs to be in sync with the actual text
        '''  in the buffer.
        ''' This class keeps the buffer locked until it is disposed.
        ''' </summary>
        ''' <remarks></remarks>
        Private Class BufferLock
            Implements IDisposable

            Private m_isDisposed As Boolean
            Private m_buffer As IVsTextLines
            Private m_debugLockCheck As IDebugLockCheck 'Used by the document to verify BufferLock is used when it's needed

            Public Sub New(ByVal buffer As IVsTextLines, ByVal debugLockCheck As IDebugLockCheck)
                If buffer Is Nothing Then
                    Throw New ArgumentNullException("buffer")
                End If
                If debugLockCheck Is Nothing Then
                    Throw New ArgumentNullException("debugLockCheck")
                End If

                m_buffer = buffer
                m_debugLockCheck = debugLockCheck

                m_buffer.LockBuffer()
                m_debugLockCheck.OnBufferLock()
            End Sub

#If DEBUG Then
            Protected Overrides Sub Finalize()
                Debug.Assert(m_isDisposed, "Didn't dispose a BufferLock object")
            End Sub
#End If

#Region "IDisposable Support"

            Protected Overridable Sub Dispose(ByVal disposing As Boolean)
                Try
                    If disposing Then
                        If Not m_isDisposed Then
                            m_buffer.UnlockBuffer()
                            m_debugLockCheck.OnBufferUnlock()
                            m_buffer = Nothing
                        End If
                        m_isDisposed = True
                    End If
                Catch ex As Exception
                    Debug.Fail("Exception in Dispose: " & ex.ToString())
                    Throw
                End Try
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
                ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
                Dispose(True)
                GC.SuppressFinalize(Me)
            End Sub

#End Region

        End Class

#End Region

#Region "Nested class XamlProperty"

        ''' <summary>
        ''' Represents a property value found in the XAML file.
        ''' </summary>
        ''' <remarks></remarks>
        <DebuggerDisplay("{ActualDefinitionText}, Value={UnescapedValue}")> _
        Friend MustInherit Class XamlProperty
            Protected m_vsTextLines As IVsTextLines
            Private m_definitionIncludesQuotes As Boolean
            Private m_unescapedValue As String 'Unescaped, translated value of the property from the XmlReader
            Private m_startLocation As Location
            Private m_endLocationPlusOne As Location 'Points to the index *after* the last character in the range, just like IVsTextLines expects


            Public Sub New(ByVal vsTextLines As IVsTextLines, ByVal startLocation As Location, ByVal endLocation As Location, ByVal unescapedValue As String, ByVal definitionIncludesQuotes As Boolean)
                If vsTextLines Is Nothing Then
                    Throw New ArgumentNullException("vsTextLines")
                End If
                If startLocation Is Nothing Then
                    Throw New ArgumentNullException("nodeStart")
                End If
                If endLocation Is Nothing Then
                    Throw New ArgumentNullException("nodeEnd")
                End If
                If unescapedValue Is Nothing Then
                    unescapedValue = ""
                End If

                m_startLocation = startLocation
                m_endLocationPlusOne = endLocation
                m_unescapedValue = unescapedValue
                m_vsTextLines = vsTextLines
                m_definitionIncludesQuotes = definitionIncludesQuotes
            End Sub

            ''' <summary>
            ''' Retrieves the actual text for the value of the property, unescaped, as it
            '''   appears in the .xaml file.  If DefinitionIncludesQuotes=True, then this
            '''   includes the beginning/ending quote
            ''' </summary>
            ''' <value></value>
            ''' <returns></returns>
            ''' <remarks></remarks>
            Public Overridable ReadOnly Property ActualDefinitionText() As String
                Get
                    Dim buffer As String = Nothing
                    ErrorHandler.ThrowOnFailure(m_vsTextLines.GetLineText(DefinitionStart.LineIndex, DefinitionStart.CharIndex, DefinitionEndPlusOne.LineIndex, DefinitionEndPlusOne.CharIndex, buffer))
                    Return buffer
                End Get
            End Property

            Public ReadOnly Property UnescapedValue() As String
                Get
                    Return m_unescapedValue
                End Get
            End Property

            Public ReadOnly Property DefinitionStart() As Location
                Get
                    Return m_startLocation
                End Get
            End Property

            Public ReadOnly Property DefinitionEndPlusOne() As Location
                Get
                    Return m_endLocationPlusOne
                End Get
            End Property

            ''' <summary>
            ''' Replace the property's value in the XAML
            ''' </summary>
            ''' <param name="replaceTextInstance"></param>
            ''' <param name="value"></param>
            ''' <remarks></remarks>
            Public Overridable Sub SetProperty(ByVal replaceTextInstance As IReplaceText, ByVal value As String)
                If Me.UnescapedValue.Equals(value, StringComparison.Ordinal) Then
                    'The property value is not changing.  Leave things alone.
                    Return
                End If

                'Replace just the string in the buffer with the new value.
                Dim replaceStart As Location = Me.DefinitionStart
                Dim replaceEnd As Location = Me.DefinitionEndPlusOne
                Dim newText As String = EscapeXmlString(value)
                If m_definitionIncludesQuotes Then
                    newText = """" & newText & """"
                End If

                'We know where to replace, so go ahead and do it.
                replaceTextInstance.ReplaceText(replaceStart, replaceEnd, newText)
            End Sub

        End Class

        ''' <summary>
        ''' Represents a property that was found in the XAML file in attribute syntax
        ''' </summary>
        ''' <remarks></remarks>
        <DebuggerDisplay("{ActualDefinitionText}, Value={UnescapedValue}")> _
        Friend Class XamlPropertyInAttributeSyntax
            Inherits XamlProperty

            Public Sub New(ByVal vsTextLines As IVsTextLines, ByVal definitionStart As Location, ByVal definitionEnd As Location, ByVal unescapedValue As String)
                MyBase.New(vsTextLines, definitionStart, definitionEnd, unescapedValue, definitionIncludesQuotes:=True)
            End Sub

        End Class

        ''' <summary>
        ''' Represents a property that was found in property element syntax with a start and end tag.
        ''' </summary>
        ''' <remarks></remarks>
        <DebuggerDisplay("{ActualDefinitionText}, Value={UnescapedValue}")> _
        Friend Class XamlPropertyInPropertyElementSyntax
            Inherits XamlProperty

            Public Sub New(ByVal vsTextLines As IVsTextLines, ByVal valueStart As Location, ByVal valueEnd As Location, ByVal unescapedValue As String)
                MyBase.New(vsTextLines, valueStart, valueEnd, unescapedValue, definitionIncludesQuotes:=False)
            End Sub
        End Class

        ''' <summary>
        ''' Represents a property that was found in property element syntax with an empty tag.
        ''' </summary>
        ''' <remarks></remarks>
        <DebuggerDisplay("{ActualDefinitionText}, Value={UnescapedValue}")> _
        Friend Class XamlPropertyInPropertyElementSyntaxWithEmptyTag
            Inherits XamlPropertyInPropertyElementSyntax

            'This class represents a property that was found in property element syntax with an empty tag,
            '  e.g. <Application.StartupUri/>


            Private m_fullyQualifiedPropertyName As String

            ''' <summary>
            ''' Constructor.
            ''' </summary>
            ''' <param name="vsTextLines"></param>
            ''' <param name="elementStart"></param>
            ''' <param name="elementEnd"></param>
            ''' <remarks>
            ''' In the case of XamlPropertyInPropertyElementSyntaxWithEmptyTag, the elementStart/elementEnd
            '''   location pair indicates the start/end of the tag itself, not the value (since there is no
            '''   value - the element is an empty tag).
            ''' </remarks>
            Public Sub New(ByVal vsTextLines As IVsTextLines, ByVal fullyQualifiedPropertyName As String, ByVal elementStart As Location, ByVal elementEnd As Location)
                MyBase.New(vsTextLines, elementStart, elementEnd, UnescapedValue:="")

                If fullyQualifiedPropertyName Is Nothing Then
                    Throw New ArgumentNullException("fullyQualifiedPropertyName")
                End If

                m_fullyQualifiedPropertyName = fullyQualifiedPropertyName
            End Sub

            Public Overrides ReadOnly Property ActualDefinitionText() As String
                Get
                    Return ""
                End Get
            End Property

            Public Overrides Sub SetProperty(ByVal replaceTextInstance As IReplaceText, ByVal value As String)
                If Me.UnescapedValue.Equals(value, StringComparison.Ordinal) Then
                    'The property value is not changing.  Leave things alone.
                    Return
                End If

                'Replace the empty tag in the buffer with a start/end element tag
                '  and the new value
                Dim replaceStart As Location = Me.DefinitionStart
                Dim replaceEnd As Location = Me.DefinitionEndPlusOne
                Dim newText As String = _
                    "<" & m_fullyQualifiedPropertyName & ">" _
                    & EscapeXmlString(value) _
                    & "</" & m_fullyQualifiedPropertyName & ">"

                'We know where to replace, so go ahead and do it.
                replaceTextInstance.ReplaceText(replaceStart, replaceEnd, newText)
            End Sub

        End Class

#End Region

        Private Const ELEMENT_APPLICATION As String = "Application"
        Private Const PROPERTY_STARTUPURI As String = "StartupUri"
        Private Const PROPERTY_SHUTDOWNMODE As String = "ShutdownMode"

        'A pointer to the text buffer as IVsTextLines
        Private m_vsTextLines As IVsTextLines
        Private m_isDisposed As Boolean

#Region "Constructor"

        Public Sub New(ByVal vsTextLines As IVsTextLines)
            If vsTextLines Is Nothing Then
                Throw New ArgumentNullException("vsTextLines")
            End If
            m_vsTextLines = vsTextLines
        End Sub

#End Region

#Region "Dispose"

        Protected Overrides Sub Finalize()
            Debug.Assert(m_isDisposed, "Didn't dispose an AppDotXamlDocument object")
        End Sub

        ' IDisposable
        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not Me.m_isDisposed Then
                If disposing Then
                    Debug.Assert(m_debugBufferLockCount = 0, "Missing buffer unlock")
                End If

                m_vsTextLines = Nothing
                m_isDisposed = True
            End If
            Me.m_isDisposed = True
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

#End Region

#Region "GetText/GetAllText utilities"

        ''' <summary>
        ''' Retrieves the text between the given buffer line/char points
        ''' </summary>
        ''' <param name="startLine"></param>
        ''' <param name="startIndex"></param>
        ''' <param name="endLine"></param>
        ''' <param name="endIndex"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetText(ByVal startLine As Integer, ByVal startIndex As Integer, ByVal endLine As Integer, ByVal endIndex As Integer) As String
            Dim text As String = Nothing
            ErrorHandler.ThrowOnFailure(m_vsTextLines.GetLineText(startLine, startIndex, endLine, endIndex, text))
            Return text
        End Function

        ''' <summary>
        ''' Retrieves the text starting at the given point and with the given length
        ''' </summary>
        ''' <param name="startLine"></param>
        ''' <param name="startIndex"></param>
        ''' <param name="count">Count of characters to return</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetText(ByVal startLine As Integer, ByVal startIndex As Integer, ByVal count As Integer) As String
            Return GetText(startLine, startIndex, startLine, startIndex + count)
        End Function

        ''' <summary>
        ''' Retrieves the text between the given buffer line/char points
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetText(ByVal startLocation As Location, ByVal endLocation As Location) As String
            Return GetText(startLocation.LineIndex, startLocation.CharIndex, endLocation.LineIndex, endLocation.CharIndex)
        End Function

        ''' <summary>
        ''' Retrieves the text starting at the given point and with the given length
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetText(ByVal startLocation As Location, ByVal count As Integer) As String
            Return GetText(startLocation.LineIndex, startLocation.CharIndex, count)
        End Function

        ''' <summary>
        ''' Retrieves all of the text in the buffer
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function GetAllText() As String
            Dim lastLine, lastIndex As Integer
            ErrorHandler.ThrowOnFailure(m_vsTextLines.GetLastLineIndex(lastLine, lastIndex))
            Return GetText(0, 0, lastLine, lastIndex)
        End Function

#End Region

#Region "Escaping/Unescaping XML strings"

        ''' <summary>
        ''' Escape a string in XML format, including double and single quotes
        ''' </summary>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function EscapeXmlString(ByVal value As String) As String
            If value Is Nothing Then
                value = String.Empty
            End If

            Dim sb As New StringBuilder()
            Dim settings As New XmlWriterSettings
            settings.ConformanceLevel = ConformanceLevel.Fragment
            Dim xmlWriter As XmlWriter = XmlTextWriter.Create(sb, settings)
            xmlWriter.WriteString(value)
            xmlWriter.Close()
            Dim escapedString As String = sb.ToString()

            'Now escape double and single quotes
            Return escapedString.Replace("""", "&quot;").Replace("'", "&apos;")
        End Function

        ''' <summary>
        ''' Unescapes an element's content value from escaped XML format.
        ''' </summary>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function UnescapeXmlContent(ByVal value As String) As String
            If value Is Nothing Then
                value = String.Empty
            End If

            'Escape any double quotes

            'Make as content of an element
            Dim xml As String = "<a>" & value & "</a>"
            Dim stringReader As New StringReader(xml)
            Dim settings As New XmlReaderSettings
            settings.ConformanceLevel = ConformanceLevel.Fragment
            Dim xmlReader As XmlReader = XmlTextReader.Create(stringReader, settings)
            xmlReader.ReadToFollowing("a")

            Dim content As String = xmlReader.ReadElementContentAsString()
            Return content
        End Function

#End Region

        ''' <summary>
        ''' Finds the Application element that all application.xaml files must include as
        '''   the single root node.
        ''' </summary>
        ''' <param name="reader"></param>
        ''' <remarks></remarks>
        Public Shared Sub MoveToApplicationRootElement(ByVal reader As XmlTextReader)
            'XAML files must have only one root element.  For app.xaml, it must be "Application"
            If reader.MoveToContent() = XmlNodeType.Element And reader.Name = ELEMENT_APPLICATION Then
                'Okay
                Return
            End If

            Throw New XamlReadWriteException(SR.GetString(SR.PPG_WPFApp_Xaml_CouldntFindRootElement, ELEMENT_APPLICATION))
        End Sub

#Region "CreateXmlTextReader"

        ''' <summary>
        ''' Creates an XmlTextReader for the text
        '''   in the buffer.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function CreateXmlTextReader() As XmlTextReader
            Debug.Assert(m_debugBufferLockCount > 0, "Should be using BufferLock!")
            Dim stringReader As New StringReader(GetAllText())
            Dim xmlTextReader As xmlTextReader = New xmlTextReader(stringReader)

            ' Required by Fxcop rule CA3054 - DoNotAllowDTDXmlTextReader
            xmlTextReader.DtdProcessing = DtdProcessing.Prohibit
            Return xmlTextReader
        End Function

#End Region

#Region "Helpers for reading properties"

        ''' <summary>
        ''' Finds the value of the given property inside the xaml file.  If
        '''   the property is not set in the xaml, an empty string is returned.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetApplicationPropertyValue(ByVal propertyName As String) As String
            Using lock As New BufferLock(m_vsTextLines, Me)
                Dim reader As XmlTextReader = CreateXmlTextReader()
                Dim prop As XamlProperty = FindApplicationPropertyInXaml(reader, propertyName)
                If prop IsNot Nothing Then
                    Return prop.UnescapedValue
                Else
                    Return String.Empty
                End If
            End Using
        End Function

        ''' <summary>
        ''' Find the closing angle bracket on a single line.
        ''' See comments on FindClosingAngleBracket.
        ''' </summary>
        ''' <param name="line"></param>
        ''' <returns>The index on the line where the closing angle bracket is found, or -1 if not found.</returns>
        ''' <remarks></remarks>
        Public Function FindClosingAngleBracketHelper(ByVal line As String) As Integer
            Dim index As Integer = 0

            Const SingleQuote As Char = "'"c
            Const DoubleQuote As Char = """"c
            While index < line.Length
                'Find the next character of interest
                index = line.IndexOfAny( _
                    New Char() {SingleQuote, DoubleQuote, "/"c, ">"c}, _
                    index)
                If index < 0 Then
                    Return -1
                End If

                Dim characterOfInterest As Char = line.Chars(index)
                Select Case characterOfInterest
                    Case SingleQuote, DoubleQuote
                        'We have a string.  Skip past it.
                        Dim closingQuote As Integer = line.IndexOf( _
                            characterOfInterest, _
                            index + 1)
                        If closingQuote < 0 Then
                            'String not terminated.
                            Return -1
                        Else
                            index = closingQuote + 1
                        End If
                    Case ">"c
                        'Found ">"
                        Return index
                    Case "/"c
                        If line.Substring(index).StartsWith("/>") Then
                            'Found "/>"
                            Return index
                        Else
                            'Keep searching past the '/'
                            index += 1
                        End If
                    Case Else
                        Debug.Fail("Shouldn't reach here")
                End Select

            End While

            Return -1
        End Function

        ''' <summary>
        ''' Searches forward from the given location, skipping quoted strings
        '''   (single and double quoted), until it finds a closing angle 
        '''   bracket (">" or "/">).
        ''' </summary>
        ''' <param name="startLocation"></param>
        ''' <returns>The location of the found ">" or "/>".  If it is not found, returns Nothing.</returns>
        ''' <remarks>
        ''' It's assumed that the XML is well-formed
        ''' </remarks>
        Public Function FindClosingAngleBracket(ByVal startLocation As Location) As Location
            Dim iLastLine, iLastIndex As Integer
            ErrorHandler.ThrowOnFailure(m_vsTextLines.GetLastLineIndex(iLastLine, iLastIndex))

            For iLine As Integer = startLocation.LineIndex To iLastLine
                Dim iStartIndexForLine, iEndIndexForLine As Integer

                If iLine = startLocation.LineIndex Then
                    iStartIndexForLine = startLocation.CharIndex
                Else
                    iStartIndexForLine = 0
                End If

                If iLine = iLastLine Then
                    iEndIndexForLine = iLastIndex
                Else
                    Dim iLineLength As Integer
                    ErrorHandler.ThrowOnFailure(m_vsTextLines.GetLengthOfLine(iLine, iLineLength))
                    iEndIndexForLine = iLineLength
                End If

                Dim lineText As String = GetText(iLine, iStartIndexForLine, iLine, iEndIndexForLine)

                Dim foundIndex As Integer = FindClosingAngleBracketHelper(lineText)
                If foundIndex >= 0 Then
                    Return New Location(iLine, iStartIndexForLine + foundIndex)
                End If
            Next

            'Not found
            Return Nothing
        End Function

        ''' <summary>
        ''' From the root of a document, finds the given attribute inside the Application
        '''   element, if it exists.  If not, returns Nothing.
        ''' </summary>
        ''' <param name="reader"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function FindApplicationPropertyInXaml(ByVal reader As XmlTextReader, ByVal propertyName As String) As XamlProperty
            MoveToApplicationRootElement(reader)
            Dim prop As XamlProperty = FindPropertyAsAttributeInCurrentElement(reader, ELEMENT_APPLICATION, propertyName)
            If prop Is Nothing Then
                prop = FindPropertyAsChildElementInCurrentElement(reader, ELEMENT_APPLICATION, propertyName)
            End If

            Return prop
        End Function

        ''' <summary>
        ''' From the root of a document, finds the given attribute inside the Application
        '''   element, if it exists.  If not, returns Nothing.
        ''' </summary>
        ''' <param name="reader"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function FindPropertyAsAttributeInCurrentElement(ByVal reader As XmlTextReader, ByVal optionalPropertyQualifier As String, ByVal propertyName As String) As XamlPropertyInAttributeSyntax
            'Look for either simple attribute syntax (StartupUri=xxx) or
            '  fully-qualified attribute syntax (Application.StartupUri=xxx)
            Dim fullyQualifiedPropertyName As String = Nothing
            If optionalPropertyQualifier <> "" Then
                fullyQualifiedPropertyName = optionalPropertyQualifier & "." & propertyName
            End If

            Dim foundPropertyName As String = Nothing
            If reader.MoveToAttribute(propertyName) Then
                foundPropertyName = propertyName
            ElseIf fullyQualifiedPropertyName <> "" AndAlso reader.MoveToAttribute(fullyQualifiedPropertyName) Then
                foundPropertyName = fullyQualifiedPropertyName
            Else
                'Not found
                Return Nothing
            End If

            Dim startLocation As New Location(reader)
            Dim boundedEndLocation As Location

            'Remember the quote character actually found in the XML
            Dim quoteCharacterUsedByAttribute As String = reader.QuoteChar

            'Remember the actual value of the property
            Dim unescapedValue As String = reader.Value

            'Find the end location of the attribute
            If reader.MoveToNextAttribute() Then
                boundedEndLocation = New Location(reader)
            Else
                reader.Read()
                boundedEndLocation = New Location(reader)
                Debug.Assert(boundedEndLocation.LineIndex >= startLocation.LineIndex)
                Debug.Assert(boundedEndLocation.LineIndex > startLocation.LineIndex _
                    OrElse boundedEndLocation.CharIndex > startLocation.CharIndex)
            End If

            'Now we have an approximate location.  Find the exact location.
            Dim vsTextFind As IVsTextFind = TryCast(m_vsTextLines, IVsTextFind)
            If vsTextFind Is Nothing Then
                Debug.Fail("IVsTextFind not supported?")
                Throw New InvalidOperationException()
            Else
                'startLocation should be pointing to the attribute name.  Verify that.
                Dim afterAttributeName As New Location(startLocation.LineIndex, startLocation.CharIndex + foundPropertyName.Length)
#If DEBUG Then
                Dim attributeNameBuffer As String = Nothing
                If (ErrorHandler.Failed(m_vsTextLines.GetLineText(startLocation.LineIndex, startLocation.CharIndex, afterAttributeName.LineIndex, afterAttributeName.CharIndex, attributeNameBuffer)) _
                            OrElse Not foundPropertyName.Equals(attributeNameBuffer, StringComparison.Ordinal)) Then
                    Debug.Fail("Didn't find the attribute name at the expected location")
                End If
#End If

                'Find the equals sign ('=')
                Dim equalsLine, equalsIndex As Integer
                Const EqualsSign As String = "="
                If (ErrorHandler.Failed(vsTextFind.Find(EqualsSign, afterAttributeName.LineIndex, afterAttributeName.CharIndex, boundedEndLocation.LineIndex, boundedEndLocation.CharIndex, 0, equalsLine, equalsIndex))) Then
                    ThrowUnexpectedFormatException(startLocation)
                End If
                Debug.Assert(EqualsSign.Equals(GetText(equalsLine, equalsIndex, 1), StringComparison.Ordinal))

                'Find the starting quote
                Dim startQuoteLine, startQuoteIndex As Integer
                If (ErrorHandler.Failed(vsTextFind.Find(quoteCharacterUsedByAttribute, equalsLine, equalsIndex, boundedEndLocation.LineIndex, boundedEndLocation.CharIndex, 0, startQuoteLine, startQuoteIndex))) Then
                    ThrowUnexpectedFormatException(startLocation)
                End If
                Debug.Assert(quoteCharacterUsedByAttribute.Equals(GetText(startQuoteLine, startQuoteIndex, 1), StringComparison.Ordinal))

                'Find the ending quote, assuming it's on the same line
                Dim endQuoteLine, endQuoteIndex As Integer
                If (ErrorHandler.Failed(vsTextFind.Find(quoteCharacterUsedByAttribute, startQuoteLine, startQuoteIndex + 1, boundedEndLocation.LineIndex, boundedEndLocation.CharIndex, 0, endQuoteLine, endQuoteIndex))) Then
                    ThrowUnexpectedFormatException(startLocation)
                End If
                Debug.Assert(quoteCharacterUsedByAttribute.Equals(GetText(endQuoteLine, endQuoteIndex, 1), StringComparison.Ordinal))

                'Now we have the start and end of the attribute's value definition
                Dim valueStart As New Location(startQuoteLine, startQuoteIndex)
                Dim valueEnd As New Location(endQuoteLine, endQuoteIndex + 1)
                Return New XamlPropertyInAttributeSyntax(m_vsTextLines, valueStart, valueEnd, unescapedValue)
            End If
        End Function

        ''' <summary>
        ''' From the root of a document, finds the given attribute inside the Application
        '''   element, using property element syntax, if it exists.  If not, returns Nothing.
        ''' </summary>
        ''' <param name="reader"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function FindPropertyAsChildElementInCurrentElement(ByVal reader As XmlTextReader, ByVal propertyQualifier As String, ByVal propertyName As String) As XamlPropertyInPropertyElementSyntax
            'See http://windowssdk.msdn.microsoft.com/en-us/library/ms788723(VS.80).aspx#PESyntax
            '
            'Looking for something of this form:
            '  <Application xmlns=...>
            '    <Application.StartupUri>MainWindow.xaml</Application.StartupUri>
            '  </Application>

            'In this case, the "Application." prefix is required, not optional.
            Dim fullyQualifiedPropertyName As String = propertyQualifier & "." & propertyName

            If reader.ReadToDescendant(fullyQualifiedPropertyName) Then
                'Found

                Dim tagStart As New Location(reader)
                Dim tagEnd As New Location(reader)

                Dim startTagEndingBracketLocation As Location = FindClosingAngleBracket(tagStart)
                If startTagEndingBracketLocation Is Nothing Then
                    ThrowUnexpectedFormatException(tagStart)
                End If

                If reader.IsEmptyElement Then
                    'It's an empty tag of the form <xyz/>.  The reader is at the 'x' in "xyz", so the
                    '  beginning is at -1 from that location.
                    Dim elementTagStart As Location = New Location(reader).Shift(-1)

                    'Read through the start tag
                    If Not reader.Read() Then
                        ThrowUnexpectedFormatException(tagStart)
                    End If

                    'The reader is now right after the empty element tag
                    Dim elementTagEndPlusOne As New Location(reader)
                    Return New XamlPropertyInPropertyElementSyntaxWithEmptyTag( _
                        m_vsTextLines, fullyQualifiedPropertyName, _
                        elementTagStart, elementTagEndPlusOne)
                Else
                    Dim valueStart As Location
                    Dim valueEndPlusOne As Location = Nothing

                    'The element tag is of the <xyz>blah</xyz> form.  The reader is at the 'x' in "xyz"

                    Dim foundEndTag As Boolean = False
                    Dim unescapedValue As String

                    'Find the start of the content (reader's location after doing a Read through
                    '  the element will not give us reliable results, since it depends on the type of
                    '  node following the start tag).
                    valueStart = FindClosingAngleBracket(New Location(reader)).Shift(1) '+1 to get past the ">"

                    'Read through the start tag
                    If Not reader.Read() Then
                        ThrowUnexpectedFormatException(tagStart)
                    End If

                    'Unfortunately, simply doing a ReadInnerXml() will take us too far.  We need to know
                    '  exactly where the value ends in the text, so we'll read manually.

                    While reader.NodeType <> XmlNodeType.EndElement OrElse Not fullyQualifiedPropertyName.Equals(reader.Name, StringComparison.Ordinal)
                        If Not reader.Read() Then
                            'End tag not found
                            ThrowUnexpectedFormatException(tagStart)
                        End If
                    End While

                    'Reader is at location 'x' of </xyz>.  So we want -2 from this location.
                    Dim currentPosition2 As New Location(reader)
                    valueEndPlusOne = New Location(reader).Shift(-2)

                    'Get the inner text and unescape it.
                    Dim innerText As String = GetText(valueStart, valueEndPlusOne)
                    unescapedValue = UnescapeXmlContent(innerText).Trim()

                    Return New XamlPropertyInPropertyElementSyntax(m_vsTextLines, valueStart, valueEndPlusOne, unescapedValue:=unescapedValue)
                End If
            End If

            Return Nothing
        End Function

#End Region

#Region "Helpers for writing properties"

        ''' <summary>
        ''' Returns the location where a new attribute can be added to the
        '''   Application root element.  Returns Nothing if can't find the
        '''   correct position.
        ''' </summary>
        ''' <remarks></remarks>
        Private Function FindLocationToAddNewApplicationAttribute() As Location
            Using lock As New BufferLock(m_vsTextLines, Me)
                Dim reader As XmlTextReader = CreateXmlTextReader()
                MoveToApplicationRootElement(reader)
                Return FindClosingAngleBracket(New Location(reader))
            End Using
        End Function

        ''' <summary>
        ''' Finds the value of the StartupUri property inside the xaml file.  If
        '''   the property is not set in the xaml, an empty string is returned.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub SetApplicationPropertyValue(ByVal propertyName As String, ByVal value As String)
            If value Is Nothing Then
                value = ""
            End If

            Using lock As New BufferLock(m_vsTextLines, Me)
                Dim reader As XmlTextReader = CreateXmlTextReader()
                Dim prop As XamlProperty = FindApplicationPropertyInXaml(reader, propertyName)

                If prop IsNot Nothing Then
                    'The property is already in the .xaml.
                    prop.SetProperty(Me, value)
                Else
                    'It's not in the .xaml yet.  We'll add this xxx=yyy definition
                    '  as the last attribute in the Application element.
                    If value = "" Then
                        'The new value is blank, just like the current value.
                        '  Don't change anything.
                        Return
                    End If

                    Dim replaceStart As Location = FindLocationToAddNewApplicationAttribute()
                    If replaceStart Is Nothing Then
                        ThrowUnexpectedFormatException(New Location(0, 0))
                    End If
                    Dim replaceEnd As Location = replaceStart
                    Dim newText As String = propertyName & "=""" & EscapeXmlString(value) & """"

                    'Is the anything non-whitespace on the line where we're adding the
                    '  new code?  If so, put in a CR/LF pair before it.
                    If replaceStart.CharIndex > 0 Then
                        Dim lineTextBeforeInsertion As String = GetText(replaceStart.LineIndex, 0, replaceStart.LineIndex, replaceStart.CharIndex)
                        If lineTextBeforeInsertion.Trim().Length > 0 Then
                            newText = vbCrLf & newText
                        End If
                    End If

                    'We know where to replace, so go ahead and do it.
                    ReplaceText(replaceStart, replaceEnd, newText)
                End If

#If DEBUG Then
                Dim newPropertyValue As String = "(error)"
                Try
                    newPropertyValue = GetApplicationPropertyValue(propertyName)
                Catch ex As Exception
                    Debug.Fail("Got an exception trying to verify the new property value in SetApplicationPropertyValue: " & ex.ToString())
                End Try

                If (Not value.Equals(newPropertyValue, StringComparison.Ordinal)) Then
                    Debug.Fail("SetApplicationPropertyValue() didn't seem to work properly.  New .xaml text: " & vbCrLf & GetAllText())
                End If
#End If
            End Using
        End Sub

        ''' <summary>
        ''' Replace the text at the given location in the buffer with new text.
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub ReplaceText(ByVal sourceStart As Location, ByVal sourceLength As Integer, ByVal newText As String)
            ReplaceText(sourceStart, New Location(sourceStart.LineIndex, sourceStart.CharIndex + sourceLength), newText)
        End Sub

        ''' <summary>
        ''' Replace the text at the given location in the buffer with new text.
        ''' </summary>
        ''' <param name="sourceStart"></param>
        ''' <param name="sourceEnd"></param>
        ''' <param name="newText"></param>
        ''' <remarks></remarks>
        Private Sub ReplaceText(ByVal sourceStart As Location, ByVal sourceEnd As Location, ByVal newText As String) Implements IReplaceText.ReplaceText
            If newText Is Nothing Then
                newText = String.Empty
            End If

            Dim bstrNewText As IntPtr = Marshal.StringToBSTR(newText)
            Try
                ErrorHandler.ThrowOnFailure(m_vsTextLines.ReplaceLines( _
                    sourceStart.LineIndex, sourceStart.CharIndex, _
                    sourceEnd.LineIndex, sourceEnd.CharIndex, _
                    bstrNewText, newText.Length, Nothing))
            Finally
                Marshal.FreeBSTR(bstrNewText)
            End Try
        End Sub

        ''' <summary>
        ''' Given the location of the start of an element tag, makes sure that it has an end tag.
        '''   If the element tag is closed by "/>" instead of an end element, it is expanded
        '''   into a start and end tag.
        ''' </summary>
        ''' <param name="tagStartLocation"></param>
        ''' <param name="elementName">The name of the element at the given location</param>
        ''' <remarks></remarks>
        Public Sub MakeSureElementHasStartAndEndTags(ByVal tagStartLocation As Location, ByVal elementName As String)
            If Not "<".Equals(GetText(tagStartLocation, 1), StringComparison.Ordinal) Then
                Debug.Fail("MakeSureElementHasStartAndEndTags: The start location doesn't point to the start of an element tag")
                ThrowUnexpectedFormatException(tagStartLocation)
            End If

            Dim startTagEndingBracketLocation As Location = FindClosingAngleBracket(tagStartLocation)
            If startTagEndingBracketLocation Is Nothing Then
                ThrowUnexpectedFormatException(tagStartLocation)
            End If

            If ">".Equals(GetText(startTagEndingBracketLocation, 1), StringComparison.Ordinal) Then
                'The element tag is of the <xxx> form.  We assume that there is an ending </xxx> tag, and
                '  we don't need to do anything.
            Else
                'It must be an empty tag of the <xxx/> form.
                Const SlashAndEndBracket As String = "/>"
                If Not SlashAndEndBracket.Equals(GetText(startTagEndingBracketLocation, SlashAndEndBracket.Length), StringComparison.Ordinal) Then
                    Debug.Fail("FindClosingAngleBracket returned the wrong location?")
                    ThrowUnexpectedFormatException(startTagEndingBracketLocation)
                End If

                'We need to change <xxx attributes/> into <xxx attributes></xxx>
                Dim newText As String = "></" & elementName & ">"
                ReplaceText(startTagEndingBracketLocation, SlashAndEndBracket.Length, newText)
            End If
        End Sub

#End Region


#Region "StartupUri"

        ''' <summary>
        ''' Finds the value of the StartupUri property inside the xaml file.  If
        '''   the property is not set in the xaml, an empty string is returned.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetStartupUri() As String
            Return GetApplicationPropertyValue(PROPERTY_STARTUPURI)
        End Function

        ''' <summary>
        ''' Finds the value of the StartupUri property inside the xaml file.  If
        '''   the property is not set in the xaml, an empty string is returned.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub SetStartupUri(ByVal value As String)
            SetApplicationPropertyValue(PROPERTY_STARTUPURI, value)
        End Sub

#End Region

#Region "ShutdownMode"

        ''' <summary>
        ''' Finds the value of the ShutdownMode property inside the xaml file.  If
        '''   the property is not set in the xaml, an empty string is returned.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetShutdownMode() As String
            Return GetApplicationPropertyValue(PROPERTY_SHUTDOWNMODE)
        End Function

        ''' <summary>
        ''' Finds the value of the StartupUri property inside the xaml file.  If
        '''   the property is not set in the xaml, an empty string is returned.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub SetShutdownMode(ByVal value As String)
            SetApplicationPropertyValue(PROPERTY_SHUTDOWNMODE, value)
        End Sub

#End Region

#Region "Throwing exceptions"

        ''' <summary>
        ''' Throw an exception for an unexpected format, with line/col information
        ''' </summary>
        ''' <param name="location"></param>
        ''' <remarks></remarks>
        Private Sub ThrowUnexpectedFormatException(ByVal location As Location)
            Throw New XamlReadWriteException( _
                SR.GetString(SR.PPG_WPFApp_Xaml_UnexpectedFormat_2Args, _
                    CStr(location.LineIndex + 1), CStr(location.CharIndex + 1)))
        End Sub

#End Region

#Region "Validation"

        ''' <summary>
        ''' Verify the validity of the Application.xaml file, and throw an exception if
        '''   problems are found.
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub VerifyAppXamlIsValidAndThrowIfNot()
            Using lock As New BufferLock(m_vsTextLines, Me)
                Dim reader As XmlTextReader = CreateXmlTextReader()
                MoveToApplicationRootElement(reader)

                'Read through the Application element, including any child elements, to
                '  ensure everything is properly closed.
                'The name of the element to find is irrelevant, as there shouldn't be
                '  any elements following Application.
                reader.ReadToFollowing("Dummy Element")

                'If we made it to here, the .xaml file should be well-formed enough for us to read
                '  it properly.  As a final check, though, try getting some common properties.
                Call GetStartupUri()
                Call GetShutdownMode()
            End Using
        End Sub

#End Region

#Region "IDebugLockCheck"

        Private m_debugBufferLockCount As Integer

        Public Sub OnBufferLock() Implements IDebugLockCheck.OnBufferLock
            m_debugBufferLockCount += 1
        End Sub

        Public Sub OnBufferUnlock() Implements IDebugLockCheck.OnBufferUnlock
            m_debugBufferLockCount -= 1
            Debug.Assert(m_debugBufferLockCount >= 0, "Extra buffer unlock")
        End Sub

#End Region

    End Class

End Namespace
