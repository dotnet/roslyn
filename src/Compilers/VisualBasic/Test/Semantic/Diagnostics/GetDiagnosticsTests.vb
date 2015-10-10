' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class GetDiagnosticsTests
        Inherits BasicTestBase

        <Fact>
        Public Sub DiagnosticsFilteredInMethodBody()
            Dim source = <project><file>
Class C
    Sub S()
        @
        #
        !
    End Sub
End Class
</file></project>

            Dim compilation = CreateCompilationWithMscorlib(source)
            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single())

            ' CreateCompilation... method normalizes EOL chars of xml literals,
            ' so we cannot verify against spans in the original xml here as positions would differ
            Dim sourceText = compilation.SyntaxTrees.Single().GetText().ToString()
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "(?s)^.*$", "BC30035", "BC30248", "BC30203", "BC30157")
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "@", "BC30035")
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "#", "BC30248")
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "(?<=\!)", "BC30203", "BC30157")
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "!", "BC30203", "BC30157")
        End Sub

        <Fact>
        Public Sub DiagnosticsFilteredInMethodBodyInsideNamespace()
            Dim source = <project><file>
Namespace N
    Class C
        Sub S()
            X
        End Sub
    End Class
End Namespace

Class D
    ReadOnly Property P As Integer
        Get
            Return Y
        End Get
    End Property
End Class
</file></project>

            Dim compilation = CreateCompilationWithMscorlib(source)
            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single())

            Dim sourceText = compilation.SyntaxTrees.Single().GetText().ToString()
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "X", "BC30451")
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "Y", "BC30451")
        End Sub

        <Fact>
        Public Sub DiagnosticsFilteredForInsersectingIntervals()
            Dim source = <project><file>
Class C
    Inherits Abracadabra
End Class
</file></project>

            Dim compilation = CreateCompilationWithMscorlib(source)
            Dim model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single())

            ' CreateCompilation... method normalizes EOL chars of xml literals,
            ' so we cannot verify against spans in the original xml here as positions would differ
            Dim sourceText = compilation.SyntaxTrees.Single().GetText().ToString()
            Const ErrorId = "BC30002"
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "(?s)^.*$", ErrorId)
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "Abracadabra", ErrorId)
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "ts Abracadabra", ErrorId)
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "ts Abracadabr", ErrorId)
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "Abracadabra[\r\n]+", ErrorId)
            DiagnosticsHelper.VerifyDiagnostics(model, sourceText, "bracadabra[\r\n]+", ErrorId)
        End Sub

        <Fact, WorkItem(1066483)>
        Public Sub TestDiagnosticWithSeverity()
            Dim source = <project><file>
Class C
    Sub Foo()
        Dim x
    End Sub
End Class
</file></project>
            Dim compilation = CreateCompilationWithMscorlib(source)
            Dim diag = compilation.GetDiagnostics().Single()

            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity)
            Assert.Equal(1, diag.WarningLevel)

            Dim [error] = diag.WithSeverity(DiagnosticSeverity.Error)
            Assert.Equal(DiagnosticSeverity.Error, [error].Severity)
            Assert.Equal(DiagnosticSeverity.Warning, [error].DefaultSeverity)
            Assert.Equal(0, [error].WarningLevel)

            Dim warning = [error].WithSeverity(DiagnosticSeverity.Warning)
            Assert.Equal(DiagnosticSeverity.Warning, warning.Severity)
            Assert.Equal(DiagnosticSeverity.Warning, warning.DefaultSeverity)
            Assert.Equal(1, warning.WarningLevel)

            Dim hidden = warning.WithSeverity(DiagnosticSeverity.Hidden)
            Assert.Equal(DiagnosticSeverity.Hidden, hidden.Severity)
            Assert.Equal(DiagnosticSeverity.Warning, hidden.DefaultSeverity)
            Assert.Equal(4, hidden.WarningLevel)

            Dim info = warning.WithSeverity(DiagnosticSeverity.Info)
            Assert.Equal(DiagnosticSeverity.Info, info.Severity)
            Assert.Equal(DiagnosticSeverity.Warning, info.DefaultSeverity)
            Assert.Equal(4, info.WarningLevel)
        End Sub
    End Class
End Namespace
