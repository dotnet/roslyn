// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Ref = System.Reflection;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// Runtime representation of an interactive session.
    /// </summary>
    /// <remarks>
    /// Session is not thread-safe, i.e. parallel executions against the same session object might fail.
    /// However executing methods defined in a context of a session in parallel is safe as long as the methods themselves are thread-safe.
    /// (i.e. accessing data from previous submissions is safe as long as they are readonly or guarded by a user maintained lock).
    /// </remarks>
    internal sealed class Session
    {
        private readonly ScriptEngine _engine;
        private ScriptOptions _options;
        private Type _globalsType;
        private object _globals;
        private Script _previousScript;
        private Lazy<object> _nextInputState;

        internal Session(ScriptEngine engine, ScriptOptions options, object globals, Type globalsType = null)
        {
            _engine = engine;
            _options = options;
            _globals = globals;
            _globalsType = globalsType != null ? globalsType : (globals != null ? globals.GetType() : null);
            _nextInputState = new Lazy<object>(() => globals);
        }

        public ScriptEngine Engine
        {
            get { return _engine; }
        }

        internal MetadataFileReferenceResolver MetadataReferenceResolver
        {
            get
            {
                return _options.AssemblyResolver.PathResolver;
            }
        }

        internal Type HostObjectType
        {
            get { return _globalsType; }
        }

        internal Compilation LastSubmission
        {
            get { return _previousScript != null ? _previousScript.GetCompilation() : null; }
        }

        public void SetReferenceSearchPaths(params string[] paths)
        {
            _options = _options.WithSearchPaths(paths);
        }

        public void SetReferenceSearchPaths(ImmutableArray<string> paths)
        {
            _options = _options.WithSearchPaths(paths);
        }

        public ImmutableArray<string> ReferenceSearchPaths
        {
            get { return _options.SearchPaths; }
        }

        public ImmutableArray<MetadataReference> References
        {
            get { return _options.References; }
        }

        public void AddReference(string assemblyDisplayNameOrPath)
        {
            if (assemblyDisplayNameOrPath == null)
            {
                throw new ArgumentNullException(nameof(assemblyDisplayNameOrPath));
            }

            _options = _options.AddReferences(assemblyDisplayNameOrPath);
        }

        public void AddReference(Ref.Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            _options = _options.AddReferences(assembly);
        }

        public void AddReference(MetadataReference reference)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            _options = _options.AddReferences(reference);
        }

        internal void SetReferences(IEnumerable<MetadataReference> references)
        {
            _options = _options.WithReferences(references);
        }

        public void ImportNamespace(string @namespace)
        {
            ScriptEngine.ValidateNamespace(@namespace);
            _options = _options.AddNamespaces(@namespace);
        }

        internal void SetNamespaces(IEnumerable<string> namespaces)
        {
            _options = _options.WithNamespaces(namespaces);
        }

        public void ExecuteFile(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            string code = File.ReadAllText(path);
            this.Execute(code);
        }

        public object Execute(string code)
        {
            return this.Execute<object>(code);
        }

        public T Execute<T>(string code)
        {
            if (code == null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            var script = _engine.Create(code, _options, _globalsType, typeof(T)).WithPrevious(_previousScript);
            var endState = script.Run(_nextInputState.Value);

            _previousScript = endState.Script;
            _nextInputState = new Lazy<object>(() => endState);

            return (T)endState.ReturnValue;
        }

        public Submission<T> CompileSubmission<T>(string code, string path = null, bool isInteractive = true)
        {
            if (code == null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            var script = _engine.Create(code, _options.WithIsInteractive(isInteractive), _globalsType, typeof(T))
                .WithPath(path)
                .WithPrevious(_previousScript);

            var submission = new Submission<T>(script, _nextInputState);

            script.Build(); // force compilation now

            _previousScript = script;
            _nextInputState = new Lazy<object>(() => submission.Run());

            return submission;
        }
    }
}
