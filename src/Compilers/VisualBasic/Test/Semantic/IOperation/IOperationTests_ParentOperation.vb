' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact>
        Public Sub TestParentOperations()
            Dim sourceCode = TestResource.AllInOneVisualBasicCode

            Dim fileName = "a.vb"
            Dim syntaxTree = Parse(sourceCode, fileName, options:=Nothing)

            Dim compilation = CreateCompilationWithMscorlib45AndVBRuntime({syntaxTree}, DefaultVbReferences.Concat({ValueTupleRef, SystemRuntimeFacadeRef}))
            Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = fileName).Single()
            Dim model = compilation.GetSemanticModel(tree)

            VerifyParentOperations(model)
        End Sub
    End Class
End Namespace

