// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void RemoveParameters1()
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
            var updatedSignature = new[] { 0, 2, 5 };
            var updatedCode = @"
static class Ext
{
    /// <summary>
    /// This is a summary of <see cref=""M(object, string, string)""/>
    /// </summary>
    /// <param name=""o""></param>
    /// <param name=""b""></param>
    /// <param name=""y""></param>
    /// 
    /// 
    /// 
    /// 
    static void M(this object o, string b, string y = ""Zero"")
    {
        object t = new object();

        M(t, ""two"", ""four"");
        M(t, ""two"", ""four"");
        t.M(""two"", ""four"");
        t.M(""two"", ""four"");

        M(t, ""two"", ""four"");
        M(t, ""two"");
        M(t, ""two"");

        M(t, ""two"");
        M(t, ""two"", y: ""four"");

        M(t, ""two"");
        M(t, ""two"");
        M(t, ""two"", y: ""four"");
        M(t, ""two"");

        M(t, ""two"", y: ""four"");
        M(t, y: ""four"", b: ""two"");
        M(t, y: ""four"", b: ""two"");
        M(t, y: ""four"", b: ""two"");
        M(y: ""four"", b: ""two"", o: t);
    }
}";

            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void RemoveParameters_GenericParameterType()
        {
            var markup = @"
class DA
{
    void M(params int[] i) { }

    void B()
    {
        DP20<int>.D d = new DP20<int>.D(M);

        /*DA19*/$$d(0);
        d();
        d(0, 1);
    }
}
public class DP20<T>
{
    public delegate void /*DP20*/D(params T[] t);
    public event D E1;
    public event D E2;

    public void M1(params T[] t) { }
    public void M2(params T[] t) { }
    public void M3(params T[] t) { }

    void B()
    {
        D d = new D(M1);
        E1 += new D(M2);
        E2 -= new D(M3);
    }
}";
            var updatedSignature = Array.Empty<int>();
            var updatedCode = @"
class DA
{
    void M() { }

    void B()
    {
        DP20<int>.D d = new DP20<int>.D(M);

        /*DA19*/d();
        d();
        d();
    }
}
public class DP20<T>
{
    public delegate void /*DP20*/D();
    public event D E1;
    public event D E2;

    public void M1() { }
    public void M2() { }
    public void M3() { }

    void B()
    {
        D d = new D(M1);
        E1 += new D(M2);
        E2 -= new D(M3);
    }
}";

            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }
    }
}
