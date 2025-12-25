// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Metadata.Tools;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    public abstract class CSharpTestBase : CommonTestBase
    {
        public static readonly TheoryData<LanguageVersion> LanguageVersions13AndNewer = new TheoryData<LanguageVersion>([LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersion.CSharp14]);

        protected static readonly string NullableAttributeDefinition = @"
namespace System.Runtime.CompilerServices
{
    [System.AttributeUsage(AttributeTargets.Event | // The type of the event is nullable, or has a nullable reference type as one of its constituents
                    AttributeTargets.Field | // The type of the field is a nullable reference type, or has a nullable reference type as one of its constituents
                    AttributeTargets.GenericParameter | // The generic parameter is a nullable reference type
                    AttributeTargets.Module | // Nullable reference types in this module are annotated by means of NullableAttribute applied to other targets in it
                    AttributeTargets.Parameter | // The type of the parameter is a nullable reference type, or has a nullable reference type as one of its constituents
                    AttributeTargets.ReturnValue | // The return type is a nullable reference type, or has a nullable reference type as one of its constituents
                    AttributeTargets.Property | // The type of the property is a nullable reference type, or has a nullable reference type as one of its constituents
                    AttributeTargets.Class, // Base type has a nullable reference type as one of its constituents
                   AllowMultiple = false)]
    public class NullableAttribute : Attribute
    {
        public NullableAttribute(byte transformFlag) { }
        public NullableAttribute(byte[] transformFlags)
        {
        }
    }
}
";

        protected static readonly string NullableContextAttributeDefinition = @"
namespace System.Runtime.CompilerServices
{
    [System.AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Delegate |
        AttributeTargets.Interface |
        AttributeTargets.Method |
        AttributeTargets.Struct,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class NullableContextAttribute : Attribute
    {
        public readonly byte Flag;
        public NullableContextAttribute(byte flag)
        {
            Flag = flag;
        }
    }
}";

        protected static readonly string NullablePublicOnlyAttributeDefinition = @"
namespace System.Runtime.CompilerServices
{
    [System.AttributeUsage(AttributeTargets.Module, AllowMultiple = false)]
    public sealed class NullablePublicOnlyAttribute : Attribute
    {
        public readonly bool IncludesInternals;
        public NullablePublicOnlyAttribute(bool includesInternals)
        {
            IncludesInternals = includesInternals;
        }
    }
}";

        // Nullable flow analysis attributes are defined at
        // https://github.com/dotnet/coreclr/blob/4a1275434fff99206f2a28f5f0e87f124069eb7f/src/System.Private.CoreLib/shared/System/Diagnostics/CodeAnalysis/NullableAttributes.cs
        protected static readonly string AllowNullAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property)]
    public sealed class AllowNullAttribute : Attribute
    {
    }
}";

        protected static readonly string DisallowNullAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property)]
    public sealed class DisallowNullAttribute : Attribute
    {
    }
}";

        protected static readonly string MaybeNullAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue)]
    public sealed class MaybeNullAttribute : Attribute
    {
    }
}
";

        protected static readonly string MaybeNullWhenAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class MaybeNullWhenAttribute : Attribute
    {
        public MaybeNullWhenAttribute(bool when) { }
    }
}
";

        protected static readonly string NotNullAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue)]
    public sealed class NotNullAttribute : Attribute
    {
    }
}
";

        protected static readonly string NotNullWhenAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class NotNullWhenAttribute : Attribute
    {
        public NotNullWhenAttribute(bool when) { }
    }
}
";

        protected static readonly string MemberNotNullAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class MemberNotNullAttribute : Attribute
    {
        public MemberNotNullAttribute(params string[] members) { }
        public MemberNotNullAttribute(string member) { }
    }
}
";

        protected static readonly string MemberNotNullWhenAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class MemberNotNullWhenAttribute : Attribute
    {
        public MemberNotNullWhenAttribute(bool when, params string[] members) { }
        public MemberNotNullWhenAttribute(bool when, string member) { }
    }
}
";

        protected static readonly string DoesNotReturnIfAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class DoesNotReturnIfAttribute : Attribute
    {
        public DoesNotReturnIfAttribute(bool condition) { }
    }
}
";

        protected static readonly string DoesNotReturnAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DoesNotReturnAttribute : Attribute
    {
        public DoesNotReturnAttribute() { }
    }
}
";

        protected static readonly string NotNullIfNotNullAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    public sealed class NotNullIfNotNullAttribute : Attribute
    {
        public NotNullIfNotNullAttribute(string parameterName) { }
    }
}
";

        protected static readonly string CallerArgumentExpressionAttributeDefinition = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
                public sealed class CallerArgumentExpressionAttribute : Attribute
                {
                    public CallerArgumentExpressionAttribute(string parameterName)
                    {
                        ParameterName = parameterName;
                    }

                    public string ParameterName { get; }
                }
            }
            """;

        protected static readonly string IsExternalInitTypeDefinition = @"
namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit
    {
    }
}
";

        protected static readonly string IAsyncDisposableDefinition = @"
namespace System
{
    public interface IAsyncDisposable
    {
       System.Threading.Tasks.ValueTask DisposeAsync();
    }
}
";

        protected static readonly string NonDisposableAsyncEnumeratorDefinition = @"
#nullable disable

namespace System.Collections.Generic
{
    public interface IAsyncEnumerator<out T>
    {
        System.Threading.Tasks.ValueTask<bool> MoveNextAsync();
        T Current { get; }
    }
}
";

        protected static readonly string DisposableAsyncEnumeratorDefinition = @"
#nullable disable

namespace System.Collections.Generic
{
    public interface IAsyncEnumerator<out T> : System.IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask<bool> MoveNextAsync();
        T Current { get; }
    }
}
" + IAsyncDisposableDefinition;

        protected static readonly string CommonAsyncStreamsTypes = @"
#nullable disable

namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken token = default);
    }
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class AsyncIteratorStateMachineAttribute : StateMachineAttribute
    {
        public AsyncIteratorStateMachineAttribute(Type stateMachineType) : base(stateMachineType)
        {
        }
    }
}

namespace System.Threading.Tasks.Sources
{
    // From https://github.com/dotnet/runtime/blob/f580068aa93fb3c6d5fbc7e33f6cd7d52fa86b24/src/libraries/Microsoft.Bcl.AsyncInterfaces/src/System/Threading/Tasks/Sources/ManualResetValueTaskSourceCore.cs
    using System.Diagnostics;
    using System.Runtime.ExceptionServices;
    using System.Runtime.InteropServices;

    /// <summary>Provides the core logic for implementing a manual-reset <see cref=""IValueTaskSource""/> or <see cref=""IValueTaskSource{TResult}""/>.</summary>
    /// <typeparam name=""TResult""></typeparam>
    [StructLayout(LayoutKind.Auto)]
    public struct ManualResetValueTaskSourceCore<TResult>
    {
        /// <summary>
        /// The callback to invoke when the operation completes if <see cref=""OnCompleted""/> was called before the operation completed,
        /// or <see cref=""ManualResetValueTaskSourceCoreShared.s_sentinel""/> if the operation completed before a callback was supplied,
        /// or null if a callback hasn't yet been provided and the operation hasn't yet completed.
        /// </summary>
        private Action<object> _continuation;
        /// <summary>State to pass to <see cref=""_continuation""/>.</summary>
        private object _continuationState;
        /// <summary><see cref=""ExecutionContext""/> to flow to the callback, or null if no flowing is required.</summary>
        private ExecutionContext _executionContext;
        /// <summary>
        /// A ""captured"" <see cref=""SynchronizationContext""/> or <see cref=""TaskScheduler""/> with which to invoke the callback,
        /// or null if no special context is required.
        /// </summary>
        private object _capturedContext;
        /// <summary>Whether the current operation has completed.</summary>
        private bool _completed;
        /// <summary>The result with which the operation succeeded, or the default value if it hasn't yet completed or failed.</summary>
        private TResult _result;
        /// <summary>The exception with which the operation failed, or null if it hasn't yet completed or completed successfully.</summary>
        private ExceptionDispatchInfo _error;
        /// <summary>The current version of this value, used to help prevent misuse.</summary>
        private short _version;

        /// <summary>Gets or sets whether to force continuations to run asynchronously.</summary>
        /// <remarks>Continuations may run asynchronously if this is false, but they'll never run synchronously if this is true.</remarks>
        public bool RunContinuationsAsynchronously { get; set; }

        /// <summary>Resets to prepare for the next operation.</summary>
        public void Reset()
        {
            // Reset/update state for the next use/await of this instance.
            _version++;
            _completed = false;
            _result = default;
            _error = null;
            _executionContext = null;
            _capturedContext = null;
            _continuation = null;
            _continuationState = null;
        }

        /// <summary>Completes with a successful result.</summary>
        /// <param name=""result"">The result.</param>
        public void SetResult(TResult result)
        {
            _result = result;
            SignalCompletion();
        }

        /// <summary>Complets with an error.</summary>
        /// <param name=""error""></param>
        public void SetException(Exception error)
        {
            _error = ExceptionDispatchInfo.Capture(error);
            SignalCompletion();
        }

        /// <summary>Gets the operation version.</summary>
        public short Version => _version;

        /// <summary>Gets the status of the operation.</summary>
        /// <param name=""token"">Opaque value that was provided to the <see cref=""ValueTask""/>'s constructor.</param>
        public ValueTaskSourceStatus GetStatus(short token)
        {
            ValidateToken(token);
            return
                _continuation == null || !_completed ? ValueTaskSourceStatus.Pending :
                _error == null ? ValueTaskSourceStatus.Succeeded :
                _error.SourceException is OperationCanceledException ? ValueTaskSourceStatus.Canceled :
                ValueTaskSourceStatus.Faulted;
        }

        /// <summary>Gets the result of the operation.</summary>
        /// <param name=""token"">Opaque value that was provided to the <see cref=""ValueTask""/>'s constructor.</param>
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

        /// <summary>Schedules the continuation action for this operation.</summary>
        /// <param name=""continuation"">The continuation to invoke when the operation has completed.</param>
        /// <param name=""state"">The state object to pass to <paramref name=""continuation""/> when it's invoked.</param>
        /// <param name=""token"">Opaque value that was provided to the <see cref=""ValueTask""/>'s constructor.</param>
        /// <param name=""flags"">The flags describing the behavior of the continuation.</param>
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

            // We need to set the continuation state before we swap in the delegate, so that
            // if there's a race between this and SetResult/Exception and SetResult/Exception
            // sees the _continuation as non-null, it'll be able to invoke it with the state
            // stored here.  However, this also means that if this is used incorrectly (e.g.
            // awaited twice concurrently), _continuationState might get erroneously overwritten.
            // To minimize the chances of that, we check preemptively whether _continuation
            // is already set to something other than the completion sentinel.

            object oldContinuation = _continuation;
            if (oldContinuation == null)
            {
                _continuationState = state;
                oldContinuation = Interlocked.CompareExchange(ref _continuation, continuation, null);
            }

            if (oldContinuation != null)
            {
                // Operation already completed, so we need to queue the supplied callback.
                if (!ReferenceEquals(oldContinuation, ManualResetValueTaskSourceCoreShared.s_sentinel))
                {
                    throw new InvalidOperationException();
                }

                switch (_capturedContext)
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

        /// <summary>Ensures that the specified token matches the current version.</summary>
        /// <param name=""token"">The token supplied by <see cref=""ValueTask""/>.</param>
        private void ValidateToken(short token)
        {
            if (token != _version)
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>Signals that the operation has completed.  Invoked after the result or error has been set.</summary>
        private void SignalCompletion()
        {
            if (_completed)
            {
                throw new InvalidOperationException();
            }
            _completed = true;

            if (_continuation != null || Interlocked.CompareExchange(ref _continuation, ManualResetValueTaskSourceCoreShared.s_sentinel, null) != null)
            {
                if (_executionContext != null)
                {
                    ExecutionContext.Run(
                        _executionContext,
                        s => ((ManualResetValueTaskSourceCore<TResult>)s).InvokeContinuation(),
                        this);
                }
                else
                {
                    InvokeContinuation();
                }
            }
        }

        /// <summary>
        /// Invokes the continuation with the appropriate captured context / scheduler.
        /// This assumes that if <see cref=""_executionContext""/> is not null we're already
        /// running within that <see cref=""ExecutionContext""/>.
        /// </summary>
        private void InvokeContinuation()
        {
            Debug.Assert(_continuation != null);

            switch (_capturedContext)
            {
                case null:
                    if (RunContinuationsAsynchronously)
                    {
                        Task.Factory.StartNew(_continuation, _continuationState, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                    }
                    else
                    {
                        _continuation(_continuationState);
                    }
                    break;

                case SynchronizationContext sc:
                    sc.Post(s =>
                    {
                        var state = (Tuple<Action<object>, object>)s;
                        state.Item1(state.Item2);
                    }, Tuple.Create(_continuation, _continuationState));
                    break;

                case TaskScheduler ts:
                    Task.Factory.StartNew(_continuation, _continuationState, CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                    break;
            }
        }
    }

    internal static class ManualResetValueTaskSourceCoreShared // separated out of generic to avoid unnecessary duplication
    {
        internal static readonly Action<object> s_sentinel = CompletionSentinel;
        private static void CompletionSentinel(object _) // named method to aid debugging
        {
            // Instrumented with FailFast to investigate CI failure:
            // https://github.com/dotnet/roslyn/issues/34207
            System.Environment.FailFast(""The sentinel delegate should never be invoked."");
            Debug.Fail(""The sentinel delegate should never be invoked."");
            throw new InvalidOperationException();
        }
    }
}

namespace System.Runtime.CompilerServices
{
    using System.Runtime.InteropServices;

    /// <summary>Represents a builder for asynchronous iterators.</summary>
    [StructLayout(LayoutKind.Auto)]
    public struct AsyncIteratorMethodBuilder
    {
        // AsyncIteratorMethodBuilder is used by the language compiler as part of generating
        // async iterators. For now, the implementation just wraps AsyncTaskMethodBuilder, as
        // most of the logic is shared.  However, in the future this could be changed and
        // optimized.  For example, we do need to allocate an object (once) to flow state like
        // ExecutionContext, which AsyncTaskMethodBuilder handles, but it handles it by
        // allocating a Task-derived object.  We could optimize this further by removing
        // the Task from the hierarchy, but in doing so we'd also lose a variety of optimizations
        // related to it, so we'd need to replicate all of those optimizations (e.g. storing
        // that box object directly into a Task's continuation field).

        private AsyncTaskMethodBuilder _methodBuilder; // mutable struct; do not make it readonly

        public static AsyncIteratorMethodBuilder Create() =>
            new AsyncIteratorMethodBuilder() { _methodBuilder = AsyncTaskMethodBuilder.Create() };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MoveNext<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine =>
            _methodBuilder.Start(ref stateMachine);

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            _methodBuilder.AwaitOnCompleted(ref awaiter, ref stateMachine);

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            _methodBuilder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        /// <summary>Marks iteration as being completed, whether successfully or otherwise.</summary>
        public void Complete() => _methodBuilder.SetResult();
    }
}
";

        public static readonly string AsyncStreamsTypes = DisposableAsyncEnumeratorDefinition + CommonAsyncStreamsTypes;

        protected static readonly string EnumeratorCancellationAttributeType = @"
namespace System.Runtime.CompilerServices
{
    [System.AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class EnumeratorCancellationAttribute : Attribute
    {
        public EnumeratorCancellationAttribute() { }
    }
}
";

        protected static readonly string NativeIntegerAttributeDefinition =
@"using System.Collections.Generic;
namespace System.Runtime.CompilerServices
{
    [System.AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Event |
        AttributeTargets.Field |
        AttributeTargets.GenericParameter |
        AttributeTargets.Parameter |
        AttributeTargets.Property |
        AttributeTargets.ReturnValue,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class NativeIntegerAttribute : Attribute
    {
        public NativeIntegerAttribute()
        {
            TransformFlags = new[] { true };
        }
        public NativeIntegerAttribute(bool[] flags)
        {
            TransformFlags = flags;
        }
        public readonly IList<bool> TransformFlags;
    }
}";

        protected static readonly string UnmanagedCallersOnlyAttributeDefinition =
@"namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class UnmanagedCallersOnlyAttribute : Attribute
    {
        public UnmanagedCallersOnlyAttribute() { }
        public Type[] CallConvs;
        public string EntryPoint;
    }
}";

        protected static readonly string UnscopedRefAttributeDefinition =
@"namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public sealed class UnscopedRefAttribute : Attribute
    {
    }
}";

        protected static readonly string RefSafetyRulesAttributeDefinition =
@"namespace System.Runtime.CompilerServices
{
    public sealed class RefSafetyRulesAttribute : Attribute
    {
        public RefSafetyRulesAttribute(int version) { Version = version; }
        public int Version;
    }
}";

        protected static MetadataReference RefSafetyRulesAttributeLib =>
            CreateCompilation(RefSafetyRulesAttributeDefinition).EmitToImageReference();

        protected static readonly string RequiredMemberAttribute = @"
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class RequiredMemberAttribute : Attribute
    {
        public RequiredMemberAttribute()
        {
        }
    }
}
";

        protected static readonly string SetsRequiredMembersAttribute = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
    public sealed class SetsRequiredMembersAttribute : Attribute
    {
        public SetsRequiredMembersAttribute()
        {
        }
    }
}
";

        internal static readonly string CompilerFeatureRequiredAttribute = """
            #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
                public sealed class CompilerFeatureRequiredAttribute : Attribute
                {
                    public CompilerFeatureRequiredAttribute(string featureName)
                    {
                        FeatureName = featureName;
                    }
                    public string FeatureName { get; }
                    public bool IsOptional { get; set; }
                }
            }

            #pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
            """;

        internal static readonly string CompilerFeatureRequiredAttributeIL = @"
.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute
     extends [mscorlib]System.Attribute
 {
     .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
         01 00 ff 7f 00 00 02 00 54 02 0d 41 6c 6c 6f 77
         4d 75 6c 74 69 70 6c 65 01 54 02 09 49 6e 68 65
         72 69 74 65 64 00
     )
     // Fields
     .field private initonly string '<FeatureName>k__BackingField'
     .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
         01 00 00 00
     )
     .field private initonly bool '<IsOptional>k__BackingField'
     .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
         01 00 00 00
     )

     .field public static literal string RefStructs = ""RefStructs""
     .field public static literal string RequiredMembers = ""RequiredMembers""
 
     // Methods
     .method public hidebysig specialname rtspecialname 
         instance void .ctor (
             string featureName
         ) cil managed 
     {
         ldarg.0
         call instance void [mscorlib]System.Attribute::.ctor()
         ldarg.0
         ldarg.1
         stfld string System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::'<FeatureName>k__BackingField'
         ret
     } // end of method CompilerFeatureRequiredAttribute::.ctor
 
     .method public hidebysig specialname 
         instance string get_FeatureName () cil managed 
     {
         ldarg.0
         ldfld string System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::'<FeatureName>k__BackingField'
         ret
     } // end of method CompilerFeatureRequiredAttribute::get_FeatureName
 
     .method public hidebysig specialname 
         instance bool get_IsOptional () cil managed 
     {
         ldarg.0
         ldfld bool System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::'<IsOptional>k__BackingField'
         ret
     } // end of method CompilerFeatureRequiredAttribute::get_IsOptional
 
     .method public hidebysig specialname 
         instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit) set_IsOptional (
             bool 'value'
         ) cil managed 
     {
         ldarg.0
         ldarg.1
         stfld bool System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::'<IsOptional>k__BackingField'
         ret
     } // end of method CompilerFeatureRequiredAttribute::set_IsOptional
 
     // Properties
     .property instance string FeatureName()
     {
         .get instance string System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::get_FeatureName()
     }
     .property instance bool IsOptional()
     {
         .get instance bool System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::get_IsOptional()
         .set instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit) System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::set_IsOptional(bool)
     }
 
 } // end of class System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute
";

        internal static readonly string CollectionBuilderAttributeDefinition = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
                public sealed class CollectionBuilderAttribute : Attribute
                {
                    public CollectionBuilderAttribute(Type builderType, string methodName) { }
                }
            }
            """;

        internal static readonly string OverloadResolutionPriorityAttributeDefinition = """
            namespace System.Runtime.CompilerServices;

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
            public sealed class OverloadResolutionPriorityAttribute(int priority) : Attribute
            {
                public int Priority => priority;
            }
            """;

        internal static readonly string OverloadResolutionPriorityAttributeILDefinition = """
            .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute
                extends [mscorlib]System.Attribute
            {
                .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
                    01 00 e0 00 00 00 02 00 54 02 0d 41 6c 6c 6f 77
                    4d 75 6c 74 69 70 6c 65 00 54 02 09 49 6e 68 65
                    72 69 74 65 64 00
                )
                .field private int32 '<priority>P'
                .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                    01 00 00 00
                )
                .method public hidebysig specialname rtspecialname 
                    instance void .ctor (
                        int32 priority
                    ) cil managed 
                {
                    ldarg.0
                    ldarg.1
                    stfld int32 System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::'<priority>P'
                    ldarg.0
                    call instance void [mscorlib]System.Attribute::.ctor()
                    ret
                }
                .method public hidebysig specialname 
                    instance int32 get_Priority () cil managed 
                {
                    ldarg.0
                    ldfld int32 System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::'<priority>P'
                    ret
                }
                .property instance int32 Priority()
                {
                    .get instance int32 System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute::get_Priority()
                }
            }
            """;

        /// <summary>
        /// The shape of the attribute comes from https://github.com/dotnet/runtime/issues/103430
        /// </summary>
        internal static readonly string CompilerLoweringPreserveAttributeDefinition = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                public class CompilerLoweringPreserveAttribute : Attribute
                {
                    public CompilerLoweringPreserveAttribute() { }
                }
            }
            """;

        internal static readonly string ExtensionMarkerAttributeDefinition = """
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false)]
    public sealed class ExtensionMarkerAttribute : Attribute
    {
        public ExtensionMarkerAttribute(string name)
            => Name = name;

        public string Name { get; }
    }
}
""";

        internal static readonly string ExtensionMarkerAttributeIL = """

.class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.ExtensionMarkerAttribute
    extends [mscorlib]System.Attribute
{
    .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
        01 00 ff 7f 00 00 01 00 54 02 09 49 6e 68 65 72
        69 74 65 64 00
    )

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            string name
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: nop
        IL_0008: ret
    }
}
""";

        #region A string containing expression-tree dumping utilities
        protected static readonly string ExpressionTestLibrary = """
using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;

public class TestBase
{
    protected static void DCheck<T>(Expression<T> e, string expected) { Check(e.Dump(), expected); }
    protected static void Check<T>(Expression<Func<T>> e, string expected) { Check(e.Dump(), expected); }
    protected static void Check<T1, T2>(Expression<Func<T1, T2>> e, string expected) { Check(e.Dump(), expected); }
    protected static void Check<T1, T2, T3>(Expression<Func<T1, T2, T3>> e, string expected) { Check(e.Dump(), expected); }
    protected static void Check<T1, T2, T3, T4>(Expression<Func<T1, T2, T3, T4>> e, string expected) { Check(e.Dump(), expected); }
    protected static string ToString<T>(Expression<Func<T>> e) { return e.Dump(); }
    protected static string ToString<T1, T2>(Expression<Func<T1, T2>> e) { return e.Dump(); }
    protected static string ToString<T1, T2, T3>(Expression<Func<T1, T2, T3>> e) { return e.Dump(); }
    private static void Check(string actual, string expected)
    {
        if (expected != actual)
        {
            Console.WriteLine("FAIL");
            Console.WriteLine("expected: " + expected);
            Console.WriteLine("actual:   " + actual);
        }
    }
}

public static class ExpressionExtensions
{
    public static string Dump<T>(this Expression<T> self)
    {
        return ExpressionPrinter.Print(self.Body);
    }
}

class ExpressionPrinter : System.Linq.Expressions.ExpressionVisitor
{
    private StringBuilder s = new StringBuilder();

    public static string Print(Expression e)
    {
        var p = new ExpressionPrinter();
        p.Visit(e);
        return p.s.ToString();
    }

    public override Expression Visit(Expression node)
    {
        if (node == null) { s.Append("null"); return null; }
        s.Append(node.NodeType.ToString());
        s.Append("(");
        base.Visit(node);
        s.Append(" Type:" + node.Type);
        s.Append(")");
        return null;
    }

    protected override MemberBinding VisitMemberBinding(MemberBinding node)
    {
        if (node == null) { s.Append("null"); return null; }
        return base.VisitMemberBinding(node);
    }

    protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
    {
        s.Append("MemberMemberBinding(Member=");
        s.Append(node.Member.ToString());
        foreach (var b in node.Bindings)
        {
            s.Append(" ");
            VisitMemberBinding(b);
        }
        s.Append(")");
        return null;
    }

    protected override MemberListBinding VisitMemberListBinding(MemberListBinding node)
    {
        s.Append("MemberListBinding(Member=");
        s.Append(node.Member.ToString());
        foreach (var i in node.Initializers)
        {
            s.Append(" ");
            VisitElementInit(i);
        }
        s.Append(")");
        return null;
    }

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        s.Append("MemberAssignment(Member=");
        s.Append(node.Member.ToString());
        s.Append(" Expression=");
        Visit(node.Expression);
        s.Append(")");
        return null;
    }

    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        s.Append("NewExpression: ");
        Visit(node.NewExpression);
        s.Append(" Bindings:[");
        bool first = true;
        foreach (var b in node.Bindings)
        {
            if (!first) s.Append(" ");
            VisitMemberBinding(b);
            first = false;
        }
        s.Append("]");
        return null;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        Visit(node.Left);
        s.Append(" ");
        Visit(node.Right);
        if (node.Conversion != null)
        {
            s.Append(" Conversion:");
            Visit(node.Conversion);
        }
        if (node.IsLifted) s.Append(" Lifted");
        if (node.IsLiftedToNull) s.Append(" LiftedToNull");
        if (node.Method != null) s.Append(" Method:[" + node.Method + "]");
        return null;
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        Visit(node.Test);
        s.Append(" ? ");
        Visit(node.IfTrue);
        s.Append(" : ");
        Visit(node.IfFalse);
        return null;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        s.Append(node.Value == null ? "null" : GetCultureInvariantString(node.Value));
        return null;
    }

    protected override Expression VisitDefault(DefaultExpression node)
    {
        return null;
    }

    protected override Expression VisitIndex(IndexExpression node)
    {
        Visit(node.Object);
        s.Append("[");
        int n = node.Arguments.Count;
        for (int i = 0; i < n; i++)
        {
            if (i != 0) s.Append(" ");
            Visit(node.Arguments[i]);
        }
        s.Append("]");
        if (node.Indexer != null) s.Append(" Indexer:" + node.Indexer);
        return null;
    }

    protected override Expression VisitInvocation(InvocationExpression node)
    {
        Visit(node.Expression);
        s.Append("(");
        int n = node.Arguments.Count;
        for (int i = 0; i < n; i++)
        {
            if (i != 0) s.Append(" ");
            Visit(node.Arguments[i]);
        }
        s.Append(")");
        return null;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        s.Append("(");
        int n = node.Parameters.Count;
        for (int i = 0; i < n; i++)
        {
            if (i != 0) s.Append(" ");
            Visit(node.Parameters[i]);
        }
        s.Append(") => ");
        if (node.Name != null) s.Append(node.Name);
        Visit(node.Body);
        if (node.ReturnType != null) s.Append(" ReturnType:" + node.ReturnType);
        if (node.TailCall) s.Append(" TailCall");
        return null;
    }

    protected override Expression VisitListInit(ListInitExpression node)
    {
        Visit(node.NewExpression);
        s.Append("{");
        int n = node.Initializers.Count;
        for (int i = 0; i < n; i++)
        {
            if (i != 0) s.Append(" ");
            Visit(node.Initializers[i]);
        }
        s.Append("}");
        return null;
    }

    protected override ElementInit VisitElementInit(ElementInit node)
    {
        Visit(node);
        return null;
    }

    private void Visit(ElementInit node)
    {
        s.Append("ElementInit(");
        s.Append(node.AddMethod);
        int n = node.Arguments.Count;
        for (int i = 0; i < n; i++)
        {
            s.Append(" ");
            Visit(node.Arguments[i]);
        }
        s.Append(")");
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        Visit(node.Expression);
        s.Append(".");
        s.Append(node.Member.Name);
        return null;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        Visit(node.Object);
        s.Append(".[" + node.Method + "]");
        s.Append("(");
        int n = node.Arguments.Count;
        for (int i = 0; i < n; i++)
        {
            if (i != 0) s.Append(", ");
            Visit(node.Arguments[i]);
        }
        s.Append(")");
        return null;
    }

    protected override Expression VisitNew(NewExpression node)
    {
        s.Append((node.Constructor != null) ? "[" + node.Constructor + "]" : "<.ctor>");
        s.Append("(");
        int n = node.Arguments.Count;
        for (int i = 0; i < n; i++)
        {
            if (i != 0) s.Append(", ");
            Visit(node.Arguments[i]);
        }
        s.Append(")");
        if (node.Members != null)
        {
            n = node.Members.Count;
            if (n != 0)
            {
                s.Append("{");
                for (int i = 0; i < n; i++)
                {
                    var info = node.Members[i];
                    if (i != 0) s.Append(" ");
                    s.Append(info);
                }
                s.Append("}");
            }
        }
        return null;
    }

    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        s.Append("[");
        int n = node.Expressions.Count;
        for (int i = 0; i < n; i++)
        {
            if (i != 0) s.Append(" ");
            Visit(node.Expressions[i]);
        }
        s.Append("]");
        return null;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        s.Append(node.Name);
        if (node.IsByRef) s.Append(" ByRef");
        return null;
    }

    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        Visit(node.Expression);
        s.Append(" TypeOperand:" + node.TypeOperand);
        return null;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        Visit(node.Operand);
        if (node.IsLifted) s.Append(" Lifted");
        if (node.IsLiftedToNull) s.Append(" LiftedToNull");
        if (node.Method != null) s.Append(" Method:[" + node.Method + "]");
        return null;
    }

    public static string GetCultureInvariantString(object value)
    {
        var valueType = value.GetType();
        if (valueType == typeof(string))
        {
            return value as string;
        }

        if (valueType == typeof(DateTime))
        {
            return ((DateTime)value).ToString("M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);
        }

        if (valueType == typeof(float))
        {
            return ((float)value).ToString(CultureInfo.InvariantCulture);
        }

        if (valueType == typeof(double))
        {
            return ((double)value).ToString(CultureInfo.InvariantCulture);
        }

        if (valueType == typeof(decimal))
        {
            return ((decimal)value).ToString(CultureInfo.InvariantCulture);
        }

        return value.ToString();
    }
}
""";
        #endregion A string containing expression-tree dumping utilities

        internal const string RuntimeAsyncAwaitHelpers = """
            namespace System.Runtime.CompilerServices
            {
                public static class AsyncHelpers
                {
                    public static void AwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion
                    {}
                    public static void UnsafeAwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
                    {}

                    public static void Await(System.Threading.Tasks.Task task) => task.GetAwaiter().GetResult();
                    public static void Await(System.Threading.Tasks.ValueTask task) => task.GetAwaiter().GetResult();
                    public static T Await<T>(System.Threading.Tasks.Task<T> task) => task.GetAwaiter().GetResult();
                    public static T Await<T>(System.Threading.Tasks.ValueTask<T> task) => task.GetAwaiter().GetResult();
                }
            }
            """;

        internal const string RuntimeAsyncMethodGenerationAttributeDefinition = """
            namespace System.Runtime.CompilerServices;

            [AttributeUsage(AttributeTargets.Method)]
            public class RuntimeAsyncMethodGenerationAttribute(bool runtimeAsync) : Attribute();
            """;

        protected static T GetSyntax<T>(SyntaxTree tree, string text)
            where T : notnull
        {
            return GetSyntaxes<T>(tree, text).Single();
        }

        protected static IEnumerable<T> GetSyntaxes<T>(SyntaxTree tree, string text)
            where T : notnull
        {
            return tree.GetRoot().DescendantNodes().OfType<T>().Where(e => e.ToString() == text);
        }

        protected static CSharpCompilationOptions WithNullableEnable(CSharpCompilationOptions? options = null)
        {
            return WithNullable(options, NullableContextOptions.Enable);
        }

        protected static CSharpCompilationOptions WithNullableDisable(CSharpCompilationOptions? options = null)
        {
            return WithNullable(options, NullableContextOptions.Disable);
        }

        protected static CSharpCompilationOptions WithNullable(NullableContextOptions nullableContextOptions)
        {
            return WithNullable(null, nullableContextOptions);
        }

        protected static CSharpCompilationOptions WithNullable(CSharpCompilationOptions? options, NullableContextOptions nullableContextOptions)
        {
            return (options ?? TestOptions.ReleaseDll).WithNullableContextOptions(nullableContextOptions);
        }

        internal CompilationVerifier CompileAndVerifyWithMscorlib40(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references = null,
            IEnumerable<ResourceDescription>? manifestResources = null,
            IEnumerable<ModuleData>? dependencies = null,
            Action<ModuleSymbol>? sourceSymbolValidator = null,
            Action<PEAssembly>? assemblyValidator = null,
            Action<ModuleSymbol>? symbolValidator = null,
            SignatureDescription[]? expectedSignatures = null,
            string? expectedOutput = null,
            bool trimOutput = true,
            int? expectedReturnCode = null,
            string[]? args = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            EmitOptions? emitOptions = null,
            Verification verify = default) =>
            CompileAndVerify(
                source,
                references,
                manifestResources,
                dependencies,
                sourceSymbolValidator,
                assemblyValidator,
                symbolValidator,
                expectedSignatures,
                expectedOutput,
                trimOutput,
                expectedReturnCode,
                args,
                options,
                parseOptions,
                emitOptions,
                TargetFramework.Mscorlib40,
                verify);

        internal CompilationVerifier CompileAndVerifyWithMscorlib46(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references = null,
            IEnumerable<ResourceDescription>? manifestResources = null,
            IEnumerable<ModuleData>? dependencies = null,
            Action<ModuleSymbol>? sourceSymbolValidator = null,
            Action<PEAssembly>? assemblyValidator = null,
            Action<ModuleSymbol>? symbolValidator = null,
            SignatureDescription[]? expectedSignatures = null,
            string? expectedOutput = null,
            bool trimOutput = true,
            int? expectedReturnCode = null,
            string[]? args = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            EmitOptions? emitOptions = null,
            Verification verify = default) =>
            CompileAndVerify(
                source,
                references,
                manifestResources,
                dependencies,
                sourceSymbolValidator,
                assemblyValidator,
                symbolValidator,
                expectedSignatures,
                expectedOutput,
                trimOutput,
                expectedReturnCode,
                args,
                options,
                parseOptions,
                emitOptions,
                TargetFramework.Mscorlib46,
                verify);

        internal CompilationVerifier CompileAndVerifyExperimental(
            CSharpTestSource source,
            MessageID feature,
            IEnumerable<MetadataReference>? references = null,
            IEnumerable<ResourceDescription>? manifestResources = null,
            IEnumerable<ModuleData>? dependencies = null,
            Action<ModuleSymbol>? sourceSymbolValidator = null,
            Action<PEAssembly>? assemblyValidator = null,
            Action<ModuleSymbol>? symbolValidator = null,
            SignatureDescription[]? expectedSignatures = null,
            string? expectedOutput = null,
            bool trimOutput = true,
            int? expectedReturnCode = null,
            string[]? args = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            EmitOptions? emitOptions = null,
            Verification verify = default)
        {
            options = options ?? TestOptions.ReleaseDll.WithOutputKind((expectedOutput != null) ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary);
            var compilation = CreateExperimentalCompilationWithMscorlib461(source, feature, references, options, parseOptions, assemblyName: GetUniqueName());

            return CompileAndVerify(
                source,
                references,
                manifestResources,
                dependencies,
                sourceSymbolValidator,
                assemblyValidator,
                symbolValidator,
                expectedSignatures,
                expectedOutput,
                trimOutput,
                expectedReturnCode,
                args,
                options,
                parseOptions,
                emitOptions,
                TargetFramework.Mscorlib46,
                verify);
        }

        internal CompilationVerifier CompileAndVerifyWithWinRt(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references = null,
            IEnumerable<ResourceDescription>? manifestResources = null,
            IEnumerable<ModuleData>? dependencies = null,
            Action<ModuleSymbol>? sourceSymbolValidator = null,
            Action<PEAssembly>? assemblyValidator = null,
            Action<ModuleSymbol>? symbolValidator = null,
            SignatureDescription[]? expectedSignatures = null,
            string? expectedOutput = null,
            bool trimOutput = true,
            int? expectedReturnCode = null,
            string[]? args = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            EmitOptions? emitOptions = null,
            Verification verify = default) =>
            CompileAndVerify(
                source,
                references,
                manifestResources,
                dependencies,
                sourceSymbolValidator,
                assemblyValidator,
                symbolValidator,
                expectedSignatures,
                expectedOutput,
                trimOutput,
                expectedReturnCode,
                args,
                options,
                parseOptions,
                emitOptions,
                TargetFramework.WinRT,
                verify);

        internal CompilationVerifier CompileAndVerifyWithCSharp(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references = null,
            IEnumerable<ResourceDescription>? manifestResources = null,
            IEnumerable<ModuleData>? dependencies = null,
            Action<ModuleSymbol>? sourceSymbolValidator = null,
            Action<PEAssembly>? assemblyValidator = null,
            Action<ModuleSymbol>? symbolValidator = null,
            SignatureDescription[]? expectedSignatures = null,
            string? expectedOutput = null,
            bool trimOutput = true,
            int? expectedReturnCode = null,
            string[]? args = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            EmitOptions? emitOptions = null,
            Verification verify = default) =>
            CompileAndVerify(
                source,
                references,
                manifestResources,
                dependencies,
                sourceSymbolValidator,
                assemblyValidator,
                symbolValidator,
                expectedSignatures,
                expectedOutput,
                trimOutput,
                expectedReturnCode,
                args,
                options,
                parseOptions,
                emitOptions,
                TargetFramework.StandardAndCSharp,
                verify);

        internal CompilationVerifier CompileAndVerify(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references = null,
            IEnumerable<ResourceDescription>? manifestResources = null,
            IEnumerable<ModuleData>? dependencies = null,
            Action<ModuleSymbol>? sourceSymbolValidator = null,
            Action<PEAssembly>? assemblyValidator = null,
            Action<ModuleSymbol>? symbolValidator = null,
            SignatureDescription[]? expectedSignatures = null,
            string? expectedOutput = null,
            bool trimOutput = true,
            int? expectedReturnCode = null,
            string[]? args = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            EmitOptions? emitOptions = null,
            TargetFramework targetFramework = TargetFramework.Standard,
            Verification verify = default)
        {
            options = options ?? (expectedOutput != null ? TestOptions.ReleaseExe : CheckForTopLevelStatements(source.GetSyntaxTrees(parseOptions)));
            var compilation = CreateCompilation(source, references, options, parseOptions, targetFramework, assemblyName: GetUniqueName());
            return CompileAndVerify(
                compilation,
                manifestResources,
                dependencies,
                sourceSymbolValidator,
                assemblyValidator,
                symbolValidator,
                expectedSignatures,
                expectedOutput,
                trimOutput,
                expectedReturnCode,
                args,
                emitOptions,
                verify);
        }

        internal CompilationVerifier CompileAndVerify(
            Compilation compilation,
            IEnumerable<ResourceDescription>? manifestResources = null,
            IEnumerable<ModuleData>? dependencies = null,
            Action<ModuleSymbol>? sourceSymbolValidator = null,
            Action<PEAssembly>? validator = null,
            Action<ModuleSymbol>? symbolValidator = null,
            SignatureDescription[]? expectedSignatures = null,
            string? expectedOutput = null,
            bool trimOutput = true,
            int? expectedReturnCode = null,
            string[]? args = null,
            EmitOptions? emitOptions = null,
            Verification verify = default)
        {
            Action<IModuleSymbol>? translate(Action<ModuleSymbol>? action)
            {
                if (action != null)
                {
                    return (m) => action(m.GetSymbol<ModuleSymbol>());
                }
                else
                {
                    return null;
                }
            }

            return CompileAndVerifyCommon(
                compilation,
                manifestResources,
                dependencies,
                translate(sourceSymbolValidator),
                validator,
                translate(symbolValidator),
                expectedSignatures,
                expectedOutput,
                trimOutput,
                expectedReturnCode,
                args,
                emitOptions,
                verify);
        }

        internal CompilationVerifier CompileAndVerifyFieldMarshal(CSharpTestSource source, Dictionary<string, byte[]> expectedBlobs, bool isField = true) =>
            CompileAndVerifyFieldMarshal(
                source,
                (s, _) =>
                {
                    Assert.True(expectedBlobs.ContainsKey(s), "Expecting marshalling blob for " + (isField ? "field " : "parameter ") + s);
                    return expectedBlobs[s];
                },
                isField);

        internal CompilationVerifier CompileAndVerifyFieldMarshal(CSharpTestSource source, Func<string, PEAssembly, byte[]> getExpectedBlob, bool isField = true) =>
            CompileAndVerifyFieldMarshalCommon(
                CreateCompilationWithMscorlib40(source, parseOptions: TestOptions.RegularPreview.WithNoRefSafetyRulesAttribute()),
                getExpectedBlob,
                isField);

        #region SyntaxTree Factories

        public static SyntaxTree Parse(string text, string filename = "", CSharpParseOptions? options = null, Encoding? encoding = null, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1)
            => CSharpTestSource.Parse(text, filename, options, encoding, checksumAlgorithm);

        public static SyntaxTree[] Parse(IEnumerable<string> sources, CSharpParseOptions? options = null)
        {
            if (sources == null || !sources.Any())
            {
                return new SyntaxTree[] { };
            }

            return Parse(options, sources.ToArray());
        }

        public static SyntaxTree[] Parse(CSharpParseOptions? options = null, params string[] sources)
        {
            if (sources == null || (sources.Length == 1 && null == sources[0]))
            {
                return new SyntaxTree[] { };
            }

            return sources.Select((src, index) => Parse(src, filename: $"{index}.cs", options: options)).ToArray();
        }

        public static SyntaxTree ParseWithRoundTripCheck(string text, CSharpParseOptions? options = null)
        {
            var tree = Parse(text, options: options ?? TestOptions.RegularPreview);
            var parsedText = tree.GetRoot();
            // we validate the text roundtrips
            Assert.Equal(text, parsedText.ToFullString());
            return tree;
        }

        #endregion

        #region Compilation Factories

        public static CSharpCompilation CreateCompilationWithIL(
            CSharpTestSource source,
            string ilSource,
            TargetFramework targetFramework = TargetFramework.Standard,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            bool appendDefaultHeader = true) => CreateCompilationWithILAndMscorlib40(source, ilSource, targetFramework, references, options, parseOptions, appendDefaultHeader);

        public static CSharpCompilation CreateCompilationWithILAndMscorlib40(
            CSharpTestSource source,
            string ilSource,
            TargetFramework targetFramework = TargetFramework.Mscorlib40,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            bool appendDefaultHeader = true)
        {
            MetadataReference ilReference = CompileIL(ilSource, appendDefaultHeader);
            var allReferences = TargetFrameworkUtil.GetReferences(targetFramework, references).Add(ilReference);
            return CreateEmptyCompilation(source, allReferences, options, parseOptions);
        }

        public static CSharpCompilation CreateCompilationWithMscorlib40(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            string? assemblyName = null,
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib40, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithMscorlib461(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            string? assemblyName = null,
            string sourceFileName = "",
            bool skipUsesIsNullable = false) => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib461, assemblyName, sourceFileName, skipUsesIsNullable);

        public static CSharpCompilation CreateCompilationWithMscorlib461(
            string[] source,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            string? assemblyName = null,
            string sourceFileName = "",
            bool skipUsesIsNullable = false) => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib461, assemblyName, sourceFileName, skipUsesIsNullable);

        public static CSharpCompilation CreateCompilationWithMscorlib46(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            string? assemblyName = null,
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib46, assemblyName, sourceFileName);

        internal static CSharpCompilation CreateExperimentalCompilationWithMscorlib461(
            CSharpTestSource source,
            MessageID feature,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            string? assemblyName = null,
            string sourceFileName = "",
            bool skipUsesIsNullable = false) => CreateCompilationCore(source, TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib461, references), options, parseOptions, assemblyName, sourceFileName, skipUsesIsNullable, experimentalFeature: feature);

        public static CSharpCompilation CreateCompilationWithWinRT(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            string? assemblyName = null,
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.WinRT, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithMscorlib461AndCSharp(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            string? assemblyName = null,
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib461AndCSharp, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithMscorlib40AndSystemCore(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            string? assemblyName = null,
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib40AndSystemCore, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithMscorlib40AndSystemCore(
            string[] source,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            string? assemblyName = null,
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib40AndSystemCore, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithCSharp(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            string? assemblyName = null,
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.StandardAndCSharp, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithMscorlib40AndDocumentationComments(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            string? assemblyName = null,
            string sourceFileName = "")
        {
            parseOptions = parseOptions != null ? parseOptions.WithDocumentationMode(DocumentationMode.Diagnose) : TestOptions.RegularPreviewWithDocumentationComments;
            options = (options ?? TestOptions.ReleaseDll).WithXmlReferenceResolver(XmlFileResolver.Default);
            return CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib40, assemblyName, sourceFileName);
        }

        public static CSharpCompilation CreateCompilationWithTasksExtensions(
                CSharpTestSource source,
                IEnumerable<MetadataReference>? references = null,
                CSharpCompilationOptions? options = null,
                CSharpParseOptions? parseOptions = null,
                string? assemblyName = null,
                string sourceFileName = "")
        {
            IEnumerable<MetadataReference> allReferences;

            if (RuntimeUtilities.IsCoreClrRuntime)
            {
                allReferences = [.. NetStandard20.References.All, NetStandard20.ExtraReferences.SystemThreadingTasksExtensions];
            }
            else
            {
                allReferences = [.. TargetFrameworkUtil.Mscorlib461ExtendedReferences, Net461.ExtraReferences.SystemThreadingTasksExtensions];
            }

            if (references != null)
            {
                allReferences = allReferences.Concat(references);
            }

            return CreateCompilation(source, allReferences, options, parseOptions, TargetFramework.Empty, assemblyName, sourceFileName);
        }

        public static CSharpCompilation CreateCompilation(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            TargetFramework targetFramework = TargetFramework.Standard,
            string? assemblyName = null,
            string sourceFileName = "",
            bool skipUsesIsNullable = false)
        {
            return CreateEmptyCompilation(source, TargetFrameworkUtil.GetReferences(targetFramework, references), options, parseOptions, assemblyName, sourceFileName, skipUsesIsNullable);
        }

        public static CSharpCompilation CreateEmptyCompilation(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references = null,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            string? assemblyName = null,
            string sourceFileName = "",
            bool skipUsesIsNullable = false,
            bool skipExtraValidation = false) => CreateCompilationCore(source, references, options, parseOptions, assemblyName, sourceFileName, skipUsesIsNullable, experimentalFeature: null, skipExtraValidation: skipExtraValidation);

        private static CSharpCompilation CreateCompilationCore(
            CSharpTestSource source,
            IEnumerable<MetadataReference>? references,
            CSharpCompilationOptions? options,
            CSharpParseOptions? parseOptions,
            string? assemblyName,
            string sourceFileName,
            bool skipUsesIsNullable,
            MessageID? experimentalFeature,
            bool skipExtraValidation = false)
        {
            var syntaxTrees = source.GetSyntaxTrees(parseOptions, sourceFileName);

            options ??= CheckForTopLevelStatements(syntaxTrees);

            // Using single-threaded build if debugger attached, to simplify debugging.
            if (Debugger.IsAttached)
            {
                options = options.WithConcurrentBuild(false);
            }

            if (experimentalFeature.HasValue)
            {
                parseOptions = (parseOptions ?? TestOptions.RegularPreview).WithExperimental(experimentalFeature.Value);
            }

            Func<CSharpCompilation> createCompilationLambda = () => CSharpCompilation.Create(
                string.IsNullOrEmpty(assemblyName) ? GetUniqueName() : assemblyName,
                syntaxTrees,
                references,
                options);

            if (!skipExtraValidation)
            {
                ValidateCompilation(createCompilationLambda);
            }

            var compilation = createCompilationLambda();
            // 'skipUsesIsNullable' may need to be set for some tests, particularly those that want to verify
            // symbols are created lazily, since 'UsesIsNullableVisitor' will eagerly visit all members.
            if (!skipUsesIsNullable && !IsNullableEnabled(compilation))
            {
                VerifyUsesOfNullability(createCompilationLambda().SourceModule.GlobalNamespace, expectedUsesOfNullable: ImmutableArray<string>.Empty);
            }

            return compilation;
        }

        protected static CSharpCompilationOptions CheckForTopLevelStatements(SyntaxTree[] syntaxTrees)
        {
            bool hasTopLevelStatements = syntaxTrees.Any(s => s.GetRoot().ChildNodes().OfType<GlobalStatementSyntax>().Any());

            var options = hasTopLevelStatements ? TestOptions.ReleaseExe : TestOptions.ReleaseDll;
            return options;
        }

        private static void ValidateCompilation(Func<CSharpCompilation> createCompilationLambda)
        {
            CompilationExtensions.ValidateIOperations(createCompilationLambda);
            VerifyUsedAssemblyReferences(createCompilationLambda);
        }

        private static void VerifyUsedAssemblyReferences(Func<CSharpCompilation> createCompilationLambda)
        {
            // To run the additional validation below, comment this out or define ROSLYN_TEST_USEDASSEMBLIES
            if (!CompilationExtensions.EnableVerifyUsedAssemblies)
            {
                return;
            }

            var comp = createCompilationLambda();
            var used = comp.GetUsedAssemblyReferences();

            var compileDiagnostics = comp.GetDiagnostics();
            var emitDiagnostics = comp.GetEmitDiagnostics();

            var resolvedReferences = comp.References.Where(r => r.Properties.Kind == MetadataImageKind.Assembly);

            if (!compileDiagnostics.Any(d => d.DefaultSeverity == DiagnosticSeverity.Error) &&
                !resolvedReferences.Any(r => r.Properties.HasRecursiveAliases))
            {
                if (resolvedReferences.Count() > used.Length)
                {
                    assertSubset(used, resolvedReferences);

                    if (!compileDiagnostics.Any(d => d.Code == (int)ErrorCode.HDN_UnusedExternAlias || d.Code == (int)ErrorCode.HDN_UnusedUsingDirective))
                    {
                        var comp2 = comp.RemoveAllReferences().AddReferences(used.Concat(comp.References.Where(r => r.Properties.Kind == MetadataImageKind.Module)));
                        comp2.GetEmitDiagnostics().Where(d => shouldCompare(d)).Verify(
                            emitDiagnostics.Where(d => shouldCompare(d)).
                                            Select(d => new DiagnosticDescription(d, errorCodeOnly: false, includeDefaultSeverity: false, includeEffectiveSeverity: false)).ToArray());
                    }
                }
                else
                {
                    AssertEx.Equal(resolvedReferences, used);
                }
            }
            else
            {
                assertSubset(used, resolvedReferences);
            }

            static bool shouldCompare(Diagnostic d)
            {
                return d.Code != (int)ErrorCode.WRN_SameFullNameThisAggAgg &&
                       d.Code != (int)ErrorCode.WRN_SameFullNameThisNsAgg &&
                       d.Code != (int)ErrorCode.WRN_AmbiguousXMLReference &&
                       d.Code != (int)ErrorCode.WRN_MultiplePredefTypes &&
                       d.Code != (int)ErrorCode.WRN_SameFullNameThisAggNs;
            }

            static void assertSubset(ImmutableArray<MetadataReference> used, IEnumerable<MetadataReference> resolvedReferences)
            {
                foreach (var reference in used)
                {
                    Assert.Contains(reference, resolvedReferences);
                }
            }
        }

        internal static bool IsNullableEnabled(CSharpCompilation compilation)
        {
            // This method should not cause any binding, including resolving references, etc.
            var trees = compilation.SyntaxTrees;
            if (trees.IsDefaultOrEmpty)
            {
                return false;
            }
            var options = (CSharpParseOptions)trees[0].Options;
            return options.IsFeatureEnabled(MessageID.IDS_FeatureNullableReferenceTypes);
        }

        internal static void VerifyUsesOfNullability(Symbol symbol, ImmutableArray<string> expectedUsesOfNullable)
        {
            var builder = ArrayBuilder<Symbol>.GetInstance();
            UsesIsNullableVisitor.GetUses(builder, symbol);

            var format = SymbolDisplayFormat.TestFormat
                .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.IncludeNotNullableReferenceTypeModifier)
                .RemoveParameterOptions(SymbolDisplayParameterOptions.IncludeName);

            var symbols = builder.SelectAsArray(s => s.ToDisplayString(format));
            builder.Free();

            AssertEx.Equal(expectedUsesOfNullable, symbols, itemInspector: s => $"\"{s}\"");
        }

        public static CSharpCompilation CreateCompilation(
            AssemblyIdentity identity,
            CSharpTestSource? source,
            IEnumerable<MetadataReference> references,
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null)
        {
            var trees = (source ?? CSharpTestSource.None).GetSyntaxTrees(parseOptions);
            Func<CSharpCompilation> createCompilationLambda = () => CSharpCompilation.Create(identity.Name, options: options ?? TestOptions.ReleaseDll, references: references, syntaxTrees: trees);

            ValidateCompilation(createCompilationLambda);
            var c = createCompilationLambda();
            Assert.NotNull(c.Assembly); // force creation of SourceAssemblySymbol

            ((SourceAssemblySymbol)c.Assembly).lazyAssemblyIdentity = identity;
            return c;
        }

        public static CSharpCompilation CreateSubmissionWithExactReferences(
           string source,
           IEnumerable<MetadataReference>? references = null,
           CSharpCompilationOptions? options = null,
           CSharpParseOptions? parseOptions = null,
           CSharpCompilation? previous = null,
           Type? returnType = null,
           Type? hostObjectType = null)
        {
            Func<CSharpCompilation> createCompilationLambda = () => CSharpCompilation.CreateScriptCompilation(
                GetUniqueName(),
                references: references,
                options: options,
                syntaxTree: Parse(source, options: parseOptions ?? TestOptions.Script),
                previousScriptCompilation: previous,
                returnType: returnType,
                globalsType: hostObjectType);
            ValidateCompilation(createCompilationLambda);
            return createCompilationLambda();
        }

        private static readonly ImmutableArray<MetadataReference> s_scriptRefs = ImmutableArray.Create(MscorlibRef_v4_0_30316_17626);

        public static CSharpCompilation CreateSubmission(
           string code,
           IEnumerable<MetadataReference>? references = null,
           CSharpCompilationOptions? options = null,
           CSharpParseOptions? parseOptions = null,
           CSharpCompilation? previous = null,
           Type? returnType = null,
           Type? hostObjectType = null)
        {
            Func<CSharpCompilation> createCompilationLambda = () => CSharpCompilation.CreateScriptCompilation(
                GetUniqueName(),
                references: (references != null) ? s_scriptRefs.Concat(references) : s_scriptRefs,
                options: options,
                syntaxTree: Parse(code, options: parseOptions ?? TestOptions.Script),
                previousScriptCompilation: previous,
                returnType: returnType,
                globalsType: hostObjectType);
            ValidateCompilation(createCompilationLambda);
            return createCompilationLambda();
        }

        public CompilationVerifier CompileWithCustomILSource(string cSharpSource, string ilSource, Action<CSharpCompilation>? compilationVerifier = null, bool importInternals = true, string? expectedOutput = null, TargetFramework targetFramework = TargetFramework.Standard)
        {
            var compilationOptions = (expectedOutput != null) ? TestOptions.ReleaseExe : TestOptions.ReleaseDll;

            if (importInternals)
            {
                compilationOptions = compilationOptions.WithMetadataImportOptions(MetadataImportOptions.Internal);
            }

            if (ilSource == null)
            {
                var c = CreateCompilation(cSharpSource, options: compilationOptions, targetFramework: targetFramework);
                return CompileAndVerify(c, expectedOutput: expectedOutput);
            }

            MetadataReference reference = CreateMetadataReferenceFromIlSource(ilSource);

            var compilation = CreateCompilation(cSharpSource, new[] { reference }, compilationOptions, targetFramework: targetFramework);
            compilationVerifier?.Invoke(compilation);

            return CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        public static MetadataReference CreateMetadataReferenceFromIlSource(string ilSource, bool prependDefaultHeader = true)
        {
            using (var tempAssembly = IlasmUtilities.CreateTempAssembly(ilSource, prependDefaultHeader))
            {
                return MetadataReference.CreateFromImage(ReadFromFile(tempAssembly.Path));
            }
        }

        /// <summary>
        /// Like CompileAndVerify, but confirms that execution raises an exception.
        /// </summary>
        /// <typeparam name="T">Expected type of the exception.</typeparam>
        /// <param name="source">Program to compile and execute.</param>
        /// <param name="expectedMessage">Ignored if null.</param>
        internal CompilationVerifier CompileAndVerifyException<T>(string source, string? expectedMessage = null, bool allowUnsafe = false, Verification verify = default) where T : Exception
        {
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe));
            return CompileAndVerifyException<T>(comp, expectedMessage, verify);
        }

        internal CompilationVerifier CompileAndVerifyException<T>(CSharpCompilation comp, string? expectedMessage = null, Verification verify = default) where T : Exception
        {
            try
            {
                CompileAndVerify(comp, expectedOutput: "", verify: verify); //need expected output to force execution
                Assert.False(true, string.Format("Expected exception {0}({1})", typeof(T).Name, expectedMessage));
            }
            catch (TargetInvocationException x)
            {
                var e = x.InnerException;
                Assert.IsType<T>(e);
                if (expectedMessage != null)
                {
                    Assert.Equal(expectedMessage, e.Message);
                }
            }

            return CompileAndVerify(comp, verify: verify);
        }

        protected static List<SyntaxNode> GetSyntaxNodeList(SyntaxTree syntaxTree)
        {
            return GetSyntaxNodeList(syntaxTree.GetRoot(), null);
        }

        protected static List<SyntaxNode> GetSyntaxNodeList(SyntaxNode node, List<SyntaxNode>? synList)
        {
            if (synList == null)
                synList = new List<SyntaxNode>();

            synList.Add(node);

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                    synList = GetSyntaxNodeList(child.AsNode()!, synList);
            }

            return synList;
        }

        protected static SyntaxNode? GetSyntaxNodeForBinding(List<SyntaxNode> synList)
        {
            return GetSyntaxNodeOfTypeForBinding<SyntaxNode>(synList);
        }

        protected const string StartString = "/*<bind>*/";
        protected const string EndString = "/*</bind>*/";

        protected static TNode? GetSyntaxNodeOfTypeForBinding<TNode>(List<SyntaxNode> synList) where TNode : SyntaxNode
        {
            foreach (var node in synList.OfType<TNode>())
            {
                string exprFullText = node.ToFullString();
                exprFullText = exprFullText.Trim();

                // Account for comments being added as leading trivia for this node.
                while (exprFullText.StartsWith("//"))
                {
                    exprFullText = exprFullText[exprFullText.IndexOf('\n')..].Trim();
                }

                if (exprFullText.StartsWith(StartString, StringComparison.Ordinal))
                {
                    if (exprFullText.Contains(EndString))
                    {
                        if (exprFullText.EndsWith(EndString, StringComparison.Ordinal))
                        {
                            return node;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        return node;
                    }
                }

                if (exprFullText.EndsWith(EndString, StringComparison.Ordinal))
                {
                    if (exprFullText.Contains(StartString))
                    {
                        if (exprFullText.StartsWith(StartString, StringComparison.Ordinal))
                        {
                            return node;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        return node;
                    }
                }
            }

            return null;
        }

        #endregion

        #region Semantic Model Helpers

        public Tuple<TNode, SemanticModel> GetBindingNodeAndModel<TNode>(CSharpCompilation compilation, int treeIndex = 0) where TNode : SyntaxNode
        {
            var node = GetBindingNode<TNode>(compilation, treeIndex);
            Assert.NotNull(node);
            return new Tuple<TNode, SemanticModel>(node, compilation.GetSemanticModel(compilation.SyntaxTrees[treeIndex]));
        }

        public Tuple<TNode, SemanticModel> GetBindingNodeAndModel<TNode>(Compilation compilation, int treeIndex = 0) where TNode : SyntaxNode
        {
            return GetBindingNodeAndModel<TNode>((CSharpCompilation)compilation, treeIndex);
        }

        public Tuple<IList<TNode>, SemanticModel> GetBindingNodesAndModel<TNode>(CSharpCompilation compilation, int treeIndex = 0, int which = -1) where TNode : SyntaxNode
        {
            var nodes = GetBindingNodes<TNode>(compilation, treeIndex, which);
            return new Tuple<IList<TNode>, SemanticModel>(nodes, compilation.GetSemanticModel(compilation.SyntaxTrees[treeIndex]));
        }

        /// <summary>
        /// This method handles one binding text with strong SyntaxNode type
        /// </summary>
        public TNode? GetBindingNode<TNode>(CSharpCompilation compilation, int treeIndex = 0) where TNode : SyntaxNode
        {
            Assert.True(compilation.SyntaxTrees.Length > treeIndex, "Compilation has enough trees");
            var tree = compilation.SyntaxTrees[treeIndex];

            const string bindStart = "/*<bind>*/";
            const string bindEnd = "/*</bind>*/";
            return FindBindingNode<TNode>(tree, bindStart, bindEnd);
        }

        /// <summary>
        /// Find multiple binding nodes by looking for pair /*&lt;bind#&gt;*/ &amp; /*&lt;/bind#&gt;*/ in source text
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="treeIndex">which tree</param>
        /// <param name="which">
        ///     * if which &lt; 0, find ALL wrapped nodes
        ///     * if which &gt;=0, find a specific binding node wrapped by /*&lt;bind#&gt;*/ &amp; /*&lt;/bind#&gt;*/
        ///       e.g. if which = 1, find node wrapped by /*&lt;bind1&gt;*/ &amp; /*&lt;/bind1&gt;*/
        /// </param>
        /// <returns></returns>
        public IList<TNode> GetBindingNodes<TNode>(CSharpCompilation compilation, int treeIndex = 0, int which = -1) where TNode : SyntaxNode
        {
            Assert.True(compilation.SyntaxTrees.Length > treeIndex, "Compilation has enough trees");
            var tree = compilation.SyntaxTrees[treeIndex];

            var nodeList = new List<TNode>();
            string text = tree.GetRoot().ToFullString();

            const string bindStartFmt = "/*<bind{0}>*/";
            const string bindEndFmt = "/*</bind{0}>*/";
            // find all
            if (which < 0)
            {
                // assume tags with number are in increasing order, no jump
                for (byte i = 0; i < 255; i++)
                {
                    var start = String.Format(bindStartFmt, i);
                    var end = String.Format(bindEndFmt, i);

                    var bindNode = FindBindingNode<TNode>(tree, start, end);
                    // done
                    if (bindNode == null)
                        break;

                    nodeList.Add(bindNode);
                }
            }
            else
            {
                var start2 = String.Format(bindStartFmt, which);
                var end2 = String.Format(bindEndFmt, which);

                var bindNode = FindBindingNode<TNode>(tree, start2, end2);
                // done
                if (bindNode != null)
                    nodeList.Add(bindNode);
            }

            return nodeList;
        }

        public IList<TNode> GetBindingNodes<TNode>(Compilation compilation, int treeIndex = 0, int which = -1) where TNode : SyntaxNode
        {
            return GetBindingNodes<TNode>((CSharpCompilation)compilation, treeIndex, which);
        }

        private static TNode? FindBindingNode<TNode>(SyntaxTree tree, string startTag, string endTag) where TNode : SyntaxNode
        {
            // =================
            // Get Binding Text
            string text = tree.GetRoot().ToFullString();
            int start = text.IndexOf(startTag, StringComparison.Ordinal);
            if (start < 0)
                return null;

            start += startTag.Length;
            int end = text.IndexOf(endTag, StringComparison.Ordinal);
            Assert.True(end > start, "Bind Pos: end > start");
            // get rid of white spaces if any
            var bindText = text.Substring(start, end - start).Trim();
            if (String.IsNullOrWhiteSpace(bindText))
                return null;

            // =================
            // Get Binding Node
            var node = tree.GetRoot().FindToken(start).Parent;
            while ((node != null && node.ToString() != bindText))
            {
                node = node.Parent;
            }
            // =================
            // Get Binding Node with match node type
            if (node != null)
            {
                while ((node as TNode) == null)
                {
                    if (node.Parent != null && node.Parent.ToString() == bindText)
                    {
                        node = node.Parent;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            Assert.NotNull(node); // If this trips, then node  wasn't found
            Assert.IsAssignableFrom(typeof(TNode), node);
            Assert.Equal(bindText, node.ToString());
            return ((TNode)node);
        }
        #endregion

        #region Attributes

        internal static IEnumerable<string> GetAttributeNames(ImmutableArray<SynthesizedAttributeData> attributes)
        {
            return attributes.Select(a => a.AttributeClass!.Name);
        }

        internal static IEnumerable<string> GetAttributeNames(ImmutableArray<CSharpAttributeData> attributes)
        {
            return attributes.Select(a => a.AttributeClass!.Name);
        }

        internal static IEnumerable<string> GetAttributeStrings(ImmutableArray<CSharpAttributeData> attributes)
        {
            return attributes.Select(a => a.ToString()!);
        }

        internal static IEnumerable<string> GetAttributeStrings(IEnumerable<CSharpAttributeData> attributes)
        {
            return attributes.Select(a => a.ToString()!);
        }

        #endregion

        #region Documentation Comments

        internal static string GetDocumentationCommentText(CSharpCompilation compilation, params DiagnosticDescription[] expectedDiagnostics)
        {
            return GetDocumentationCommentText(compilation, outputName: null, filterTree: null, ensureEnglishUICulture: true, expectedDiagnostics: expectedDiagnostics);
        }

        internal static string GetDocumentationCommentText(CSharpCompilation compilation, bool ensureEnglishUICulture, params DiagnosticDescription[] expectedDiagnostics)
        {
            return GetDocumentationCommentText(compilation, outputName: null, filterTree: null, ensureEnglishUICulture: ensureEnglishUICulture, expectedDiagnostics: expectedDiagnostics);
        }

        internal static string GetDocumentationCommentText(CSharpCompilation compilation, string? outputName = null, SyntaxTree? filterTree = null, TextSpan? filterSpanWithinTree = null, bool ensureEnglishUICulture = false, params DiagnosticDescription[] expectedDiagnostics)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
                CultureInfo? saveUICulture = null;

                if (ensureEnglishUICulture)
                {
                    var preferred = EnsureEnglishUICulture.PreferredOrNull;

                    if (preferred == null)
                    {
                        ensureEnglishUICulture = false;
                    }
                    else
                    {
                        saveUICulture = CultureInfo.CurrentUICulture;
                        CultureInfo.CurrentUICulture = preferred;
                    }
                }

                var bindingDiagnostics = BindingDiagnosticBag.GetInstance(withDiagnostics: true, withDependencies: false);

                try
                {
                    DocumentationCommentCompiler.WriteDocumentationCommentXml(compilation, outputName, stream, bindingDiagnostics, default(CancellationToken), filterTree, filterSpanWithinTree);
                }
                finally
                {
                    Debug.Assert(bindingDiagnostics.DiagnosticBag is not null);
                    diagnostics.AddRange(bindingDiagnostics.DiagnosticBag);
                    bindingDiagnostics.Free();

                    if (ensureEnglishUICulture)
                    {
                        Debug.Assert(saveUICulture is not null);
                        CultureInfo.CurrentUICulture = saveUICulture;
                    }
                }

                if (expectedDiagnostics != null)
                {
                    diagnostics.Verify(expectedDiagnostics);
                }
                diagnostics.Free();

                string text = Encoding.UTF8.GetString(stream.ToArray());
                int length = text.IndexOf('\0');
                if (length >= 0)
                {
                    text = text.Substring(0, length);
                }
                return text.Trim();
            }
        }

        internal static IEnumerable<CrefSyntax> GetCrefSyntaxes(Compilation compilation) => GetCrefSyntaxes((CSharpCompilation)compilation);

        internal static IEnumerable<CrefSyntax> GetCrefSyntaxes(CSharpCompilation compilation)
        {
            return compilation.SyntaxTrees.SelectMany(tree =>
            {
                var docComments = tree.GetCompilationUnitRoot().DescendantTrivia().Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>();
                return docComments.SelectMany(docComment => docComment.DescendantNodes().OfType<XmlCrefAttributeSyntax>().Select(attr => attr.Cref));
            });
        }

        internal static Symbol? GetReferencedSymbol(CrefSyntax crefSyntax, CSharpCompilation compilation, params DiagnosticDescription[] expectedDiagnostics)
        {
            Symbol ambiguityWinner;
            var references = GetReferencedSymbols(crefSyntax, compilation, out ambiguityWinner, expectedDiagnostics);
            Assert.Null(ambiguityWinner);
            Assert.InRange(references.Length, 0, 1); //Otherwise, call GetReferencedSymbols

            return references.FirstOrDefault();
        }

        internal static ImmutableArray<Symbol> GetReferencedSymbols(CrefSyntax crefSyntax, CSharpCompilation compilation, out Symbol ambiguityWinner, params DiagnosticDescription[] expectedDiagnostics)
        {
            var binderFactory = compilation.GetBinderFactory(crefSyntax.SyntaxTree);
            var binder = binderFactory.GetBinder(crefSyntax);

            DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
            var references = binder.BindCref(crefSyntax, out ambiguityWinner, diagnostics);
            diagnostics.Verify(expectedDiagnostics);
            diagnostics.Free();
            return references;
        }

        #endregion

        #region IL Validation

        internal override string VisualizeRealIL(IModuleSymbol peModule, CompilationTestData.MethodData methodData, IReadOnlyDictionary<int, string>? markers, bool areLocalsZeroed)
        {
            return VisualizeRealIL((PEModuleSymbol)peModule.GetSymbol(), methodData, markers, areLocalsZeroed);
        }

        /// <summary>
        /// Returns a string representation of IL read from metadata.
        /// </summary>
        /// <remarks>
        /// Currently unsupported IL decoding:
        /// - multidimensional arrays
        /// - vararg calls
        /// - winmd
        /// - global methods
        /// </remarks>
        internal static unsafe string VisualizeRealIL(PEModuleSymbol peModule, CompilationTestData.MethodData methodData, IReadOnlyDictionary<int, string>? markers, bool areLocalsZeroed)
        {
            var typeName = GetContainingTypeMetadataName(methodData.Method);
            Debug.Assert(typeName is not null);
            // TODO (tomat): global methods (typeName == null)

            var type = peModule.ContainingAssembly.GetTypeByMetadataName(typeName);
            Debug.Assert(type is not null);

            // TODO (tomat): overloaded methods
            var method = (PEMethodSymbol)type.GetMembers(methodData.Method.MetadataName).Single();

            var bodyBlock = peModule.Module.GetMethodBodyOrThrow(method.Handle);
            Assert.NotNull(bodyBlock);

            var moduleDecoder = new MetadataDecoder(peModule);
            var peMethod = (PEMethodSymbol)moduleDecoder.GetSymbolForILToken(method.Handle);

            StringBuilder sb = new StringBuilder();
            var ilBytes = bodyBlock.GetILContent();

            var ehHandlerRegions = ILVisualizer.GetHandlerSpans(bodyBlock.ExceptionRegions);

            var methodDecoder = new MetadataDecoder(peModule, peMethod);

            ImmutableArray<ILVisualizer.LocalInfo> localDefinitions;
            if (!bodyBlock.LocalSignature.IsNil)
            {
                var signature = peModule.Module.MetadataReader.GetStandaloneSignature(bodyBlock.LocalSignature).Signature;
                var signatureReader = peModule.Module.GetMemoryReaderOrThrow(signature);
                var localInfos = methodDecoder.DecodeLocalSignatureOrThrow(ref signatureReader);
                localDefinitions = ToLocalDefinitions(localInfos, methodData.ILBuilder);
            }
            else
            {
                localDefinitions = ImmutableArray.Create<ILVisualizer.LocalInfo>();
            }

            // TODO (tomat): the .maxstack in IL can't be less than 8, but many tests expect .maxstack < 8
            int maxStack = (bodyBlock.MaxStack == 8 && methodData.ILBuilder.MaxStack < 8) ? methodData.ILBuilder.MaxStack : bodyBlock.MaxStack;

            var visualizer = new Visualizer(new MetadataDecoder(peModule, peMethod));

            visualizer.DumpMethod(sb, maxStack, ilBytes, localDefinitions, ehHandlerRegions, markers, areLocalsZeroed);

            return sb.ToString();
        }

        private static string? GetContainingTypeMetadataName(IMethodSymbolInternal method)
        {
            var type = method.ContainingType;
            if (type == null)
            {
                return null;
            }

            string ns = type.ContainingNamespace.MetadataName;
            var result = type.MetadataName;

            while ((type = type.ContainingType) != null)
            {
                result = type.MetadataName + "+" + result;
            }

            return (ns.Length > 0) ? ns + "." + result : result;
        }

        private static ImmutableArray<ILVisualizer.LocalInfo> ToLocalDefinitions(ImmutableArray<LocalInfo<TypeSymbol>> localInfos, ILBuilder builder)
        {
            if (localInfos.IsEmpty)
            {
                return ImmutableArray.Create<ILVisualizer.LocalInfo>();
            }

            Debug.Assert(builder.LocalSlotManager != null);

            var result = new ILVisualizer.LocalInfo[localInfos.Length];
            for (int i = 0; i < result.Length; i++)
            {
                var typeRef = localInfos[i].Type;
                var builderLocal = builder.LocalSlotManager.LocalsInOrder()[i];
                result[i] = new ILVisualizer.LocalInfo(builderLocal.Name, typeRef, localInfos[i].IsPinned, localInfos[i].IsByRef);
            }

            return result.AsImmutableOrNull();
        }

        private sealed class Visualizer : ILVisualizer
        {
            private readonly MetadataDecoder _decoder;

            public Visualizer(MetadataDecoder decoder)
            {
                _decoder = decoder;
            }

            public override string VisualizeUserString(uint token)
            {
                var reader = _decoder.Module.GetMetadataReader();
                return "\"" + reader.GetUserString((UserStringHandle)MetadataTokens.Handle((int)token)) + "\"";
            }

            public override string VisualizeSymbol(uint token, OperandType operandType)
            {
                Symbol reference = _decoder.GetSymbolForILToken(MetadataTokens.EntityHandle((int)token));
                return string.Format("\"{0}\"", (reference is Symbol symbol) ? symbol.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat) : (object)reference);
            }

            public override string VisualizeLocalType(object type)
            {
                Symbol? symbol;

                if (type is int)
                {
                    symbol = _decoder.GetSymbolForILToken(MetadataTokens.EntityHandle((int)type));
                }
                else
                {
                    symbol = type as Symbol;

                    if (symbol is null)
                    {
                        symbol = (type as Cci.IReference)?.GetInternalSymbol() as Symbol;
                    }
                }

                return symbol?.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat) ?? type.ToString()!;
            }
        }

        #endregion

        #region IOperation tree validation

        protected static (IOperation? operation, SyntaxNode? node) GetOperationAndSyntaxForTest<TSyntaxNode>(CSharpCompilation compilation)
            where TSyntaxNode : SyntaxNode
        {
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            SyntaxNode? syntaxNode = GetSyntaxNodeOfTypeForBinding<TSyntaxNode>(GetSyntaxNodeList(tree));
            if (syntaxNode == null)
            {
                return (null, null);
            }

            var operation = model.GetOperation(syntaxNode);
            if (operation != null)
            {
                Assert.Same(model, operation.SemanticModel);
            }
            return (operation, syntaxNode);
        }

        protected static string? GetOperationTreeForTest<TSyntaxNode>(CSharpCompilation compilation)
            where TSyntaxNode : SyntaxNode
        {
            var (operation, syntax) = GetOperationAndSyntaxForTest<TSyntaxNode>(compilation);
            return operation != null ? OperationTreeVerifier.GetOperationTree(compilation, operation) : null;
        }

        protected static string? GetOperationTreeForTest(CSharpCompilation compilation, IOperation? operation)
        {
            return operation != null ? OperationTreeVerifier.GetOperationTree(compilation, operation) : null;
        }

        protected static string? GetOperationTreeForTest<TSyntaxNode>(
            CSharpTestSource testSrc,
            CSharpCompilationOptions? compilationOptions = null,
            CSharpParseOptions? parseOptions = null,
            bool useLatestFrameworkReferences = false)
            where TSyntaxNode : SyntaxNode
        {
            var targetFramework = useLatestFrameworkReferences ? TargetFramework.Mscorlib46Extended : TargetFramework.Standard;
            var compilation = CreateCompilation(testSrc, targetFramework: targetFramework, options: compilationOptions ?? TestOptions.ReleaseDll, parseOptions: parseOptions);
            return GetOperationTreeForTest<TSyntaxNode>(compilation);
        }

        protected static IOperation? VerifyOperationTreeForTest<TSyntaxNode>(CSharpCompilation compilation, string expectedOperationTree, Action<IOperation?, Compilation, SyntaxNode?>? additionalOperationTreeVerifier = null)
            where TSyntaxNode : SyntaxNode
        {
            var (actualOperation, syntaxNode) = GetOperationAndSyntaxForTest<TSyntaxNode>(compilation);
            var actualOperationTree = GetOperationTreeForTest(compilation, actualOperation);
            OperationTreeVerifier.Verify(expectedOperationTree, actualOperationTree);
            additionalOperationTreeVerifier?.Invoke(actualOperation, compilation, syntaxNode);

            return actualOperation;
        }

        protected static void VerifyOperationTreeForNode(CSharpCompilation compilation, SemanticModel model, SyntaxNode syntaxNode, string expectedOperationTree)
        {
            VerifyOperationTree(compilation, model.GetOperation(syntaxNode), expectedOperationTree);
        }

        protected static void VerifyOperationTree(CSharpCompilation compilation, IOperation? operation, string expectedOperationTree)
        {
            Assert.NotNull(operation);
            var actualOperationTree = GetOperationTreeForTest(compilation, operation);
            OperationTreeVerifier.Verify(expectedOperationTree, actualOperationTree);
        }

        protected static void VerifyFlowGraphForTest<TSyntaxNode>(CSharpCompilation compilation, string expectedFlowGraph)
            where TSyntaxNode : SyntaxNode
        {
            var tree = compilation.SyntaxTrees[0];
            SyntaxNode? syntaxNode = GetSyntaxNodeOfTypeForBinding<TSyntaxNode>(GetSyntaxNodeList(tree));
            Debug.Assert(syntaxNode is not null, $"Ensure a /*<bind>*/ comment is used around syntax matching the type argument for '{nameof(TSyntaxNode)}'.");
            VerifyFlowGraph(compilation, syntaxNode, expectedFlowGraph);
        }

        protected static void VerifyFlowGraph(CSharpCompilation compilation, SyntaxNode syntaxNode, string expectedFlowGraph)
        {
            var model = compilation.GetSemanticModel(syntaxNode.SyntaxTree);
            (ControlFlowGraph graph, ISymbol associatedSymbol) = ControlFlowGraphVerifier.GetControlFlowGraph(syntaxNode, model);
            ControlFlowGraphVerifier.VerifyGraph(compilation, expectedFlowGraph, graph, associatedSymbol);
        }

        protected static void VerifyOperationTreeForTest<TSyntaxNode>(
            CSharpTestSource testSrc,
            string expectedOperationTree,
            CSharpCompilationOptions? compilationOptions = null,
            CSharpParseOptions? parseOptions = null,
            bool useLatestFrameworkReferences = false)
            where TSyntaxNode : SyntaxNode
        {
            var actualOperationTree = GetOperationTreeForTest<TSyntaxNode>(testSrc, compilationOptions, parseOptions, useLatestFrameworkReferences);
            OperationTreeVerifier.Verify(expectedOperationTree, actualOperationTree);
        }

        protected static void VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(
            CSharpCompilation compilation,
            string expectedOperationTree,
            DiagnosticDescription[] expectedDiagnostics,
            Action<IOperation?, Compilation, SyntaxNode?>? additionalOperationTreeVerifier = null)
            where TSyntaxNode : SyntaxNode
        {
            var actualDiagnostics = compilation.GetDiagnostics().Where(d => d.Severity != DiagnosticSeverity.Hidden);
            actualDiagnostics.Verify(expectedDiagnostics);
            VerifyOperationTreeForTest<TSyntaxNode>(compilation, expectedOperationTree, additionalOperationTreeVerifier);
        }

        protected static void VerifyFlowGraphAndDiagnosticsForTest<TSyntaxNode>(
            CSharpCompilation compilation,
            string expectedFlowGraph,
            DiagnosticDescription[] expectedDiagnostics)
            where TSyntaxNode : SyntaxNode
        {
            var actualDiagnostics = compilation.GetDiagnostics().Where(d => d.Severity != DiagnosticSeverity.Hidden);
            actualDiagnostics.Verify(expectedDiagnostics);
            VerifyFlowGraphForTest<TSyntaxNode>(compilation, expectedFlowGraph);
        }

        protected static void VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(
            CSharpTestSource testSrc,
            string expectedOperationTree,
            DiagnosticDescription[] expectedDiagnostics,
            CSharpCompilationOptions? compilationOptions = null,
            CSharpParseOptions? parseOptions = null,
            MetadataReference[]? references = null,
            Action<IOperation?, Compilation, SyntaxNode?>? additionalOperationTreeVerifier = null,
            TargetFramework targetFramework = TargetFramework.Standard)
            where TSyntaxNode : SyntaxNode =>
            VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(
                testSrc,
                expectedOperationTree,
                targetFramework,
                expectedDiagnostics,
                compilationOptions,
                parseOptions,
                references,
                additionalOperationTreeVerifier);

        protected static void VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(
            CSharpTestSource testSrc,
            string expectedOperationTree,
            TargetFramework targetFramework,
            DiagnosticDescription[] expectedDiagnostics,
            CSharpCompilationOptions? compilationOptions = null,
            CSharpParseOptions? parseOptions = null,
            MetadataReference[]? references = null,
            Action<IOperation?, Compilation, SyntaxNode?>? additionalOperationTreeVerifier = null)
            where TSyntaxNode : SyntaxNode
        {
            var compilation = CreateCompilation(
                testSrc,
                references,
                parseOptions: parseOptions,
                options: compilationOptions,
                targetFramework: targetFramework);
            VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(compilation, expectedOperationTree, expectedDiagnostics, additionalOperationTreeVerifier);
        }

        protected static void VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(
            SyntaxTree[] testSyntaxes,
            string expectedOperationTree,
            DiagnosticDescription[] expectedDiagnostics,
            CSharpCompilationOptions? compilationOptions = null,
            MetadataReference[]? references = null,
            Action<IOperation?, Compilation, SyntaxNode?>? additionalOperationTreeVerifier = null,
            bool useLatestFrameworkReferences = false)
            where TSyntaxNode : SyntaxNode
        {
            var compilation = CreateCompilation(
                testSyntaxes,
                references,
                options: compilationOptions,
                targetFramework: useLatestFrameworkReferences ? TargetFramework.Mscorlib46Extended : TargetFramework.Standard);
            VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(compilation, expectedOperationTree, expectedDiagnostics, additionalOperationTreeVerifier);
        }

        protected static void VerifyFlowGraphAndDiagnosticsForTest<TSyntaxNode>(
            CSharpTestSource testSrc,
            string expectedFlowGraph,
            DiagnosticDescription[] expectedDiagnostics,
            CSharpCompilationOptions? compilationOptions = null,
            CSharpParseOptions? parseOptions = null,
            MetadataReference[]? references = null,
            bool useLatestFrameworkReferences = false)
            where TSyntaxNode : SyntaxNode
        {
            VerifyFlowGraphAndDiagnosticsForTest<TSyntaxNode>(
                testSrc,
                expectedFlowGraph,
                expectedDiagnostics,
                targetFramework: useLatestFrameworkReferences ? TargetFramework.Mscorlib46Extended : TargetFramework.Standard,
                compilationOptions,
                parseOptions,
                references);
        }

        protected static void VerifyFlowGraphAndDiagnosticsForTest<TSyntaxNode>(
            CSharpTestSource testSrc,
            string expectedFlowGraph,
            DiagnosticDescription[] expectedDiagnostics,
            TargetFramework targetFramework,
            CSharpCompilationOptions? compilationOptions = null,
            CSharpParseOptions? parseOptions = null,
            MetadataReference[]? references = null)
            where TSyntaxNode : SyntaxNode
        {
            var compilation = CreateCompilation(
                testSrc,
                references,
                parseOptions: parseOptions,
                options: compilationOptions,
                targetFramework: targetFramework);
            VerifyFlowGraphAndDiagnosticsForTest<TSyntaxNode>(compilation, expectedFlowGraph, expectedDiagnostics);
        }

        protected static MetadataReference VerifyOperationTreeAndDiagnosticsForTestWithIL<TSyntaxNode>(string testSrc,
            string ilSource,
            string expectedOperationTree,
            DiagnosticDescription[] expectedDiagnostics,
            CSharpCompilationOptions? compilationOptions = null,
            CSharpParseOptions? parseOptions = null,
            MetadataReference[]? references = null,
            Action<IOperation?, Compilation, SyntaxNode?>? additionalOperationTreeVerifier = null,
            TargetFramework targetFramework = TargetFramework.Standard)
            where TSyntaxNode : SyntaxNode
        {
            var ilReference = CreateMetadataReferenceFromIlSource(ilSource);
            VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(testSrc, expectedOperationTree, expectedDiagnostics, compilationOptions, parseOptions, new[] { ilReference }, additionalOperationTreeVerifier, targetFramework);
            return ilReference;
        }

        #endregion

        #region Span

        protected static CSharpCompilation CreateCompilationWithSpan(CSharpTestSource tree, CSharpCompilationOptions? options = null, CSharpParseOptions? parseOptions = null)
        {
            var reference = CreateCompilation(
                TestSources.Span,
                options: TestOptions.UnsafeReleaseDll);

            reference.VerifyDiagnostics();

            var comp = CreateCompilation(
                tree,
                references: new[] { reference.EmitToImageReference() },
                options: options,
                parseOptions: parseOptions);

            return comp;
        }

        protected static CSharpCompilation CreateCompilationWithMscorlibAndSpan(CSharpTestSource text, CSharpCompilationOptions? options = null, CSharpParseOptions? parseOptions = null)
        {
            var reference = CreateEmptyCompilation(
                TestSources.Span,
                references: new List<MetadataReference>() { NetFramework.mscorlib, NetFramework.SystemCore, NetFramework.MicrosoftCSharp },
                options: TestOptions.UnsafeReleaseDll);

            reference.VerifyDiagnostics();

            var comp = CreateEmptyCompilation(
                text,
                references: new List<MetadataReference>() { NetFramework.mscorlib, NetFramework.SystemCore, NetFramework.MicrosoftCSharp, reference.EmitToImageReference() },
                options: options,
                parseOptions: parseOptions);

            return comp;
        }

        protected static CSharpCompilation CreateCompilationWithMscorlibAndSpanSrc(string text, CSharpCompilationOptions? options = null, CSharpParseOptions? parseOptions = null)
        {
            var textWitSpan = new string[] { text, TestSources.Span };
            var comp = CreateEmptyCompilation(
                textWitSpan,
                references: new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef },
                options: options ?? TestOptions.UnsafeReleaseDll,
                parseOptions: parseOptions);

            return comp;
        }
        #endregion

        #region Index and Range
        protected static CSharpCompilation CreateCompilationWithIndex(CSharpTestSource text, CSharpCompilationOptions? options = null, CSharpParseOptions? parseOptions = null)
        {
            var reference = CreateCompilation(TestSources.Index).VerifyDiagnostics();

            return CreateCompilation(
                text,
                references: new List<MetadataReference>() { reference.EmitToImageReference() },
                options: options,
                parseOptions: parseOptions);
        }

        protected static CSharpCompilation CreateCompilationWithIndexAndRange(CSharpTestSource text, CSharpCompilationOptions? options = null, CSharpParseOptions? parseOptions = null)
        {
            var reference = CreateCompilation(new[] { TestSources.Index, TestSources.Range }).VerifyDiagnostics();

            return CreateCompilation(
                text,
                references: new List<MetadataReference>() { reference.EmitToImageReference() },
                options: options,
                parseOptions: parseOptions);
        }

        protected static CSharpCompilation CreateCompilationWithIndexAndRangeAndSpan(CSharpTestSource text, CSharpCompilationOptions? options = null, CSharpParseOptions? parseOptions = null)
        {
            var reference = CreateCompilation(new[] { TestSources.Index, TestSources.Range, TestSources.Span }, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();

            return CreateCompilation(
                text,
                references: new List<MetadataReference>() { reference.EmitToImageReference() },
                options: options,
                parseOptions: parseOptions);
        }

        protected static CSharpCompilation CreateCompilationWithSpanAndMemoryExtensions(CSharpTestSource text, CSharpCompilationOptions? options = null, CSharpParseOptions? parseOptions = null, TargetFramework targetFramework = TargetFramework.NetCoreApp)
        {
            if (ExecutionConditionUtil.IsCoreClr)
            {
                return CreateCompilation(text, targetFramework: targetFramework, options: options, parseOptions: parseOptions);
            }
            else
            {
                var reference = CreateCompilation(new[] { TestSources.Span, TestSources.MemoryExtensions }, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();

                return CreateCompilation(
                    text,
                    references: new List<MetadataReference>() { reference.EmitToImageReference() },
                    options: options,
                    parseOptions: parseOptions);
            }
        }

        protected static CSharpCompilation CreateCompilationWithIndexAndRangeAndSpanAndMemoryExtensions(CSharpTestSource text, CSharpCompilationOptions? options = null, CSharpParseOptions? parseOptions = null, TargetFramework targetFramework = TargetFramework.NetCoreApp)
        {
            if (ExecutionConditionUtil.IsCoreClr)
            {
                return CreateCompilation(text, targetFramework: targetFramework, options: options, parseOptions: parseOptions);
            }
            else
            {
                var reference = CreateCompilation(new[] { TestSources.Index, TestSources.Range, TestSources.Span, TestSources.MemoryExtensions }, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();

                return CreateCompilation(
                    text,
                    references: new List<MetadataReference>() { reference.EmitToImageReference() },
                    options: options,
                    parseOptions: parseOptions);
            }
        }

        internal static string GetIdForErrorCode(ErrorCode code)
        {
            return MessageProvider.Instance.GetIdForErrorCode((int)code);
        }

        internal static ImmutableDictionary<string, ReportDiagnostic> ReportStructInitializationWarnings { get; } = ImmutableDictionary.CreateRange(
            new[]
            {
                KeyValuePair.Create(GetIdForErrorCode(ErrorCode.WRN_UseDefViolationPropertySupportedVersion), ReportDiagnostic.Warn),
                KeyValuePair.Create(GetIdForErrorCode(ErrorCode.WRN_UseDefViolationFieldSupportedVersion), ReportDiagnostic.Warn),
                KeyValuePair.Create(GetIdForErrorCode(ErrorCode.WRN_UseDefViolationThisSupportedVersion), ReportDiagnostic.Warn),
                KeyValuePair.Create(GetIdForErrorCode(ErrorCode.WRN_UnassignedThisAutoPropertySupportedVersion), ReportDiagnostic.Warn),
                KeyValuePair.Create(GetIdForErrorCode(ErrorCode.WRN_UnassignedThisSupportedVersion), ReportDiagnostic.Warn),
            });

        #endregion

        #region Interpolated string handlers

        internal static string GetInterpolatedStringHandlerDefinition(bool includeSpanOverloads, bool useDefaultParameters, bool useBoolReturns, string? returnExpression = null, bool constructorBoolArg = false, bool constructorSuccessResult = true)
        {
            Debug.Assert(returnExpression == null || useBoolReturns);

            var builder = new StringBuilder();
            builder.AppendLine(@"
namespace System.Runtime.CompilerServices
{
    using System.Text;
    public ref partial struct DefaultInterpolatedStringHandler
    {
        private readonly StringBuilder _builder;
        public DefaultInterpolatedStringHandler(int literalLength, int formattedCount" + (constructorBoolArg ? ", out bool success" : "") + @")
        {
            _builder = new StringBuilder();
            " + (constructorBoolArg ? $"success = {(constructorSuccessResult ? "true" : "false")};" : "") + @"
        }
        public string ToStringAndClear() => _builder.ToString();");

            appendSignature("AppendLiteral(string s)");
            appendBody(includeValue: false, includeAlignment: false, includeFormat: false, isSpan: false);

            if (useDefaultParameters)
            {
                appendSignature("AppendFormatted<T>(T value, int alignment = 0, string format = null)");
                appendBody(includeValue: true, includeAlignment: true, includeFormat: true, isSpan: false);
                appendSignature("AppendFormatted(object value, int alignment = 0, string format = null)");
                appendBody(includeValue: true, includeAlignment: true, includeFormat: true, isSpan: false);
                appendSignature("AppendFormatted(string value, int alignment = 0, string format = null)");
                appendBody(includeValue: true, includeAlignment: true, includeFormat: true, isSpan: false);
            }
            else
            {
                appendNonDefaultVariantsWithGenericAndType("T", "<T>");
                appendNonDefaultVariantsWithGenericAndType("object", generic: null);
                appendNonDefaultVariantsWithGenericAndType("string", generic: null);
            }

            if (includeSpanOverloads)
            {
                if (useDefaultParameters)
                {
                    appendSignature("AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string format = null)");
                    appendBody(includeValue: true, includeAlignment: true, includeFormat: true, isSpan: true);
                }
                else
                {
                    appendNonDefaultVariantsWithGenericAndType("ReadOnlySpan<char>", generic: null, isSpan: true);
                }
            }

            builder.Append(@"
    }
}");
            return builder.ToString();

            void appendBody(bool includeValue, bool includeAlignment, bool includeFormat, bool isSpan)
            {
                if (includeValue)
                {
                    builder.Append($@"
        {{
            _builder.Append(""value:"");
            _builder.Append(value{(isSpan ? "" : "?")}.ToString());");
                }
                else
                {
                    builder.Append(@"
        {
            _builder.Append(s);");
                }

                if (includeAlignment)
                {
                    builder.Append(@"
            _builder.Append("",alignment:"");
            _builder.Append(alignment);");
                }

                if (includeFormat)
                {
                    builder.Append(@"
            _builder.Append("":format:"");
            _builder.Append(format);");
                }

                builder.Append(@"
            _builder.AppendLine();");

                if (useBoolReturns)
                {
                    builder.Append($@"
            return {returnExpression ?? "true"};");
                }

                builder.AppendLine(@"
        }");
            }

            void appendSignature(string nameAndParams)
            {
                builder.Append(@$"
        public {(useBoolReturns ? "bool" : "void")} {nameAndParams}");
            }

            void appendNonDefaultVariantsWithGenericAndType(string type, string? generic, bool isSpan = false)
            {
                appendSignature($"AppendFormatted{generic}({type} value)");
                appendBody(includeValue: true, includeAlignment: false, includeFormat: false, isSpan);
                appendSignature($"AppendFormatted{generic}({type} value, int alignment)");
                appendBody(includeValue: true, includeAlignment: true, includeFormat: false, isSpan);
                appendSignature($"AppendFormatted{generic}({type} value, string format)");
                appendBody(includeValue: true, includeAlignment: false, includeFormat: true, isSpan);
                appendSignature($"AppendFormatted{generic}({type} value, int alignment, string format)");
                appendBody(includeValue: true, includeAlignment: true, includeFormat: true, isSpan);
            }
        }

        internal static readonly string InterpolatedStringHandlerAttribute = @"
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class InterpolatedStringHandlerAttribute : Attribute
    {
        public InterpolatedStringHandlerAttribute()
        {
        }
    }
}
";

        internal static string GetInterpolatedStringCustomHandlerType(string name, string type, bool useBoolReturns, bool includeOneTimeHelpers = true, bool includeTrailingOutConstructorParameter = false)
        {
            var returnType = useBoolReturns ? "bool" : "void";
            var returnStatement = useBoolReturns ? "return true;" : "return;";

            var cultureInfoHandler = @"
public class CultureInfoNormalizer
{
    private static CultureInfo originalCulture;

    public static void Normalize()
    {
        originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    public static void Reset()
    {
        CultureInfo.CurrentCulture = originalCulture;
    }
}
";

            var nameWithGenericsTrimmed = name.IndexOf("<") is not -1 and var index ? name[..index] : name;

            return (includeOneTimeHelpers ? "using System.Globalization;\n" : "") + @"
using System.Text;
[System.Runtime.CompilerServices.InterpolatedStringHandler]
public " + type + " " + name + @"
{
    private readonly StringBuilder _builder;
    public " + nameWithGenericsTrimmed + @"(int literalLength, int formattedCount" + (includeTrailingOutConstructorParameter ? ", out bool success" : "") + @")
    {
        " + (includeTrailingOutConstructorParameter ? "success = true;" : "") + @"
        _builder = new();
    }
    public " + returnType + @" AppendLiteral(string literal)
    {
        _builder.AppendLine(""literal:"" + literal);
        " + returnStatement + @"
    }
    public " + returnType + @" AppendFormatted(object o, int alignment = 0, string format = null)
    {
        _builder.AppendLine(""value:"" + o?.ToString());
        _builder.AppendLine(""alignment:"" + alignment.ToString());
        _builder.AppendLine(""format:"" + format);
        " + returnStatement + @"
    }
    public override string ToString() => _builder.ToString();
}
" + (includeOneTimeHelpers ? InterpolatedStringHandlerAttribute + cultureInfoHandler : "");
        }

        internal static readonly string InterpolatedStringHandlerArgumentAttribute = @"
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class InterpolatedStringHandlerArgumentAttribute : Attribute
    {
        public InterpolatedStringHandlerArgumentAttribute(string argument) => Arguments = new string[] { argument };
        public InterpolatedStringHandlerArgumentAttribute(params string[] arguments) => Arguments = arguments;
        public string[] Arguments { get; }
    }
}
";

        #endregion

        #region Theory Helpers

        public static IEnumerable<object[]> NonNullTypesTrueAndFalseDebugDll
        {
            get
            {
                return new List<object[]>()
                {
                    new object[] { WithNullableEnable(TestOptions.DebugDll) },
                    new object[] { WithNullableDisable(TestOptions.DebugDll) }
                };
            }
        }

        public static IEnumerable<object[]> NonNullTypesTrueAndFalseReleaseDll
        {
            get
            {
                return new List<object[]>()
                {
                    new object[] { WithNullableEnable(TestOptions.ReleaseDll) },
                    new object[] { WithNullableDisable(TestOptions.ReleaseDll) }
                };
            }
        }

        public static IEnumerable<object[]> FileScopedOrBracedNamespace
        {
            get
            {
                return new List<object[]>()
                {
                    new object[] { ";", "" },
                    new object[] { "{", "}" }
                };
            }
        }
        #endregion

        #region Runtime Async

        internal static CSharpParseOptions WithRuntimeAsync(CSharpParseOptions options) => options.WithFeature("runtime-async", "on");

        internal static CSharpCompilation CreateRuntimeAsyncCompilation(CSharpTestSource source, CSharpCompilationOptions? options = null, CSharpParseOptions? parseOptions = null, bool includeSuppression = true)
        {
            parseOptions ??= WithRuntimeAsync(TestOptions.RegularPreview);
            var syntaxTrees = source.GetSyntaxTrees(parseOptions, sourceFileName: "");
            if (options == null)
            {
                options = CheckForTopLevelStatements(syntaxTrees);
            }

            if (includeSuppression)
            {
                options = options.WithSpecificDiagnosticOptions("SYSLIB5007", ReportDiagnostic.Suppress);
            }

            return CreateCompilation(source, options: options, parseOptions: parseOptions, targetFramework: TargetFramework.Net100);
        }

        /// <summary>
        /// Dumps all the cref xml doc nodes with their associated symbols in a format convenient for testing.
        /// </summary>
        internal static IEnumerable<string> PrintXmlCrefSymbols(SyntaxTree tree, SemanticModel model)
        {
            var docComments = tree.GetCompilationUnitRoot().DescendantTrivia().Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>();
            var crefs = docComments.SelectMany(doc => doc.DescendantNodes().OfType<XmlCrefAttributeSyntax>());
            var result = crefs.Select(name => print(name));
            return result;

            string print(XmlCrefAttributeSyntax cref)
            {
                CrefSyntax crefSyntax = cref.Cref;
                var symbol = model.GetSymbolInfo(crefSyntax).Symbol;
                var symbolDisplay = symbol is null ? "null" : symbol.ToTestDisplayString();
                return (crefSyntax, symbolDisplay).ToString();
            }
        }

        /// <summary>
        /// Dumps all the name xml doc attributes with their associated symbols in a format convenient for testing.
        /// </summary>
        internal static IEnumerable<string> PrintXmlNameSymbols(SyntaxTree tree, SemanticModel model)
        {
            var docComments = tree.GetCompilationUnitRoot().DescendantTrivia().Select(trivia => trivia.GetStructure()).OfType<DocumentationCommentTriviaSyntax>();
            var xmlNames = docComments.SelectMany(doc => doc.DescendantNodes().OfType<XmlNameAttributeSyntax>());
            var result = xmlNames.Select(name => print(name));
            return result;

            string print(XmlNameAttributeSyntax name)
            {
                IdentifierNameSyntax identifier = name.Identifier;
                var symbol = model.GetSymbolInfo(identifier).Symbol;
                var symbolDisplay = symbol is null ? "null" : symbol.ToTestDisplayString();
                return (identifier, symbolDisplay).ToString();
            }
        }

        #endregion

        protected static readonly string s_IAsyncEnumerable = @"
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken token = default);
    }

    public interface IAsyncEnumerator<out T> : System.IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask<bool> MoveNextAsync();
        T Current { get; }
    }
}
namespace System
{
    public interface IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask DisposeAsync();
    }
}
";
    }
}
