// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;

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

        private ImmutableArray<ScriptVariable> _lazyVariables;
        private IReadOnlyDictionary<string, int> _lazyVariableMap;

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
            var result = ImmutableArray.CreateBuilder<ScriptVariable>();

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

            return result.ToImmutable();
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
