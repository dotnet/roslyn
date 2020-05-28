// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using CSLanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion;
using VBLanguageVersion = Microsoft.CodeAnalysis.VisualBasic.LanguageVersion;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.CreateTestAccessor,
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpCreateTestAccessorFixer>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.CreateTestAccessor,
    Roslyn.Diagnostics.VisualBasic.Analyzers.VisualBasicCreateTestAccessorFixer>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class CreateTestAccessorTests
    {
        [Fact]
        public async Task CreateTestAccessorCSharp()
        {
            var source = @"class [|TestClass|] {
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

            var test = new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
                LanguageVersion = CSLanguageVersion.CSharp7_2,
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
            };

            await test.RunAsync();
        }

        [Fact]
        public async Task CreateTestAccessorVisualBasic()
        {
            var source = @"Class [|TestClass|]
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

            var test = new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
                LanguageVersion = VBLanguageVersion.Default,
                TestBehaviors = TestBehaviors.SkipSuppressionCheck,
            };

            await test.RunAsync();
        }
    }
}
