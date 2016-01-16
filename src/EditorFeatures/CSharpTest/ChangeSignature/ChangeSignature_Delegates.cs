// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        protected override object CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new ChangeSignatureCodeRefactoringProvider();
        }

        protected override string GetLanguage()
        {
            return LanguageNames.CSharp;
        }

        protected override Task<TestWorkspace> CreateWorkspaceFromFileAsync(string definition, ParseOptions parseOptions, CompilationOptions compilationOptions)
        {
            return TestWorkspace.CreateCSharpAsync(definition, (CSharpParseOptions)parseOptions, (CSharpCompilationOptions)compilationOptions);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_ImplicitInvokeCalls()
        {
            var markup = @"
delegate void $$MyDelegate(int x, string y, bool z);

class C
{
    void M()
    {
        MyDelegate d1 = null;
        d1(1, ""Two"", true);
    }
}";
            var updatedSignature = new[] { 2, 1 };
            var expectedUpdatedCode = @"
delegate void MyDelegate(bool z, string y);

class C
{
    void M()
    {
        MyDelegate d1 = null;
        d1(true, ""Two"");
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_ExplicitInvokeCalls()
        {
            var markup = @"
delegate void $$MyDelegate(int x, string y, bool z);

class C
{
    void M()
    {
        MyDelegate d1 = null;
        d1.Invoke(1, ""Two"", true);
    }
}";
            var updatedSignature = new[] { 2, 1 };
            var expectedUpdatedCode = @"
delegate void MyDelegate(bool z, string y);

class C
{
    void M()
    {
        MyDelegate d1 = null;
        d1.Invoke(true, ""Two"");
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_BeginInvokeCalls()
        {
            var markup = @"
delegate void $$MyDelegate(int x, string y, bool z);

class C
{
    void M()
    {
        MyDelegate d1 = null;
        d1.BeginInvoke(1, ""Two"", true, null, null);
    }
}";
            var updatedSignature = new[] { 2, 1 };
            var expectedUpdatedCode = @"
delegate void MyDelegate(bool z, string y);

class C
{
    void M()
    {
        MyDelegate d1 = null;
        d1.BeginInvoke(true, ""Two"", null, null);
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_AnonymousMethods()
        {
            var markup = @"
delegate void $$MyDelegate(int x, string y, bool z);

class C
{
    void M()
    {
        MyDelegate d1 = null;
        d1 = delegate (int e, string f, bool g) { var x = f.Length + (g ? 0 : 1); };
        d1 = delegate { };
    }
}";
            var updatedSignature = new[] { 2, 1 };
            var expectedUpdatedCode = @"
delegate void MyDelegate(bool z, string y);

class C
{
    void M()
    {
        MyDelegate d1 = null;
        d1 = delegate (bool g, string f) { var x = f.Length + (g ? 0 : 1); };
        d1 = delegate { };
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_Lambdas()
        {
            var markup = @"
delegate void $$MyDelegate(int x, string y, bool z);

class C
{
    void M()
    {
        MyDelegate d1 = null;
        d1 = (r, s, t) => { var x = s.Length + (t ? 0 : 1); };
    }
}";
            var updatedSignature = new[] { 2, 1 };
            var expectedUpdatedCode = @"
delegate void MyDelegate(bool z, string y);

class C
{
    void M()
    {
        MyDelegate d1 = null;
        d1 = (t, s) => { var x = s.Length + (t ? 0 : 1); };
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_Lambdas_RemovingOnlyParameterIntroducesParentheses()
        {
            var markup = @"
delegate void $$MyDelegate(int x);

class C
{
    void M()
    {
        MyDelegate d1 = null;
        d1 = (r) => { System.Console.WriteLine(""Test""); };
        d1 = r => { System.Console.WriteLine(""Test""); };
        d1 =r=>{ System.Console.WriteLine(""Test""); };
    }
}";
            var updatedSignature = Array.Empty<int>();
            var expectedUpdatedCode = @"
delegate void MyDelegate();

class C
{
    void M()
    {
        MyDelegate d1 = null;
        d1 = () => { System.Console.WriteLine(""Test""); };
        d1 = () => { System.Console.WriteLine(""Test""); };
        d1 =()=>{ System.Console.WriteLine(""Test""); };
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_CascadeThroughMethodGroups_AssignedToVariable()
        {
            var markup = @"
delegate void $$MyDelegate(int x, string y, bool z);

class C
{
    void M()
    {
        MyDelegate d1 = null;
        d1 = Foo;
        Foo(1, ""Two"", true);
        Foo(1, false, false);
    }

    void Foo(int a, string b, bool c) { }
    void Foo(int a, object b, bool c) { }
}";
            var updatedSignature = new[] { 2, 1 };
            var expectedUpdatedCode = @"
delegate void MyDelegate(bool z, string y);

class C
{
    void M()
    {
        MyDelegate d1 = null;
        d1 = Foo;
        Foo(true, ""Two"");
        Foo(1, false, false);
    }

    void Foo(bool c, string b) { }
    void Foo(int a, object b, bool c) { }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_CascadeThroughMethodGroups_DelegateConstructor()
        {
            var markup = @"
delegate void $$MyDelegate(int x, string y, bool z);

class C
{
    void M()
    {
        MyDelegate d1 = new MyDelegate(Foo);
        Foo(1, ""Two"", true);
        Foo(1, false, false);
    }

    void Foo(int a, string b, bool c) { }
    void Foo(int a, object b, bool c) { }
}";
            var updatedSignature = new[] { 2, 1 };
            var expectedUpdatedCode = @"
delegate void MyDelegate(bool z, string y);

class C
{
    void M()
    {
        MyDelegate d1 = new MyDelegate(Foo);
        Foo(true, ""Two"");
        Foo(1, false, false);
    }

    void Foo(bool c, string b) { }
    void Foo(int a, object b, bool c) { }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_CascadeThroughMethodGroups_PassedAsArgument()
        {
            var markup = @"
delegate void $$MyDelegate(int x, string y, bool z);

class C
{
    void M()
    {
        Target(Foo);
        Foo(1, ""Two"", true);
        Foo(1, false, false);
    }

    void Target(MyDelegate d) { }

    void Foo(int a, string b, bool c) { }
    void Foo(int a, object b, bool c) { }
}";
            var updatedSignature = new[] { 2, 1 };
            var expectedUpdatedCode = @"
delegate void MyDelegate(bool z, string y);

class C
{
    void M()
    {
        Target(Foo);
        Foo(true, ""Two"");
        Foo(1, false, false);
    }

    void Target(MyDelegate d) { }

    void Foo(bool c, string b) { }
    void Foo(int a, object b, bool c) { }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_CascadeThroughMethodGroups_ReturnValue()
        {
            var markup = @"
delegate void $$MyDelegate(int x, string y, bool z);

class C
{
    void M()
    {
        MyDelegate d1 = Result();
        Foo(1, ""Two"", true);
    }

    private MyDelegate Result()
    {
        return Foo;
    }

    void Foo(int a, string b, bool c) { }
    void Foo(int a, object b, bool c) { }
}";
            var updatedSignature = new[] { 2, 1 };
            var expectedUpdatedCode = @"
delegate void MyDelegate(bool z, string y);

class C
{
    void M()
    {
        MyDelegate d1 = Result();
        Foo(true, ""Two"");
    }

    private MyDelegate Result()
    {
        return Foo;
    }

    void Foo(bool c, string b) { }
    void Foo(int a, object b, bool c) { }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_CascadeThroughMethodGroups_YieldReturnValue()
        {
            var markup = @"
using System.Collections.Generic;

delegate void $$MyDelegate(int x, string y, bool z);

class C
{
    void M()
    {
        Foo(1, ""Two"", true);
    }

    private IEnumerable<MyDelegate> Result()
    {
        yield return Foo;
    }

    void Foo(int a, string b, bool c) { }
    void Foo(int a, object b, bool c) { }
}";
            var updatedSignature = new[] { 2, 1 };
            var expectedUpdatedCode = @"
using System.Collections.Generic;

delegate void MyDelegate(bool z, string y);

class C
{
    void M()
    {
        Foo(true, ""Two"");
    }

    private IEnumerable<MyDelegate> Result()
    {
        yield return Foo;
    }

    void Foo(bool c, string b) { }
    void Foo(int a, object b, bool c) { }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_ReferencingLambdas_MethodArgument()
        {
            var markup = @"
delegate void $$MyDelegate(int x, string y, bool z);

class C
{
    void M6()
    {
        Target((m, n, o) => { var x = n.Length + (o ? 0 : 1); });
    }

    void Target(MyDelegate d) { }
}";
            var updatedSignature = new[] { 2, 1 };
            var expectedUpdatedCode = @"
delegate void MyDelegate(bool z, string y);

class C
{
    void M6()
    {
        Target((o, n) => { var x = n.Length + (o ? 0 : 1); });
    }

    void Target(MyDelegate d) { }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_ReferencingLambdas_YieldReturn()
        {
            var markup = @"
using System.Collections.Generic;

delegate void $$MyDelegate(int x, string y, bool z);
class C
{
    private IEnumerable<MyDelegate> Result3()
    {
        yield return (g, h, i) => { var x = h.Length + (i ? 0 : 1); };
    }
}";
            var updatedSignature = new[] { 2, 1 };
            var expectedUpdatedCode = @"
using System.Collections.Generic;

delegate void MyDelegate(bool z, string y);
class C
{
    private IEnumerable<MyDelegate> Result3()
    {
        yield return (i, h) => { var x = h.Length + (i ? 0 : 1); };
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_Recursive()
        {
            var markup = @"
delegate RecursiveDelegate $$RecursiveDelegate(int x, string y, bool z);

class C
{
    void M()
    {
        RecursiveDelegate rd = null;
        rd(1, ""Two"", true)(1, ""Two"", true)(1, ""Two"", true)(1, ""Two"", true)(1, ""Two"", true);
    }
}";
            var updatedSignature = new[] { 2, 1 };
            var expectedUpdatedCode = @"
delegate RecursiveDelegate RecursiveDelegate(bool z, string y);

class C
{
    void M()
    {
        RecursiveDelegate rd = null;
        rd(true, ""Two"")(true, ""Two"")(true, ""Two"")(true, ""Two"")(true, ""Two"");
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_DocComments()
        {
            var markup = @"
/// <summary>
/// This is <see cref=""MyDelegate""/>, which has these methods:
///     <see cref=""MyDelegate.MyDelegate(object, IntPtr)""/>
///     <see cref=""MyDelegate.Invoke(int, string, bool)""/>
///     <see cref=""MyDelegate.EndInvoke(IAsyncResult)""/>
///     <see cref=""MyDelegate.BeginInvoke(int, string, bool, AsyncCallback, object)""/>
/// </summary>
/// <param name=""x"">x!</param>
/// <param name=""y"">y!</param>
/// <param name=""z"">z!</param>
delegate void $$MyDelegate(int x, string y, bool z);

class C
{
    void M()
    {
        MyDelegate d1 = Foo;
        Foo(1, ""Two"", true);
    }

    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    void Foo(int a, string b, bool c) { }
}";
            var updatedSignature = new[] { 2, 1 };
            var expectedUpdatedCode = @"
/// <summary>
/// This is <see cref=""MyDelegate""/>, which has these methods:
///     <see cref=""MyDelegate.MyDelegate(object, IntPtr)""/>
///     <see cref=""MyDelegate.Invoke( bool, string)""/>
///     <see cref=""MyDelegate.EndInvoke(IAsyncResult)""/>
///     <see cref=""MyDelegate.BeginInvoke(int, string, bool, AsyncCallback, object)""/>
/// </summary>
/// <param name=""z"">z!</param>
/// <param name=""y"">y!</param>
/// 
delegate void MyDelegate(bool z, string y);

class C
{
    void M()
    {
        MyDelegate d1 = Foo;
        Foo(true, ""Two"");
    }

    /// <param name=""c""></param>
    /// <param name=""b""></param>
    /// 
    void Foo(bool c, string b) { }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_CascadeThroughEventAdd()
        {
            var markup = @"
delegate void $$MyDelegate(int x, string y, bool z);

class Program
{
    void M()
    {
        MyEvent += Program_MyEvent;
    }

    event MyDelegate MyEvent;
    void Program_MyEvent(int a, string b, bool c) { }
}";
            var updatedSignature = new[] { 2, 1 };
            var expectedUpdatedCode = @"
delegate void MyDelegate(bool z, string y);

class Program
{
    void M()
    {
        MyEvent += Program_MyEvent;
    }

    event MyDelegate MyEvent;
    void Program_MyEvent(bool c, string b) { }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_Generics1()
        {
            var markup = @"
public class DP16a
{
    public delegate void D<T>($$T t);
    public event D<int> E1;
    public event D<int> E2;

    public void M1(int i) { }
    public void M2(int i) { }
    public void M3(int i) { }

    void B()
    {
        D<int> d = new D<int>(M1);
        E1 += new D<int>(M2);
        E2 -= new D<int>(M3);
    }
}";
            var updatedSignature = Array.Empty<int>();
            var expectedUpdatedCode = @"
public class DP16a
{
    public delegate void D<T>();
    public event D<int> E1;
    public event D<int> E2;

    public void M1() { }
    public void M2() { }
    public void M3() { }

    void B()
    {
        D<int> d = new D<int>(M1);
        E1 += new D<int>(M2);
        E2 -= new D<int>(M3);
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_Generics2()
        {
            var markup = @"
public class D17<T>
{
    public delegate void $$D(T t);
}
public class D17Test
{
    void Test() { var x = new D17<string>.D(M17); }
    internal void M17(string s) { }
}";
            var updatedSignature = Array.Empty<int>();
            var expectedUpdatedCode = @"
public class D17<T>
{
    public delegate void D();
}
public class D17Test
{
    void Test() { var x = new D17<string>.D(M17); }
    internal void M17() { }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_GenericParams()
        {
            var markup = @"
class DA
{
    void M(params int[] i) { }
    void B()
    {
        DP20<int>.D d = new DP20<int>.D(M);
        d();
        d(0);
        d(0, 1);
    }
}
public class DP20<T>
{
    public delegate void $$D(params T[] t);
    public void M1(params T[] t) { }

    void B()
    {
        D d = new D(M1);
    }
}";
            var updatedSignature = Array.Empty<int>();
            var expectedUpdatedCode = @"
class DA
{
    void M() { }
    void B()
    {
        DP20<int>.D d = new DP20<int>.D(M);
        d();
        d();
        d();
    }
}
public class DP20<T>
{
    public delegate void D();
    public void M1() { }

    void B()
    {
        D d = new D(M1);
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegates_Generic_RemoveArgumentAtReference()
        {
            var markup = @"public class CD<T>
{
    public delegate void D(T t);
}
class Test
{
    public void M()
    {
        var dele = new CD<int>.$$D((int x) => { });
    }
}";
            var updatedSignature = Array.Empty<int>();
            var expectedUpdatedCode = @"public class CD<T>
{
    public delegate void D();
}
class Test
{
    public void M()
    {
        var dele = new CD<int>.D(() => { });
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ChangeSignature_Delegate_Generics_RemoveStaticArgument()
        {
            var markup = @"
public class C2<T>
{
    public delegate void D(T t);
}

public class D2
{
    public static D2 Instance = null;
    void M(D2 m) { }

    void B()
    {
        C2<D2>.D d = new C2<D2>.D(M);
        $$d(D2.Instance);
    }
}";
            var updatedSignature = Array.Empty<int>();
            var expectedUpdatedCode = @"
public class C2<T>
{
    public delegate void D();
}

public class D2
{
    public static D2 Instance = null;
    void M() { }

    void B()
    {
        C2<D2>.D d = new C2<D2>.D(M);
        d();
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }
    }
}
