' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public Class TypeVariablesExpansionTests
        Inherits VisualBasicResultProviderTestBase

        <Fact>
        Public Sub TypeVariables()
            Dim source0 =
"Class A
End Class
Class B
    Inherits A
    Friend Shared F As Object = 1
End Class"
            Dim assembly0 = GetAssembly(source0)
            Dim type0 = assembly0.GetType("B")
            Dim source1 =
".class private abstract sealed beforefieldinit specialname '<>c__TypeVariables'<T,U>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
}"
            Dim assemblyBytes As ImmutableArray(Of Byte) = Nothing
            Dim pdbBytes As ImmutableArray(Of Byte) = Nothing
            BasicTestBase.EmitILToArray(source1, appendDefaultHeader:=True, includePdb:=False, assemblyBytes:=assemblyBytes, pdbBytes:=pdbBytes)
            Dim assembly1 = ReflectionUtilities.Load(assemblyBytes)
            Dim type1 = assembly1.GetType(ExpressionCompilerConstants.TypeVariablesClassName).MakeGenericType({GetType(Integer), type0})
            Dim value = CreateDkmClrValue(value:=Nothing, type:=type1, valueFlags:=DkmClrValueFlags.Synthetic)
            Dim result = FormatResult("typevars", value)
            Verify(result,
                EvalResult("Type variables", "", "", Nothing, DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data))
            Dim children = GetChildren(result)
            Verify(children,
                EvalResult("T", "Integer", "Integer", Nothing, DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data),
                EvalResult("U", "B", "B", Nothing, DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data))
        End Sub

    End Class

End Namespace
