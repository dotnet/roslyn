// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class BindingAwaitTests : CompilingTestBase
    {
        [WorkItem(547172, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547172")]
        [Fact, WorkItem(531516, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531516")]
        public void Bug18241()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(" class C { void M() { await X() on ");
            SourceText text = tree.GetText();
            TextSpan span = new TextSpan(text.Length, 0);
            TextChange change = new TextChange(span, "/*comment*/");
            SourceText newText = text.WithChanges(change);
            // This line caused an assertion and then crashed in the parser.
            var newTree = tree.WithChangedText(newText);
        }

        [Fact]
        public void AwaitBadExpression()
        {
            var source = @"
static class Program
{
    static void Main() { }

    static async void f()
    {
        await goo;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (8,15): error CS0103: The name 'goo' does not exist in the current context
                //         await goo;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "goo").WithArguments("goo"));
        }

        [Fact]
        public void MissingGetAwaiterInstanceMethod()
        {
            var source = @"
static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (8,9): error CS1061: 'A' does not contain a definition for 'GetAwaiter' and no extension method 'GetAwaiter' accepting a first argument of type 'A' could be found (are you missing a using directive or an assembly reference?)
                //         await new A();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "await new A()").WithArguments("A", "GetAwaiter")
                );
        }

        [Fact]
        public void InaccessibleGetAwaiterInstanceMethod()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
        await new B();
        await new C();
        await new D();
    }
}

class A
{
    Awaiter GetAwaiter() { return new Awaiter(); }
}

class B
{
    private Awaiter GetAwaiter() { return new Awaiter(); }
}

class C
{
    protected Awaiter GetAwaiter() { return new Awaiter(); }
}

class D
{
    public Awaiter GetAwaiter() { return new Awaiter(); }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS0122: 'A.GetAwaiter()' is inaccessible due to its protection level
                //         await new A();
                Diagnostic(ErrorCode.ERR_BadAccess, "await new A()").WithArguments("A.GetAwaiter()"),
                // (11,9): error CS0122: 'B.GetAwaiter()' is inaccessible due to its protection level
                //         await new B();
                Diagnostic(ErrorCode.ERR_BadAccess, "await new B()").WithArguments("B.GetAwaiter()"),
                // (12,9): error CS0122: 'C.GetAwaiter()' is inaccessible due to its protection level
                //         await new C();
                Diagnostic(ErrorCode.ERR_BadAccess, "await new C()").WithArguments("C.GetAwaiter()")
                );
        }

        [Fact]
        public void StaticGetAwaiterMethod()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
        await new B();
    }
}

class A
{
    public Awaiter GetAwaiter() { return new Awaiter(); }
}

class B
{
    public static Awaiter GetAwaiter() { return new Awaiter(); }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (11,9): error CS1986: 'await' requires that the type B have a suitable GetAwaiter method
                //         await new B();
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "await new B()").WithArguments("B")
                );
        }

        [Fact]
        public void GetAwaiterFieldOrProperty()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
        await new B(null);
    }
}

class A
{
    Awaiter GetAwaiter { get { return new Awaiter(); } }
}

class B
{
    public Awaiter GetAwaiter;

    public B(Awaiter getAwaiter)
    {
        this.GetAwaiter = getAwaiter;
    }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS1955: Non-invocable member 'A.GetAwaiter' cannot be used like a method.
                //         await new A();
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "await new A()").WithArguments("A.GetAwaiter"),
                // (11,9): error CS1955: Non-invocable member 'B.GetAwaiter' cannot be used like a method.
                //         await new B(null);
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "await new B(null)").WithArguments("B.GetAwaiter")
                );
        }

        [Fact]
        public void GetAwaiterParams()
        {
            var source = @"
using System;

public class A
{
    public Awaiter GetAwaiter(params object[] xs) { throw new Exception(); }
}

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

public static class Test
{
    static async void F()
    {
        await new A();
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (22,9): error CS1986: 'await' requires that the type A have a suitable GetAwaiter method
                //         await new A();
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "await new A()").WithArguments("A"));
        }

        [Fact]
        public void VoidReturningGetAwaiterMethod()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public void GetAwaiter() { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS1986: 'await' requires that the type A have a suitable GetAwaiter method
                //         await new A();
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "await new A()").WithArguments("A"));
        }

        [Fact]
        public void InaccessibleGetAwaiterExtensionMethod()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
        await new B();
        await new C();
    }
}

class A { }

class B { }

class C { }

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

static class MyExtensions
{
    static Awaiter GetAwaiter(this A a)
    {
        return new Awaiter();
    }

    private static Awaiter GetAwaiter(this B a)
    {
        return new Awaiter();
    }

    public static Awaiter GetAwaiter(this C a)
    {
        return new Awaiter();
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,15): error CS1929: 'A' does not contain a definition for 'GetAwaiter' and the best extension method overload 'MyExtensions.GetAwaiter(C)' requires a receiver of type 'C'
                //         await new A();
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "new A()").WithArguments("A", "GetAwaiter", "MyExtensions.GetAwaiter(C)", "C"),
                // (11,15): error CS1929: 'B' does not contain a definition for 'GetAwaiter' and the best extension method overload 'MyExtensions.GetAwaiter(C)' requires a receiver of type 'C'
                //         await new B();
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "new B()").WithArguments("B", "GetAwaiter", "MyExtensions.GetAwaiter(C)", "C")
                );
        }

        [Fact]
        public void GetAwaiterExtensionMethodLookup()
        {
            var source = @"
using System;

class A { }

class B { }

class C { }

static class Test
{
    static async void F()
    {
        new A().GetAwaiter();
        new B().GetAwaiter();
        new C().GetAwaiter();

        await new A();
        await new B();
        await new C();
    }
    static Awaiter GetAwaiter(this A a) { throw new Exception(); }

    static void GetAwaiter(this B a) { throw new Exception(); }
}

static class E
{
    public static void GetAwaiter(this A a) { throw new Exception(); }

    public static Awaiter GetAwaiter(this B a) { throw new Exception(); }

    public static Awaiter GetAwaiter(this C a) { throw new Exception(); }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (14,9): error CS0121: The call is ambiguous between the following methods or properties: 'Test.GetAwaiter(A)' and 'E.GetAwaiter(A)'
                //         new A().GetAwaiter();
                Diagnostic(ErrorCode.ERR_AmbigCall, "GetAwaiter").WithArguments("Test.GetAwaiter(A)", "E.GetAwaiter(A)"),
                // (15,9): error CS0121: The call is ambiguous between the following methods or properties: 'Test.GetAwaiter(B)' and 'E.GetAwaiter(B)'
                //         new B().GetAwaiter();
                Diagnostic(ErrorCode.ERR_AmbigCall, "GetAwaiter").WithArguments("Test.GetAwaiter(B)", "E.GetAwaiter(B)"),
                // (18,9): error CS0121: The call is ambiguous between the following methods or properties: 'Test.GetAwaiter(A)' and 'E.GetAwaiter(A)'
                //         await new A();
                Diagnostic(ErrorCode.ERR_AmbigCall, "await new A()").WithArguments("Test.GetAwaiter(A)", "E.GetAwaiter(A)"),
                // (19,9): error CS0121: The call is ambiguous between the following methods or properties: 'Test.GetAwaiter(B)' and 'E.GetAwaiter(B)'
                //         await new B();
                Diagnostic(ErrorCode.ERR_AmbigCall, "await new B()").WithArguments("Test.GetAwaiter(B)", "E.GetAwaiter(B)")
                );
        }

        [Fact]
        public void ExtensionDuellingLookup()
        {
            var source = @"
using System;

public interface I1 { }

public interface I2 { }

public class A : I1, I2 { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

public static class E
{
    public static Awaiter GetAwaiter(this I1 a) { throw new Exception(); }

    public static Awaiter GetAwaiter(this I2 a) { throw new Exception(); }
}

public static class Test
{

    static async void F()
    {
        await new A();
    }

    static void Main()
    {
        F();
    }

    public static void GetAwaiter(this I1 a) { throw new Exception(); }

    public static Awaiter GetAwaiter(this I2 a) { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (31,9): error CS0121: The call is ambiguous between the following methods or properties: 'E.GetAwaiter(I1)' and 'E.GetAwaiter(I2)'
                //         await new A();
                Diagnostic(ErrorCode.ERR_AmbigCall, "await new A()").WithArguments("E.GetAwaiter(I1)", "E.GetAwaiter(I2)")
                );
        }

        [Fact]
        public void ExtensionDuellingMoreDerivedMoreOptional()
        {
            var source = @"
using System;

public interface I1 { }

public interface I2 { }

public class A : I1, I2 { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

public static class Test
{

    static async void F()
    {
        await new A();
    }

    static void Main()
    {
        F();
    }

    public static Awaiter GetAwaiter(this I1 a) { throw new Exception(); }

    public static Awaiter GetAwaiter(this A a, object o = null) { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (19,9): error CS1986: 'await' requires that the type A have a suitable GetAwaiter method
                //         await new A();
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "await new A()").WithArguments("A"));
        }

        [Fact]
        public void ExtensionDuellingLessDerivedLessOptional()
        {
            var source = @"
using System;

public interface I1 { }

public interface I2 { }

public class A : I1, I2 { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

public static class Test
{

    static async void F()
    {
        await new A();
    }

    static void Main()
    {
        F();
    }

    public static void GetAwaiter(this A a, object o = null) { throw new Exception(); }

    public static Awaiter GetAwaiter(this I1 a) { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (19,9): error CS1986: 'await' requires that the type A have a suitable GetAwaiter method
                //         await new A();
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "await new A()").WithArguments("A"));
        }

        [Fact]
        public void ExtensionSiblingLookupOnExtraOptionalParam()
        {
            var source = @"
using System;

public class A { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

public static class Test
{
    static async void F()
    {
        await new A();
    }

    public static void GetAwaiter(this A a, object o = null) { throw new Exception(); }
}

public static class E
{
    public static Awaiter GetAwaiter(this A a) { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [Fact]
        public void ExtensionSiblingLookupOnVoidReturn()
        {
            var source = @"
using System;

public class A { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

public static class Test
{
    static async void F()
    {
        await new A();
    }

    public static void GetAwaiter(this A a) { throw new Exception(); }
}

public static class E
{
    public static Awaiter GetAwaiter(this A a) { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (19,9): error CS0121: The call is ambiguous between the following methods or properties: 'Test.GetAwaiter(A)' and 'E.GetAwaiter(A)'
                //         await new A();
                Diagnostic(ErrorCode.ERR_AmbigCall, "await new A()").WithArguments("Test.GetAwaiter(A)", "E.GetAwaiter(A)"));
        }

        [Fact]
        public void ExtensionSiblingLookupOnInapplicable()
        {
            var source = @"
using System;

public class A { }

public class B { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

public static class Test
{
    static async void F()
    {
        await new A();
    }

    public static void GetAwaiter(this B a) { throw new Exception(); }
}

public static class E
{
    public static Awaiter GetAwaiter(this A a) { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [Fact]
        public void ExtensionSiblingLookupOnOptional()
        {
            var source = @"
using System;

public class A { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

public static class Test
{

    static async void F()
    {
        await new A();
    }

    public static Awaiter GetAwaiter(this object a, object o = null) { throw new Exception(); }
}

public static class E
{
    public static void GetAwaiter(this object a) { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (24,9): error CS1986: 'await' requires that the type A have a suitable GetAwaiter method
                //         await new A();
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "await new A()").WithArguments("A"));
        }

        [Fact]
        public void ExtensionSiblingDuellingLookupOne()
        {
            var source = @"
using System;

public class A { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

public static class Test
{

    static async void F()
    {
        await new A();
    }

    static void Main()
    {
        F();
    }
}

public static class E1
{
    public static void GetAwaiter(this A a) { throw new Exception(); }
}

public static class E2
{
    public static Awaiter GetAwaiter(this A a) { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (20,9): error CS0121: The call is ambiguous between the following methods or properties: 'E1.GetAwaiter(A)' and 'E2.GetAwaiter(A)'
                //         await new A();
                Diagnostic(ErrorCode.ERR_AmbigCall, "await new A()").WithArguments("E1.GetAwaiter(A)", "E2.GetAwaiter(A)")
                );
        }

        [Fact]
        public void ExtensionSiblingDuellingLookupTwo()
        {
            var source = @"
using System;

public class A { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

public static class Test
{

    static async void F()
    {
        await new A();
    }

    static void Main()
    {
        F();
    }
}

public static class E1
{
    public static Awaiter GetAwaiter(this A a) { throw new Exception(); }
}

public static class E2
{
    public static void GetAwaiter(this A a) { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (20,9): error CS0121: The call is ambiguous between the following methods or properties: 'E1.GetAwaiter(A)' and 'E2.GetAwaiter(A)'
                //         await new A();
                Diagnostic(ErrorCode.ERR_AmbigCall, "await new A()").WithArguments("E1.GetAwaiter(A)", "E2.GetAwaiter(A)")
                );
        }

        [Fact]
        public void ExtensionSiblingLookupOnLessDerived()
        {
            var source = @"
using System;

public class A { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

public static class Test
{
    static async void F()
    {
        await new A();
    }

    public static void GetAwaiter(this object a) { throw new Exception(); }
}

public static class E
{
    public static Awaiter GetAwaiter(this A a) { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [Fact]
        public void ExtensionSiblingLookupOnEquallyDerived()
        {
            var source = @"
using System;

public class A { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

static class Test
{
    static void Main()
    {
        F();
    }

    static async void F()
    {
        await new A();
    }

    public static Awaiter GetAwaiter(this object a) { throw new Exception(); }
}

public static class EE
{
    public static void GetAwaiter(this object a) { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (24,9): error CS0121: The call is ambiguous between the following methods or properties: 'Test.GetAwaiter(object)' and 'EE.GetAwaiter(object)'
                //         await new A();
                Diagnostic(ErrorCode.ERR_AmbigCall, "await new A()").WithArguments("Test.GetAwaiter(object)", "EE.GetAwaiter(object)")
                );
        }

        [Fact]
        public void ExtensionSiblingBadLookupOnEquallyDerived()
        {
            var source = @"
using System;

public class A { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

static class Test
{
    static void Main()
    {
        F();
    }

    static async void F()
    {
        await new A();
    }

    public static void GetAwaiter(this object a) { throw new Exception(); }
}

public static class EE
{
    public static Awaiter GetAwaiter(this object a) { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (24,9): error CS0121: The call is ambiguous between the following methods or properties: 'Test.GetAwaiter(object)' and 'EE.GetAwaiter(object)'
                //         await new A();
                Diagnostic(ErrorCode.ERR_AmbigCall, "await new A()").WithArguments("Test.GetAwaiter(object)", "EE.GetAwaiter(object)")
                );
        }

        [Fact]
        public void ExtensionParentNamespaceLookupOnOnReturnTypeMismatch()
        {
            var source = @"
using System;

public class A { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

namespace parent
{
    namespace child
    {
        static class Test
        {
            static async void F()
            {
                await new A();
            }

            public static void GetAwaiter(this A a) { throw new Exception(); }
        }
    }

    public static class E
    {
        public static Awaiter GetAwaiter(this A a) { throw new Exception(); }
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (24,17): error CS1986: 'await' requires that the type A have a suitable GetAwaiter method
                //                 await new A();
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "await new A()").WithArguments("A"));
        }

        [Fact]
        public void ExtensionParentNamespaceLookupOnOnInapplicableCandidate()
        {
            var source = @"
using System;

public class A { }

public class B { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

namespace parent
{
    namespace child
    {
        static class Test
        {
            static async void F()
            {
                await new A();
            }

            public static void GetAwaiter(this B a) { throw new Exception(); }
        }
    }

    public static class E
    {
        public static Awaiter GetAwaiter(this A a) { throw new Exception(); }
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [Fact]
        public void ExtensionParentNamespaceLookupOnOptional()
        {
            var source = @"
using System;

public class A { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

namespace parent
{
    namespace child
    {
        static class Test
        {
            static void Main()
            {
                F();
            }

            static async void F()
            {
                await new A();
            }

            public static Awaiter GetAwaiter(this A a, object o = null) { throw new Exception(); }
        }
    }

    public static class EE
    {
        public static Awaiter GetAwaiter(this A a) { throw new Exception(); }
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (11,9): error CS1986: 'await' requires that the type A have a suitable GetAwaiter method
                //         await new A();
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "await new A()").WithArguments("A"));
        }

        [Fact]
        public void ExtensionParentNamespaceLookupOnLessDerived()
        {
            var source = @"
using System;

public class A { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

namespace parent
{
    namespace child
    {
        static class Test
        {
            static void Main()
            {
                F();
            }

            static async void F()
            {
                await new A();
            }

            public static void GetAwaiter(this object a) { throw new Exception(); }
        }
    }

    public static class EE
    {
        public static Awaiter GetAwaiter(this A a) { throw new Exception(); }
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (11,9): error CS1986: 'await' requires that the type A have a suitable GetAwaiter method
                //         await new A();
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "await new A()").WithArguments("A"));
        }

        [Fact]
        public void ExtensionParentNamespaceDuellingLookupBad()
        {
            var source = @"
using System;

public class A { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

namespace child
{

    public static class Test
    {

        static async void F()
        {
            await new A();
        }

        static void Main()
        {
            F();
        }
    }
}

public static class E1
{
    public static void GetAwaiter(this A a) { throw new Exception(); }
}

public static class E2
{
    public static Awaiter GetAwaiter(this A a) { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (23,13): error CS0121: The call is ambiguous between the following methods or properties: 'E1.GetAwaiter(A)' and 'E2.GetAwaiter(A)'
                //             await new A();
                Diagnostic(ErrorCode.ERR_AmbigCall, "await new A()").WithArguments("E1.GetAwaiter(A)", "E2.GetAwaiter(A)")
                );
        }

        [Fact]
        public void ExtensionParentNamespaceDuellingLookupWasGoodNowBad()
        {
            var source = @"
using System;

public class A { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

namespace child
{

    public static class Test
    {

        static async void F()
        {
            await new A();
        }

        static void Main()
        {
            F();
        }
    }
}

public static class E1
{
    public static Awaiter GetAwaiter(this A a) { throw new Exception(); }
}

public static class E2
{
    public static void GetAwaiter(this A a) { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (23,13): error CS0121: The call is ambiguous between the following methods or properties: 'E1.GetAwaiter(A)' and 'E2.GetAwaiter(A)'
                //             await new A();
                Diagnostic(ErrorCode.ERR_AmbigCall, "await new A()").WithArguments("E1.GetAwaiter(A)", "E2.GetAwaiter(A)")
                );
        }

        [Fact]
        public void ExtensionParentNamespaceSingleClassDuel()
        {
            var source = @"
using System;

public interface I1 { }

public interface I2 { }

public class A : I1, I2 { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

namespace parent
{

    namespace child
    {

        public static class Test
        {

            static async void F()
            {
                await new A();
            }

            static void Main()
            {
                F();
            }
        }

    }

}

public static class E2
{
    public static void GetAwaiter(this I1 a) { throw new Exception(); }

    public static Awaiter GetAwaiter(this A a) { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [Fact]
        public void TruncateExtensionMethodLookupAfterFirstNamespace()
        {
            var source = @"
using System;

public interface I1 { }

public interface I2 { }

public class A : I1, I2 { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

namespace parent
{

    namespace child
    {

        public static class Test
        {
            public static void Main()
            {
                F();
            }

            static async void F()
            {
                await new A();
            }

            public static void GetAwaiter(this I1 a) { throw new Exception(); }
        }

        public static class E
        {
            public static Awaiter GetAwaiter(this A a) { throw new Exception(); }
        }
    }
}

public static class E2
{
    public static void GetAwaiter(this I1 a) { throw new Exception(); }

    public static Awaiter GetAwaiter(this A a) { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }


        [Fact]
        public void BadTruncateExtensionMethodLookupAfterFirstNamespace()
        {
            var source = @"
using System;

public interface I1 { }

public interface I2 { }

public class A : I1, I2 { }

public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(System.Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

namespace parent
{

    namespace child
    {

        public static class Test
        {
            public static void Main()
            {
                F();
            }

            static async void F()
            {
                await new A();
            }

            public static void GetAwaiter(this I1 a) { throw new Exception(); }
        }
    }

    public static class E
    {
        public static Awaiter GetAwaiter(this A a) { throw new Exception(); }
    }
}

public static class E2
{
    public static void GetAwaiter(this I1 a) { throw new Exception(); }

    public static Awaiter GetAwaiter(this A a) { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (11,9): error CS1986: 'await' requires that the type A have a suitable GetAwaiter method
                //         await new A();
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "await new A()").WithArguments("A"));
        }

        [Fact]
        public void FallbackToGetAwaiterExtensionMethod()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    private Awaiter GetAwaiter() { return null; }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

static class MyExtensions
{
    public static Awaiter GetAwaiter(this A a)
    {
        return new Awaiter();
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
        }

        [Fact]
        public void BadFallbackToGetAwaiterExtensionMethodInPresenceOfInstanceGetAwaiterMethodWithOptionalParameter()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public Awaiter GetAwaiter(object o = null) { return null; }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

static class MyExtensions
{
    public static Awaiter GetAwaiter(this A a)
    {
        return new Awaiter();
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS1986: 'await' requires that the type A have a suitable GetAwaiter method
                //         await new A();
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "await new A()").WithArguments("A"));
        }

        [Fact]
        public void GetAwaiterMethodWithNonZeroArity()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
        await new B();
        await new C();
        await new D();
        await new E();
    }
}

class A
{
    public Awaiter GetAwaiter(object o = null) { return null; }
}

class B
{
    public Awaiter GetAwaiter(object o) { return null; }
}

class C
{
}

class D
{
}

class E
{
    public Awaiter GetAwaiter() { return null; }
    public Awaiter GetAwaiter(object o = null) { return null; }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

static class MyExtensions
{
    public static Awaiter GetAwaiter(this C a, object o = null)
    {
        return new Awaiter();
    }

    public static Awaiter GetAwaiter(this D a, object o)
    {
        return new Awaiter();
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS1986: 'await' requires that the type A have a suitable GetAwaiter method
                //         await new A();
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "await new A()").WithArguments("A").WithLocation(10, 9),
                // (11,9): error CS7036: There is no argument given that corresponds to the required formal parameter 'o' of 'B.GetAwaiter(object)'
                //         await new B();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "await new B()").WithArguments("o", "B.GetAwaiter(object)").WithLocation(11, 9),
                // (12,9): error CS1986: 'await' requires that the type C have a suitable GetAwaiter method
                //         await new C();
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "await new C()").WithArguments("C").WithLocation(12, 9),
                // (13,15): error CS1929: 'D' does not contain a definition for 'GetAwaiter' and the best extension method overload 'MyExtensions.GetAwaiter(C, object)' requires a receiver of type 'C'
                //         await new D();
                Diagnostic(ErrorCode.ERR_BadInstanceArgType, "new D()").WithArguments("D", "GetAwaiter", "MyExtensions.GetAwaiter(C, object)", "C").WithLocation(13, 15));
        }

        [Fact]
        public void GetAwaiterMethodWithNonZeroTypeParameterArity()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
        await new B();
    }
}

class A
{
    public Awaiter GetAwaiter<T>() { return null; }
}

class B
{
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}


static class MyExtensions
{
    public static Awaiter GetAwaiter<T>(this B a)
    {
        return null;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS0411: The type arguments for method 'A.GetAwaiter<T>()' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         await new A();
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "await new A()").WithArguments("A.GetAwaiter<T>()"),
                // (11,9): error CS0411: The type arguments for method 'MyExtensions.GetAwaiter<T>(B)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         await new B();
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "await new B()").WithArguments("MyExtensions.GetAwaiter<T>(B)")
                );
        }

        [Fact]
        public void AwaiterImplementsINotifyCompletion()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
        await new B();
        await new C();
        await new D();
    }
}

class A
{
    public Awaiter1 GetAwaiter() { return null; }
}

class B
{
    public Awaiter2 GetAwaiter() { return null; }
}

class C
{
    public Awaiter3 GetAwaiter() { return null; }
}

class D
{
    public Awaiter4 GetAwaiter() { return null; }
}

class Awaiter1 : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

class OnCompletedImpl : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }
}

class Awaiter2 : OnCompletedImpl
{
    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

interface OnCompletedInterface : System.Runtime.CompilerServices.INotifyCompletion
{
}

class Awaiter3 : OnCompletedInterface
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

class Awaiter4
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (13,9): error CS4027: 'Awaiter4' does not implement 'System.Runtime.CompilerServices.INotifyCompletion'
                //         await new D();
                Diagnostic(ErrorCode.ERR_DoesntImplementAwaitInterface, "await new D()").WithArguments("Awaiter4", "System.Runtime.CompilerServices.INotifyCompletion"));
        }

        [WorkItem(770448, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/770448")]
        [Fact]
        public void AwaiterImplementsINotifyCompletion_Constraint()
        {
            var source =
@"using System.Runtime.CompilerServices;
class Awaitable<T>
{
    internal T GetAwaiter() { return default(T); }
}
interface IA
{
    bool IsCompleted { get; }
    object GetResult();
}
interface IB : IA, INotifyCompletion
{
}
class A
{
    public void OnCompleted(System.Action a) { }
    internal bool IsCompleted { get { return true; } }
    internal object GetResult() { return null; }
}
class B : A, INotifyCompletion
{
}
class C
{
    static async void F<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
        where T1 : IA
        where T2 : IA, INotifyCompletion
        where T3 : IB
        where T4 : T1, INotifyCompletion
        where T5 : T3
        where T6 : A
        where T7 : A, INotifyCompletion
        where T8 : B
        where T9 : T6, INotifyCompletion
        where T10 : T8
    {
        await new Awaitable<T1>();
        await new Awaitable<T2>();
        await new Awaitable<T3>();
        await new Awaitable<T4>();
        await new Awaitable<T5>();
        await new Awaitable<T6>();
        await new Awaitable<T7>();
        await new Awaitable<T8>();
        await new Awaitable<T9>();
        await new Awaitable<T10>();
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (37,9): error CS4027: 'T1' does not implement 'System.Runtime.CompilerServices.INotifyCompletion'
                //         await new Awaitable<T1>();
                Diagnostic(ErrorCode.ERR_DoesntImplementAwaitInterface, "await new Awaitable<T1>()").WithArguments("T1", "System.Runtime.CompilerServices.INotifyCompletion").WithLocation(37, 9),
                // (42,9): error CS4027: 'T6' does not implement 'System.Runtime.CompilerServices.INotifyCompletion'
                //         await new Awaitable<T6>();
                Diagnostic(ErrorCode.ERR_DoesntImplementAwaitInterface, "await new Awaitable<T6>()").WithArguments("T6", "System.Runtime.CompilerServices.INotifyCompletion").WithLocation(42, 9));
        }

        [WorkItem(770448, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/770448")]
        [Fact]
        public void AwaiterImplementsINotifyCompletion_InheritedConstraint()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
class Awaitable<T>
{
    internal T GetAwaiter() { return default(T); }
}
interface IA
{
    bool IsCompleted { get; }
    object GetResult();
}
interface IB : IA, INotifyCompletion
{
}
class B : IA, INotifyCompletion
{
    public void OnCompleted(Action a) { }
    public bool IsCompleted { get { return true; } }
    public object GetResult() { return null; }
}
struct S : IA, INotifyCompletion
{
    public void OnCompleted(Action a) { }
    public bool IsCompleted { get { return true; } }
    public object GetResult() { return null; }
}
class C<T> where T : IA
{
    internal virtual async void F<U>() where U : T
    {
        await new Awaitable<U>();
    }
}
class D1 : C<IB>
{
    internal override async void F<T1>()
    {
        await new Awaitable<T1>();
    }
}
class D2 : C<B>
{
    internal override async void F<T2>()
    {
        await new Awaitable<T2>();
    }
}
class D3 : C<S>
{
    internal override async void F<T3>()
    {
        await new Awaitable<T3>();
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (31,9): error CS4027: 'U' does not implement 'System.Runtime.CompilerServices.INotifyCompletion'
                //         await new Awaitable<U>();
                Diagnostic(ErrorCode.ERR_DoesntImplementAwaitInterface, "await new Awaitable<U>()").WithArguments("U", "System.Runtime.CompilerServices.INotifyCompletion").WithLocation(31, 9),
                // (52,9): error CS0117: 'T3' does not contain a definition for 'IsCompleted'
                //         await new Awaitable<T3>();
                Diagnostic(ErrorCode.ERR_NoSuchMember, "await new Awaitable<T3>()").WithArguments("T3", "IsCompleted").WithLocation(52, 9));
        }

        [WorkItem(770448, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/770448")]
        [Fact]
        public void AwaiterImplementsINotifyCompletion_UserDefinedConversion()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
class Awaitable<T>
{
    internal T GetAwaiter() { return default(T); }
}
interface IA : INotifyCompletion
{
    bool IsCompleted { get; }
    object GetResult();
}
class A : INotifyCompletion
{
    public void OnCompleted(Action a) { }
    public bool IsCompleted { get { return true; } }
    public object GetResult() { return null; }
}
class B
{
    public void OnCompleted(Action a) { }
    public bool IsCompleted { get { return true; } }
    public static implicit operator A(B b) { return default(A); }
}
class B<T> where T : INotifyCompletion
{
    public void OnCompleted(Action a) { }
    public bool IsCompleted { get { return true; } }
    public static implicit operator T(B<T> b) { return default(T); }
}
class C
{
    async void F()
    {
        await new Awaitable<B>();
        await new Awaitable<B<IA>>();
        await new Awaitable<B<A>>();
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (34,9): error CS4027: 'B' does not implement 'System.Runtime.CompilerServices.INotifyCompletion'
                //         await new Awaitable<B>();
                Diagnostic(ErrorCode.ERR_DoesntImplementAwaitInterface, "await new Awaitable<B>()").WithArguments("B", "System.Runtime.CompilerServices.INotifyCompletion").WithLocation(34, 9),
                // (35,9): error CS4027: 'B<IA>' does not implement 'System.Runtime.CompilerServices.INotifyCompletion'
                //         await new Awaitable<B<IA>>();
                Diagnostic(ErrorCode.ERR_DoesntImplementAwaitInterface, "await new Awaitable<B<IA>>()").WithArguments("B<IA>", "System.Runtime.CompilerServices.INotifyCompletion").WithLocation(35, 9),
                // (36,9): error CS4027: 'B<A>' does not implement 'System.Runtime.CompilerServices.INotifyCompletion'
                //         await new Awaitable<B<A>>();
                Diagnostic(ErrorCode.ERR_DoesntImplementAwaitInterface, "await new Awaitable<B<A>>()").WithArguments("B<A>", "System.Runtime.CompilerServices.INotifyCompletion").WithLocation(36, 9));
        }

        /// <summary>
        /// Should call ICriticalNotifyCompletion.UnsafeOnCompleted
        /// if the awaiter type implements ICriticalNotifyCompletion.
        /// </summary>
        [Fact]
        public void AwaiterImplementsICriticalNotifyCompletion_Constraint()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
class Awaitable<T>
{
    internal T GetAwaiter() { return default(T); }
}
class A : INotifyCompletion
{
    public void OnCompleted(Action a) { }
    public bool IsCompleted { get { return true; } }
    public object GetResult() { return null; }
}
class B : A, ICriticalNotifyCompletion
{
    public void UnsafeOnCompleted(Action a) { }
}
class C
{
    static async void F<T1, T2, T3, T4, T5, T6>()
        where T1 : A
        where T2 : A, ICriticalNotifyCompletion
        where T3 : B
        where T4 : T1
        where T5 : T2
        where T6 : T1, ICriticalNotifyCompletion
    {
        await new Awaitable<T1>();
        await new Awaitable<T2>();
        await new Awaitable<T3>();
        await new Awaitable<T4>();
        await new Awaitable<T5>();
        await new Awaitable<T6>();
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
            var verifier = CompileAndVerify(compilation);
            var actualIL = verifier.VisualizeIL("C.<F>d__0<T1, T2, T3, T4, T5, T6>.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()");
            var calls = actualIL.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries).Where(s => s.Contains("OnCompleted")).ToArray();
            Assert.Equal(6, calls.Length);
            Assert.Equal("    IL_0056:  call       \"void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitOnCompleted<T1, C.<F>d__0<T1, T2, T3, T4, T5, T6>>(ref T1, ref C.<F>d__0<T1, T2, T3, T4, T5, T6>)\"", calls[0]);
            Assert.Equal("    IL_00b9:  call       \"void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<T2, C.<F>d__0<T1, T2, T3, T4, T5, T6>>(ref T2, ref C.<F>d__0<T1, T2, T3, T4, T5, T6>)\"", calls[1]);
            Assert.Equal("    IL_011c:  call       \"void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<T3, C.<F>d__0<T1, T2, T3, T4, T5, T6>>(ref T3, ref C.<F>d__0<T1, T2, T3, T4, T5, T6>)\"", calls[2]);
            Assert.Equal("    IL_0182:  call       \"void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitOnCompleted<T4, C.<F>d__0<T1, T2, T3, T4, T5, T6>>(ref T4, ref C.<F>d__0<T1, T2, T3, T4, T5, T6>)\"", calls[3]);
            Assert.Equal("    IL_01ea:  call       \"void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<T5, C.<F>d__0<T1, T2, T3, T4, T5, T6>>(ref T5, ref C.<F>d__0<T1, T2, T3, T4, T5, T6>)\"", calls[4]);
            Assert.Equal("    IL_0252:  call       \"void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.AwaitUnsafeOnCompleted<T6, C.<F>d__0<T1, T2, T3, T4, T5, T6>>(ref T6, ref C.<F>d__0<T1, T2, T3, T4, T5, T6>)\"", calls[5]);
        }

        [Fact]
        public void ConditionalOnCompletedImplementation()
        {
            var source = @"
using System;
using System.Diagnostics;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
        await new B();
    }
}

class A
{
    public Awaiter GetAwaiter() { return null; }
}

class B
{
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    [Conditional(""Condition"")]
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}

static class MyExtensions
{
    public static Awaiter GetAwaiter(this B a)
    {
        return null;
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (28,17): error CS0629: Conditional member 'Awaiter.OnCompleted(System.Action)' cannot implement interface member 'System.Runtime.CompilerServices.INotifyCompletion.OnCompleted(System.Action)' in type 'Awaiter'
                //     public void OnCompleted(Action x) { }
                Diagnostic(ErrorCode.ERR_InterfaceImplementedByConditional, "OnCompleted").WithArguments("Awaiter.OnCompleted(System.Action)", "System.Runtime.CompilerServices.INotifyCompletion.OnCompleted(System.Action)", "Awaiter"));
        }

        [Fact]
        public void MissingIsCompletedProperty()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public Awaiter GetAwaiter() { return null; }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS0117: 'Awaiter' does not contain a definition for 'IsCompleted'
                //         await new A();
                Diagnostic(ErrorCode.ERR_NoSuchMember, "await new A()").WithArguments("Awaiter", "IsCompleted"));
        }

        [Fact]
        public void InaccessibleIsCompletedProperty()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public Awaiter GetAwaiter() { return null; }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    bool IsCompleted { get { return false; } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS0117: 'Awaiter' does not contain a definition for 'IsCompleted'
                //         await new A();
                Diagnostic(ErrorCode.ERR_NoSuchMember, "await new A()").WithArguments("Awaiter", "IsCompleted"));
        }

        [Fact]
        public void StaticIsCompletedProperty()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public Awaiter GetAwaiter() { return null; }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public static bool IsCompleted { get { return false; } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS0176: Member 'Awaiter.IsCompleted' cannot be accessed with an instance reference; qualify it with a type name instead
                //         await new A();
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "await new A()").WithArguments("Awaiter.IsCompleted")
                );
        }

        [Fact]
        public void StaticWriteonlyIsCompletedProperty()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public Awaiter GetAwaiter() { return null; }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public static bool IsCompleted { set { } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS0176: Member 'Awaiter.IsCompleted' cannot be accessed with an instance reference; qualify it with a type name instead
                //         await new A();
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "await new A()").WithArguments("Awaiter.IsCompleted")
                );
        }

        [Fact]
        public void StaticAccessorlessIsCompletedProperty()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public Awaiter GetAwaiter() { return null; }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public static bool IsCompleted { }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (25,24): error CS0548: 'Awaiter.IsCompleted': property or indexer must have at least one accessor
                //     public static bool IsCompleted { }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "IsCompleted").WithArguments("Awaiter.IsCompleted"),
                // (10,9): error CS0176: Member 'Awaiter.IsCompleted' cannot be accessed with an instance reference; qualify it with a type name instead
                //         await new A();
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "await new A()").WithArguments("Awaiter.IsCompleted")
                );
        }

        [Fact]
        public void NonBooleanIsCompletedProperty()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public Awaiter GetAwaiter() { return null; }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public int IsCompleted { get { return -1; } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS4011: 'await' requires that the return type 'Awaiter' of 'A.GetAwaiter()' have suitable IsCompleted, OnCompleted, and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion
                //         await new A();
                Diagnostic(ErrorCode.ERR_BadAwaiterPattern, "await new A()").WithArguments("Awaiter", "A"));
        }

        [Fact]
        public void WriteonlyIsCompletedProperty()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public Awaiter GetAwaiter() { return null; }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { set { } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS0117: 'A' does not contain a definition for 'IsCompleted'
                //         await new A();
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "await new A()").WithArguments("Awaiter.IsCompleted"));
        }

        [Fact]
        public void WriteonlyNonBooleanIsCompletedProperty()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public Awaiter GetAwaiter() { return null; }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult() { throw new Exception(); }

    public int IsCompleted { set { } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS0117: 'A' does not contain a definition for 'IsCompleted'
                //         await new A();
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "await new A()").WithArguments("Awaiter.IsCompleted"));
        }

        [Fact]
        public void MissingGetResultInstanceMethod()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public Awaiter GetAwaiter() { return null; }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool IsCompleted { get { return false; } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS0117: 'Awaiter' does not contain a definition for 'GetResult'
                //         await new A();
                Diagnostic(ErrorCode.ERR_NoSuchMember, "await new A()").WithArguments("Awaiter", "GetResult"));
        }

        [Fact]
        public void InaccessibleGetResultInstanceMethod()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public Awaiter GetAwaiter() { return null; }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    private bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return false; } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS0122: 'Awaiter.GetResult()' is inaccessible due to its protection level
                //         await new A();
                Diagnostic(ErrorCode.ERR_BadAccess, "await new A()").WithArguments("Awaiter.GetResult()")
                );
        }

        [Fact]
        public void StaticResultMethod()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public Awaiter GetAwaiter() { return null; }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public static bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return false; } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS0176: Member 'Awaiter.GetResult()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         await new A();
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "await new A()").WithArguments("Awaiter.GetResult()"));
        }

        [Fact]
        public void GetResultExtensionMethod()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public Awaiter GetAwaiter() { return null; }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool IsCompleted { get { return false; } }
}

static class MyExtensions
{
    public static bool GetResult(this Awaiter a)
    {
        throw new Exception();
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS0117: 'Awaiter' does not contain a definition for 'GetResult'
                //         await new A();
                Diagnostic(ErrorCode.ERR_NoSuchMember, "await new A()").WithArguments("Awaiter", "GetResult"));
        }

        [Fact]
        public void GetResultWithNonZeroArity()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public Awaiter GetAwaiter() { return null; }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult(object o = null) { throw new Exception(); }

    public bool IsCompleted { get { return false; } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS4011: 'await' requires that the return type 'Awaiter' of 'A.GetAwaiter()' have suitable IsCompleted, OnCompleted, and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion
                //         await new A();
                Diagnostic(ErrorCode.ERR_BadAwaiterPattern, "await new A()").WithArguments("Awaiter", "A"));
        }

        [Fact]
        public void GetResultWithNonZeroTypeParameterArity()
        {
            var source = @"
using System;

static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public Awaiter GetAwaiter() { return null; }
}

class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    public bool GetResult<T>() { throw new Exception(); }

    public bool IsCompleted { get { return false; } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS0411: The type arguments for method 'Awaiter.GetResult<T>()' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         await new A();
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "await new A()").WithArguments("Awaiter.GetResult<T>()")
                );
        }

        [Fact]
        public void ConditionalGetResult()
        {
            var source = @"
using System;
using System.Diagnostics;

static class Program
{
    static async void f()
    {
        await new A();
    }
}
class A
{
    public Awaiter GetAwaiter() { return new Awaiter(); }
}
class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
{
    public void OnCompleted(Action x) { }

    [Conditional(""X"")]
    public void GetResult() { Console.WriteLine(""unconditional""); }

    public bool IsCompleted { get { return true; } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (15,9): error CS4011: 'await' requires that the return type 'Awaiter' of 'A.GetAwaiter()' have suitable IsCompleted, OnCompleted, and GetResult members, and implement INotifyCompletion or ICriticalNotifyCompletion
                //         await new A();
                Diagnostic(ErrorCode.ERR_BadAwaiterPattern, "await new A()").WithArguments("Awaiter", "A"));
        }

        [Fact]
        public void Missing_IsCompleted_INotifyCompletion_GetResult()
        {
            var source = @"
static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public Awaiter GetAwaiter() { return null; }
}

class Awaiter //: System.Runtime.CompilerServices.INotifyCompletion
{
    //public void OnCompleted(Action x) { }

    //public bool GetResult() { throw new Exception(); }

    //public bool IsCompleted { get { return true; } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (8,9): error CS0117: 'Awaiter' does not contain a definition for 'IsCompleted'
                //         await new A();
                Diagnostic(ErrorCode.ERR_NoSuchMember, "await new A()").WithArguments("Awaiter", "IsCompleted"));
        }

        [Fact]
        public void Missing_INotifyCompletion_GetResult()
        {
            var source = @"
static class Program
{
    static void Main() { }

    static async void f()
    {
        await new A();
    }
}

class A
{
    public Awaiter GetAwaiter() { return null; }
}

class Awaiter //: System.Runtime.CompilerServices.INotifyCompletion
{
    //public void OnCompleted(Action x) { }

    //public bool GetResult() { throw new Exception(); }

    public bool IsCompleted { get { return true; } }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (8,9): error CS4027: 'Awaiter' does not implement 'System.Runtime.CompilerServices.INotifyCompletion'
                //         await new A();
                Diagnostic(ErrorCode.ERR_DoesntImplementAwaitInterface, "await new A()").WithArguments("Awaiter", "System.Runtime.CompilerServices.INotifyCompletion"));
        }

        [Fact]
        public void BadAwaitArg_NeedSystem()
        {
            var source = @"
// using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

class App {
    static void Main() {
        EnumDevices().Wait();
    }

    private static async Task EnumDevices() {
        await DeviceInformation.FindAllAsync();
        return;
    }
}";
            CreateCompilationWithWinRT(source).VerifyDiagnostics(
                // (12,9): error CS4035: 'Windows.Foundation.IAsyncOperation<Windows.Devices.Enumeration.DeviceInformationCollection>' does not contain a definition for 'GetAwaiter' and no extension method 'GetAwaiter' accepting a first argument of type 'Windows.Foundation.IAsyncOperation<Windows.Devices.Enumeration.DeviceInformationCollection>' could be found (are you missing a using directive for 'System'?)
                //         await DeviceInformation.FindAllAsync();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtensionNeedUsing, "await DeviceInformation.FindAllAsync()").WithArguments("Windows.Foundation.IAsyncOperation<Windows.Devices.Enumeration.DeviceInformationCollection>", "GetAwaiter", "System")
                );
        }

        [Fact]
        public void ErrorInAwaitSubexpression()
        {
            var source = @"
class C
{
    async void M()
    {
        using (await goo())
        {
        }
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (6,22): error CS0103: The name 'goo' does not exist in the current context
                //         using (await goo())
                Diagnostic(ErrorCode.ERR_NameNotInContext, "goo").WithArguments("goo"));
        }

        [Fact]
        public void BadAwaitArgIntrinsic()
        {
            var source = @"
class Test
{
    public void goo() { }

    public async void awaitVoid()
    {
        await goo();
    }

    public async void awaitNull()
    {
        await null;
    }

    public async void awaitMethodGroup()
    {
        await goo;
    }

    public async void awaitLambda()
    {
        await (x => x);
    }

    public static void Main() { }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (8,9): error CS4008: Cannot await 'void'
                //         await goo();
                Diagnostic(ErrorCode.ERR_BadAwaitArgVoidCall, "await goo()"),
                // (13,9): error CS4001: Cannot await '<null>;'
                //         await null;
                Diagnostic(ErrorCode.ERR_BadAwaitArgIntrinsic, "await null").WithArguments("<null>"),
                // (18,9): error CS4001: Cannot await 'method group'
                //         await goo;
                Diagnostic(ErrorCode.ERR_BadAwaitArgIntrinsic, "await goo").WithArguments("method group"),
                // (23,9): error CS4001: Cannot await 'lambda expression'
                //         await (x => x);
                Diagnostic(ErrorCode.ERR_BadAwaitArgIntrinsic, "await (x => x)").WithArguments("lambda expression"));
        }

        [Fact]
        public void BadAwaitArgVoidCall()
        {
            var source = @"
using System.Threading.Tasks;

class Test
{
    public async void goo()
    {
        await Task.Factory.StartNew(() => { });
    }

    public async void bar()
    {
        await goo();
    }

    public static void Main() { }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (10,9): error CS4008: Cannot await 'void'
                //         await goo();
                Diagnostic(ErrorCode.ERR_BadAwaitArgVoidCall, "await goo()"));
        }

        [Fact, WorkItem(531356, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531356")]
        public void Repro_17997()
        {
            var source = @"
class C
{
    public IVsTask ResolveReferenceAsync()
    {
        return this.VsTasksService.InvokeAsync(async delegate
        {
            return null;
        });
    }
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (4,12): error CS0246: The type or namespace name 'IVsTask' could not be found (are you missing a using directive or an assembly reference?)
                //     public IVsTask ResolveReferenceAsync()
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "IVsTask").WithArguments("IVsTask").WithLocation(4, 12),
                // (6,21): error CS1061: 'C' does not contain a definition for 'VsTasksService' and no extension method 'VsTasksService' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //         return this.VsTasksService.InvokeAsync(async delegate
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "VsTasksService").WithArguments("C", "VsTasksService").WithLocation(6, 21),
                // (6,54): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         return this.VsTasksService.InvokeAsync(async delegate
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "delegate").WithLocation(6, 54));
        }

        [Fact, WorkItem(627123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627123")]
        public void Repro_627123()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;
 
interface IA : INotifyCompletion
{
    bool IsCompleted { get; }
    void GetResult();
}
 
interface IB : IA
{
    new Action GetResult { get; }
}
 
interface IC
{
    IB GetAwaiter();
}
 
class D
{
    Action<IC> a = async x => await x;
}";
            CreateCompilationWithMscorlib45(source).VerifyDiagnostics(
                // (23,31): error CS0118: 'GetResult' is a property but is used like a method
                //     Action<IC> a = async x => await x;
                Diagnostic(ErrorCode.ERR_BadSKknown, "await x").WithArguments("GetResult", "property", "method")
                );
        }

        [Fact, WorkItem(1091911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1091911")]
        public void Repro_1091911()
        {
            const string source = @"
using System;
using System.Threading.Tasks;
 
class Repro
{
    int Boom { get { return 42; } }

    static Task<dynamic> Compute()
    {
        return Task.FromResult<dynamic>(new Repro());
    }
 
    static async Task<int> Bug()
    {
        dynamic results = await Compute().ConfigureAwait(false);
        var x = results.Boom;
        return (int)x;
    }

    static void Main()
    {
        Console.WriteLine(Bug().Result);
    }
}";

            var comp = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef, CSharpRef }, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [Fact]
        public void DynamicResultTypeCustomAwaiter()
        {
            const string source = @"
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public struct MyTask
{
    public readonly Task task;
    private readonly Func<dynamic> getResult;

    public MyTask(Task task, Func<dynamic> getResult)
    {
        this.task = task;
        this.getResult = getResult;
    }

    public dynamic Result { get { return this.getResult(); } }
}

public struct MyAwaiter : INotifyCompletion
{
    private readonly MyTask task;

    public MyAwaiter(MyTask task)
    {
        this.task = task;
    }

    public bool IsCompleted { get { return true; } }
    public dynamic GetResult() { Console.Write(""dynamic""); return task.Result; }
    public void OnCompleted(System.Action continuation) { task.task.ContinueWith(_ => continuation()); }
}

public static class TaskAwaiter
{
    public static MyAwaiter GetAwaiter(this MyTask task)
    {
        return new MyAwaiter(task);
    }
}

class Repro
{
    int Boom { get { return 42; } }

    static MyTask Compute()
    {
        var task = Task.FromResult(new Repro());
        return new MyTask(task, () => task.Result);
    }
 
    static async Task<int> Bug()
    {
        return (await Compute()).Boom;
    }

    static void Main()
    {
        Console.WriteLine(Bug().Result);
    }
}
";

            var comp = CreateCompilationWithMscorlib45(source, new[] { TestMetadata.Net40.SystemCore, TestMetadata.Net40.MicrosoftCSharp }, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // warning CS1685: The predefined type 'ExtensionAttribute' is defined in multiple assemblies in the global alias; using definition from 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
                Diagnostic(ErrorCode.WRN_MultiplePredefTypes).WithArguments("System.Runtime.CompilerServices.ExtensionAttribute", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089").WithLocation(1, 1));

            // PEVerify: Cannot change initonly field outside its .ctor.
            var compiled = CompileAndVerify(comp, expectedOutput: "dynamic42", verify: Verification.FailsPEVerify);

            compiled.VerifyIL("MyAwaiter.OnCompleted(System.Action)", @"
{
  // Code size       43 (0x2b)
  .maxstack  3
  .locals init (MyAwaiter.<>c__DisplayClass5_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""MyAwaiter.<>c__DisplayClass5_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.1
  IL_0008:  stfld      ""System.Action MyAwaiter.<>c__DisplayClass5_0.continuation""
  IL_000d:  ldarg.0
  IL_000e:  ldflda     ""MyTask MyAwaiter.task""
  IL_0013:  ldfld      ""System.Threading.Tasks.Task MyTask.task""
  IL_0018:  ldloc.0
  IL_0019:  ldftn      ""void MyAwaiter.<>c__DisplayClass5_0.<OnCompleted>b__0(System.Threading.Tasks.Task)""
  IL_001f:  newobj     ""System.Action<System.Threading.Tasks.Task>..ctor(object, System.IntPtr)""
  IL_0024:  callvirt   ""System.Threading.Tasks.Task System.Threading.Tasks.Task.ContinueWith(System.Action<System.Threading.Tasks.Task>)""
  IL_0029:  pop
  IL_002a:  ret
}");
        }
    }
}
