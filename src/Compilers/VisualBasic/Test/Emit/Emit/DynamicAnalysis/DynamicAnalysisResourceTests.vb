' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Reflection.PortableExecutable
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.DynamicAnalysis.UnitTests

    Public Class DynamicAnalysisResourceTests
        Inherits BasicTestBase

        ReadOnly InstrumentationHelperSource As Xml.Linq.XElement = <file name="a.vb">
                                                                        <![CDATA[
Namespace Microsoft.CodeAnalysis.Runtime
    Public Class Instrumentation
        Public Shared Function CreatePayload(mvid As System.Guid, methodToken As Integer, ByRef payload As Boolean(), payloadLength As Integer) As Boolean()
            Return payload
        End Function

        Public Shared Sub FlushPayload()
        End Sub
    End Class
End Namespace
]]>
                                                                    </file>

        ReadOnly ExampleSource As Xml.Linq.XElement = <file name="c.vb">
                                                                            <![CDATA[
Imports System

Public Class C
    Public Shared Sub Main()
        Console.WriteLine(123)
        Console.WriteLine(123)
    End Sub

    Public Shared Function Fred As Integer
        Return 3
    End Function

    Public Shared Function Barney(x As Integer)
        Return x
    End Function

    Public Shared Property Wilma As Integer
        Get
            Return 12
        End Get
        Set
        End Set
    End Property

    Public Shared ReadOnly Property Betty As Integer
End Class
]]>
                                                                        </file>

#If True Then
        <Fact>
        Public Sub TestSpansPresentInResource()
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(InstrumentationHelperSource)
            source.Add(ExampleSource)

            Dim c = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            Dim peImage = c.EmitToArray(EmitOptions.Default.WithInstrument("Test.Flag"))

            Dim PEReader As New PEReader(peImage)
            Dim reader = DynamicAnalysisDataReader.TryCreateFromPE(PEReader, "<DynamicAnalysisData>")

            VerifyDocuments(reader, reader.Documents,
                "'C:\myproject\doc1.cs' 87-3F-1A-28-F7-34-C9-43-19-00-ED-0F-8F-2F-0D-EB-DD-32-D4-8E (SHA1)")

            Assert.Equal(10, reader.Methods.Length)

            VerifySpans(reader, reader.Methods(0),                                      ' Main
                "(5,4)-(9,5)",
                "(7,8)-(7,31)",
                "(8,8)-(8,31)")

            VerifySpans(reader, reader.Methods(1),                                      ' Fred get
                "(11,4)-(11,32)",
                "(11,30)-(11,31)")

            VerifySpans(reader, reader.Methods(2),                                      ' Barney
                "(13,4)-(13,41)",
                "(13,39)-(13,40)")

            VerifySpans(reader, reader.Methods(3),                                      ' Wilma get
                "(17,8)-(17,26)",
                "(17,14)-(17,24)")

            VerifySpans(reader, reader.Methods(4),                                      ' Wilma set
                "(18,8)-(18,15)")

            VerifySpans(reader, reader.Methods(5),                                      ' Betty get
                "(21,4)-(21,36)",
                "(21,30)-(21,34)")

            VerifySpans(reader, reader.Methods(6))
        End Sub

        <Fact>
        Public Sub TestDynamicAnalysisResourceMissingWhenInstrumentationFlagIsDisabled()
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(InstrumentationHelperSource)
            source.Add(ExampleSource)

            Dim c = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            Dim peImage = c.EmitToArray(EmitOptions.Default)

            Dim PEReader As New PEReader(peImage)
            Dim reader = DynamicAnalysisDataReader.TryCreateFromPE(PEReader, "<DynamicAnalysisData>")

            Assert.Null(reader)
        End Sub

        Private Shared Sub VerifySpans(reader As DynamicAnalysisDataReader, methodData As DynamicAnalysisMethod, ParamArray expected As String())
            AssertEx.Equal(expected, reader.GetSpans(methodData.Blob).Select(Function(s) $"({s.StartLine},{s.StartColumn})-({s.EndLine},{s.EndColumn})"))
        End Sub

        Private Sub VerifyDocuments(reader As DynamicAnalysisDataReader, documents As ImmutableArray(Of DynamicAnalysisDocument), ParamArray expected As String())
            Dim sha1 = New Guid("ff1816ec-aa5e-4d10-87f7-6f4963833460")

            Dim actual = From d In documents
                         Let name = reader.GetDocumentName(d.Name)
                         Let hash = If(d.Hash.IsNil, "", " " + BitConverter.ToString(reader.GetBytes(d.Hash)))
                         Let hashAlgGuid = reader.GetGuid(d.HashAlgorithm)
                         Let hashAlg = If(hashAlgGuid = sha1, " (SHA1)", If(hashAlgGuid = New Guid, "", " " + hashAlgGuid.ToString()))
                         Select $"'{name}'{hash}{hashAlg}"

            AssertEx.Equal(expected, actual)
        End Sub
#End If
    End Class
End Namespace
