// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Test.Utilities.CSharpSecurityCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.DoNotCallGetTestAccessor,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicSecurityCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.DoNotCallGetTestAccessor,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class DoNotCallGetTestAccessorTests
    {
        [Fact]
        public async Task DoNotCallGetTestAccessor_CSharpAsync()
        {
            var source = @"class TestClass {
    internal void Method()
    {
        _ = [|GetTestAccessor()|];
        _ = [|this.GetTestAccessor()|];
        _ = [|new TestClass().GetTestAccessor()|];
    }

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

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task DoNotCallGetTestAccessor_VisualBasicAsync()
        {
            var source = @"Class TestClass
    Friend Sub Method()
        Dim a = [|GetTestAccessor()|]
        Dim b = [|Me.GetTestAccessor()|]
        Dim c = [|New TestClass().GetTestAccessor()|]
    End Sub

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

            await VerifyVB.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task DoNotConstructTestAccessor_CSharpAsync()
        {
            var source = @"class TestClass {
    internal void Method()
    {
        _ = [|new TestAccessor(this)|];
        _ = [|new TestAccessor(new TestClass())|];
    }

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

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task DoNotConstructTestAccessor_VisualBasicAsync()
        {
            var source = @"Class TestClass
    Friend Sub Method()
        Dim a = [|New TestAccessor(Me)|]
        Dim b = [|New TestAccessor(New TestClass())|]
    End Sub

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

            await VerifyVB.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task DoNotAccessTestAccessorStaticMember_CSharpAsync()
        {
            var source = @"
using System;

class TestClass {
    internal void Method()
    {
        _ = [|TestAccessor.ExposedField|];
        _ = [|TestAccessor.ExposedProperty|];
        [|TestAccessor.ExposedEvent|] += delegate { };
        [|TestAccessor.ExposedMethod()|];
    }

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

        public static int ExposedField;
        public static int ExposedProperty => 0;
        public static event EventHandler ExposedEvent { add { } remove { } }
        public static void ExposedMethod() { }
    }
}

class OtherClass {
    internal void Method()
    {
        _ = [|TestClass.TestAccessor.ExposedField|];
        _ = [|TestClass.TestAccessor.ExposedProperty|];
        [|TestClass.TestAccessor.ExposedEvent|] += delegate { };
        [|TestClass.TestAccessor.ExposedMethod()|];
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task DoNotAccessTestAccessorStaticMember_VisualBasicAsync()
        {
            var source = @"
Imports System

Class TestClass
    Friend Sub Method()
        Dim a = [|TestAccessor.ExposedField|]
        Dim b = [|TestAccessor.ExposedProperty|]
        AddHandler [|TestAccessor.ExposedEvent|], Sub(_1, _2) Return
        [|TestAccessor.ExposedMethod()|]
    End Sub

    Friend Function GetTestAccessor() As TestAccessor
        Return New TestAccessor(Me)
    End Function

    Friend Structure TestAccessor
        Private ReadOnly _testClass As TestClass

        Friend Sub New(testClass As TestClass)
            _testClass = testClass
        End Sub

        Public Shared ExposedField As Integer
        Public Shared ReadOnly Property ExposedProperty As Integer = 0
        Public Shared Event ExposedEvent As EventHandler

        Public Shared Sub ExposedMethod()
        End Sub
    End Structure
End Class

Class OtherClass
    Friend Sub Method()
        Dim a = [|TestClass.TestAccessor.ExposedField|]
        Dim b = [|TestClass.TestAccessor.ExposedProperty|]
        AddHandler [|TestClass.TestAccessor.ExposedEvent|], Sub(_1, _2) Return
        [|TestClass.TestAccessor.ExposedMethod()|]
    End Sub
End Class";

            await VerifyVB.VerifyAnalyzerAsync(source);
        }
    }
}
