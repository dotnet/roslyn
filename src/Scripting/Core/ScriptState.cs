// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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

        internal ScriptExecutionState ExecutionState { get; }

        internal ScriptState(ScriptExecutionState executionState, Script script)
        {
            Debug.Assert(executionState != null);
            Debug.Assert(script != null);

            ExecutionState = executionState;
            Script = script;
        }

        /// <summary>
        /// The final value produced by running the script.
        /// </summary>
        public object ReturnValue => GetReturnValue();
        internal abstract object GetReturnValue();

        private ScriptVariables _lazyVariables;

        /// <summary>
        /// The global variables accessible to or declared by the script.
        /// </summary>
        public ScriptVariables Variables
        {
            get
            {
                if (_lazyVariables == null)
                {
                    Interlocked.CompareExchange(ref _lazyVariables, new ScriptVariables(ExecutionState), null);
                }

                return _lazyVariables;
            }
        }

        public Task<ScriptState<object>> ContinueWithAsync(string code, ScriptOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ContinueWithAsync<object>(code, options, cancellationToken);
        }

        public Task<ScriptState<TResult>> ContinueWithAsync<TResult>(string code, ScriptOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Script.ContinueWith<TResult>(code, options).ContinueAsync(this, cancellationToken);
        }

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

        internal ScriptState(ScriptExecutionState executionState, T value, Script script) 
            : base(executionState, script)
        {
            ReturnValue = value;
        }
    }
}
