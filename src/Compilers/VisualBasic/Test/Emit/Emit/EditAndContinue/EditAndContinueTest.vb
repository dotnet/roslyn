' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Xunit.Abstractions

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests

    Friend NotInheritable Class EditAndContinueTest
        Inherits EditAndContinueTest(Of EditAndContinueTest)

        Private ReadOnly _options As VisualBasicCompilationOptions
        Private ReadOnly _parseOptions As VisualBasicParseOptions
        Private ReadOnly _targetFramework As TargetFramework
        Private ReadOnly _references As IEnumerable(Of MetadataReference)
        Private ReadOnly _assemblyName As String

        Sub New(Optional output As ITestOutputHelper = Nothing,
                Optional options As VisualBasicCompilationOptions = Nothing,
                Optional parseOptions As VisualBasicParseOptions = Nothing,
                Optional targetFramework As TargetFramework = TargetFramework.StandardAndVBRuntime,
                Optional references As IEnumerable(Of MetadataReference) = Nothing,
                Optional assemblyName As String = "",
                Optional verification As Verification? = Nothing)

            MyBase.New(output, verification)

            _options = If(options, TestOptions.DebugDll).WithConcurrentBuild(False)
            _parseOptions = If(parseOptions, TestOptions.Regular)
            _targetFramework = targetFramework
            _references = references
            _assemblyName = assemblyName
        End Sub

        Protected Overrides Function CreateCompilation(tree As SyntaxTree) As Compilation
            Return CompilationUtils.CreateCompilation(tree, _references, options:=_options, assemblyName:=_assemblyName, targetFramework:=_targetFramework)
        End Function

        Protected Overrides Function CreateSourceWithMarkedNodes(source As String) As SourceWithMarkedNodes
            Return EditAndContinueTestBase.MarkedSource(source, options:=_parseOptions)
        End Function

        Protected Overrides Function GetEquivalentNodesMap(left As ISymbol, right As ISymbol) As Func(Of SyntaxNode, SyntaxNode)
            Return EditAndContinueTestBase.GetEquivalentNodesMap(DirectCast(left, MethodSymbol), DirectCast(right, MethodSymbol))
        End Function
    End Class
End Namespace
