// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.AsyncStreams)]
    public class CodeGenAsyncIteratorTests : EmitMetadataTestBase
    {
        // PROTOTYPE(async-streams): Consider moving this common test code to TestSources.cs
        private static readonly string s_common = @"
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator();
    }

    public interface IAsyncEnumerator<out T> : System.IAsyncDisposable
    {
        System.Threading.Tasks.Task<bool> WaitForNextAsync();
        T TryGetNext(out bool success);
    }
}
namespace System
{
    public interface IAsyncDisposable
    {
        System.Threading.Tasks.Task DisposeAsync();
    }
}

namespace System.Runtime.CompilerServices
{
    public interface IStrongBox<T>
    {
        ref T Value { get; }
    }
}

namespace System.Threading.Tasks
{
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks.Sources;

    public struct ManualResetValueTaskSourceLogic<TResult>
    {
        public ManualResetValueTaskSourceLogic(IStrongBox<ManualResetValueTaskSourceLogic<TResult>> parent)
        {
        }

        public short Version
            => throw null;

        public ValueTaskSourceStatus GetStatus(short token)
            => throw null;

        public TResult GetResult(short token)
            => throw null;

        public void Reset()
            => throw null;

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
            => throw null;

        public void SetResult(TResult result)
            => throw null;

        public void SetException(Exception error)
            => throw null;
    }
}
";

        [Fact]
        public void TestWellKnownMembers()
        {
            var comp = CreateCompilation(s_common, references: new[] { TestReferences.NetStandard20.TasksExtensionsRef }, targetFramework: Roslyn.Test.Utilities.TargetFramework.NetStandard20);
            comp.VerifyDiagnostics();

            verifyType(WellKnownType.System_Runtime_CompilerServices_IStrongBox_T,
                "System.Runtime.CompilerServices.IStrongBox<T>");

            verifyMember(WellKnownMember.System_Runtime_CompilerServices_IStrongBox_T__Value,
                "ref T System.Runtime.CompilerServices.IStrongBox<T>.Value { get; }");

            verifyMember(WellKnownMember.System_Runtime_CompilerServices_IStrongBox_T__get_Value,
                "ref T System.Runtime.CompilerServices.IStrongBox<T>.Value.get");

            verifyType(WellKnownType.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T,
                "System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>");

            verifyMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__ctor,
                "System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>..ctor(System.Runtime.CompilerServices.IStrongBox<System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>> parent)");

            verifyMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__GetResult,
                "TResult System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.GetResult(System.Int16 token)");

            verifyMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__GetStatus,
                "System.Threading.Tasks.Sources.ValueTaskSourceStatus System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.GetStatus(System.Int16 token)");

            verifyMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__OnCompleted,
                "void System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.OnCompleted(System.Action<System.Object> continuation, System.Object state, System.Int16 token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)");

            verifyMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__Reset,
                "void System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.Reset()");

            verifyMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__SetResult,
                "void System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.SetResult(TResult result)");

            verifyMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__get_Version,
                "System.Int16 System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.Version.get");

            verifyType(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceStatus,
                "System.Threading.Tasks.Sources.ValueTaskSourceStatus");

            verifyType(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceOnCompletedFlags,
                "System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags");

            verifyType(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource_T,
                "System.Threading.Tasks.Sources.IValueTaskSource<out TResult>");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetResult,
                "TResult System.Threading.Tasks.Sources.IValueTaskSource<out TResult>.GetResult(System.Int16 token)");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetStatus,
                "System.Threading.Tasks.Sources.ValueTaskSourceStatus System.Threading.Tasks.Sources.IValueTaskSource<out TResult>.GetStatus(System.Int16 token)");

            verifyMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__OnCompleted,
                "void System.Threading.Tasks.Sources.IValueTaskSource<out TResult>.OnCompleted(System.Action<System.Object> continuation, System.Object state, System.Int16 token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)");

            verifyType(WellKnownType.System_Threading_Tasks_ValueTask_T,
                "System.Threading.Tasks.ValueTask<TResult>");

            verifyMember(WellKnownMember.System_Threading_Tasks_ValueTask_T__ctor,
                "System.Threading.Tasks.ValueTask<TResult>..ctor(System.Threading.Tasks.Sources.IValueTaskSource<TResult> source, System.Int16 token)");

            void verifyType(WellKnownType type, string expected)
            {
                var symbol = comp.GetWellKnownType(type);
                Assert.Equal(expected, symbol.ToTestDisplayString());
            }

            void verifyMember(WellKnownMember member, string expected)
            {
                var symbol = comp.GetWellKnownTypeMember(member);
                Assert.Equal(expected, symbol.ToTestDisplayString());
            }
        }
    }
}
