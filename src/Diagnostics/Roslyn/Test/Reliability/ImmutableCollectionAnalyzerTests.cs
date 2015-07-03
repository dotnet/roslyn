// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Diagnostics.Analyzers.CSharp.Reliability;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Performance
{
    public class ImmutableCollectionAnalyzerTests : DiagnosticAnalyzerTestBase
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ImmutableCollectionAnalyzer();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public void ToImmutableArray()
        {
            var code = @"
using System.Collections.Generic;
using System.Collections.Immutable;

class C
{
    ImmutableArray<string> field;

    ImmutableArray<U> GetGeneric<U>() { return ImmutableArray<U>.Empty; }
    ImmutableArray<int> GetConcrete() { return ImmutableArray<int>.Empty; }
    IEnumerable<T> GetEnumerableGeneric<T>() { return null; } 
    IEnumerable<T> GetEnumerableConcrete<T>() { return null; } 

    void Test() 
    {
        ImmutableArray<int> local = ImmutableArray<int>.Empty;
        var temp1 = local.ToImmutableArray();
        var temp2 = GetGeneric<T>().ToImmutableArray();
        var temp3 = GetConcrete().ToImmutableArray();
        var temp4 = GetEnumerableGeneric<T>().ToImmutableArray();
        var temp5 = GetEnumerableConcrete<T>().ToImmutableArray();
    }
}";

            VerifyCSharp(
                code,
                GetCSharpResultAt(17, 21, ImmutableCollectionAnalyzer.DoNotCallToImmutableArrayDescriptor),
                GetCSharpResultAt(18, 21, ImmutableCollectionAnalyzer.DoNotCallToImmutableArrayDescriptor),
                GetCSharpResultAt(19, 21, ImmutableCollectionAnalyzer.DoNotCallToImmutableArrayDescriptor));
        }
    }
}
