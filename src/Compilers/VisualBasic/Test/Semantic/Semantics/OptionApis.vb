' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Linq
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class OptionApis
        Inherits SemanticModelTestBase

        <Fact>
        Public Sub Options1()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="allon.vb">
Option Strict On
Option Infer On
Option Explicit On
Option Compare Text
    </file>
    <file name="alloff.vb">
Option Strict Off
Option Infer Off
Option Explicit Off
Option Compare Binary
    </file>
    <file name="empty.vb"></file>
</compilation>, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.Custom).WithOptionInfer(False).WithOptionExplicit(True).WithOptionCompareText(False))

            Dim semanticModelAllOn = CompilationUtils.GetSemanticModel(compilation, "allon.vb")
            Assert.Equal(OptionStrict.On, semanticModelAllOn.OptionStrict)
            Assert.Equal(True, semanticModelAllOn.OptionInfer)
            Assert.Equal(True, semanticModelAllOn.OptionExplicit)
            Assert.Equal(True, semanticModelAllOn.OptionCompareText)

            Dim semanticModelAllOff = CompilationUtils.GetSemanticModel(compilation, "alloff.vb")
            Assert.Equal(OptionStrict.Off, semanticModelAllOff.OptionStrict)
            Assert.Equal(False, semanticModelAllOff.OptionInfer)
            Assert.Equal(False, semanticModelAllOff.OptionExplicit)
            Assert.Equal(False, semanticModelAllOff.OptionCompareText)

            Dim semanticModelEmpty = CompilationUtils.GetSemanticModel(compilation, "empty.vb")
            Assert.Equal(OptionStrict.Custom, semanticModelEmpty.OptionStrict)
            Assert.Equal(False, semanticModelEmpty.OptionInfer)
            Assert.Equal(True, semanticModelEmpty.OptionExplicit)
            Assert.Equal(False, semanticModelEmpty.OptionCompareText)
        End Sub
    End Class
End Namespace
