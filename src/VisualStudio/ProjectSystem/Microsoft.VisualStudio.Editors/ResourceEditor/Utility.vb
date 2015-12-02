'******************************************************************************
'* Utility.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Option Explicit On
Option Strict On
Option Compare Binary

Imports Microsoft.VisualStudio.Editors.Common
Imports Microsoft.VisualStudio.Editors.Common.Utils
Imports Microsoft.VisualStudio.Editors.Package
Imports System
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports Microsoft.VisualBasic

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' Utility functions.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Module Utility

        ''' <summary>
        ''' Given a source Image of any size, draws a thumbnail at a specified size.  The thumbnail is intended to be used inside
        '''    an ImageList and displayed inside a ListView.  If requested, it will include a selection border (which is visible 
        '''    only when the ListViewItem is selected) and a border.
        ''' </summary>
        ''' <param name="SourceImage"></param>
        ''' <param name="ThumbnailSize">The expected size of the returned thumbnail image</param>
        ''' <param name="DrawBorder">Whether or not to draw a border around the image</param>
        ''' <param name="SelectionBorderWidth">The width of the selection border to be drawn (ignored if DrawBorder=False)</param>
        ''' <param name="BorderWidth">The width of the border (ignored if DrawBorder=False)</param>
        ''' <param name="ImageListTransparentColor">The TransparentColor property of the ImageList that this will be used for.  This is required to get the selection border drawing to work properly.</param>
        ''' <returns>The drawn thumbnail image</returns>
        ''' <remarks></remarks>
        Public Function CreateThumbnail(ByVal SourceImage As Image, ByVal ThumbnailSize As Size, ByVal DrawBorder As Boolean, ByVal BorderWidth As Integer, ByVal SelectionBorderWidth As Integer, ByVal ImageListTransparentColor As Color) As Bitmap
            If SourceImage Is Nothing Then
                Debug.Fail("SourceImage can't be nothing")
                Return Nothing
            End If

            Dim Thumbnail As New Bitmap(ThumbnailSize.Width, ThumbnailSize.Height)
            Using ThumbnailGraphics As Graphics = Graphics.FromImage(Thumbnail)
                If Not Switches.RSEDisableHighQualityThumbnails.Enabled Then
                    'This gives us much better quality for thumbnails of larger bitmaps, albeit at somewhat
                    '  lower performance, so only use it for images larger than the thumbnail size.
                    If SourceImage.Size.Width > ThumbnailSize.Width OrElse SourceImage.Height > ThumbnailSize.Height Then
                        ThumbnailGraphics.InterpolationMode = Drawing2D.InterpolationMode.High
                    End If
                End If

                'The actual area inside the thumbnail image that will hold the image.  When drawing a border, this will be
                '  smaller than the full size of the thumbnail.
                Dim ImageRect As Rectangle = New Rectangle(0, 0, ThumbnailSize.Width, ThumbnailSize.Height)

                If DrawBorder Then
                    'We use a trick to get the ListView to draw a selection border around our image (it doesn't
                    '  do that normally).  Everything in the image that is of the transparent color set into the 
                    '  ImageList will be drawn non-dithered when the ListViewItem is drawn.  Everything else is
                    '  dithered.  We draw the background of thumbnail with this transparent color, so it is non-dithered.
                    '  Around that, we draw a single-pixel visible border.  Around this (width = SelectionBorderWidth), we
                    '  draw a color that is just a single unit off from being the same as the ImageList's transparent color.
                    '  Thus, when the ListViewImage is selected, it is dithered and looks like a selection rectangle.  When
                    '  it is not selected, the eye can't tell it apart from the transparent color.
                    Dim AlmostTransparent As Color
                    If ImageListTransparentColor.R > 128 Then
                        AlmostTransparent = Color.FromArgb(ImageListTransparentColor.R - 1, ImageListTransparentColor.G, ImageListTransparentColor.B)
                    Else
                        AlmostTransparent = Color.FromArgb(ImageListTransparentColor.R + 1, ImageListTransparentColor.G, ImageListTransparentColor.B)
                    End If

                    'First draw the "selection rectangle" area (actually, we draw the whole rect,
                    '  but we'll eraase it with background afterwards)
                    ThumbnailGraphics.FillRectangle(New SolidBrush(AlmostTransparent), ImageRect)

                    '... then the border
                    ThumbnailGraphics.DrawRectangle(SystemPens.ButtonFace, _
                        New Rectangle(SelectionBorderWidth, SelectionBorderWidth, _
                        ThumbnailSize.Width - 2 * SelectionBorderWidth, ThumbnailSize.Height - 2 * SelectionBorderWidth))

                    '... then remove the area of both of these from the area the image will use.
                    ImageRect.X += BorderWidth + SelectionBorderWidth
                    ImageRect.Y += BorderWidth + SelectionBorderWidth
                    ImageRect.Width -= 2 * (BorderWidth + SelectionBorderWidth)
                    ImageRect.Height -= 2 * (BorderWidth + SelectionBorderWidth)
                End If

                ThumbnailGraphics.FillRectangle(SystemBrushes.Window, ImageRect)

                'Scale Bitmap size download if necessary so that it fits with the largest possible size that retains 
                '  the original aspect ratio inside the specified size.  Do not resize to a larger size.
                Dim ScaledBitmapSize As Size = ScaleSizeProportionally(SourceImage.Size, ImageRect.Size, OnlyScaleDownward:=True)

                'Center the image inside the given bounds
                Dim CenteredBitmapRect As New Rectangle( _
                    ImageRect.X + (ImageRect.Width - ScaledBitmapSize.Width) \ 2, _
                    ImageRect.Y + (ImageRect.Height - ScaledBitmapSize.Height) \ 2, _
                    ScaledBitmapSize.Width, _
                    ScaledBitmapSize.Height)
                Debug.Assert(Rectangle.Intersect(CenteredBitmapRect, ImageRect).Equals(CenteredBitmapRect), _
                    "CenteredBitmapRect should be entirely within ImageRect")

                ThumbnailGraphics.DrawImage(SourceImage, CenteredBitmapRect)

                Return Thumbnail
            End Using

        End Function


        ''' <summary>
        ''' Given the original size of an image, plus the maximum desired scaled size, returns the proper
        '''   size to draw an image.  The returned size will be no large than the maximum size given, and
        '''   will be scaled proportionally.
        ''' </summary>
        ''' <param name="OriginalSize">Original size of the image</param>
        ''' <param name="MaxScaledSize">Maximum size that is allowed</param>
        ''' <param name="OnlyScaleDownward">If true, image sizes which are smaller than maximum size will *not* be
        '''    scaled upward.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ScaleSizeProportionally(ByVal OriginalSize As Size, ByVal MaxScaledSize As Size, ByVal OnlyScaleDownward As Boolean) As Size
            'Get the scale required to match the original width to the maximum scaled width
            Dim ScaleBasedOnWidth As Double = MaxScaledSize.Width / OriginalSize.Width

            'Get the scale required to match the original heightto the maximum scaled height
            Dim ScaleBasedOnHeight As Double = MaxScaledSize.Height / OriginalSize.Height

            'The maximum scale that we can actually use is the minimum of the two.
            Dim ActualScale As Double = Math.Min(ScaleBasedOnWidth, ScaleBasedOnHeight)

            If OnlyScaleDownward Then
                'If we're not allowed to scale upward, our maximum scale value is 1.0
                ActualScale = Math.Min(ActualScale, 1.0)
            End If

            Dim ScaledSize As Size = New Size(CInt(Math.Floor(OriginalSize.Width * ActualScale)), CInt(Math.Floor(OriginalSize.Height * ActualScale)))

            Debug.Assert(OnlyScaleDownward = True OrElse ScaledSize.Width = MaxScaledSize.Width OrElse ScaledSize.Height = MaxScaledSize.Height, _
                "One of the measures should have been scaled to exactly the maximum requested size - rounding error?")
            Debug.Assert(ScaledSize.Width <= MaxScaledSize.Width AndAlso ScaledSize.Height <= MaxScaledSize.Height)

            Return ScaledSize
        End Function


        ''' <summary>
        ''' Given a suggested filename (with no path), converts that filename into a legal filename by replacing
        '''   invalid characters with the underscore ("_").
        ''' </summary>
        ''' <param name="SuggestedFileName">The suggested (desired) filename, with no path</param>
        ''' <returns>A valid path based on the suggested one.</returns>
        ''' <remarks></remarks>
        Public Function CreateLegalFileName(ByVal SuggestedFileName As String) As String
            Debug.Assert(SuggestedFileName <> "")
            If SuggestedFileName = "" Then
                Return ""
            End If

            'Start with a standard set of invalidate file path characters
            Static InvalidChars As Char()

            'Initialize InvalidChars (once)
            Static InvalidCharsInitialized As Boolean = False
            If Not InvalidCharsInitialized Then
                'Merge Path.InvalidPathChars with additional invalid characters that Visual Studio doesn't like
                Dim BadVisualStudioChars() As Char = {"/"c, "?"c, ":"c, "&"c, "\"c, "*"c, """"c, "<"c, ">"c, "|"c, "#"c, "%"c}
                Dim InvalidPathChars As Char() = Path.GetInvalidFileNameChars()

                ReDim InvalidChars(BadVisualStudioChars.Length + InvalidPathChars.Length - 1)
                BadVisualStudioChars.CopyTo(InvalidChars, 0)
                InvalidPathChars.CopyTo(InvalidChars, BadVisualStudioChars.Length)

                InvalidCharsInitialized = True
            End If

            'Main loop - replace each invalid character with an underscore
            Dim FileNameBuilder As New System.Text.StringBuilder(SuggestedFileName)
            Do
                'Search for invalid characters
                Dim CurrentFileName As String = FileNameBuilder.ToString
                Dim InvalidCharIndex As Integer = CurrentFileName.IndexOfAny(InvalidChars)
                If InvalidCharIndex < 0 Then
                    'No more invalid characters found
                    SuggestedFileName = CurrentFileName
                    Exit Do
                Else
                    'We found one.  Replace it and try again.
                    FileNameBuilder.Chars(InvalidCharIndex) = "_"c
                End If
            Loop

            Dim strInvalidName As String = "^(NUL|CON|AUX|PRN|((COM|LPT)[0-9]))(\..*)?$"
            Dim mc As MatchCollection = Regex.Matches(SuggestedFileName.ToUpperInvariant(), strInvalidName)
            If mc.Count > 0 Then
                SuggestedFileName = "_" & SuggestedFileName
            End If

            Return SuggestedFileName
        End Function


        ''' <summary>
        ''' Makes sure that a particular string value is not Nothing.
        ''' </summary>
        ''' <param name="StringValue">The string value to check against Nothing.</param>
        ''' <returns>Empty string if the string is Nothing, or else the original string value.</returns>
        ''' <remarks></remarks>
        Public Function NonNothingString(ByVal StringValue As String) As String
            If StringValue Is Nothing Then
                Return ""
            Else
                Return StringValue
            End If
        End Function


        ''' <summary>
        ''' Given a file path to a text file, tries to determine the encoding used by that file.
        ''' </summary>
        ''' <param name="FilePath">The file path and name</param>
        ''' <returns>The best-guess Encoding to use for this file.</returns>
        ''' <remarks>
        ''' Handles the cases most common for use in Visual Studio.  In particular, it can detect:
        '''
        '''   a) Unicode variants with BOM (byte order mark)
        '''   b) UTF-8 with or without BOM
        '''   c) UTF-8 without BOM (best guess)
        '''   d) If the file is not detected to fall into the above cases, it is assumed to be ANSI based on the machine's current code page
        '''
        ''' It does not handle these cases:
        '''
        '''   a) Unicode variants without BOM
        '''
        ''' This behavior is good enough for all our common scenarios because Visual Studio does not handle Unicode variants without BOM, and
        '''   Notepad.exe can read Unicode files without the BOM, it refuses to write them that way.
        ''' Visual Studio can write UTF-8 with or without the BOM, so detecting those cases is important.
        '''
        ''' Guessing may be less accurate for very small files.
        ''' </remarks>
        Public Function GuessFileEncoding(ByVal FilePath As String) As Encoding
            'The StreamReader knows how to interpret the byte order marks at the beginning of a file, so we'll let it do just
            '  that.  We create a StreamReader that starts out in ANSI with detectEncodingFromByteOrderMarks:=True.  After reading
            '  a few bytes, if it detects a BOM, it will change the encoding to the proper one.
            Dim SystemAnsiEncoding As Encoding = Encoding.Default
            Dim Reader As New StreamReader(FilePath, SystemAnsiEncoding, detectEncodingFromByteOrderMarks:=True)
            Try
                'It has to read at least a few bytes - in practice should need no more than three (UTF-8 BOM)
                Const BytesNeededForBOMDetection As Integer = 3
                Dim DummyChars(BytesNeededForBOMDetection + 100) As Char 'Let it read a few more bytes for safety's sake :-) ... it's okay if there aren't that many bytes in the file
                Call Reader.Read(DummyChars, 0, DummyChars.Length)
                If Not Reader.CurrentEncoding.Equals(SystemAnsiEncoding) Then
                    'The reader changed its encoding.  It must know something we don't (found a BOM)...  Return that encoding.
                    Return Reader.CurrentEncoding
                End If
            Finally
                Reader.Close()
            End Try

            'Okay, so we know the file does not have a BOM.

            'Is it UTF-8?
            If IsLikelyUtf8FileWithoutBOM(FilePath) Then
                Return Encoding.UTF8
            End If

            'Nothing else matches.  Our best guess at this point is plain old Ansi (in particular, with the system's codepage).
            Return SystemAnsiEncoding
        End Function


        ''' <summary>
        ''' Given a file name and path, analyzes the file to see if it's likely a UTF-8 file.
        ''' </summary>
        ''' <param name="FilePath">File name and path to analyze.</param>
        ''' <returns>True iff the file is a valid UTF-8 file and is likely actually in that encoding.</returns>
        ''' <remarks>
        ''' If the encoding is invalid UTF-8, returns False.
        ''' </remarks>
        Private Function IsLikelyUtf8FileWithoutBOM(ByVal FilePath As String) As Boolean
            Dim Stream As FileStream = File.Open(FilePath, FileMode.Open, FileAccess.Read)

            'If the file doesn't violate the UTF-8 encoding, *and* it also contains UTF-8
            '  characters (i.e., anything above 7F), then it's most likely UTF-8 rather than
            '  ANSI (it's unlikely for an ANSI file with characters above 7F to accidentally
            '  conform to the UTF-8 encoding).  If everything's less than or equal to 7F, then
            '  it's really moot as to whether it's UTF-8 or ANSI - they're the same thing in 
            '  this case (really, it's ASCII).  We'll assume in this case that it's most 
            '  likely intended to be ANSI and return False (since that's the default for most
            '  editor to save in still).
            Dim Contains7FOrAbove As Boolean = False

            Try
                While True
                    'UTF-8 encoding is one-to-one mapping from Unicode (in our case, 2 bytes per char) to/from a multi-byte character set.
                    '  A Unicode character maps as follows, where "x" represents a bit from the Unicode character:
                    '
                    '    0x0000 - 0x007F:  0xxxxxxx  (i.e., anything in the ASCII range is simply the original ASCII byte value)
                    '    0x0080 - 0x07FF:  110xxxxx 10xxxxxx
                    '    0x0800 - 0xFFFF:  1110xxxx 10xxxxxx 10xxxxxx 
                    '
                    'Example: a Unicode character of 0x00A9 (= 00000000 10101001) is encoded in UTF-8 as 11000010 10101001

                    Const LeadByteCodeFor2Bytes As Byte = &HC0 'binary 110x xxxx - UTF-8 lead byte when there's a total of 2 bytes in the character
                    Const LeadByteMaskFor2Bytes As Byte = &HE0 'binary 1110 0000 - masks out the first 3 bits in the byte
                    Const LeadByteCodeFor3Bytes As Byte = &HE0 'binary 1110 xxxx - UTF-8 lead byte when there's a total of 3 bytes in the character
                    Const LeadByteMaskFor3Bytes As Byte = &HF0 'binary 1111 0000 - masks out the first 4 bits in the byte

                    Const ContinuationByteCode As Byte = &H80  'binary 10xx xxxx - all continuation bytes must start with this
                    Const ContinuationByteMask As Byte = &HC0  'binary 1100 0000 - masks out the first 2 bits in the byte

                    Dim LeadByte As Integer = Stream.ReadByte() 'Must declare as Integer, ReadByte() returns (int)(-1) on EOF
                    Debug.Assert(LeadByte = -1 OrElse LeadByte = CByte(LeadByte))
                    If LeadByte < 0 Then
                        'We've reached EOF without finding anything invalid for a UTF-8 file.
                        'If we've seen at least one byte above 7F, then we're most likely a UTF-8 file because it's unlikely for an ANSI
                        '  file with characters above 7F to accidentally be valid UTF-8.
                        Return Contains7FOrAbove
                    ElseIf LeadByte < &H80 Then
                        'ASCII character.  Skip it.  Valid single byte for both UTF-8 and ANSI files.
                    Else
                        Debug.Assert(LeadByte >= &H80)

                        'We have a possible start of a UTF-8 character.
                        Contains7FOrAbove = True

                        Dim ExpectedTotalBytesInChar As Integer
                        If (LeadByte And LeadByteMaskFor2Bytes) = LeadByteCodeFor2Bytes Then
                            ExpectedTotalBytesInChar = 2
                        ElseIf (LeadByte And LeadByteMaskFor3Bytes) = LeadByteCodeFor3Bytes Then
                            ExpectedTotalBytesInChar = 3
                        Else
                            'Whoops, this isn't a valid UTF-8 lead byte.
                            Return False
                        End If

                        'Read the remaining bytes.  Each following byte *must* start with 10xxxxxx, or it's not valid UTF-8.
                        For i As Integer = 1 To ExpectedTotalBytesInChar - 1
                            Dim ContinuationByte As Integer = Stream.ReadByte()
                            Debug.Assert(ContinuationByte = -1 OrElse ContinuationByte = CByte(ContinuationByte))
                            If ContinuationByte < 0 Then
                                'EOF - not valid, since we're still expecting a continuation byte.
                                Return False
                            End If

                            If (ContinuationByte And ContinuationByteMask) <> ContinuationByteCode Then
                                'Not a valid continuation byte.
                                Return False
                            End If
                        Next
                    End If
                End While
            Finally
                Stream.Close()
            End Try
        End Function


        ''' <summary>
        ''' Given a stream representation of a file, determine whether or not it is a .wav file in
        '''   a format supported by the Fx.
        ''' </summary>
        ''' <param name="Data">A stream containing the bytes in the file</param>
        ''' <returns>True iff the file is recognized as a .wav file</returns>
        ''' <remarks>
        ''' Shamelessly stolen from System.Windows.Forms.SoundPlayer code
        ''' </remarks>
        Public Function IsWavSoundFile(ByVal Data As Stream) As Boolean
            'Need to seek to the beginning.  With our streams we always assume we want the full data in the stream.
            Data.Seek(0, SeekOrigin.Begin)

            If Data.Length > Integer.MaxValue Then
                'We can't handle something that big.
                Throw New OutOfMemoryException
            End If

            Dim Bytes(CInt(Data.Length - 1)) As Byte
            Data.Read(Bytes, 0, CInt(Data.Length))
            Return IsWavSoundFile(Bytes)
        End Function


        ''' <summary>
        ''' Given a byte representation of a file, determine whether or not it is a .wav file in
        '''   a format supported by the Fx.
        ''' </summary>
        ''' <param name="Data">The bytes in the file</param>
        ''' <returns>True iff the file is recognized as a .wav file</returns>
        ''' <remarks>
        ''' Shamelessly stolen from System.Windows.Forms.SoundPlayer code
        ''' </remarks>
        Public Function IsWavSoundFile(ByVal Data As Byte()) As Boolean
            Try
                Dim Position As Integer = 0
                Dim wFormatTag As Int16 = -1
                Dim FmtChunkFound As Boolean = False

                ' validate the RIFF header
                If Data(0) <> Asc("R"c) OrElse Data(1) <> Asc("I"c) OrElse Data(2) <> Asc("F"c) OrElse Data(3) <> Asc("F"c) Then
                    'Invalid wave header
                    Return False
                End If
                If Data(8) <> Asc("W"c) OrElse Data(9) <> Asc("A"c) OrElse Data(10) <> Asc("V"c) OrElse Data(11) <> Asc("E"c) Then
                    'Invalid wave header
                    Return False
                End If

                ' We only care about the "fmt " chunk (yes, the space is intentional)
                Position = 12
                Dim Length As Integer = Data.Length
                While Not FmtChunkFound AndAlso Position < Length - 4
                    If Data(Position) = Asc("f"c) AndAlso Data(Position + 1) = Asc("m"c) AndAlso Data(Position + 2) = Asc("t"c) AndAlso Data(Position + 3) = Asc(" "c) Then
                        '
                        ' fmt chunk
                        '
                        FmtChunkFound = True
                        Dim ChunkSize As Integer = BytesToInt(Data(Position + 7), Data(Position + 6), Data(Position + 5), Data(Position + 4))

                        '
                        ' get the cbSize from the WAVEFORMATEX
                        '

                        Dim SizeOfWAVEFORMAT As Integer = 16
                        If ChunkSize <> SizeOfWAVEFORMAT Then
                            ' we are dealing w/ WAVEFORMATEX
                            ' do extra validation
                            Dim sizeOfWAVEFORMATEX As Integer = 18
                            Dim cbSize As Int16 = BytesToInt16(Data(Position + 8 + sizeOfWAVEFORMATEX - 1), _
                                                        Data(Position + 8 + sizeOfWAVEFORMATEX - 2))
                            If cbSize + sizeOfWAVEFORMATEX <> ChunkSize Then
                                'Invalid wave header
                                Return False
                            End If
                        End If

                        wFormatTag = BytesToInt16(Data(Position + 9), Data(Position + 8))

                        Position += ChunkSize + 8
                    Else
                        Position += 8 + BytesToInt(Data(Position + 7), Data(Position + 6), Data(Position + 5), Data(Position + 4))
                    End If
                End While

                If Not FmtChunkFound Then
                    'Invalid wave header
                    Return False
                End If

                If wFormatTag <> Interop.win.WAVE_FORMAT_PCM _
                    AndAlso wFormatTag <> Interop.win.WAVE_FORMAT_ADPCM _
                    AndAlso wFormatTag <> Interop.win.WAVE_FORMAT_IEEE_FLOAT _
                Then
                    'Sound format not supported by Fx
                    Return False
                End If

                Return True
            Catch ex As IndexOutOfRangeException
                'Rudely hit the end of the file
                Return False
            End Try
        End Function


        ''' <summary>
        ''' Given two bytes, creates an Int16 out of them.
        ''' </summary>
        ''' <param name="ch0">First byte</param>
        ''' <param name="ch1">Second byte</param>
        ''' <returns>The Int16combined from the bytes</returns>
        ''' <remarks></remarks>
        Private Function BytesToInt16(ByVal ch0 As Byte, ByVal ch1 As Byte) As Int16
            Return CShort(ch1) Or CShort(CInt(ch0) << 8)
        End Function


        ''' <summary>
        ''' Given a set of four bytes (as used in the mmio functions), returns as Int32
        '''   out of them.
        ''' </summary>
        ''' <param name="ch0">First byte</param>
        ''' <param name="ch1">Second byte</param>
        ''' <param name="ch2">Third byte</param>
        ''' <param name="ch3">Fourth byte</param>
        ''' <returns>The Int32 combined from these four bytes in a way used by the mmio functions.</returns>
        ''' <remarks></remarks>
        Private Function BytesToInt(ByVal ch0 As Byte, ByVal ch1 As Byte, ByVal ch2 As Byte, ByVal ch3 As Byte) As Integer
            Dim Result As Integer = 0
            Result = ch3
            Result = Result Or (CInt(ch2) << 8)
            Result = Result Or (CInt(ch1) << 16)
            Result = Result Or (CInt(ch0) << 24)
            Return Result
        End Function


        ''' <summary>
        ''' Given a length in bytes, turns it into either an "x Bytes" or "x.x KB" display, depending on the
        '''   actual length.
        ''' </summary>
        ''' <param name="LengthInBytes">The length in bytes</param>
        ''' <returns>A friendly formatted string</returns>
        ''' <remarks></remarks>
        Public Function GetKBDisplay(ByVal LengthInBytes As Long) As String
            Const BytesInKilobyte As Integer = 1024

            If LengthInBytes > BytesInKilobyte Then
                Dim FormattedKB As String = (LengthInBytes / BytesInKilobyte).ToString("0.0")
                Return String.Format(SR.GetString(SR.RSE_FileSizeFormatKB, FormattedKB))
            Else
                Return String.Format(SR.GetString(SR.RSE_FileSizeFormatBytes_1Arg, LengthInBytes))
            End If
        End Function


        ''' <summary>
        ''' Creates a new exception with the given message and help link.
        ''' </summary>
        ''' <param name="Message">The message for the exception.</param>
        ''' <param name="HelpLink">The help link for the exception</param>
        ''' <param name="InnerException">The inner exception.</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function NewException(ByVal Message As String, Optional ByVal HelpLink As String = Nothing, Optional ByVal InnerException As Exception = Nothing) As Exception
            Dim ex As Exception
            If InnerException IsNot Nothing Then
                ex = New Exception(Message, InnerException)
            Else
                ex = New Exception(Message)
            End If

            If HelpLink <> "" Then
                ex.HelpLink = HelpLink
            End If

            Return ex
        End Function


        ''' <summary>
        ''' Given a path and filename, looks up that file on disk and returns its filename (w/o directory)
        '''   in the actual case on the hard drive).  If the file isn't found, it returns the filename
        '''   as specified in the input.
        ''' </summary>
        ''' <param name="FilePath">The full path and filename of the file</param>
        ''' <returns>The filename only, as actually found in the file system.</returns>
        ''' <remarks></remarks>
        Public Function GetFileNameInActualCase(ByVal FilePath As String) As String
            If File.Exists(FilePath) Then
                'Strange, but there appears to be no way to do this from the CLR/NDP, other than searching for it.
                Dim FilesMatchingName() As String = Directory.GetFiles(Path.GetDirectoryName(FilePath), Path.GetFileName(FilePath))
                Debug.Assert(FilesMatchingName.Length = 1)
                If FilesMatchingName.Length = 1 Then
                    Return Path.GetFileName(FilesMatchingName(0))
                End If
            End If

            Return Path.GetFileName(FilePath)
        End Function


        ''' <summary>
        ''' Given a resource ID for a font name and size, creates a font based on those specifications and returns it.  Returns
        '''   Nothing if the font was not set in the resx file (the strings are empty).
        ''' </summary>
        ''' <param name="FontResourceString">The font described as a string, just as if it were in a form's resx file.  Example: "Arial, 12pt"</param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetFontFromResources(ByVal FontResourceString As String) As Font
            Dim FontAsString As String = FontResourceString

            If FontAsString = "" Then
                Return Nothing
            End If

            Try
                Dim Converter As TypeConverter = TypeDescriptor.GetConverter(GetType(Font))
                Return DirectCast(Converter.ConvertFromInvariantString(FontAsString), Font)
            Catch ex As Exception
                RethrowIfUnrecoverable(ex)
                Debug.Fail("Unable to create requested font: " & FontAsString)
            End Try

            Return Nothing
        End Function

        ''' <summary>
        ''' Determines if the provided file name has the .resw file extension
        ''' </summary>
        ''' <param name="fileName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function HasResWExtension(fileName As String) As Boolean
            Return Path.GetExtension(fileName).Equals(".resw", StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        ''' Determines if the provided file name has any resource file extension (.resx or .resw)
        ''' </summary>
        ''' <param name="fileName"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function HasResourceFileExtension(fileName As String) As Boolean
            Dim extension As String = Path.GetExtension(fileName)
            Return extension.Equals(".resx", StringComparison.OrdinalIgnoreCase) OrElse
                   extension.Equals(".resw", StringComparison.OrdinalIgnoreCase)
        End Function

    End Module

End Namespace
