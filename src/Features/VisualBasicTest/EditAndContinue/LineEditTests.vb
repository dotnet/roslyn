' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.Contracts.EditAndContinue
Imports Microsoft.CodeAnalysis.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests
    <UseExportProvider>
    Public Class LineEditTests
        Inherits EditingTestBase

#Region "Methods"

        <Fact>
        Public Sub Method_Update1()
            Dim src1 = "
Class C
    Shared Sub Bar()
        Console.ReadLine(1)
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub Bar()


        Console.ReadLine(2)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Bar"))})
        End Sub

        <Fact>
        Public Sub Method_Reorder1()
            Dim src1 = "
Class C
    Shared Sub Goo()
        Console.ReadLine(1)
    End Sub

    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub

    Shared Sub Goo()
        Console.ReadLine(1)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
            {
                New SourceLineUpdate(2, 6),
                New SourceLineUpdate(5, 5),
                New SourceLineUpdate(6, 2)
            }, {})
        End Sub

        <Fact>
        Public Sub Method_Reorder2()
            Dim src1 = "
Class Program
    Shared Sub Main()
        Goo()
        Bar()
    End Sub

    Shared Function Goo() As Integer
        Return 1
    End Function

    Shared Function Bar() As Integer
        Return 2
    End Function
End Class
"

            Dim src2 = "
Class Program
    Shared Function Goo() As Integer
        Return 1
    End Function

    Shared Sub Main()
        Goo()
        Bar()
    End Sub

    Shared Function Bar() As Integer
        Return 2
    End Function
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
            {
                New SourceLineUpdate(2, 6),
                New SourceLineUpdate(6, 6),
                New SourceLineUpdate(7, 2),
                New SourceLineUpdate(10, 10)
            }, {})
        End Sub

        <Fact>
        Public Sub Method_LineChange1()
            Dim src1 = "
Class C
    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub
End Class
"

            Dim src2 = "
Class C


    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New SourceLineUpdate(2, 4)}, {})
        End Sub

        <Fact>
        Public Sub Method_LineChangeWithLambda1()
            Dim src1 = "
Class C
    Shared Sub Bar()
        F(Function() 1)
    End Sub
End Class
"

            Dim src2 = "
Class C


    Shared Sub Bar()
        F(Function() 1)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New SourceLineUpdate(2, 4)}, {})
        End Sub

        <Fact>
        Public Sub Method_Recompile1()
            Dim src1 = "
Class C
    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub _
            Bar()
        Console.ReadLine(2)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Bar"))})
        End Sub

        <Fact>
        Public Sub Method_Recompile2()
            Dim src1 = "
Class C
    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub Bar()
              Console.ReadLine(2)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Bar"))})
        End Sub

        <Fact>
        Public Sub Method_PartialBodyLineUpdate1()
            Dim src1 = "
Class C
    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub Bar()
        Console.ReadLine(2)
        
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New SourceLineUpdate(4, 5)}, {})
        End Sub

        <Fact>
        Public Sub Method_PartialBodyLineUpdate2()
            Dim src1 = "
Class C
    Shared Sub Bar()

        Console.ReadLine(1)
        Console.ReadLine(2)
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub Bar()
        Console.ReadLine(1)

        Console.ReadLine(2)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New SourceLineUpdate(4, 3)}, {})
        End Sub

        <Fact>
        Public Sub Method_Recompile5()
            Dim src1 = "
Class C
    Shared Sub Bar()
        Dim <N:0.0>a</N:0.0> = 1
        Dim <N:0.1>b</N:0.1> = 2
        <AS:0>System.Console.WriteLine(1)</AS:0>
    End Sub
End Class
"
            Dim src2 = "
Class C
    Shared Sub Bar()
             Dim <N:0.0>a</N:0.0> = 1
        Dim <N:0.1>b</N:0.1> = 2
        <AS:0>System.Console.WriteLine(1)</AS:0>
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            Dim active = GetActiveStatements(src1, src2)
            Dim syntaxMap = GetSyntaxMap(src1, src2)

            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Bar"))})

            edits.VerifySemantics(
                active,
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Bar"), syntaxMap:=syntaxMap(0))})
        End Sub

        <Fact>
        Public Sub Method_Recompile6()
            Dim src1 = "
Class C
    Shared Sub Bar() : End Sub
End Class
"

            Dim src2 = "
Class C
        Shared Sub Bar() : End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Bar"))})
        End Sub

        <Fact>
        Public Sub Method_PartialBodyLineUpdate3()
            Dim src1 = "
Class C(Of T)
    Shared Sub Bar()
        
        Console.ReadLine(2)
    End Sub
End Class
"

            Dim src2 = "
Class C(Of T)
    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                {New SourceLineUpdate(4, 3)})
        End Sub

        <Fact>
        Public Sub Method_RudeRecompile2()
            Dim src1 = "
Class C(Of T)
    Shared Sub Bar()
        Console.ReadLine(2)
    End Sub
End Class
"

            Dim src2 = "
Class C(Of T)
    Shared Sub Bar()
            Console.ReadLine(2)
    End Sub
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                diagnostics:={Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "            ", FeaturesResources.method)},
                capabilities:=EditAndContinueCapabilities.Baseline)

            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Bar"))},
                capabilities:=EditAndContinueCapabilities.GenericUpdateMethod)

        End Sub

        <Fact>
        Public Sub Method_RudeRecompile3()
            Dim src1 = "
Class C
    Shared Sub Bar(Of T)()
            Console.ReadLine(2)
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub Bar(Of T)()
        Console.ReadLine(2)
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                diagnostics:={Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "        ", FeaturesResources.method)},
                capabilities:=EditAndContinueCapabilities.Baseline)

            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Bar"))},
                capabilities:=EditAndContinueCapabilities.GenericUpdateMethod)
        End Sub

        <Fact>
        Public Sub Method_Async_Recompile()
            Dim src1 = "
Class C
    Shared Async Function Bar() As Task(Of Integer)
        Console.WriteLine(2)
    End Function
End Class
"

            Dim src2 = "
Class C
    Shared Async Function Bar() As Task(Of Integer)
        Console.WriteLine(
            2)
    End Function
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.Bar"), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Method_StaticLocal_LineChange()
            Dim src1 = "
Class C
    Shared Sub F()
        Static a = 0
        a = 1
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub F()

        Static a = 0
        a = 1
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New SourceLineUpdate(3, 4)}, {})
        End Sub

        <Fact>
        Public Sub Method_StaticLocal_Recompile()
            Dim src1 = "
Class C
    Shared Sub F()
        Static a = 0
        a = 1
    End Sub
End Class
"

            Dim src2 = "
Class C
    Shared Sub F()
             Static a = 0
        a = 1
    End Sub
End Class
"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                diagnostics:={Diagnostic(RudeEditKind.UpdateStaticLocal, "Static a = 0", GetResource("method"))})
        End Sub

#End Region

#Region "Constructors"

        <Fact>
        Public Sub Constructor_Recompile1()
            Dim src1 =
"Class C
    Shared Sub New()
        Console.ReadLine(2)
    End Sub
End Class"

            Dim src2 =
"Class C
    Shared Sub _
                New()
        Console.ReadLine(2)
    End Sub
End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").SharedConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Constructor_Recompile2()
            Dim src1 =
"Class C
    Sub New()
        MyBase.New()
    End Sub
End Class"

            Dim src2 =
"Class C
    Sub _
                New()
        MyBase.New()
    End Sub
End Class"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single())})
        End Sub

#End Region

#Region "Fields"

        <Fact>
        Public Sub Field_Init_Reorder1()
            Dim src1 = "
Class C
    Shared Goo As Integer = 1
    Shared Bar As Integer = 2
End Class
"
            Dim src2 = "
Class C
    Shared Bar As Integer = 2
    Shared Goo As Integer = 1
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                diagnostics:={Diagnostic(RudeEditKind.Move, "Shared Bar As Integer = 2", FeaturesResources.field)})
        End Sub

        <Fact>
        Public Sub Field_AsNew_Reorder1()
            Dim src1 = "
Class C
    Shared a As New C()
    Shared c As New C()
End Class
"
            Dim src2 = "
Class C
    Shared c As New C()
    Shared a As New C()
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                diagnostics:={Diagnostic(RudeEditKind.Move, "Shared c As New C()", FeaturesResources.field)})
        End Sub

        <Fact>
        Public Sub Field_AsNew_Reorder2()
            Dim src1 = "
Class C
    Shared a, b As New C()
    Shared c, d As New C()
End Class
"

            Dim src2 = "
Class C
    Shared c, d As New C()
    Shared a, b As New C()
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                diagnostics:={Diagnostic(RudeEditKind.Move, "Shared c, d As New C()", FeaturesResources.field)})
        End Sub

        <Fact>
        Public Sub Field_Init_LineChange1()
            Dim src1 = "
Class C
    Dim Goo = 1
End Class
"

            Dim src2 = "
Class C


    Dim Goo = 1
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New SourceLineUpdate(2, 4)}, {})
        End Sub

        <Fact>
        Public Sub Field_Init_LineChange2()
            Dim src1 = "
Class C
    Dim Goo = 1
End Class
"

            Dim src2 = "
Class C
    Dim _
        Goo = 1
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New SourceLineUpdate(2, 3)}, {})
        End Sub

        <Fact>
        Public Sub Field_AsNew_LineChange1()
            Dim src1 = "
Class C
    Dim Goo As New D()
End Class
"

            Dim src2 = "
Class C
    Dim _
        Goo As New D()
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New SourceLineUpdate(2, 3)}, {})
        End Sub

        <Fact>
        Public Sub Field_AsNew_LineChange2()
            Dim src1 = "
Class C
    Private Shared Goo As New D()
End Class
"

            Dim src2 = "
Class C
    Private _
            Shared Goo As New D()
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New SourceLineUpdate(2, 3)}, {})
        End Sub

        <Fact>
        Public Sub Field_AsNew_LineChange_WithLambda()
            Dim src1 = "
Class C
    Dim Goo, Bar As New D(Function() 1)
End Class
"

            Dim src2 = "
Class C
    Dim Goo, _
             Bar As New D(Function() 1)
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                {New SourceLineUpdate(2, 3)},
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_ArrayInit_LineChange1()
            Dim src1 = "
Class C
    Dim Goo(1)
End Class
"

            Dim src2 = "
Class C


    Dim Goo(1)
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New SourceLineUpdate(2, 4)}, {})
        End Sub

        <Fact>
        Public Sub Field_ArrayInit_LineChange2()
            Dim src1 = "
Class C
    Dim Goo(1)
End Class
"

            Dim src2 = "
Class C
    Dim _
        Goo(1)
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New SourceLineUpdate(2, 3)}, {})
        End Sub

        <Fact>
        Public Sub Field_Init_Recompile1a()
            Dim src1 = "
Class C
    Dim Goo = 1
End Class
"

            Dim src2 = "
Class C
    Dim Goo = _
              1
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_Init_Recompile1b()
            Dim src1 = "
Class C
    Dim Goo = 1
End Class
"

            Dim src2 = "
Class C
    Dim Goo _ 
            = 1
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_Init_Recompile1c()
            Dim src1 = "
Class C
    Dim Goo ? = 1
End Class
"

            Dim src2 = "
Class C
    Dim Goo _
            ? = 1
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_Init_Recompile1()
            Dim src1 = "
Class C
    Dim Goo = 1
End Class
"

            Dim src2 = "
Class C
    Dim Goo =  1
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_Init_Recompile2()
            Dim src1 = "
Class C
    Dim Goo As Integer = 1 + 1
End Class
"

            Dim src2 = "
Class C
    Dim Goo As Integer = 1 +  1
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_Init_Recompile_Reloadable()
            Dim src1 = ReloadableAttributeSrc & "
<CreateNewOnMetadataUpdate>
Class C
    Dim Goo As Integer = 1 + 1
End Class
"

            Dim src2 = ReloadableAttributeSrc & "
<CreateNewOnMetadataUpdate>
Class C
    Dim Goo As Integer = 1 +  1
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Replace, Function(c) c.GetMember("C"))},
                capabilities:=EditAndContinueCapabilities.NewTypeDefinition)
        End Sub

        <Fact>
        Public Sub Field_SingleAsNew_Recompile1()
            Dim src1 = "
Class C
    Dim Goo As New D()
End Class
"

            Dim src2 = "
Class C
    Dim Goo As _
               New D()
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_SingleAsNew_Recompile2()
            Dim src1 = "
Class C
    Dim Goo As New D()
End Class
"

            Dim src2 = "
Class C
    Dim Goo _
            As New D()
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_MultiAsNew_Recompile1()
            Dim src1 = "
Class C
    Dim Goo, Bar As New D()
End Class
"

            Dim src2 = "
Class C
    Dim Goo, _
             Bar As New D()
End Class
"

            Dim edits = GetTopEdits(src1, src2)

            ' to make it simpler, we recompile the constructor (by reporting a field as a node update)
            edits.VerifyLineEdits(
                {New SourceLineUpdate(2, 3)},
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_MultiAsNew_Recompile2()
            Dim src1 = "
Class C
    Dim Goo, Bar As New D()
End Class
"

            Dim src2 = "
Class C
    Dim Goo,  Bar As New D()
End Class
"

            Dim edits = GetTopEdits(src1, src2)

            ' we treat "Goo + New D()" as a whole for simplicity
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_MultiAsNew_Recompile3()
            Dim src1 = "
Class C
    Dim  Goo, Bar As New D()
End Class
"

            Dim src2 = "
Class C
    Dim Goo, Bar As New D()
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_MultiAsNew_Recompile4()
            Dim src1 = "
Class C
    Dim Goo, Bar As New D()
End Class
"

            Dim src2 = "
Class C
    Dim Goo, Bar As _
                    New D()
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_ArrayInit_Recompile1()
            Dim src1 = "
Class C
    Dim Goo(1)
End Class
"

            Dim src2 = "
Class C
    Dim  Goo(1)
End Class
"
            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Field_RudeRecompile1()
            Dim src1 = "
Class C(Of T)
    Dim Goo As Integer = 1 + 1
End Class
"

            Dim src2 = "
Class C(Of T)
    Dim Goo As Integer = 1 +  1
End Class
"

            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                diagnostics:=
                {
                    Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "  ", GetResource("field"))
                },
                capabilities:=EditAndContinueCapabilities.Baseline)

            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                semanticEdits:=
                {
                    SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)
                },
                capabilities:=EditAndContinueCapabilities.GenericUpdateMethod)
        End Sub

        <Fact>
        Public Sub Field_Generic_Reloadable()
            Dim src1 = ReloadableAttributeSrc & "
<CreateNewOnMetadataUpdate>
Class C(Of T)
    Dim Goo As Integer = 1 + 1
End Class
"

            Dim src2 = ReloadableAttributeSrc & "
<CreateNewOnMetadataUpdate>
Class C(Of T)
    Dim Goo As Integer = 1 +  1
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                semanticEdits:={SemanticEdit(SemanticEditKind.Replace, Function(c) c.GetMember("C"))},
                capabilities:=EditAndContinueCapabilities.NewTypeDefinition)
        End Sub
#End Region

#Region "Auto-Properties"
        <Fact>
        Public Sub Property_NoChange1()
            Dim src1 = "
Class C
    Property Goo As Integer = 1 Implements I.P
End Class
"

            Dim src2 = "
Class C
    Property Goo As Integer = 1 _
                                Implements I.P
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(Array.Empty(Of SequencePointUpdates), {})
        End Sub

        <Fact>
        Public Sub PropertyTypeChar_NoChange1()
            Dim src1 = "
Class C
    Property Goo$ = """" Implements I.P
End Class
"

            Dim src2 = "
Class C
    Property Goo$ = """" _
                       Implements I.P
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(Array.Empty(Of SequencePointUpdates), {})
        End Sub

        <Fact>
        Public Sub PropertyAsNew_NoChange1()
            Dim src1 = "
Class C
    Property Goo As New C() Implements I.P
End Class
"

            Dim src2 = "
Class C
    Property Goo As New C() _
                            Implements I.P
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(Array.Empty(Of SequencePointUpdates), {})
        End Sub

        <Fact>
        Public Sub Property_LineChange1()
            Dim src1 = "
Class C
    Property Goo As Integer = 1
End Class
"

            Dim src2 = "
Class C

    Property Goo As Integer = 1
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New SourceLineUpdate(2, 3)}, {})
        End Sub

        <Fact>
        Public Sub Property_LineChange2()
            Dim src1 = "
Class C
    Property Goo As Integer = 1
End Class
"

            Dim src2 = "
Class C
    Property _
             Goo As Integer = 1
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New SourceLineUpdate(2, 3)}, {})
        End Sub

        <Fact>
        Public Sub PropertyTypeChar_LineChange2()
            Dim src1 = "
Class C
    Property Goo$ = """"
End Class
"

            Dim src2 = "
Class C
    Property _
             Goo$ = """"
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New SourceLineUpdate(2, 3)}, {})
        End Sub

        <Fact>
        Public Sub PropertyAsNew_LineChange1()
            Dim src1 = "
Class C
    Property Goo As New C()
End Class
"

            Dim src2 = "
Class C
    Property _
             Goo As New C()
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits({New SourceLineUpdate(2, 3)}, {})
        End Sub

        <Fact>
        Public Sub Property_Recompile1()
            Dim src1 = "
Class C
    Property Goo As Integer = 1
End Class
"

            Dim src2 = "
Class C
    Property Goo _
                 As Integer = 1
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Property_Recompile2()
            Dim src1 = "
Class C
    Property Goo As Integer = 1
End Class
"

            Dim src2 = "
Class C
    Property Goo As _
                    Integer = 1
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Property_Recompile3()
            Dim src1 = "
Class C
    Property Goo As Integer = 1
End Class
"

            Dim src2 = "
Class C
    Property Goo As Integer _
                            = 1
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub Property_Recompile4()
            Dim src1 = "
Class C
    Property Goo As Integer = 1
End Class
"

            Dim src2 = "
Class C
    Property Goo As Integer = _
                              1
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub PropertyAsNew_Recompile1()
            Dim src1 = "
Class C
    Property Goo As New C()
End Class
"

            Dim src2 = "
Class C
    Property Goo As _
                    New C()
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub

        <Fact>
        Public Sub PropertyTypeChar_Recompile1()
            Dim src1 = "
Class C
    Property Goo$ = """"
End Class
"

            Dim src2 = "
Class C
    Property Goo$ = _
                    """"
End Class
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                Array.Empty(Of SequencePointUpdates),
                {SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").InstanceConstructors.Single(), preserveLocalVariables:=True)})
        End Sub
#End Region

#Region "Line Mappings"

        ' <summary>
        ' Validates that changes in #line directives produce semantic updates of the containing method.
        ' </summary>
        <Fact>
        Public Sub LineMapping_ChangeLineNumber_OutsideOfMethod()
            Dim src1 = "
#ExternalSource(""a"", 1)
Class C
    Dim x As Integer = 1
    Shared Dim y As Integer = 1
    Sub F1() : End Sub
    Sub F2() : End Sub
End Class
Class D
    Sub New() : End Sub
#End ExternalSource
#ExternalSource(""a"", 4)
    Sub F3() : End Sub
#End ExternalSource
#ExternalSource(""a"", 5)
    Sub F4() : End Sub
#End ExternalSource
End Class
"

            Dim src2 = "
#ExternalSource(""a"", 11)
Class C
    Dim x As Integer = 1
    Shared Dim y As Integer = 1
    Sub F1() : End Sub
    Sub F2() : End Sub
End Class
Class D
    Sub New() : End Sub
#End ExternalSource
#ExternalSource(""a"", 4)
    Sub F3() : End Sub
    Sub F4() : End Sub
#End ExternalSource
End Class
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyLineEdits(
                {
                    New SequencePointUpdates("a", ImmutableArray.Create(
                        New SourceLineUpdate(1, 11), ' x, y, F1, F2
                        New SourceLineUpdate(5, 5),' lines between F2 And D ctor
                        New SourceLineUpdate(7, 17)))' D ctor
                },
                semanticEdits:=
                {
                    SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("D.F3")),
                    SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("D.F4"))
                })
        End Sub

        <Fact>
        Public Sub LineMapping_LineDirectivesAndWhitespace()
            Dim src1 = "
Class C
#ExternalSource(""a"", 5)
#End ExternalSource
#ExternalSource(""a"", 6)



    Sub F() : End Sub ' line 9
End Class
#End ExternalSource
"
            Dim src2 = "
Class C
#ExternalSource(""a"", 9)
    Sub F() : End Sub
End Class
#End ExternalSource
"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifySemantics()
        End Sub

        <Fact>
        Public Sub LineMapping_MultipleFiles()
            Dim src1 = "
Class C
    Sub F()
#ExternalSource(""a"", 1)
        A()
#End ExternalSource
#ExternalSource(""b"", 1)
        B()
#End ExternalSource
    End Sub
End Class"
            Dim src2 = "
Class C
    Sub F()
#ExternalSource(""a"", 2)
        A()
#End ExternalSource
#ExternalSource(""b"", 2)
        B()
#End ExternalSource
    End Sub
End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyLineEdits(
                {
                    New SequencePointUpdates("a", ImmutableArray.Create(New SourceLineUpdate(0, 1))),
                    New SequencePointUpdates("b", ImmutableArray.Create(New SourceLineUpdate(0, 1)))
                })
        End Sub

        <Fact>
        Public Sub LineMapping_FileChange_Recompile()
            Dim src1 = "
Class C
    Sub F()
        A()
#ExternalSource(""a"", 1)
        B()
#End ExternalSource
#ExternalSource(""a"", 3)
        C()
    End Sub


    Dim x As Integer = 1
#End ExternalSource
End Class"
            Dim src2 = "
Class C
    Sub F()
        A()
#ExternalSource(""b"", 1)
        B()
#End ExternalSource
#ExternalSource(""a"", 2)
        C()
    End Sub

    Dim x As Integer = 1
#End ExternalSource
End Class"
            Dim edits = GetTopEdits(src1, src2)

            edits.VerifyLineEdits(
                {
                    New SequencePointUpdates("a", ImmutableArray.Create(New SourceLineUpdate(6, 4)))
                },
                semanticEdits:={SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember("C.F"))})

            edits.VerifySemantics(ActiveStatementsDescription.Empty,
            {
                SemanticEdit(SemanticEditKind.Update, Function(c) c.GetMember(Of NamedTypeSymbol)("C").GetMember(Of MethodSymbol)("F"))
            })
        End Sub

        <Fact>
        Public Sub LineMapping_FileChange_RudeEdit()
            Dim src1 = "
#ExternalSource(""a"", 1)
Class C
    Sub Bar(Of T)()
    End Sub
End Class
#End ExternalSource
"
            Dim src2 = "
#ExternalSource(""b"", 1)
Class C
    Sub Bar(Of T)()
    End Sub
End Class
#End ExternalSource
"

            Dim edits = GetTopEdits(src1, src2)
            edits.VerifyLineEdits(
                 Array.Empty(Of SequencePointUpdates)(),
                 diagnostics:={Diagnostic(RudeEditKind.UpdatingGenericNotSupportedByRuntime, "Sub Bar(Of T)()", FeaturesResources.method)},
                 capabilities:=EditAndContinueCapabilities.Baseline)
        End Sub

#End Region
    End Class
End Namespace
