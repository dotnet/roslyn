// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.Iterator;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Iterator
{
    public class AddYieldTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(null, new CSharpAddYieldCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)]
        public void TestAddYieldIEnumerableReturnNull()
        {
            var initial =
@"using System;
using System.Collections;

class Program
{
    static IEnumerable M()
    {
        [|return null|];
    }
}";
            TestMissing(initial);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)]
        public void TestAddYieldIEnumerableReturnObject()
        {
            var initial =
@"using System;
using System.Collections;

class Program
{
    static IEnumerable M()
    {
        [|return new object()|];
    }
}";
            var expected =
@"using System;
using System.Collections;

class Program
{
    static IEnumerable M()
    {
        yield return new object();
    }
}";
            Test(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)]
        public void TestAddYieldIEnumeratorReturnObject()
        {
            var initial =
@"using System;
using System.Collections;

class Program
{
    static IEnumerator M()
    {
        [|return new object()|];
    }
}";
            var expected =
@"using System;
using System.Collections;

class Program
{
    static IEnumerator M()
    {
        yield return new object();
    }
}";
            Test(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)]
        public void TestAddYieldIEnumeratorReturnGenericList()
        {
            var initial =
@"using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static IEnumerator M<T>()
    {
        [|return new List<T>()|];
    }
}";
            var expected =
@"using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static IEnumerator M<T>()
    {
        yield return new List<T>();
    }
}";
            Test(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)]
        public void TestAddYieldGenericIEnumeratorReturnObject()
        {
            var initial =
@"using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static IEnumerator<object> M()
    {
        [|return new object()|];
    }
}";
            var expected =
@"using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static IEnumerator<object> M()
    {
        yield return new object();
    }
}";
            Test(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)]
        public void TestAddYieldGenericIEnumerableReturnObject()
        {
            var initial =
@"using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static IEnumerable<object> M()
    {
        [|return new object()|];
    }
}";
            var expected =
@"using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static IEnumerable<object> M()
    {
        yield return new object();
    }
}";
            Test(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)]
        public void TestAddYieldIEnumerableReturnGenericList()
        {
            var initial =
@"using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static IEnumerable M<T>()
    {
        [|return new List<T>()|];
    }
}";
            TestMissing(initial);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)]
        public void TestAddYieldGenericIEnumeratorReturnDefault()
        {
            var initial =
@"using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static IEnumerator<T> M<T>()
    {
       [|return default(T)|];
    }
}";
            var expected =
@"using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static IEnumerator<T> M<T>()
    {
        yield return default(T);
    }
}";
            Test(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)]
        public void TestAddYieldGenericIEnumerableReturnConvertibleToObject()
        {
            var initial =
@"using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static IEnumerable<object> M()
    {
        [|return 0|];
    }
}";
            var expected =
@"using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static IEnumerable<object> M()
    {
        yield return 0;
    }
}";
            Test(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)]
        public void TestAddYieldGenericIEnumerableReturnConvertibleToFloat()
        {
            var initial =
@"using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static IEnumerator<float> M()
    {
        [|return 0|];
    }
}";
            var expected =
@"using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static IEnumerator<float> M()
    {
        yield return 0;
    }
}";
            Test(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)]
        public void TestAddYieldGenericIEnumeratorNonConvertableType()
        {
            var initial =
@"using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static IEnumerator<IList<DateTime>> M()
    {
        [|return new List<int>()|];
    }
}";
            TestMissing(initial);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)]
        public void TestAddYieldGenericIEnumeratorConvertableTypeDateTime()
        {
            var initial =
@"using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static IEnumerator<IList<DateTime>> M()
    {
        [|return new List<DateTime>()|];
    }
}";
            var expected =
@"using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static IEnumerator<IList<DateTime>> M()
    {
        yield return new List<DateTime>();
    }
}";
            Test(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToYield)]
        public void TestAddYieldNoTypeArguments()
        {
            var initial =
@"using System;
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
        public override A<Z>.B<Y> P1 { get; set; }
        public virtual Y P2 { get { [|return new Z()|]; } }

        public class C<X> : B<C<X>> where X : new()
        {
            public override A<A<Z>.B<Y>>.B<A<Z>.B<Y>.C<X>> P1 { get; set; }
            public override A<Z>.B<Y>.C<X> P2 { get; set; }
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
";
            TestMissing(initial);
        }
    }
}
