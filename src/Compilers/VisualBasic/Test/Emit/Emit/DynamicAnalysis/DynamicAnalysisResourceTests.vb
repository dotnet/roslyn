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

        <Fact>
        Public Sub TestSpansPresentInResource()
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(ExampleSource)
            source.Add(InstrumentationHelperSource)

            Dim c = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            Dim peImage = c.EmitToArray(EmitOptions.Default.WithInstrument("Test.Flag"))

            Dim PEReader As New PEReader(peImage)
            Dim reader = DynamicAnalysisDataReader.TryCreateFromPE(PEReader, "<DynamicAnalysisData>")

            VerifyDocuments(reader, reader.Documents, "'c.vb'", "'a.vb'")

            Assert.Equal(11, reader.Methods.Length)

            VerifySpans(reader, reader.Methods(1),                                      ' Main
                "(3,4)-(6,11)",
                "(4,8)-(4,30)",
                "(5,8)-(5,30)")

            VerifySpans(reader, reader.Methods(2),                                      ' Fred get
                "(8,4)-(10,16)",
                "(9,8)-(9,16)")

            VerifySpans(reader, reader.Methods(3),                                      ' Barney
                "(12,4)-(14,16)",
                "(13,8)-(13,16)")

            VerifySpans(reader, reader.Methods(4),                                      ' Wilma get
                "(17,8)-(19,15)",
                "(18,12)-(18,21)")

            VerifySpans(reader, reader.Methods(5),                                      ' Wilma set
                "(20,8)-(21,15)")

            VerifySpans(reader, reader.Methods(6))                                      ' Betty get -- VB does not supply a valid syntax node for the body.

            VerifySpans(reader, reader.Methods(7))
        End Sub

        <Fact>
        Public Sub TestDynamicAnalysisResourceMissingWhenInstrumentationFlagIsDisabled()
            Dim source As Xml.Linq.XElement = <compilation></compilation>
            source.Add(ExampleSource)
            source.Add(InstrumentationHelperSource)

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
            Dim sha1 = New Guid("ff1816ec-aa5e-4d10-87F7-6F4963833460")

            Dim actual = From d In documents
                         Let name = reader.GetDocumentName(d.Name)
                         Let hash = If(d.Hash.IsNil, "", " " + BitConverter.ToString(reader.GetBytes(d.Hash)))
                         Let hashAlgGuid = reader.GetGuid(d.HashAlgorithm)
                         Let hashAlg = If(hashAlgGuid = sha1, " (SHA1)", If(hashAlgGuid = New Guid, "", " " + hashAlgGuid.ToString()))
                         Select $"'{name}'{hash}{hashAlg}"

            AssertEx.Equal(expected, actual)
        End Sub
    End Class
End Namespace
