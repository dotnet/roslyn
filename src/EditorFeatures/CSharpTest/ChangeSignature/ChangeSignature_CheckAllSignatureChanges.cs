// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestAllSignatureChanges_1This_3Regular_2Default_1Params()
        {
            var markup = @"
static class Ext
{
    /// <summary>
    /// This is a summary of <see cref=""M(object, int, string, bool, int, string, int[])""/>
    /// </summary>
    /// <param name=""o""></param>
    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    /// <param name=""x""></param>
    /// <param name=""y""></param>
    /// <param name=""p""></param>
    static void $$M(this object o, int a, string b, bool c, int x = 0, string y = ""Zero"", params int[] p)
    {
        object t = new object();

        M(t, 1, ""two"", true, 3, ""four"", new[] { 5, 6 });
        M(t, 1, ""two"", true, 3, ""four"", 5, 6);
        t.M(1, ""two"", true, 3, ""four"", new[] { 5, 6 });
        t.M(1, ""two"", true, 3, ""four"", 5, 6);

        M(t, 1, ""two"", true, 3, ""four"");
        M(t, 1, ""two"", true, 3);
        M(t, 1, ""two"", true);

        M(t, 1, ""two"", c: true);
        M(t, 1, ""two"", true, 3, y: ""four"");

        M(t, 1, ""two"", true, 3, p: new[] { 5 });
        M(t, 1, ""two"", true, p: new[] { 5 });
        M(t, 1, ""two"", true, y: ""four"");
        M(t, 1, ""two"", true, x: 3);

        M(t, 1, ""two"", true, y: ""four"", x: 3);
        M(t, 1, y: ""four"", x: 3, b: ""two"", c: true);
        M(t, y: ""four"", x: 3, c: true, b: ""two"", a: 1);
        M(t, p: new[] { 5 }, y: ""four"", x: 3, c: true, b: ""two"", a: 1);
        M(p: new[] { 5 }, y: ""four"", x: 3, c: true, b: ""two"", a: 1, o: t);
    }
}";
            var signaturePartCounts = new[] { 1, 3, 2, 1 };
            await TestAllSignatureChangesAsync(LanguageNames.CSharp, markup, signaturePartCounts);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task TestAllSignatureChanges_OnDelegate_3Regular()
        {
            var markup = @"
using System;
using System.Collections.Generic;
using System.Linq;

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
        MyDelegate d1 = null;

        // Inline updates
        d1(1, ""Two"", true);
        d1.Invoke(1, ""Two"", true);
        // d1.BeginInvoke(1, ""Two"", null, null);
        d1.EndInvoke(null);
        d1 = delegate (int e, string f, bool g) { var x = e + (g ? f.Length : 0); };
        d1 = delegate { };
        d1 = (r, s, t) => { var x = r + (t ? s.Length : 0); };

        // Cascade through method groups
        d1 = Foo1;
        d1 = new MyDelegate(Foo2);
        Target(Foo3);
        Target((m, n, o) => { var x = m + (o ? n.Length : 0); });
        d1 = Result();
        d1 = Result2().First();
        d1 = Result3().First();

        // And references to those methods
        Foo1(1, ""Two"", true);
        Foo2(1, ""Two"", true);
        Foo2(1, false, false); // shouldn't change
        Foo3(1, ""Two"", true);
        Foo4(1, ""Two"", true);
        Foo5(1, ""Two"", true);
    }

    private MyDelegate Result() { return Foo4; }
    private IEnumerable<MyDelegate> Result2() { yield return Foo5; }
    private IEnumerable<MyDelegate> Result3() { yield return (g, h, i) => { var x = g + (i ? h.Length : 0); }; }

    void Target(MyDelegate d) { }
    void TargetTakesAction(Action<int, string> a) { }

    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    void Foo1(int a, string b, bool c) { }

    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    void Foo2(int a, string b, bool c) { }

    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    void Foo2(int a, object b, bool c) { }

    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    void Foo3(int a, string b, bool c) { }

    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    void Foo4(int a, string b, bool c) { }

    /// <param name=""a""></param>
    /// <param name=""b""></param>
    /// <param name=""c""></param>
    void Foo5(int a, string b, bool c) { }
}";
            var signaturePartCounts = new[] { 0, 3, 0, 0 };
            await TestAllSignatureChangesAsync(LanguageNames.CSharp, markup, signaturePartCounts);
        }
    }
}
