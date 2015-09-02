// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// A collection that holds the final state of all global variables used by the script.
    /// </summary>
    public class ScriptVariables : IEnumerable<ScriptVariable>, IEnumerable
    {
        private readonly Dictionary<string, ScriptVariable> _map;

        internal ScriptVariables(ScriptExecutionState executionState)
        {
            _map = CreateVariableMap(executionState);
        }

        public int Count
        {
            get { return _map.Count; }
        }

        /// <summary>
        /// Returns the global variable with the specified name.
        /// </summary>
        public ScriptVariable this[string name]
        {
            get
            {
                ScriptVariable global;
                if (_map.TryGetValue(name, out global))
                {
                    return global;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Determines if a global variable with the specified name exists.
        /// </summary>
        public bool ContainsVariable(string name)
        {
            return _map.ContainsKey(name);
        }

        /// <summary>
        /// A list the global variable names.
        /// </summary>
        public IEnumerable<String> Names
        {
            get { return _map.Keys; }
        }

        /// <summary>
        /// Gets an enumerator over all the variables.
        /// </summary>
        public IEnumerator<ScriptVariable> GetEnumerator()
        {
            return _map.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private static Dictionary<string, ScriptVariable> CreateVariableMap(ScriptExecutionState executionState)
        {
            var map = new Dictionary<string, ScriptVariable>();

            for (int i = 0; i < executionState.Count; i++)
            {
                var state = executionState[i];
                if (state != null)
                {
                    AddVariables(map, state);
                }
            }

            return map;
        }

        private static void AddVariables(Dictionary<string, ScriptVariable> map, object instance)
        {
            var typeInfo = instance.GetType().GetTypeInfo();

            foreach (var field in typeInfo.DeclaredFields)
            {
                if (field.IsPublic)
                {
                    AddVariable(map, instance, field);
                }
            }

            foreach (var property in typeInfo.DeclaredProperties)
            {
                if (property.GetMethod.IsPublic)
                {
                    AddVariable(map, instance, property);
                }
            }
        }

        private static void AddVariable(Dictionary<string, ScriptVariable> map, object instance, MemberInfo member)
        {
            if (member.Name.Length > 0 && char.IsLetterOrDigit(member.Name[0]) && !map.ContainsKey(member.Name))
            {
                map.Add(member.Name, new ScriptVariable(instance, member));
            }
        }
    }
}