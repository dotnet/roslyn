// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// The result of running a script.
    /// </summary>
    public class ScriptState
    {
        private readonly ScriptExecutionState _executionState;
        private readonly object _value;
        private readonly Script _script;
        private ScriptVariables _variables;

        internal ScriptState(ScriptExecutionState executionState, object value, Script script)
        {
            _executionState = executionState;
            _value = value;
            _script = script;
        }

        /// <summary>
        /// The script that ran to produce this result.
        /// </summary>
        public Script Script
        {
            get { return _script; }
        }

        internal ScriptExecutionState ExecutionState
        {
            get { return _executionState; }
        }

        /// <summary>
        /// The final value produced by running the script.
        /// </summary>
        public object ReturnValue
        {
            get { return _value; }
        }

        /// <summary>
        /// The global variables accessible to or declared by the script.
        /// </summary>
        public ScriptVariables Variables
        {
            get
            {
                if (_variables == null)
                {
                    Interlocked.CompareExchange(ref _variables, new ScriptVariables(_executionState), null);
                }

                return _variables;
            }
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
}
