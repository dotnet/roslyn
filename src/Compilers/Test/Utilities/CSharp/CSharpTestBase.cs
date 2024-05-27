// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
using Roslyn.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.TestMetadata;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    public abstract class CSharpTestBase : CommonTestBase
    {
        protected const string NullableAttributeDefinition = @"
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

        protected const string NullableContextAttributeDefinition = @"
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

        protected const string NullablePublicOnlyAttributeDefinition = @"
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
        protected const string AllowNullAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property)]
    public sealed class AllowNullAttribute : Attribute
    {
    }
}";

        protected const string DisallowNullAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property)]
    public sealed class DisallowNullAttribute : Attribute
    {
    }
}";

        protected const string MaybeNullAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue)]
    public sealed class MaybeNullAttribute : Attribute
    {
    }
}
";

        protected const string MaybeNullWhenAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class MaybeNullWhenAttribute : Attribute
    {
        public MaybeNullWhenAttribute(bool when) { }
    }
}
";

        protected const string NotNullAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue)]
    public sealed class NotNullAttribute : Attribute
    {
    }
}
";

        protected const string NotNullWhenAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class NotNullWhenAttribute : Attribute
    {
        public NotNullWhenAttribute(bool when) { }
    }
}
";

        protected const string MemberNotNullAttributeDefinition = @"
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

        protected const string MemberNotNullWhenAttributeDefinition = @"
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

        protected const string DoesNotReturnIfAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class DoesNotReturnIfAttribute : Attribute
    {
        public DoesNotReturnIfAttribute(bool condition) { }
    }
}
";

        protected const string DoesNotReturnAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DoesNotReturnAttribute : Attribute
    {
        public DoesNotReturnAttribute() { }
    }
}
";

        protected const string NotNullIfNotNullAttributeDefinition = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    public sealed class NotNullIfNotNullAttribute : Attribute
    {
        public NotNullIfNotNullAttribute(string parameterName) { }
    }
}
";

        protected const string IsExternalInitTypeDefinition = @"
namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit
    {
    }
}
";

        protected const string IAsyncDisposableDefinition = @"
namespace System
{
    public interface IAsyncDisposable
    {
       System.Threading.Tasks.ValueTask DisposeAsync();
    }
}
";

        protected const string NonDisposableAsyncEnumeratorDefinition = @"
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

        protected const string DisposableAsyncEnumeratorDefinition = @"
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

        protected const string AsyncStreamsTypes = DisposableAsyncEnumeratorDefinition + CommonAsyncStreamsTypes;

        protected const string CommonAsyncStreamsTypes = @"
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

        protected const string EnumeratorCancellationAttributeType = @"
namespace System.Runtime.CompilerServices
{
    [System.AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class EnumeratorCancellationAttribute : Attribute
    {
        public EnumeratorCancellationAttribute() { }
    }
}
";

        protected const string NativeIntegerAttributeDefinition =
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

        protected const string UnmanagedCallersOnlyAttributeDefinition =
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

        protected const string UnscopedRefAttributeDefinition =
@"namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public sealed class UnscopedRefAttribute : Attribute
    {
    }
}";

        protected const string RefSafetyRulesAttributeDefinition =
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

        protected const string RequiredMemberAttribute = @"
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

        protected const string SetsRequiredMembersAttribute = @"
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

        internal const string CompilerFeatureRequiredAttribute = """
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
            """;

        internal const string CollectionBuilderAttributeDefinition = """
            namespace System.Runtime.CompilerServices
            {
                [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
                public sealed class CollectionBuilderAttribute : Attribute
                {
                    public CollectionBuilderAttribute(Type builderType, string methodName) { }
                }
            }
            """;

        protected static T GetSyntax<T>(SyntaxTree tree, string text)
        {
            return GetSyntaxes<T>(tree, text).Single();
        }

        protected static IEnumerable<T> GetSyntaxes<T>(SyntaxTree tree, string text)
        {
            return tree.GetRoot().DescendantNodes().OfType<T>().Where(e => e.ToString() == text);
        }

        protected static CSharpCompilationOptions WithNullableEnable(CSharpCompilationOptions options = null)
        {
            return WithNullable(options, NullableContextOptions.Enable);
        }

        protected static CSharpCompilationOptions WithNullableDisable(CSharpCompilationOptions options = null)
        {
            return WithNullable(options, NullableContextOptions.Disable);
        }

        protected static CSharpCompilationOptions WithNullable(NullableContextOptions nullableContextOptions)
        {
            return WithNullable(null, nullableContextOptions);
        }

        protected static CSharpCompilationOptions WithNullable(CSharpCompilationOptions options, NullableContextOptions nullableContextOptions)
        {
            return (options ?? TestOptions.ReleaseDll).WithNullableContextOptions(nullableContextOptions);
        }

        internal CompilationVerifier CompileAndVerifyWithMscorlib40(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            IEnumerable<ResourceDescription> manifestResources = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> assemblyValidator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            bool trimOutput = true,
            int? expectedReturnCode = null,
            string[] args = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            EmitOptions emitOptions = null,
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
            IEnumerable<MetadataReference> references = null,
            IEnumerable<ResourceDescription> manifestResources = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> assemblyValidator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            bool trimOutput = true,
            int? expectedReturnCode = null,
            string[] args = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            EmitOptions emitOptions = null,
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
            IEnumerable<MetadataReference> references = null,
            IEnumerable<ResourceDescription> manifestResources = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> assemblyValidator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            bool trimOutput = true,
            int? expectedReturnCode = null,
            string[] args = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            EmitOptions emitOptions = null,
            Verification verify = default)
        {
            options = options ?? TestOptions.ReleaseDll.WithOutputKind((expectedOutput != null) ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary);
            var compilation = CreateExperimentalCompilationWithMscorlib45(source, feature, references, options, parseOptions, assemblyName: GetUniqueName());

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
            IEnumerable<MetadataReference> references = null,
            IEnumerable<ResourceDescription> manifestResources = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> assemblyValidator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            bool trimOutput = true,
            int? expectedReturnCode = null,
            string[] args = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            EmitOptions emitOptions = null,
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
            IEnumerable<MetadataReference> references = null,
            IEnumerable<ResourceDescription> manifestResources = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> assemblyValidator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            bool trimOutput = true,
            int? expectedReturnCode = null,
            string[] args = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            EmitOptions emitOptions = null,
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
            IEnumerable<MetadataReference> references = null,
            IEnumerable<ResourceDescription> manifestResources = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> assemblyValidator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            bool trimOutput = true,
            int? expectedReturnCode = null,
            string[] args = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            EmitOptions emitOptions = null,
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
            IEnumerable<ResourceDescription> manifestResources = null,
            IEnumerable<ModuleData> dependencies = null,
            Action<ModuleSymbol> sourceSymbolValidator = null,
            Action<PEAssembly> validator = null,
            Action<ModuleSymbol> symbolValidator = null,
            SignatureDescription[] expectedSignatures = null,
            string expectedOutput = null,
            bool trimOutput = true,
            int? expectedReturnCode = null,
            string[] args = null,
            EmitOptions emitOptions = null,
            Verification verify = default)
        {
            Action<IModuleSymbol> translate(Action<ModuleSymbol> action)
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

        public static SyntaxTree Parse(string text, string filename = "", CSharpParseOptions options = null, Encoding encoding = null, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1)
            => CSharpTestSource.Parse(text, filename, options, encoding, checksumAlgorithm);

        public static SyntaxTree[] Parse(IEnumerable<string> sources, CSharpParseOptions options = null)
        {
            if (sources == null || !sources.Any())
            {
                return new SyntaxTree[] { };
            }

            return Parse(options, sources.ToArray());
        }

        public static SyntaxTree[] Parse(CSharpParseOptions options = null, params string[] sources)
        {
            if (sources == null || (sources.Length == 1 && null == sources[0]))
            {
                return new SyntaxTree[] { };
            }

            return sources.Select((src, index) => Parse(src, filename: $"{index}.cs", options: options)).ToArray();
        }

        public static SyntaxTree ParseWithRoundTripCheck(string text, CSharpParseOptions options = null)
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
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            bool appendDefaultHeader = true) => CreateCompilationWithILAndMscorlib40(source, ilSource, targetFramework, references, options, parseOptions, appendDefaultHeader);

        public static CSharpCompilation CreateCompilationWithILAndMscorlib40(
            CSharpTestSource source,
            string ilSource,
            TargetFramework targetFramework = TargetFramework.Mscorlib40,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            bool appendDefaultHeader = true)
        {
            MetadataReference ilReference = CompileIL(ilSource, appendDefaultHeader);
            var allReferences = TargetFrameworkUtil.GetReferences(targetFramework, references).Add(ilReference);
            return CreateEmptyCompilation(source, allReferences, options, parseOptions);
        }

        public static CSharpCompilation CreateCompilationWithMscorlib40(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib40, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithMscorlib45(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "",
            bool skipUsesIsNullable = false) => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib45, assemblyName, sourceFileName, skipUsesIsNullable);

        public static CSharpCompilation CreateCompilationWithMscorlib45(
            string[] source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "",
            bool skipUsesIsNullable = false) => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib45, assemblyName, sourceFileName, skipUsesIsNullable);

        public static CSharpCompilation CreateCompilationWithMscorlib46(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib46, assemblyName, sourceFileName);

        internal static CSharpCompilation CreateExperimentalCompilationWithMscorlib45(
            CSharpTestSource source,
            MessageID feature,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "",
            bool skipUsesIsNullable = false) => CreateCompilationCore(source, TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib45, references), options, parseOptions, assemblyName, sourceFileName, skipUsesIsNullable, experimentalFeature: feature);

        public static CSharpCompilation CreateCompilationWithWinRT(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.WinRT, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithMscorlib45AndCSharp(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib45AndCSharp, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithMscorlib40AndSystemCore(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib40AndSystemCore, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithMscorlib40AndSystemCore(
            string[] source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib40AndSystemCore, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithCSharp(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "") => CreateCompilation(source, references, options, parseOptions, TargetFramework.StandardAndCSharp, assemblyName, sourceFileName);

        public static CSharpCompilation CreateCompilationWithMscorlib40AndDocumentationComments(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "")
        {
            parseOptions = parseOptions != null ? parseOptions.WithDocumentationMode(DocumentationMode.Diagnose) : TestOptions.RegularWithDocumentationComments;
            options = (options ?? TestOptions.ReleaseDll).WithXmlReferenceResolver(XmlFileResolver.Default);
            return CreateCompilation(source, references, options, parseOptions, TargetFramework.Mscorlib40, assemblyName, sourceFileName);
        }

        public static CSharpCompilation CreateCompilationWithTasksExtensions(
                CSharpTestSource source,
                IEnumerable<MetadataReference> references = null,
                CSharpCompilationOptions options = null,
                CSharpParseOptions parseOptions = null,
                string assemblyName = "",
                string sourceFileName = "")
        {
            IEnumerable<MetadataReference> allReferences;

            if (RuntimeUtilities.IsCoreClrRuntime)
            {
                allReferences = TargetFrameworkUtil.NetStandard20References;
                allReferences = allReferences.Concat(new[] { SystemThreadingTasksExtensions.NetStandard20Lib });
            }
            else
            {
                allReferences = TargetFrameworkUtil.Mscorlib461ExtendedReferences;
                allReferences = allReferences.Concat(new[] { Net461.References.SystemThreadingTasks, SystemThreadingTasksExtensions.PortableLib });
            }

            if (references != null)
            {
                allReferences = allReferences.Concat(references);
            }

            return CreateCompilation(source, allReferences, options, parseOptions, TargetFramework.Empty, assemblyName, sourceFileName);
        }

        public static CSharpCompilation CreateCompilation(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            TargetFramework targetFramework = TargetFramework.Standard,
            string assemblyName = "",
            string sourceFileName = "",
            bool skipUsesIsNullable = false)
        {
            return CreateEmptyCompilation(source, TargetFrameworkUtil.GetReferences(targetFramework, references), options, parseOptions, assemblyName, sourceFileName, skipUsesIsNullable);
        }

        public static CSharpCompilation CreateEmptyCompilation(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            string assemblyName = "",
            string sourceFileName = "",
            bool skipUsesIsNullable = false,
            bool skipExtraValidation = false) => CreateCompilationCore(source, references, options, parseOptions, assemblyName, sourceFileName, skipUsesIsNullable, experimentalFeature: null, skipExtraValidation: skipExtraValidation);

        private static CSharpCompilation CreateCompilationCore(
            CSharpTestSource source,
            IEnumerable<MetadataReference> references,
            CSharpCompilationOptions options,
            CSharpParseOptions parseOptions,
            string assemblyName,
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
                assemblyName == "" ? GetUniqueName() : assemblyName,
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

        private static CSharpCompilationOptions CheckForTopLevelStatements(SyntaxTree[] syntaxTrees)
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
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null)
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
           IEnumerable<MetadataReference> references = null,
           CSharpCompilationOptions options = null,
           CSharpParseOptions parseOptions = null,
           CSharpCompilation previous = null,
           Type returnType = null,
           Type hostObjectType = null)
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
           IEnumerable<MetadataReference> references = null,
           CSharpCompilationOptions options = null,
           CSharpParseOptions parseOptions = null,
           CSharpCompilation previous = null,
           Type returnType = null,
           Type hostObjectType = null)
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

        public CompilationVerifier CompileWithCustomILSource(string cSharpSource, string ilSource, Action<CSharpCompilation> compilationVerifier = null, bool importInternals = true, string expectedOutput = null, TargetFramework targetFramework = TargetFramework.Standard)
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
        internal CompilationVerifier CompileAndVerifyException<T>(string source, string expectedMessage = null, bool allowUnsafe = false, Verification verify = default) where T : Exception
        {
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe));
            return CompileAndVerifyException<T>(comp, expectedMessage, verify);
        }

        internal CompilationVerifier CompileAndVerifyException<T>(CSharpCompilation comp, string expectedMessage = null, Verification verify = default) where T : Exception
        {
            try
            {
                CompileAndVerify(comp, expectedOutput: "", verify: verify); //need expected output to force execution
                Assert.False(true, string.Format("Expected exception {0}({1})", typeof(T).Name, expectedMessage));
            }
            catch (ExecutionException x)
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

        protected static List<SyntaxNode> GetSyntaxNodeList(SyntaxNode node, List<SyntaxNode> synList)
        {
            if (synList == null)
                synList = new List<SyntaxNode>();

            synList.Add(node);

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                    synList = GetSyntaxNodeList(child.AsNode(), synList);
            }

            return synList;
        }

        protected static SyntaxNode GetSyntaxNodeForBinding(List<SyntaxNode> synList)
        {
            return GetSyntaxNodeOfTypeForBinding<SyntaxNode>(synList);
        }

        protected const string StartString = "/*<bind>*/";
        protected const string EndString = "/*</bind>*/";

        protected static TNode GetSyntaxNodeOfTypeForBinding<TNode>(List<SyntaxNode> synList) where TNode : SyntaxNode
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
        public TNode GetBindingNode<TNode>(CSharpCompilation compilation, int treeIndex = 0) where TNode : SyntaxNode
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

        private static TNode FindBindingNode<TNode>(SyntaxTree tree, string startTag, string endTag) where TNode : SyntaxNode
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
            return attributes.Select(a => a.AttributeClass.Name);
        }

        internal static IEnumerable<string> GetAttributeNames(ImmutableArray<CSharpAttributeData> attributes)
        {
            return attributes.Select(a => a.AttributeClass.Name);
        }

        internal static IEnumerable<string> GetAttributeStrings(ImmutableArray<CSharpAttributeData> attributes)
        {
            return attributes.Select(a => a.ToString());
        }

        internal static IEnumerable<string> GetAttributeStrings(IEnumerable<CSharpAttributeData> attributes)
        {
            return attributes.Select(a => a.ToString());
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

        internal static string GetDocumentationCommentText(CSharpCompilation compilation, string outputName = null, SyntaxTree filterTree = null, TextSpan? filterSpanWithinTree = null, bool ensureEnglishUICulture = false, params DiagnosticDescription[] expectedDiagnostics)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
                CultureInfo saveUICulture = null;

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
                    diagnostics.AddRange(bindingDiagnostics.DiagnosticBag);
                    bindingDiagnostics.Free();

                    if (ensureEnglishUICulture)
                    {
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

        #endregion

        #region IL Validation

        internal override string VisualizeRealIL(IModuleSymbol peModule, CompilationTestData.MethodData methodData, IReadOnlyDictionary<int, string> markers, bool areLocalsZeroed)
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
        internal static unsafe string VisualizeRealIL(PEModuleSymbol peModule, CompilationTestData.MethodData methodData, IReadOnlyDictionary<int, string> markers, bool areLocalsZeroed)
        {
            var typeName = GetContainingTypeMetadataName(methodData.Method);
            // TODO (tomat): global methods (typeName == null)

            var type = peModule.ContainingAssembly.GetTypeByMetadataName(typeName);

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

        private static string GetContainingTypeMetadataName(IMethodSymbolInternal method)
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
                Symbol symbol;

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

                return symbol?.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat) ?? type.ToString();
            }
        }

        #endregion

        #region IOperation tree validation

        protected static (IOperation operation, SyntaxNode node) GetOperationAndSyntaxForTest<TSyntaxNode>(CSharpCompilation compilation)
            where TSyntaxNode : SyntaxNode
        {
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            SyntaxNode syntaxNode = GetSyntaxNodeOfTypeForBinding<TSyntaxNode>(GetSyntaxNodeList(tree));
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

        protected static string GetOperationTreeForTest<TSyntaxNode>(CSharpCompilation compilation)
            where TSyntaxNode : SyntaxNode
        {
            var (operation, syntax) = GetOperationAndSyntaxForTest<TSyntaxNode>(compilation);
            return operation != null ? OperationTreeVerifier.GetOperationTree(compilation, operation) : null;
        }

        protected static string GetOperationTreeForTest(CSharpCompilation compilation, IOperation operation)
        {
            return operation != null ? OperationTreeVerifier.GetOperationTree(compilation, operation) : null;
        }

        protected static string GetOperationTreeForTest<TSyntaxNode>(
            CSharpTestSource testSrc,
            CSharpCompilationOptions compilationOptions = null,
            CSharpParseOptions parseOptions = null,
            bool useLatestFrameworkReferences = false)
            where TSyntaxNode : SyntaxNode
        {
            var targetFramework = useLatestFrameworkReferences ? TargetFramework.Mscorlib46Extended : TargetFramework.Standard;
            var compilation = CreateCompilation(testSrc, targetFramework: targetFramework, options: compilationOptions ?? TestOptions.ReleaseDll, parseOptions: parseOptions);
            return GetOperationTreeForTest<TSyntaxNode>(compilation);
        }

        protected static IOperation VerifyOperationTreeForTest<TSyntaxNode>(CSharpCompilation compilation, string expectedOperationTree, Action<IOperation, Compilation, SyntaxNode> additionalOperationTreeVerifier = null)
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

        protected static void VerifyOperationTree(CSharpCompilation compilation, IOperation operation, string expectedOperationTree)
        {
            Assert.NotNull(operation);
            var actualOperationTree = GetOperationTreeForTest(compilation, operation);
            OperationTreeVerifier.Verify(expectedOperationTree, actualOperationTree);
        }

        protected static void VerifyFlowGraphForTest<TSyntaxNode>(CSharpCompilation compilation, string expectedFlowGraph)
            where TSyntaxNode : SyntaxNode
        {
            var tree = compilation.SyntaxTrees[0];
            SyntaxNode syntaxNode = GetSyntaxNodeOfTypeForBinding<TSyntaxNode>(GetSyntaxNodeList(tree));
            Debug.Assert(syntaxNode is not null, "Did you forget to place /*<bind>*/ comments in your source?");
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
            CSharpCompilationOptions compilationOptions = null,
            CSharpParseOptions parseOptions = null,
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
            Action<IOperation, Compilation, SyntaxNode> additionalOperationTreeVerifier = null)
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
            CSharpCompilationOptions compilationOptions = null,
            CSharpParseOptions parseOptions = null,
            MetadataReference[] references = null,
            Action<IOperation, Compilation, SyntaxNode> additionalOperationTreeVerifier = null,
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
            CSharpCompilationOptions compilationOptions = null,
            CSharpParseOptions parseOptions = null,
            MetadataReference[] references = null,
            Action<IOperation, Compilation, SyntaxNode> additionalOperationTreeVerifier = null)
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
            CSharpCompilationOptions compilationOptions = null,
            MetadataReference[] references = null,
            Action<IOperation, Compilation, SyntaxNode> additionalOperationTreeVerifier = null,
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
            CSharpCompilationOptions compilationOptions = null,
            CSharpParseOptions parseOptions = null,
            MetadataReference[] references = null,
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
            CSharpCompilationOptions compilationOptions = null,
            CSharpParseOptions parseOptions = null,
            MetadataReference[] references = null)
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
            CSharpCompilationOptions compilationOptions = null,
            CSharpParseOptions parseOptions = null,
            MetadataReference[] references = null,
            Action<IOperation, Compilation, SyntaxNode> additionalOperationTreeVerifier = null,
            TargetFramework targetFramework = TargetFramework.Standard)
            where TSyntaxNode : SyntaxNode
        {
            var ilReference = CreateMetadataReferenceFromIlSource(ilSource);
            VerifyOperationTreeAndDiagnosticsForTest<TSyntaxNode>(testSrc, expectedOperationTree, expectedDiagnostics, compilationOptions, parseOptions, new[] { ilReference }, additionalOperationTreeVerifier, targetFramework);
            return ilReference;
        }

        #endregion

        #region Span

        protected static CSharpCompilation CreateCompilationWithSpan(CSharpTestSource tree, CSharpCompilationOptions options = null, CSharpParseOptions parseOptions = null)
        {
            var reference = CreateCompilation(
                SpanSource,
                options: TestOptions.UnsafeReleaseDll);

            reference.VerifyDiagnostics();

            var comp = CreateCompilation(
                tree,
                references: new[] { reference.EmitToImageReference() },
                options: options,
                parseOptions: parseOptions);

            return comp;
        }

        protected static CSharpCompilation CreateCompilationWithMscorlibAndSpan(CSharpTestSource text, CSharpCompilationOptions options = null, CSharpParseOptions parseOptions = null)
        {
            var reference = CreateEmptyCompilation(
                SpanSource,
                references: new List<MetadataReference>() { Net451.mscorlib, Net451.SystemCore, Net451.MicrosoftCSharp },
                options: TestOptions.UnsafeReleaseDll);

            reference.VerifyDiagnostics();

            var comp = CreateEmptyCompilation(
                text,
                references: new List<MetadataReference>() { Net451.mscorlib, Net451.SystemCore, Net451.MicrosoftCSharp, reference.EmitToImageReference() },
                options: options,
                parseOptions: parseOptions);

            return comp;
        }

        protected static CSharpCompilation CreateCompilationWithMscorlibAndSpanSrc(string text, CSharpCompilationOptions options = null, CSharpParseOptions parseOptions = null)
        {
            var textWitSpan = new string[] { text, SpanSource };
            var comp = CreateEmptyCompilation(
                textWitSpan,
                references: new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef },
                options: options ?? TestOptions.UnsafeReleaseDll,
                parseOptions: parseOptions);

            return comp;
        }

        protected static readonly string SpanSource = @"
namespace System
    {
        public readonly ref struct Span<T>
        {
            private readonly T[] arr;

            public ref T this[int i] => ref arr[i];
            public override int GetHashCode() => 1;
            public int Length { get; }
            public bool IsEmpty => Length == 0;

            unsafe public Span(void* pointer, int length)
            {
                this.arr = Helpers.ToArray<T>(pointer, length);
                this.Length = length;
            }

            public Span(T[] arr)
            {
                this.arr = arr;
                this.Length = arr.Length;
            }

            public Span(T[] arr, int start, int length)
            {
                this.arr = new T[length];
                Array.Copy(arr, start, this.arr, 0, length);
                this.Length = length;
            }

            public void CopyTo(Span<T> other) { }

            /// <summary>Gets an enumerator for this span.</summary>
            public Enumerator GetEnumerator() => new Enumerator(this);

            /// <summary>Enumerates the elements of a <see cref=""Span{T}""/>.</summary>
            public ref struct Enumerator
            {
                /// <summary>The span being enumerated.</summary>
                private readonly Span<T> _span;
                /// <summary>The next index to yield.</summary>
                private int _index;

                /// <summary>Initialize the enumerator.</summary>
                /// <param name=""span"">The span to enumerate.</param>
                internal Enumerator(Span<T> span)
                {
                    _span = span;
                    _index = -1;
                }

                /// <summary>Advances the enumerator to the next element of the span.</summary>
                public bool MoveNext()
                {
                    int index = _index + 1;
                    if (index < _span.Length)
                    {
                        _index = index;
                        return true;
                    }

                    return false;
                }

                /// <summary>Gets the element at the current position of the enumerator.</summary>
                public ref T Current
                {
                    get => ref _span[_index];
                }
            }

            public static implicit operator Span<T>(T[] array) => new Span<T>(array);
        }

        public readonly ref struct ReadOnlySpan<T>
        {
            private readonly T[] arr;

            public ref readonly T this[int i] => ref arr[i];
            public override int GetHashCode() => 2;
            public int Length { get; }
            public bool IsEmpty => Length == 0;

            unsafe public ReadOnlySpan(void* pointer, int length)
            {
                this.arr = Helpers.ToArray<T>(pointer, length);
                this.Length = length;
            }

            public ReadOnlySpan(T[] arr)
            {
                this.arr = arr;
                this.Length = arr.Length;
            }

            public ReadOnlySpan(T[] arr, int start, int length)
            {
                this.arr = new T[length];
                Array.Copy(arr, start, this.arr, 0, length);
                this.Length = length;
            }

            public void CopyTo(Span<T> other) { }

            /// <summary>Gets an enumerator for this span.</summary>
            public Enumerator GetEnumerator() => new Enumerator(this);

            /// <summary>Enumerates the elements of a <see cref=""Span{T}""/>.</summary>
            public ref struct Enumerator
            {
                /// <summary>The span being enumerated.</summary>
                private readonly ReadOnlySpan<T> _span;
                /// <summary>The next index to yield.</summary>
                private int _index;

                /// <summary>Initialize the enumerator.</summary>
                /// <param name=""span"">The span to enumerate.</param>
                internal Enumerator(ReadOnlySpan<T> span)
                {
                    _span = span;
                    _index = -1;
                }

                /// <summary>Advances the enumerator to the next element of the span.</summary>
                public bool MoveNext()
                {
                    int index = _index + 1;
                    if (index < _span.Length)
                    {
                        _index = index;
                        return true;
                    }

                    return false;
                }

                /// <summary>Gets the element at the current position of the enumerator.</summary>
                public ref readonly T Current
                {
                    get => ref _span[_index];
                }
            }

            public static implicit operator ReadOnlySpan<T>(T[] array) => array == null ? default : new ReadOnlySpan<T>(array);
        }

        public readonly ref struct SpanLike<T>
        {
            public readonly Span<T> field;
        }

        public enum Color: sbyte
        {
            Red,
            Green,
            Blue
        }

        public static unsafe class Helpers
        {
            public static T[] ToArray<T>(void* ptr, int count)
            {
                if (ptr == null)
                {
                    return null;
                }

                if (typeof(T) == typeof(sbyte))
                {
                    var arr = new sbyte[count];
                    for(int i = 0; i < count; i++)
                    {
                        arr[i] = ((sbyte*)ptr)[i];
                    }

                    return (T[])(object)arr;
                }

                if (typeof(T) == typeof(byte))
                {
                    var arr = new byte[count];
                    for(int i = 0; i < count; i++)
                    {
                        arr[i] = ((byte*)ptr)[i];
                    }

                    return (T[])(object)arr;
                }

                if (typeof(T) == typeof(short))
                {
                    var arr = new short[count];
                    for(int i = 0; i < count; i++)
                    {
                        arr[i] = ((short*)ptr)[i];
                    }

                    return (T[])(object)arr;
                }

                if (typeof(T) == typeof(ushort))
                {
                    var arr = new ushort[count];
                    for(int i = 0; i < count; i++)
                    {
                        arr[i] = ((ushort*)ptr)[i];
                    }

                    return (T[])(object)arr;
                }

                if (typeof(T) == typeof(int))
                {
                    var arr = new int[count];
                    for(int i = 0; i < count; i++)
                    {
                        arr[i] = ((int*)ptr)[i];
                    }

                    return (T[])(object)arr;
                }

                if (typeof(T) == typeof(uint))
                {
                    var arr = new uint[count];
                    for(int i = 0; i < count; i++)
                    {
                        arr[i] = ((uint*)ptr)[i];
                    }

                    return (T[])(object)arr;
                }

                if (typeof(T) == typeof(long))
                {
                    var arr = new long[count];
                    for(int i = 0; i < count; i++)
                    {
                        arr[i] = ((long*)ptr)[i];
                    }

                    return (T[])(object)arr;
                }

                if (typeof(T) == typeof(ulong))
                {
                    var arr = new ulong[count];
                    for(int i = 0; i < count; i++)
                    {
                        arr[i] = ((ulong*)ptr)[i];
                    }

                    return (T[])(object)arr;
                }

                if (typeof(T) == typeof(char))
                {
                    var arr = new char[count];
                    for(int i = 0; i < count; i++)
                    {
                        arr[i] = ((char*)ptr)[i];
                    }

                    return (T[])(object)arr;
                }

                if (typeof(T) == typeof(Color))
                {
                    var arr = new Color[count];
                    for(int i = 0; i < count; i++)
                    {
                        arr[i] = ((Color*)ptr)[i];
                    }

                    return (T[])(object)arr;
                }

                throw new Exception(""add a case for: "" + typeof(T));
            }
        }
    }";
        #endregion

        #region Index and Range
        protected static CSharpCompilation CreateCompilationWithIndex(CSharpTestSource text, CSharpCompilationOptions options = null, CSharpParseOptions parseOptions = null)
        {
            var reference = CreateCompilation(TestSources.Index).VerifyDiagnostics();

            return CreateCompilation(
                text,
                references: new List<MetadataReference>() { reference.EmitToImageReference() },
                options: options,
                parseOptions: parseOptions);
        }

        protected static CSharpCompilation CreateCompilationWithIndexAndRange(CSharpTestSource text, CSharpCompilationOptions options = null, CSharpParseOptions parseOptions = null)
        {
            var reference = CreateCompilation(new[] { TestSources.Index, TestSources.Range }).VerifyDiagnostics();

            return CreateCompilation(
                text,
                references: new List<MetadataReference>() { reference.EmitToImageReference() },
                options: options,
                parseOptions: parseOptions);
        }

        protected static CSharpCompilation CreateCompilationWithIndexAndRangeAndSpan(CSharpTestSource text, CSharpCompilationOptions options = null, CSharpParseOptions parseOptions = null)
        {
            var reference = CreateCompilation(new[] { TestSources.Index, TestSources.Range, TestSources.Span }, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();

            return CreateCompilation(
                text,
                references: new List<MetadataReference>() { reference.EmitToImageReference() },
                options: options,
                parseOptions: parseOptions);
        }

        protected static CSharpCompilation CreateCompilationWithSpanAndMemoryExtensions(CSharpTestSource text, CSharpCompilationOptions options = null, CSharpParseOptions parseOptions = null, TargetFramework targetFramework = TargetFramework.NetCoreApp)
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

        internal static string GetIdForErrorCode(ErrorCode code)
        {
            return MessageProvider.Instance.GetIdForErrorCode((int)code);
        }

        internal static ImmutableDictionary<string, ReportDiagnostic> ReportStructInitializationWarnings { get; } = ImmutableDictionary.CreateRange(
            new[]
            {
                KeyValuePairUtil.Create(GetIdForErrorCode(ErrorCode.WRN_UseDefViolationPropertySupportedVersion), ReportDiagnostic.Warn),
                KeyValuePairUtil.Create(GetIdForErrorCode(ErrorCode.WRN_UseDefViolationFieldSupportedVersion), ReportDiagnostic.Warn),
                KeyValuePairUtil.Create(GetIdForErrorCode(ErrorCode.WRN_UseDefViolationThisSupportedVersion), ReportDiagnostic.Warn),
                KeyValuePairUtil.Create(GetIdForErrorCode(ErrorCode.WRN_UnassignedThisAutoPropertySupportedVersion), ReportDiagnostic.Warn),
                KeyValuePairUtil.Create(GetIdForErrorCode(ErrorCode.WRN_UnassignedThisSupportedVersion), ReportDiagnostic.Warn),
            });

        #endregion

        #region Interpolated string handlers

        internal static string GetInterpolatedStringHandlerDefinition(bool includeSpanOverloads, bool useDefaultParameters, bool useBoolReturns, string returnExpression = null, bool constructorBoolArg = false, bool constructorSuccessResult = true)
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

            void appendNonDefaultVariantsWithGenericAndType(string type, string generic, bool isSpan = false)
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

        internal const string InterpolatedStringHandlerAttribute = @"
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
    public static void Normalize()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
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

        internal const string InterpolatedStringHandlerArgumentAttribute = @"
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
