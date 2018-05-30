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
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks.Sources;

    public struct ManualResetValueTaskSourceLogic<TResult>
    {
        private static readonly Action<object> s_sentinel = new Action<object>(s => throw new InvalidOperationException());

        private readonly IStrongBox<ManualResetValueTaskSourceLogic<TResult>> _parent;
        private Action<object> _continuation;
        private object _continuationState;
        private object _capturedContext;
        private ExecutionContext _executionContext;
        private bool _completed;
        private TResult _result;
        private ExceptionDispatchInfo _error;
        private short _version;

        public ManualResetValueTaskSourceLogic(IStrongBox<ManualResetValueTaskSourceLogic<TResult>> parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _continuation = null;
            _continuationState = null;
            _capturedContext = null;
            _executionContext = null;
            _completed = false;
            _result = default;
            _error = null;
            _version = 0;
        }

        public short Version => _version;

        private void ValidateToken(short token)
        {
            if (token != _version)
            {
                throw new InvalidOperationException();
            }
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            ValidateToken(token);

            return
                !_completed ? ValueTaskSourceStatus.Pending :
                _error == null ? ValueTaskSourceStatus.Succeeded :
                _error.SourceException is OperationCanceledException ? ValueTaskSourceStatus.Canceled :
                ValueTaskSourceStatus.Faulted;
        }

        public TResult GetResult(short token)
        {
            ValidateToken(token);

            if (!_completed)
            {
                throw new InvalidOperationException();
            }

            _error?.Throw();
            return _result;
        }

        public void Reset()
        {
            _version++;

            _completed = false;
            _continuation = null;
            _continuationState = null;
            _result = default;
            _error = null;
            _executionContext = null;
            _capturedContext = null;
        }

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            if (continuation == null)
            {
                throw new ArgumentNullException(nameof(continuation));
            }
            ValidateToken(token);

            if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
            {
                _executionContext = ExecutionContext.Capture();
            }

            if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
            {
                SynchronizationContext sc = SynchronizationContext.Current;
                if (sc != null && sc.GetType() != typeof(SynchronizationContext))
                {
                    _capturedContext = sc;
                }
                else
                {
                    TaskScheduler ts = TaskScheduler.Current;
                    if (ts != TaskScheduler.Default)
                    {
                        _capturedContext = ts;
                    }
                }
            }

            _continuationState = state;
            if (Interlocked.CompareExchange(ref _continuation, continuation, null) != null)
            {
                _executionContext = null;

                object cc = _capturedContext;
                _capturedContext = null;

                switch (cc)
                {
                    case null:
                        Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                        break;

                    case SynchronizationContext sc:
                        sc.Post(s =>
                        {
                            var tuple = (Tuple<Action<object>, object>)s;
                            tuple.Item1(tuple.Item2);
                        }, Tuple.Create(continuation, state));
                        break;

                    case TaskScheduler ts:
                        Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                        break;
                }
            }
        }

        public void SetResult(TResult result)
        {
            _result = result;
            SignalCompletion();
        }

        public void SetException(Exception error)
        {
            _error = ExceptionDispatchInfo.Capture(error);
            SignalCompletion();
        }

        private void SignalCompletion()
        {
            if (_completed)
            {
                throw new InvalidOperationException();
            }
            _completed = true;

            if (Interlocked.CompareExchange(ref _continuation, s_sentinel, null) != null)
            {
                if (_executionContext != null)
                {
                    ExecutionContext.Run(
                        _executionContext,
                        s => ((IStrongBox<ManualResetValueTaskSourceLogic<TResult>>)s).Value.InvokeContinuation(),
                        _parent ?? throw new InvalidOperationException());
                }
                else
                {
                    InvokeContinuation();
                }
            }
        }

        private void InvokeContinuation()
        {
            object cc = _capturedContext;
            _capturedContext = null;

            switch (cc)
            {
                case null:
                    _continuation(_continuationState);
                    break;

                case SynchronizationContext sc:
                    sc.Post(s =>
                    {
                        ref ManualResetValueTaskSourceLogic<TResult> logicRef = ref ((IStrongBox<ManualResetValueTaskSourceLogic<TResult>>)s).Value;
                        logicRef._continuation(logicRef._continuationState);
                    }, _parent ?? throw new InvalidOperationException());
                    break;

                case TaskScheduler ts:
                    Task.Factory.StartNew(_continuation, _continuationState, CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                    break;
            }
        }
    }
}
";

        [Fact]
        void TestWellKnownMembers()
        {
            var comp = CreateCompilation(s_common, references: new[] { TestReferences.NetStandard20.TasksExtensionsRef }, targetFramework: Roslyn.Test.Utilities.TargetFramework.NetStandard20);
            comp.VerifyDiagnostics();

            var istrongBox = comp.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_IStrongBox_T);
            Assert.Equal("System.Runtime.CompilerServices.IStrongBox<T>", istrongBox.ToTestDisplayString());

            var istrongBox_Value = comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IStrongBox_T__Value);
            Assert.Equal("ref T System.Runtime.CompilerServices.IStrongBox<T>.Value { get; }", istrongBox_Value.ToTestDisplayString());

            var istrongBox_ValueGetter = comp.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_IStrongBox_T__get_Value);
            Assert.Equal("ref T System.Runtime.CompilerServices.IStrongBox<T>.Value.get", istrongBox_ValueGetter.ToTestDisplayString());

            var mrvtsl = comp.GetWellKnownType(WellKnownType.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T);
            Assert.Equal("System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>", mrvtsl.ToTestDisplayString());

            var mrvtsl_Ctor = comp.GetWellKnownTypeMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__ctor);
            Assert.Equal("System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>..ctor(System.Runtime.CompilerServices.IStrongBox<System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>> parent)",
                mrvtsl_Ctor.ToTestDisplayString());

            var mrvtsl_GetResult = comp.GetWellKnownTypeMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__GetResult);
            Assert.Equal("TResult System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.GetResult(System.Int16 token)",
                mrvtsl_GetResult.ToTestDisplayString());

            var mrvtsl_GetStatus = comp.GetWellKnownTypeMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__GetStatus);
            Assert.Equal("System.Threading.Tasks.Sources.ValueTaskSourceStatus System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.GetStatus(System.Int16 token)",
                mrvtsl_GetStatus.ToTestDisplayString());

            var mrvtsl_OnCompleted = comp.GetWellKnownTypeMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__OnCompleted);
            Assert.Equal("void System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.OnCompleted(System.Action<System.Object> continuation, System.Object state, System.Int16 token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)",
                mrvtsl_OnCompleted.ToTestDisplayString());

            var mrvtsl_Reset = comp.GetWellKnownTypeMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__Reset);
            Assert.Equal("void System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.Reset()", mrvtsl_Reset.ToTestDisplayString());

            var mrvtsl_SetResult = comp.GetWellKnownTypeMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__SetResult);
            Assert.Equal("void System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.SetResult(TResult result)",
                mrvtsl_SetResult.ToTestDisplayString());

            var mrvtsl_get_Version = comp.GetWellKnownTypeMember(WellKnownMember.System_Threading_Tasks_ManualResetValueTaskSourceLogic_T__get_Version);
            Assert.Equal("System.Int16 System.Threading.Tasks.ManualResetValueTaskSourceLogic<TResult>.Version.get", mrvtsl_get_Version.ToTestDisplayString());

            var valueTaskSourceStatus = comp.GetWellKnownType(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceStatus);
            Assert.Equal("System.Threading.Tasks.Sources.ValueTaskSourceStatus", valueTaskSourceStatus.ToTestDisplayString());

            var valueTaskSourceOnCompletedFlags = comp.GetWellKnownType(WellKnownType.System_Threading_Tasks_Sources_ValueTaskSourceOnCompletedFlags);
            Assert.Equal("System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags", valueTaskSourceOnCompletedFlags.ToTestDisplayString());

            var ivalueTaskSource = comp.GetWellKnownType(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource_T);
            Assert.Equal("System.Threading.Tasks.Sources.IValueTaskSource<out TResult>", ivalueTaskSource.ToTestDisplayString());

            var ivalueTaskSource_GetResult = comp.GetWellKnownTypeMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetResult);
            Assert.Equal("TResult System.Threading.Tasks.Sources.IValueTaskSource<out TResult>.GetResult(System.Int16 token)", ivalueTaskSource_GetResult.ToTestDisplayString());

            var ivalueTaskSource_GetStatus = comp.GetWellKnownTypeMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__GetStatus);
            Assert.Equal("System.Threading.Tasks.Sources.ValueTaskSourceStatus System.Threading.Tasks.Sources.IValueTaskSource<out TResult>.GetStatus(System.Int16 token)",
                ivalueTaskSource_GetStatus.ToTestDisplayString());

            var ivalueTaskSource_OnCompleted = comp.GetWellKnownTypeMember(WellKnownMember.System_Threading_Tasks_Sources_IValueTaskSource_T__OnCompleted);
            Assert.Equal("void System.Threading.Tasks.Sources.IValueTaskSource<out TResult>.OnCompleted(System.Action<System.Object> continuation, System.Object state, System.Int16 token, System.Threading.Tasks.Sources.ValueTaskSourceOnCompletedFlags flags)",
                ivalueTaskSource_OnCompleted.ToTestDisplayString());

            var valueTask = comp.GetWellKnownType(WellKnownType.System_Threading_Tasks_ValueTask_T);
            Assert.Equal("System.Threading.Tasks.ValueTask<TResult>", valueTask.ToTestDisplayString());

            var valueTask_Ctor = comp.GetWellKnownTypeMember(WellKnownMember.System_Threading_Tasks_ValueTask_T__ctor);
            Assert.Equal("System.Threading.Tasks.ValueTask<TResult>..ctor(System.Threading.Tasks.Sources.IValueTaskSource<TResult> source, System.Int16 token)",
                valueTask_Ctor.ToTestDisplayString());
        }
    }
}
