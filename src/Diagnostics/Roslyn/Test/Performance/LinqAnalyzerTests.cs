// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Diagnostics.Analyzers.CSharp.Performance;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Performance
{
    public class LinqAnalyzerTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new LinqAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void Last()
        {
            var code = @"
using System;
using System.Collections.Generic;
using System.Linq;

class D : IReadOnlyList<int>
{
    public int this[int index]
    {
        get { throw new NotImplementedException(); }
    }

    public int Count
    {
        get { throw new NotImplementedException(); }
    }

    public IEnumerator<int> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}

class C
{
    void Use<U>(U p) { } 

    void Test<T>()
    {
        T[] x1 = null;
        Use(x1.Last());
        Use(Enumerable.Last(x1));
        IReadOnlyList<T> x2 = null;
        Use(x2.Last());
        Use(Enumerable.Last(x2));
        D x3 = null;
        Use(x3.Last());
        Use(Enumerable.Last(x3));
        List<T> x4 = null;
        Use(x4.Last());
        Use(Enumerable.Last(x4));

        // Don't flag the version which takes a predicate
        Use(x1.Last(x => true));
        Use(Enumerable.Last(x1, (x => true)));
        Use(x2.Last(x => true));
        Use(Enumerable.Last(x2, (x => true)));
        Use(x3.Last(x => true));
        Use(Enumerable.Last(x3, (x => true)));
        Use(x4.Last(x => true));
        Use(Enumerable.Last(x4, (x => true)));

        // Make sure we flag other bad LINQ methods
        Use(x2.LastOrDefault());
        Use(x2.FirstOrDefault());
        Use(x2.First());
        Use(x2.Count());
    }
}
";

            VerifyCSharp(code,
                GetCSharpResultAt(39, 13, LinqAnalyzer.DoNotCallLastOnIndexableDescriptor),
                GetCSharpResultAt(40, 13, LinqAnalyzer.DoNotCallLastOnIndexableDescriptor),
                GetCSharpResultAt(42, 13, LinqAnalyzer.DoNotCallLastOnIndexableDescriptor),
                GetCSharpResultAt(43, 13, LinqAnalyzer.DoNotCallLastOnIndexableDescriptor),
                GetCSharpResultAt(59, 13, LinqAnalyzer.DoNotCallLastOnIndexableDescriptor),
                GetCSharpResultAt(60, 13, LinqAnalyzer.DoNotCallLastOnIndexableDescriptor),
                GetCSharpResultAt(61, 13, LinqAnalyzer.DoNotCallLastOnIndexableDescriptor),
                GetCSharpResultAt(62, 13, LinqAnalyzer.DoNotCallLastOnIndexableDescriptor));
        }
    }
}
