' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities
Imports System.IO
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PortablePdbTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub DateTimeConstant()
            Dim source = <compilation>
                             <file>
Imports System

Public Class C
    Public Sub M()
        const dt1 as datetime = #3/01/2016#
        const dt2 as datetime = #10:53:37 AM#
        const dt3 as datetime = #3/01/2016 10:53:37 AM#
    End Sub
End Class
</file>
                         </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                source,
                TestOptions.DebugDll)

            Dim pdbStream = New MemoryStream()
            compilation.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream:=pdbStream)

            Using pdbMetadata As New PinnedMetadata(pdbStream.ToImmutable())
                Dim mdReader = pdbMetadata.Reader

                Assert.Equal(3, mdReader.LocalConstants.Count)

                For Each constantHandle In mdReader.LocalConstants
                    Dim constant = mdReader.GetLocalConstant(constantHandle)
                    Dim sigReader = mdReader.GetBlobReader(constant.Signature)

                    ' DateTime constants are always SignatureTypeCode.ValueType {17}
                    Dim rawTypeCode = sigReader.ReadCompressedInteger()
                    Assert.Equal(17, rawTypeCode)

                    ' DateTime constants are always HandleKind.TypeReference {1}
                    Dim typeHandle = sigReader.ReadTypeHandle()
                    Assert.Equal(HandleKind.TypeReference, typeHandle.Kind)

                    ' DateTime constants are always stored and retrieved with no time zone specification
                    Dim value = sigReader.ReadDateTime()
                    Assert.Equal(DateTimeKind.Unspecified, value.Kind)
                Next
            End Using
        End Sub
    End Class
End Namespace
