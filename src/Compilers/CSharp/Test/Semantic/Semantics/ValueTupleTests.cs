// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    public class ValueTupleTests : CompilingTestBase
    {
        [Fact]
        public void TestWellKnownMembersForValueTuple()
        {
            var source = @"
namespace System
{
    struct ValueTuple<T1>
    {
        public T1 Item1;
    }
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
    struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;
        public TRest Rest;
    }
}";
            var comp = CreateCompilation(source);
            Assert.Equal("T1 System.ValueTuple<T1>.Item1",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T1__Item1).ToTestDisplayString());

            Assert.Equal("T1 (T1, T2).Item1",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T2__Item1).ToTestDisplayString());
            Assert.Equal("T2 (T1, T2).Item2",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T2__Item2).ToTestDisplayString());

            Assert.Equal("T1 (T1, T2, T3).Item1",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T3__Item1).ToTestDisplayString());
            Assert.Equal("T2 (T1, T2, T3).Item2",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T3__Item2).ToTestDisplayString());
            Assert.Equal("T3 (T1, T2, T3).Item3",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T3__Item3).ToTestDisplayString());

            Assert.Equal("T1 (T1, T2, T3, T4).Item1",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T4__Item1).ToTestDisplayString());
            Assert.Equal("T2 (T1, T2, T3, T4).Item2",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T4__Item2).ToTestDisplayString());
            Assert.Equal("T3 (T1, T2, T3, T4).Item3",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T4__Item3).ToTestDisplayString());
            Assert.Equal("T4 (T1, T2, T3, T4).Item4",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T4__Item4).ToTestDisplayString());

            Assert.Equal("T1 (T1, T2, T3, T4, T5).Item1",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T5__Item1).ToTestDisplayString());
            Assert.Equal("T2 (T1, T2, T3, T4, T5).Item2",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T5__Item2).ToTestDisplayString());
            Assert.Equal("T3 (T1, T2, T3, T4, T5).Item3",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T5__Item3).ToTestDisplayString());
            Assert.Equal("T4 (T1, T2, T3, T4, T5).Item4",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T5__Item4).ToTestDisplayString());
            Assert.Equal("T5 (T1, T2, T3, T4, T5).Item5",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T5__Item5).ToTestDisplayString());

            Assert.Equal("T1 (T1, T2, T3, T4, T5, T6).Item1",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T6__Item1).ToTestDisplayString());
            Assert.Equal("T2 (T1, T2, T3, T4, T5, T6).Item2",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T6__Item2).ToTestDisplayString());
            Assert.Equal("T3 (T1, T2, T3, T4, T5, T6).Item3",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T6__Item3).ToTestDisplayString());
            Assert.Equal("T4 (T1, T2, T3, T4, T5, T6).Item4",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T6__Item4).ToTestDisplayString());
            Assert.Equal("T5 (T1, T2, T3, T4, T5, T6).Item5",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T6__Item5).ToTestDisplayString());
            Assert.Equal("T6 (T1, T2, T3, T4, T5, T6).Item6",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T6__Item6).ToTestDisplayString());

            Assert.Equal("T1 (T1, T2, T3, T4, T5, T6, T7).Item1",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T7__Item1).ToTestDisplayString());
            Assert.Equal("T2 (T1, T2, T3, T4, T5, T6, T7).Item2",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T7__Item2).ToTestDisplayString());
            Assert.Equal("T3 (T1, T2, T3, T4, T5, T6, T7).Item3",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T7__Item3).ToTestDisplayString());
            Assert.Equal("T4 (T1, T2, T3, T4, T5, T6, T7).Item4",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T7__Item4).ToTestDisplayString());
            Assert.Equal("T5 (T1, T2, T3, T4, T5, T6, T7).Item5",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T7__Item5).ToTestDisplayString());
            Assert.Equal("T6 (T1, T2, T3, T4, T5, T6, T7).Item6",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T7__Item6).ToTestDisplayString());
            Assert.Equal("T7 (T1, T2, T3, T4, T5, T6, T7).Item7",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T7__Item7).ToTestDisplayString());

            Assert.Equal("T1 System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>.Item1",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_TRest__Item1).ToTestDisplayString());
            Assert.Equal("T2 System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>.Item2",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_TRest__Item2).ToTestDisplayString());
            Assert.Equal("T3 System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>.Item3",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_TRest__Item3).ToTestDisplayString());
            Assert.Equal("T4 System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>.Item4",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_TRest__Item4).ToTestDisplayString());
            Assert.Equal("T5 System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>.Item5",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_TRest__Item5).ToTestDisplayString());
            Assert.Equal("T6 System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>.Item6",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_TRest__Item6).ToTestDisplayString());
            Assert.Equal("T7 System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>.Item7",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_TRest__Item7).ToTestDisplayString());
            Assert.Equal("TRest System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>.Rest",
                comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_TRest__Rest).ToTestDisplayString());
        }

        [Fact]
        public void TestMissingWellKnownMembersForValueTuple()
        {
            var comp = CreateCompilationWithMscorlib40("");
            Assert.True(comp.GetWellKnownType(WellKnownType.System_ValueTuple_T1).IsErrorType());
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T1__Item1));

            Assert.True(comp.GetWellKnownType(WellKnownType.System_ValueTuple_T2).IsErrorType());
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T2__Item1));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T2__Item2));

            Assert.True(comp.GetWellKnownType(WellKnownType.System_ValueTuple_T3).IsErrorType());
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T3__Item1));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T3__Item2));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T3__Item3));

            Assert.True(comp.GetWellKnownType(WellKnownType.System_ValueTuple_T4).IsErrorType());
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T4__Item1));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T4__Item2));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T4__Item3));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T4__Item4));

            Assert.True(comp.GetWellKnownType(WellKnownType.System_ValueTuple_T5).IsErrorType());
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T5__Item1));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T5__Item2));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T5__Item3));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T5__Item4));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T5__Item5));

            Assert.True(comp.GetWellKnownType(WellKnownType.System_ValueTuple_T6).IsErrorType());
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T6__Item1));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T6__Item2));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T6__Item3));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T6__Item4));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T6__Item6));

            Assert.True(comp.GetWellKnownType(WellKnownType.System_ValueTuple_T7).IsErrorType());
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T7__Item1));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T7__Item2));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T7__Item3));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T7__Item4));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T7__Item6));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_T7__Item7));

            Assert.True(comp.GetWellKnownType(WellKnownType.System_ValueTuple_TRest).IsErrorType());
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_TRest__Item1));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_TRest__Item2));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_TRest__Item3));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_TRest__Item4));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_TRest__Item6));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_TRest__Item7));
            Assert.Null(comp.GetWellKnownTypeMember(WellKnownMember.System_ValueTuple_TRest__Rest));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60961")]
        public void MissingRest()
        {
            var source = """
                (int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8) tuple = (1, 2, 3, 4, 5, 6, 7, 8);
                (_, _, _, _, _, _, _, int x) = tuple;
                System.Console.WriteLine(x);
                
                namespace System
                {
                    public struct ValueTuple<T1>
                    {
                        public T1 Item1;
                
                        public ValueTuple(T1 item1)
                        {
                            Item1 = item1;
                        }
                    }
                
                    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>
                    {
                        public T1 Item1;
                        public T2 Item2;
                        public T3 Item3;
                        public T4 Item4;
                        public T5 Item5;
                        public T6 Item6;
                        public T7 Item7;
                        // public TRest Rest;
                
                        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest)
                        {
                            Item1 = item1;
                            Item2 = item2;
                            Item3 = item3;
                            Item4 = item4;
                            Item5 = item5;
                            Item6 = item6;
                            Item7 = item7;
                            // Rest = rest;
                        }
                    }
                }
                """;
            var comp = CreateCompilation(source, assemblyName: "comp");
            comp.VerifyDiagnostics();
            comp.VerifyEmitDiagnostics(
                // (2,32): error CS8128: Member 'Rest' was not found on type 'ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>' from assembly 'comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                // (_, _, _, _, _, _, _, int x) = tuple;
                Diagnostic(ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly, "tuple").WithArguments("Rest", "System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>", "comp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(2, 32));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60961")]
        public void ExplicitInterfaceImplementation_Indexer_Partial()
        {
            var source = """
                namespace System
                {
                    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> : System.IEquatable<(T1, T2, T3, T4, T5, T6, T7, TRest)> where TRest : struct
                    {
                        public T1 Item1;
                        public T2 Item2;
                        public T3 Item3;
                        public T4 Item4;
                        public T5 Item5;
                        public T6 Item6;
                        public T7 Item7;
                        public TRest Rest;
                        object System.Runtime.CompilerServices.ITuple.this[int index] => throw null;
                    }
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyDiagnostics(
                // (3,67): error CS0535: 'ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>' does not implement interface member 'IEquatable<(T1, T2, T3, T4, T5, T6, T7, TRest)>.Equals((T1, T2, T3, T4, T5, T6, T7, TRest))'
                //     public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> : System.IEquatable<(T1, T2, T3, T4, T5, T6, T7, TRest)> where TRest : struct
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "System.IEquatable<(T1, T2, T3, T4, T5, T6, T7, TRest)>").WithArguments("System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>", "System.IEquatable<(T1, T2, T3, T4, T5, T6, T7, TRest)>.Equals((T1, T2, T3, T4, T5, T6, T7, TRest))").WithLocation(3, 67),
                // (13,16): error CS0540: 'ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>.ITuple.this[int]': containing type does not implement interface 'ITuple'
                //         object System.Runtime.CompilerServices.ITuple.this[int index] => throw null;
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "System.Runtime.CompilerServices.ITuple").WithArguments("System.ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>.System.Runtime.CompilerServices.ITuple.this[int]", "System.Runtime.CompilerServices.ITuple").WithLocation(13, 16));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60961")]
        public void ExplicitInterfaceImplementation_Indexer()
        {
            var source = """
                namespace System
                {
                    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> :
                        System.IEquatable<(T1, T2, T3, T4, T5, T6, T7, TRest)>,
                        System.Runtime.CompilerServices.ITuple
                        where TRest : struct
                    {
                        public T1 Item1;
                        public T2 Item2;
                        public T3 Item3;
                        public T4 Item4;
                        public T5 Item5;
                        public T6 Item6;
                        public T7 Item7;
                        public TRest Rest;
                        public int Length => throw null;
                        object System.Runtime.CompilerServices.ITuple.this[int index] => throw null;
                        bool System.IEquatable<(T1, T2, T3, T4, T5, T6, T7, TRest)>.Equals((T1, T2, T3, T4, T5, T6, T7, TRest) other) => false;
                    }
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyEmitDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60961")]
        public void ExplicitInterfaceImplementation_Property()
        {
            var source = """
                namespace System
                {
                    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> :
                        System.IEquatable<(T1, T2, T3, T4, T5, T6, T7, TRest)>,
                        System.Runtime.CompilerServices.ITuple
                        where TRest : struct
                    {
                        public T1 Item1;
                        public T2 Item2;
                        public T3 Item3;
                        public T4 Item4;
                        public T5 Item5;
                        public T6 Item6;
                        public T7 Item7;
                        public TRest Rest;
                        int System.Runtime.CompilerServices.ITuple.Length => throw null;
                        public object this[int index] => throw null;
                        bool System.IEquatable<(T1, T2, T3, T4, T5, T6, T7, TRest)>.Equals((T1, T2, T3, T4, T5, T6, T7, TRest) other) => false;
                    }
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.NetCoreApp);
            comp.VerifyEmitDiagnostics();
        }

        private const string Stubs = """
            namespace System
            {
                public class Object { }
                public class ValueType { }
                public class String { }
                public struct Void { }
                public struct Boolean { }
                public struct Int32 { }
                public struct IntPtr { }
                public class Exception { }
                public class MulticastDelegate { }
                public struct ValueTuple<T> { }
                public class Enum { }
                public class Attribute { }
                public enum AttributeTargets { }
                public class AttributeUsageAttribute
                {
                    public AttributeUsageAttribute(AttributeTargets validOn) { }
                    public bool AllowMultiple { get; set; }
                    public bool Inherited { get; set; }
                }
                namespace Reflection
                {
                    public class DefaultMemberAttribute
                    {
                        public DefaultMemberAttribute(string memberName) { }
                    }
                }
            }
            """;

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60961")]
        public void ExplicitInterfaceImplementation_Event()
        {
            var source = """
                namespace System
                {
                    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> :
                        System.IEquatable<(T1, T2, T3, T4, T5, T6, T7, TRest)>,
                        System.Runtime.CompilerServices.ITuple
                        where TRest : struct
                    {
                        public T1 Item1;
                        public T2 Item2;
                        public T3 Item3;
                        public T4 Item4;
                        public T5 Item5;
                        public T6 Item6;
                        public T7 Item7;
                        public TRest Rest;
                        public int Length => throw null;
                        public object this[int index] => throw null;
                        public bool Equals((T1, T2, T3, T4, T5, T6, T7, TRest) other) => throw null;
                        event D System.Runtime.CompilerServices.ITuple.Tupled
                        {
                            add => throw null;
                            remove => throw null;
                        }
                    }

                    public interface IEquatable<T>
                    {
                        bool Equals(T other);
                    }

                    namespace Runtime.CompilerServices
                    {
                        public interface ITuple
                        {
                            event D Tupled;
                            int Length { get; }
                            object this[int index] { get; }
                        }
                    }

                    public delegate void D();
                }
                """;

            var comp = CreateEmptyCompilation(new[] { source, Stubs });
            comp.VerifyEmitDiagnostics(EmitOptions.Default.WithRuntimeMetadataVersion("0.0.0.0"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60961")]
        public void ExplicitInterfaceImplementation_Operator()
        {
            var source = """
                namespace System
                {
                    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> :
                        System.IEquatable<(T1, T2, T3, T4, T5, T6, T7, TRest)>,
                        System.Runtime.CompilerServices.ITuple
                        where TRest : struct
                    {
                        public T1 Item1;
                        public T2 Item2;
                        public T3 Item3;
                        public T4 Item4;
                        public T5 Item5;
                        public T6 Item6;
                        public T7 Item7;
                        public TRest Rest;
                        public int Length => throw null;
                        public object this[int index] => throw null;
                        public bool Equals((T1, T2, T3, T4, T5, T6, T7, TRest) other) => throw null;
                        static System.Runtime.CompilerServices.ITuple System.Runtime.CompilerServices.ITuple.operator +(System.Runtime.CompilerServices.ITuple a, System.Runtime.CompilerServices.ITuple b) => throw null;
                    }

                    public interface IEquatable<T>
                    {
                        bool Equals(T other);
                    }

                    namespace Runtime.CompilerServices
                    {
                        public interface ITuple
                        {
                            abstract static ITuple operator +(ITuple a, ITuple b);
                            int Length { get; }
                            object this[int index] { get; }
                        }

                        public static class RuntimeFeature
                        {
                            public const string VirtualStaticsInInterfaces = nameof(VirtualStaticsInInterfaces);
                        }
                    }
                }
                """;

            var comp = CreateEmptyCompilation(new[] { source, Stubs });
            comp.VerifyEmitDiagnostics(EmitOptions.Default.WithRuntimeMetadataVersion("0.0.0.0"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60961")]
        public void ExplicitInterfaceImplementation_ConversionOperator()
        {
            var source = """
                namespace System
                {
                    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> :
                        System.IEquatable<(T1, T2, T3, T4, T5, T6, T7, TRest)>,
                        System.Runtime.CompilerServices.ITuple<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>
                        where TRest : struct
                    {
                        public T1 Item1;
                        public T2 Item2;
                        public T3 Item3;
                        public T4 Item4;
                        public T5 Item5;
                        public T6 Item6;
                        public T7 Item7;
                        public TRest Rest;
                        public int Length => throw null;
                        public object this[int index] => throw null;
                        public bool Equals((T1, T2, T3, T4, T5, T6, T7, TRest) other) => throw null;
                        static explicit System.Runtime.CompilerServices.ITuple<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>.operator string(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> x) => throw null;
                    }

                    public interface IEquatable<T>
                    {
                        bool Equals(T other);
                    }

                    namespace Runtime.CompilerServices
                    {
                        public interface ITuple<T> where T : ITuple<T>
                        {
                            abstract static explicit operator string(T x);
                            int Length { get; }
                            object this[int index] { get; }
                        }

                        public static class RuntimeFeature
                        {
                            public const string VirtualStaticsInInterfaces = nameof(VirtualStaticsInInterfaces);
                        }
                    }
                }
                """;

            var comp = CreateEmptyCompilation(new[] { source, Stubs });
            comp.VerifyEmitDiagnostics(EmitOptions.Default.WithRuntimeMetadataVersion("0.0.0.0"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60961")]
        public void ExplicitInterfaceImplementation_GenericProperty()
        {
            var source = """
                namespace System
                {
                    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> :
                        System.IEquatable<(T1, T2, T3, T4, T5, T6, T7, TRest)>,
                        System.Runtime.CompilerServices.ITuple<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>
                        where TRest : struct
                    {
                        public T1 Item1;
                        public T2 Item2;
                        public T3 Item3;
                        public T4 Item4;
                        public T5 Item5;
                        public T6 Item6;
                        public T7 Item7;
                        public TRest Rest;
                        int System.Runtime.CompilerServices.ITuple<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>.Length => throw null;
                        ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> System.Runtime.CompilerServices.ITuple<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>.Self => throw null;
                        object System.Runtime.CompilerServices.ITuple<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>.this[int index] => throw null;
                        ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> System.Runtime.CompilerServices.ITuple<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>.this[ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> self, int index] => throw null;
                        bool System.IEquatable<(T1, T2, T3, T4, T5, T6, T7, TRest)>.Equals((T1, T2, T3, T4, T5, T6, T7, TRest) other) => false;
                    }

                    public interface IEquatable<T>
                    {
                        bool Equals(T other);
                    }

                    namespace Runtime.CompilerServices
                    {
                        public interface ITuple<T> where T : ITuple<T>
                        {
                            int Length { get; }
                            T Self { get; }
                            object this[int index] { get; }
                            T this[T self, int index] { get; }
                        }
                    }
                }
                """;

            var comp = CreateEmptyCompilation(new[] { source, Stubs });
            comp.VerifyEmitDiagnostics(EmitOptions.Default.WithRuntimeMetadataVersion("0.0.0.0"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60961")]
        public void ExplicitInterfaceImplementation_TupleInSignatureOnly()
        {
            var source = """
                namespace System
                {
                    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> :
                        System.Runtime.CompilerServices.ITuple<T1, T2, T3, T4, T5, T6, T7, TRest>
                        where TRest : struct
                    {
                        public T1 Item1;
                        public T2 Item2;
                        public T3 Item3;
                        public T4 Item4;
                        public T5 Item5;
                        public T6 Item6;
                        public T7 Item7;
                        public TRest Rest;
                        int System.Runtime.CompilerServices.ITuple<T1, T2, T3, T4, T5, T6, T7, TRest>.Length => throw null;
                        (T1, T2, T3, T4, T5, T6, T7, TRest) System.Runtime.CompilerServices.ITuple<T1, T2, T3, T4, T5, T6, T7, TRest>.Self => throw null;
                        object System.Runtime.CompilerServices.ITuple<T1, T2, T3, T4, T5, T6, T7, TRest>.this[int index] => throw null;
                        (T1, T2, T3, T4, T5, T6, T7, TRest) System.Runtime.CompilerServices.ITuple<T1, T2, T3, T4, T5, T6, T7, TRest>.this[(T1, T2, T3, T4, T5, T6, T7, TRest) self, int index] => throw null;
                        bool System.Runtime.CompilerServices.ITuple<T1, T2, T3, T4, T5, T6, T7, TRest>.Equals((T1, T2, T3, T4, T5, T6, T7, TRest) other) => false;
                    }

                    namespace Runtime.CompilerServices
                    {
                        public interface ITuple<T1, T2, T3, T4, T5, T6, T7, TRest>
                        {
                            int Length { get; }
                            (T1, T2, T3, T4, T5, T6, T7, TRest) Self { get; }
                            object this[int index] { get; }
                            (T1, T2, T3, T4, T5, T6, T7, TRest) this[(T1, T2, T3, T4, T5, T6, T7, TRest) self, int index] { get; }
                            bool Equals((T1, T2, T3, T4, T5, T6, T7, TRest) other);
                        }
                    }
                }
                """;

            var comp = CreateEmptyCompilation(new[] { source, Stubs });
            comp.VerifyEmitDiagnostics(EmitOptions.Default.WithRuntimeMetadataVersion("0.0.0.0"));
        }
    }
}
