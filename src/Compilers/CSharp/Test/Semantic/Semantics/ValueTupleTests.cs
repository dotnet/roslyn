// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    class ValueTupleTests : CompilingTestBase
    {
        [Fact]
        public void TestWellKnownMembersForValueTuple()
        {
            var source = @"
namespace System.Runtime.CompilerServices
{
    struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
    }
    struct ValueTuple<T1, T2, T3>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
    }
    struct ValueTuple<T1, T2, T3, T4>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
    }
    struct ValueTuple<T1, T2, T3, T4, T5>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
    }
    struct ValueTuple<T1, T2, T3, T4, T5, T6>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
    }
    struct ValueTuple<T1, T2, T3, T4, T5, T6, T7>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;
    }
}";
            var comp = CreateCompilationWithMscorlib(source);
            Assert.Equal("T1 System.Runtime.CompilerServices.ValueTuple<T1, T2>.Item1", 
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2__Item1).ToTestDisplayString());
            Assert.Equal("T2 System.Runtime.CompilerServices.ValueTuple<T1, T2>.Item2",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2__Item2).ToTestDisplayString());

            Assert.Equal("T1 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3>.Item1",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3__Item1).ToTestDisplayString());
            Assert.Equal("T2 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3>.Item2",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3__Item2).ToTestDisplayString());
            Assert.Equal("T3 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3>.Item3",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3__Item3).ToTestDisplayString());

            Assert.Equal("T1 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4>.Item1",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4__Item1).ToTestDisplayString());
            Assert.Equal("T2 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4>.Item2",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4__Item2).ToTestDisplayString());
            Assert.Equal("T3 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4>.Item3",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4__Item3).ToTestDisplayString());
            Assert.Equal("T4 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4>.Item4",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4__Item4).ToTestDisplayString());

            Assert.Equal("T1 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5>.Item1",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5__Item1).ToTestDisplayString());
            Assert.Equal("T2 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5>.Item2",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5__Item2).ToTestDisplayString());
            Assert.Equal("T3 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5>.Item3",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5__Item3).ToTestDisplayString());
            Assert.Equal("T4 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5>.Item4",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5__Item4).ToTestDisplayString());
            Assert.Equal("T5 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5>.Item5",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5__Item5).ToTestDisplayString());

            Assert.Equal("T1 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5, T6>.Item1",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6__Item1).ToTestDisplayString());
            Assert.Equal("T2 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5, T6>.Item2",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6__Item2).ToTestDisplayString());
            Assert.Equal("T3 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5, T6>.Item3",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6__Item3).ToTestDisplayString());
            Assert.Equal("T4 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5, T6>.Item4",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6__Item4).ToTestDisplayString());
            Assert.Equal("T5 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5, T6>.Item5",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6__Item5).ToTestDisplayString());
            Assert.Equal("T6 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5, T6>.Item6",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6__Item6).ToTestDisplayString());

            Assert.Equal("T1 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5, T6, T7>.Item1",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6_T7__Item1).ToTestDisplayString());
            Assert.Equal("T2 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5, T6, T7>.Item2",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6_T7__Item2).ToTestDisplayString());
            Assert.Equal("T3 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5, T6, T7>.Item3",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6_T7__Item3).ToTestDisplayString());
            Assert.Equal("T4 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5, T6, T7>.Item4",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6_T7__Item4).ToTestDisplayString());
            Assert.Equal("T5 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5, T6, T7>.Item5",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6_T7__Item5).ToTestDisplayString());
            Assert.Equal("T6 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5, T6, T7>.Item6",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6_T7__Item6).ToTestDisplayString());
            Assert.Equal("T7 System.Runtime.CompilerServices.ValueTuple<T1, T2, T3, T4, T5, T6, T7>.Item7",
                comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6_T7__Item7).ToTestDisplayString());
        }

        [Fact]
        public void TestMissingWellKnownMembersForValueTuple()
        {
            var comp = CreateCompilationWithMscorlib("");
            Assert.True(comp.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_ValueTuple_T1_T2).IsErrorType());
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2__Item1));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2__Item2));

            Assert.True(comp.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3).IsErrorType());
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3__Item1));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3__Item2));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3__Item3));

            Assert.True(comp.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4).IsErrorType());
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4__Item1));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4__Item2));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4__Item3));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4__Item4));

            Assert.True(comp.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5).IsErrorType());
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5__Item1));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5__Item2));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5__Item3));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5__Item4));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5__Item5));

            Assert.True(comp.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6).IsErrorType());
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6__Item1));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6__Item2));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6__Item3));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6__Item4));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6__Item6));

            Assert.True(comp.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6_T7).IsErrorType());
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6_T7__Item1));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6_T7__Item2));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6_T7__Item3));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6_T7__Item4));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6_T7__Item6));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ValueTuple_T1_T2_T3_T4_T5_T6_T7__Item7));
        }
    }
}
