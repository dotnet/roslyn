// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// The result of running a script.
    /// </summary>
    public abstract class ScriptState
    {
        /// <summary>
        /// The script that ran to produce this result.
        /// </summary>
        public Script Script { get; }

        /// <summary>
        /// Caught exception originating from the script top-level code.
        /// </summary>
        /// <remarks>
        /// Exceptions are only caught and stored here if the API returning the <see cref="ScriptState"/> is instructed to do so. 
        /// By default they are propagated to the caller of the API.
        /// </remarks>
        public Exception Exception { get; }

        internal ScriptExecutionState ExecutionState { get; }

        private ImmutableArray<ScriptVariable> _lazyVariables;
        private IReadOnlyDictionary<string, int> _lazyVariableMap;

        internal ScriptState(ScriptExecutionState executionState, Script script, Exception exceptionOpt)
        {
            Debug.Assert(executionState != null);
            Debug.Assert(script != null);

            ExecutionState = executionState;
            Script = script;
            Exception = exceptionOpt;
        }

        /// <summary>
        /// The final value produced by running the script.
        /// </summary>
        public object ReturnValue => GetReturnValue();
        internal abstract object GetReturnValue();

        /// <summary>
        /// Returns variables defined by the scripts in the declaration order.
        /// </summary>
        public ImmutableArray<ScriptVariable> Variables
        {
            get
            {
                if (_lazyVariables == null)
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyVariables, CreateVariables());
                }

                return _lazyVariables;
            }
        }

        /// <summary>
        /// Returns a script variable of the specified name. 
        /// </summary> 
        /// <remarks>
        /// If multiple script variables are defined in the script (in distinct submissions) returns the last one.
        /// Name lookup is case sensitive in C# scripts and case insensitive in VB scripts.
        /// </remarks>
        /// <returns><see cref="ScriptVariable"/> or null, if no variable of the specified <paramref name="name"/> is defined in the script.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
        public ScriptVariable GetVariable(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            int index;
            return GetVariableMap().TryGetValue(name, out index) ? Variables[index] : null;
        }

        private ImmutableArray<ScriptVariable> CreateVariables()
        {
            var result = ArrayBuilder<ScriptVariable>.GetInstance();

            var executionState = ExecutionState;

            // Don't include the globals object (slot #0)
            for (int i = 1; i < executionState.SubmissionStateCount; i++)
            {
                var state = executionState.GetSubmissionState(i);
                Debug.Assert(state != null);

                foreach (var field in state.GetType().GetTypeInfo().DeclaredFields)
                {
                    // TODO: synthesized fields of submissions shouldn't be public
                    if (field.IsPublic && field.Name.Length > 0 && (char.IsLetterOrDigit(field.Name[0]) || field.Name[0] == '_'))
                    {
                        result.Add(new ScriptVariable(state, field));
                    }
                }
            }

            return result.ToImmutableAndFree();
        }

        private IReadOnlyDictionary<string, int> GetVariableMap()
        {
            if (_lazyVariableMap == null)
            {
                var map = new Dictionary<string, int>(Script.Compiler.IdentifierComparer);
                for (int i = 0; i < Variables.Length; i++)
                {
                    map[Variables[i].Name] = i;
                }

                _lazyVariableMap = map;
            }

            return _lazyVariableMap;
        }

        /// <summary>
        /// Continues script execution from the state represented by this instance by running the specified code snippet.
        /// </summary>
        /// <param name="code">The code to be executed.</param>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running <paramref name="code"/>, including all declared variables and return value.</returns>
        public Task<ScriptState<object>> ContinueWithAsync(string code, ScriptOptions options, CancellationToken cancellationToken)
            => ContinueWithAsync<object>(code, options, null, cancellationToken);

        /// <summary>
        /// Continues script execution from the state represented by this instance by running the specified code snippet.
        /// </summary>
        /// <param name="code">The code to be executed.</param>
        /// <param name="options">Options.</param>
        /// <param name="catchException">
        /// If specified, any exception thrown by the script top-level code is passed to <paramref name="catchException"/>.
        /// If it returns true the exception is caught and stored on the resulting <see cref="ScriptState"/>, otherwise the exception is propagated to the caller.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running <paramref name="code"/>, including all declared variables, return value and caught exception (if applicable).</returns>
        public Task<ScriptState<object>> ContinueWithAsync(string code, ScriptOptions options = null, Func<Exception, bool> catchException = null, CancellationToken cancellationToken = default(CancellationToken))
            => Script.ContinueWith<object>(code, options).RunFromAsync(this, catchException, cancellationToken);

        /// <summary>
        /// Continues script execution from the state represented by this instance by running the specified code snippet.
        /// </summary>
        /// <param name="code">The code to be executed.</param>
        /// <param name="options">Options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running <paramref name="code"/>, including all declared variables and return value.</returns>
        public Task<ScriptState<TResult>> ContinueWithAsync<TResult>(string code, ScriptOptions options, CancellationToken cancellationToken)
            => ContinueWithAsync<TResult>(code, options, null, cancellationToken);

        /// <summary>
        /// Continues script execution from the state represented by this instance by running the specified code snippet.
        /// </summary>
        /// <param name="code">The code to be executed.</param>
        /// <param name="options">Options.</param>
        /// <param name="catchException">
        /// If specified, any exception thrown by the script top-level code is passed to <paramref name="catchException"/>.
        /// If it returns true the exception is caught and stored on the resulting <see cref="ScriptState"/>, otherwise the exception is propagated to the caller.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running <paramref name="code"/>, including all declared variables, return value and caught exception (if applicable).</returns>
        public Task<ScriptState<TResult>> ContinueWithAsync<TResult>(string code, ScriptOptions options = null, Func<Exception, bool> catchException = null, CancellationToken cancellationToken = default(CancellationToken))
            => Script.ContinueWith<TResult>(code, options).RunFromAsync(this, catchException, cancellationToken);

        // How do we resolve overloads? We should use the language semantics.
        // https://github.com/dotnet/roslyn/issues/3720
#if TODO
        /// <summary>
        /// Invoke a method declared by the script.
        /// </summary>
        public object Invoke(string name, params object[] args)
        {
            var func = this.FindMethod(name, args != null ? args.Length : 0);
            if (func != null)
            {
                return func(args);
            }

            return null;
        }

        private Func<object[], object> FindMethod(string name, int argCount)
        {
            for (int i = _executionState.Count - 1; i >= 0; i--)
            {
                var sub = _executionState[i];
                if (sub != null)
                {
                    var type = sub.GetType();
                    var method = FindMethod(type, name, argCount);
                    if (method != null)
                    {
                        return (args) => method.Invoke(sub, args);
                    }
                }
            }

            return null;
        }

        private MethodInfo FindMethod(Type type, string name, int argCount)
        {
            return type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Create a delegate to a method declared by the script.
        /// </summary>
        public TDelegate CreateDelegate<TDelegate>(string name)
        {
            var delegateInvokeMethod = typeof(TDelegate).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public);

            for (int i = _executionState.Count - 1; i >= 0; i--)
            {
                var sub = _executionState[i];
                if (sub != null)
                {
                    var type = sub.GetType();
                    var method = FindMatchingMethod(type, name, delegateInvokeMethod);
                    if (method != null)
                    {
                        return (TDelegate)(object)method.CreateDelegate(typeof(TDelegate), sub);
                    }
                }
            }

            return default(TDelegate);
        }

        private MethodInfo FindMatchingMethod(Type instanceType, string name, MethodInfo delegateInvokeMethod)
        {
            var dprms = delegateInvokeMethod.GetParameters();

            foreach (var mi in instanceType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (mi.Name == name)
                {
                    var prms = mi.GetParameters();
                    if (prms.Length == dprms.Length)
                    {
                        // TODO: better matching..
                        return mi;
                    }
                }
            }

            return null;
        }
#endif
    }

    public sealed class ScriptState<T> : ScriptState
    {
        public new T ReturnValue { get; }
        internal override object GetReturnValue() => ReturnValue;

        internal ScriptState(ScriptExecutionState executionState, Script script, T value, Exception exceptionOpt)
            : base(executionState, script, exceptionOpt)
        {
            ReturnValue = value;
        }
    }
}
