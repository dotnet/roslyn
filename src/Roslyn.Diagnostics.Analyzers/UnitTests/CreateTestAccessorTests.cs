// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeRefactoringVerifier<
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpCreateTestAccessor>;
using VerifyVB = Test.Utilities.VisualBasicCodeRefactoringVerifier<
    Roslyn.Diagnostics.VisualBasic.Analyzers.VisualBasicCreateTestAccessor>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class CreateTestAccessorTests
    {
        [Theory]
        [InlineData("$$class TestClass ")]
        [InlineData("class $$TestClass ")]
        [InlineData("class TestClass$$ ")]
        [InlineData("class [|TestClass|] ")]
        [InlineData("[|class TestClass|] ")]
        public async Task CreateTestAccessorCSharp(string typeHeader)
        {
            var source = typeHeader + @"{
}";
            var fixedSource = @"class TestClass {
    internal TestAccessor GetTestAccessor()
    {
        return new TestAccessor(this);
    }

    internal readonly struct TestAccessor
    {
        private readonly TestClass _testClass;

        internal TestAccessor(TestClass testClass)
        {
            _testClass = testClass;
        }
    }
}";

            await VerifyCS.VerifyRefactoringAsync(source, fixedSource);
        }

        [Theory]
        [InlineData("$$Class TestClass")]
        [InlineData("Class $$TestClass")]
        [InlineData("Class TestClass$$")]
        [InlineData("Class [|TestClass|]")]
        [InlineData("[|Class TestClass|]")]
        public async Task CreateTestAccessorVisualBasic(string typeHeader)
        {
            var source = $@"{typeHeader}
End Class";
            var fixedSource = @"Class TestClass
    Friend Function GetTestAccessor() As TestAccessor
        Return New TestAccessor(Me)
    End Function

    Friend Structure TestAccessor
        Private ReadOnly _testClass As TestClass

        Friend Sub New(testClass As TestClass)
            _testClass = testClass
        End Sub
    End Structure
End Class";

            await VerifyVB.VerifyRefactoringAsync(source, fixedSource);
        }
    }
}
