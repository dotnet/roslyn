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

    Public Class DynamicInstrumentationTests
        Inherits BasicTestBase

        ReadOnly InstrumentationHelperSource As Xml.Linq.XElement = <compilation>
                                                                        <file name="c.vb">
                                                                            <![CDATA[
Namespace Microsoft.CodeAnalysis.Runtime

    Public Class Instrumentation
    
        Private Shared _payloads As Boolean()()
        Private Shared _mvis As System.Guid

        Public Shared Function CreatePayload(mvis As System.Guid, methodIndex As Integer, ByRef payload As Boolean(), payloadLength As Integer) As Boolean()
            If _mvid <> mvid Then
                _payloads = New Boolean(100)() {}
                _mvid = mvid
            End If

            If System.Threading.Interlocked.CompareExchange(payload, new bool(payloadLength - 1) {}, Nothing) Is Nothing Then
                _payloads(methodIndex) = payload
                Return payload
            End If

            Return _payloads(methodIndex)
        End Function

        Public Shared Sub FlushPayload()
            Console.WriteLine("Flushing")
            If _payloads Is Nothing Then
                Return
            End If
            For i As Integer = 0 To _payloads.Length - 1
                Dim payload As Boolean() = _payloads(i)
                if payload IsNot Nothing
                    Console.WriteLine(i)
                    For j As Integer = 0 To payload.Length - 1
                        Console.WriteLine(payload(j))
                        payload(j) = False
                    Next
                End If
            Next
        End Sub
    End Class
End Namespace
]]>
                                                                        </file>
                                                                    </compilation>

    End Class
End Namespace
