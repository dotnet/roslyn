// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature;

[Trait(Traits.Feature, Traits.Features.ChangeSignature)]
public partial class ChangeSignatureTests : AbstractChangeSignatureTests
{
    [Theory]
#pragma warning disable xUnit1019
    // There is a bug in xUnit analyzer might generate false alarm, temporary disable it
    // https://github.com/xunit/xunit/issues/1968
    [MemberData(nameof(AbstractChangeSignatureTests.GetAllSignatureSpecificationsForTheory), new[] { 1, 3, 2, 1 }, MemberType = typeof(AbstractChangeSignatureTests))]
#pragma warning restore xUnit1019
    public async Task TestAllSignatureChanges_1This_3Regular_2Default_1Params(int totalParameters, int[] signature)
    {
        var markup = """
            static class Ext
            {
                /// <summary>
                /// This is a summary of <see cref="M(object, int, string, bool, int, string, int[])"/>
                /// </summary>
                /// <param name="o"></param>
                /// <param name="a"></param>
                /// <param name="b"></param>
                /// <param name="c"></param>
                /// <param name="x"></param>
                /// <param name="y"></param>
                /// <param name="p"></param>
                static void $$M(this object o, int a, string b, bool c, int x = 0, string y = "Zero", params int[] p)
                {
                    object t = new object();

                    M(t, 1, "two", true, 3, "four", new[] { 5, 6 });
                    M(t, 1, "two", true, 3, "four", 5, 6);
                    t.M(1, "two", true, 3, "four", new[] { 5, 6 });
                    t.M(1, "two", true, 3, "four", 5, 6);

                    M(t, 1, "two", true, 3, "four");
                    M(t, 1, "two", true, 3);
                    M(t, 1, "two", true);

                    M(t, 1, "two", c: true);
                    M(t, 1, "two", true, 3, y: "four");

                    M(t, 1, "two", true, 3, p: new[] { 5 });
                    M(t, 1, "two", true, p: new[] { 5 });
                    M(t, 1, "two", true, y: "four");
                    M(t, 1, "two", true, x: 3);

                    M(t, 1, "two", true, y: "four", x: 3);
                    M(t, 1, y: "four", x: 3, b: "two", c: true);
                    M(t, y: "four", x: 3, c: true, b: "two", a: 1);
                    M(t, p: new[] { 5 }, y: "four", x: 3, c: true, b: "two", a: 1);
                    M(p: new[] { 5 }, y: "four", x: 3, c: true, b: "two", a: 1, o: t);
                }
            }
            """;

        await TestChangeSignatureViaCommandAsync(
            LanguageNames.CSharp,
            markup,
            expectedSuccess: true,
            updatedSignature: signature,
            totalParameters: totalParameters,
            verifyNoDiagnostics: true);
    }

    [Theory]
#pragma warning disable xUnit1019
    // There is a bug in xUnit analyzer might generate false alarm, temporary disable it
    // https://github.com/xunit/xunit/issues/1968
    [MemberData(nameof(AbstractChangeSignatureTests.GetAllSignatureSpecificationsForTheory), new[] { 0, 3, 0, 0 }, MemberType = typeof(AbstractChangeSignatureTests))]
#pragma warning restore xUnit1019
    public async Task TestAllSignatureChanges_OnDelegate_3Regular(int totalParameters, int[] signature)
    {
        var markup = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            /// <summary>
            /// This is <see cref="MyDelegate"/>, which has these methods:
            ///     <see cref="MyDelegate.MyDelegate(object, IntPtr)"/>
            ///     <see cref="MyDelegate.Invoke(int, string, bool)"/>
            ///     <see cref="MyDelegate.EndInvoke(IAsyncResult)"/>
            ///     <see cref="MyDelegate.BeginInvoke(int, string, bool, AsyncCallback, object)"/>
            /// </summary>
            /// <param name="x">x!</param>
            /// <param name="y">y!</param>
            /// <param name="z">z!</param>
            delegate void $$MyDelegate(int x, string y, bool z);

            class C
            {
                void M()
                {
                    MyDelegate d1 = null;

                    // Inline updates
                    d1(1, "Two", true);
                    d1.Invoke(1, "Two", true);
                    // d1.BeginInvoke(1, "Two", null, null);
                    d1.EndInvoke(null);
                    d1 = delegate (int e, string f, bool g) { var x = e + (g ? f.Length : 0); };
                    d1 = delegate { };
                    d1 = (r, s, t) => { var x = r + (t ? s.Length : 0); };

                    // Cascade through method groups
                    d1 = Goo1;
                    d1 = new MyDelegate(Goo2);
                    Target(Goo3);
                    Target((m, n, o) => { var x = m + (o ? n.Length : 0); });
                    d1 = Result();
                    d1 = Result2().First();
                    d1 = Result3().First();

                    // And references to those methods
                    Goo1(1, "Two", true);
                    Goo2(1, "Two", true);
                    Goo2(1, false, false); // shouldn't change
                    Goo3(1, "Two", true);
                    Goo4(1, "Two", true);
                    Goo5(1, "Two", true);
                }

                private MyDelegate Result() { return Goo4; }
                private IEnumerable<MyDelegate> Result2() { yield return Goo5; }
                private IEnumerable<MyDelegate> Result3() { yield return (g, h, i) => { var x = g + (i ? h.Length : 0); }; }

                void Target(MyDelegate d) { }
                void TargetTakesAction(Action<int, string> a) { }

                /// <param name="a"></param>
                /// <param name="b"></param>
                /// <param name="c"></param>
                void Goo1(int a, string b, bool c) { }

                /// <param name="a"></param>
                /// <param name="b"></param>
                /// <param name="c"></param>
                void Goo2(int a, string b, bool c) { }

                /// <param name="a"></param>
                /// <param name="b"></param>
                /// <param name="c"></param>
                void Goo2(int a, object b, bool c) { }

                /// <param name="a"></param>
                /// <param name="b"></param>
                /// <param name="c"></param>
                void Goo3(int a, string b, bool c) { }

                /// <param name="a"></param>
                /// <param name="b"></param>
                /// <param name="c"></param>
                void Goo4(int a, string b, bool c) { }

                /// <param name="a"></param>
                /// <param name="b"></param>
                /// <param name="c"></param>
                void Goo5(int a, string b, bool c) { }
            }
            """;

        await TestChangeSignatureViaCommandAsync(
            LanguageNames.CSharp,
            markup,
            expectedSuccess: true,
            updatedSignature: signature,
            totalParameters: totalParameters,
            verifyNoDiagnostics: true,
            parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7));
    }
}
