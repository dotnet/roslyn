// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.Iterator;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Iterator;

using VerifyCS = CSharpCodeFixVerifier<EmptyDiagnosticAnalyzer, CSharpAddYieldCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)]
public class AddYieldTests
{
    private static async Task TestMissingInRegularAndScriptAsync(string code)
    {
        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    private static async Task TestInRegularAndScriptAsync(string code, string fixedCode)
    {
        await VerifyCS.VerifyCodeFixAsync(code, fixedCode);
    }

    [Fact]
    public async Task TestAddYieldIEnumerableReturnNull()
    {
        var initial =
            """
            using System;
            using System.Collections;

            class Program
            {
                static IEnumerable M()
                {
                    return null;
                }
            }
            """;
        await TestMissingInRegularAndScriptAsync(initial);
    }

    [Fact]
    public async Task TestAddYieldIEnumerableReturnObject()
    {
        var initial =
            """
            using System;
            using System.Collections;

            class Program
            {
                static IEnumerable M()
                {
                    return {|CS0266:new object()|};
                }
            }
            """;
        var expected =
            """
            using System;
            using System.Collections;

            class Program
            {
                static IEnumerable M()
                {
                    yield return new object();
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestAddYieldIEnumeratorReturnObject()
    {
        var initial =
            """
            using System;
            using System.Collections;

            class Program
            {
                static IEnumerator M()
                {
                    return {|CS0266:new object()|};
                }
            }
            """;
        var expected =
            """
            using System;
            using System.Collections;

            class Program
            {
                static IEnumerator M()
                {
                    yield return new object();
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestAddYieldIEnumeratorReturnGenericList()
    {
        var initial =
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerator M<T>()
                {
                    return {|CS0266:new List<T>()|};
                }
            }
            """;
        var expected =
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerator M<T>()
                {
                    yield return new List<T>();
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestAddYieldGenericIEnumeratorReturnObject()
    {
        var initial =
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerator<object> M()
                {
                    return {|CS0266:new object()|};
                }
            }
            """;
        var expected =
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerator<object> M()
                {
                    yield return new object();
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestAddYieldGenericIEnumerableReturnObject()
    {
        var initial =
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerable<object> M()
                {
                    return {|CS0266:new object()|};
                }
            }
            """;
        var expected =
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerable<object> M()
                {
                    yield return new object();
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestAddYieldIEnumerableReturnGenericList()
    {
        var initial =
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerable M<T>()
                {
                    return new List<T>();
                }
            }
            """;
        await TestMissingInRegularAndScriptAsync(initial);
    }

    [Fact]
    public async Task TestAddYieldGenericIEnumeratorReturnDefault()
    {
        var initial =
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerator<T> M<T>()
                {
                   return {|CS0266:default(T)|};
                }
            }
            """;
        var expected =
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerator<T> M<T>()
                {
                    yield return default(T);
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestAddYieldGenericIEnumerableReturnConvertibleToObject()
    {
        var initial =
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerable<object> M()
                {
                    return {|CS0029:0|};
                }
            }
            """;
        var expected =
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerable<object> M()
                {
                    yield return 0;
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestAddYieldGenericIEnumerableReturnConvertibleToFloat()
    {
        var initial =
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerator<float> M()
                {
                    return {|CS0029:0|};
                }
            }
            """;
        var expected =
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerator<float> M()
                {
                    yield return 0;
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestAddYieldGenericIEnumeratorNonConvertableType()
    {
        var initial =
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerator<IList<DateTime>> M()
                {
                    return {|CS0266:new List<int>()|};
                }
            }
            """;
        await TestMissingInRegularAndScriptAsync(initial);
    }

    [Fact]
    public async Task TestAddYieldGenericIEnumeratorConvertableTypeDateTime()
    {
        var initial =
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerator<IList<DateTime>> M()
                {
                    return {|CS0266:new List<DateTime>()|};
                }
            }
            """;
        var expected =
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerator<IList<DateTime>> M()
                {
                    yield return new List<DateTime>();
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestAddYieldNoTypeArguments()
    {
        var initial =
            """
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;

            namespace ConsoleApplication13
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        var d = new A<int>.B<StringBuilder>.C<char>.D<object>();
                    }
                }
            }


            #pragma warning disable CS0108
            public class A<Z> where Z : new()
            {
                public virtual Z P1 { get { return new Z(); } }

                public class B<Y> : A<B<Y>> where Y : new()
                {
                    public override A<Z>.B<Y> P1 { get; {|CS0546:set|}; }
                    public virtual Y P2 { get { return {|CS0029:new Z()|}; } }

                    public class C<X> : B<C<X>> where X : new()
                    {
                        public override A<A<Z>.B<Y>>.B<A<Z>.B<Y>.C<X>> P1 { get; set; }
                        public override A<Z>.B<Y>.C<X> P2 { get; {|CS0546:set|}; }
                        public virtual X P3 { get; set; }

                        public class D<W> : C<D<W>> where W : new()
                        {
                            public override A<A<A<Z>.B<Y>>.B<A<Z>.B<Y>.C<X>>>.B<A<A<Z>.B<Y>>.B<A<Z>.B<Y>.C<X>>.C<A<Z>.B<Y>.C<X>.D<W>>> P1 { get; set; }
                            public override A<A<Z>.B<Y>>.B<A<Z>.B<Y>.C<X>>.C<A<Z>.B<Y>.C<X>.D<W>> P2 { get; set; }
                            public override A<Z>.B<Y>.C<X>.D<W> P3 { get; set; }
                            public virtual W P4 { get; set; }
                        }
                    }
                }
            }
            """;
        await TestMissingInRegularAndScriptAsync(initial);
    }
}
