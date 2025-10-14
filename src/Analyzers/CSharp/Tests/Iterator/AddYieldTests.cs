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
public sealed class AddYieldTests
{
    private static Task TestMissingInRegularAndScriptAsync(string code)
        => VerifyCS.VerifyCodeFixAsync(code, code);

    private static Task TestInRegularAndScriptAsync(string code, string fixedCode)
        => VerifyCS.VerifyCodeFixAsync(code, fixedCode);

    [Fact]
    public Task TestAddYieldIEnumerableReturnNull()
        => TestMissingInRegularAndScriptAsync("""
            using System;
            using System.Collections;

            class Program
            {
                static IEnumerable M()
                {
                    return null;
                }
            }
            """);

    [Fact]
    public Task TestAddYieldIEnumerableReturnObject()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Collections;

            class Program
            {
                static IEnumerable M()
                {
                    return {|CS0266:new object()|};
                }
            }
            """, """
            using System;
            using System.Collections;

            class Program
            {
                static IEnumerable M()
                {
                    yield return new object();
                }
            }
            """);

    [Fact]
    public Task TestAddYieldIEnumeratorReturnObject()
        => TestInRegularAndScriptAsync("""
            using System;
            using System.Collections;

            class Program
            {
                static IEnumerator M()
                {
                    return {|CS0266:new object()|};
                }
            }
            """, """
            using System;
            using System.Collections;

            class Program
            {
                static IEnumerator M()
                {
                    yield return new object();
                }
            }
            """);

    [Fact]
    public Task TestAddYieldIEnumeratorReturnGenericList()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestAddYieldGenericIEnumeratorReturnObject()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestAddYieldGenericIEnumerableReturnObject()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestAddYieldIEnumerableReturnGenericList()
        => TestMissingInRegularAndScriptAsync("""
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
            """);

    [Fact]
    public Task TestAddYieldGenericIEnumeratorReturnDefault()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestAddYieldGenericIEnumerableReturnConvertibleToObject()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestAddYieldGenericIEnumerableReturnConvertibleToFloat()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestAddYieldGenericIEnumeratorNonConvertableType()
        => TestMissingInRegularAndScriptAsync("""
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
            """);

    [Fact]
    public Task TestAddYieldGenericIEnumeratorConvertableTypeDateTime()
        => TestInRegularAndScriptAsync("""
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
            """, """
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
            """);

    [Fact]
    public Task TestAddYieldNoTypeArguments()
        => TestMissingInRegularAndScriptAsync("""
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
            """);
}
