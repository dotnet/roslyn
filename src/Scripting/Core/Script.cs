// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// A delegate that will run a script when invoked.
    /// </summary>
    /// <param name="globals">An object instance whose members can be accessed by the script as global variables.</param>
    public delegate object ScriptRunner(object globals = null);

    /// <summary>
    /// A class that represents a script that you can run.
    /// 
    /// Create a script using a language specific script class such as CSharpScript or VisualBasicScript.
    /// </summary>
    public abstract class Script
    {
        private readonly string _code;
        private readonly string _path;
        private readonly ScriptOptions _options;
        private readonly Type _globalsType;
        private readonly Type _returnType;
        private readonly Script _previous;

        private ScriptBuilder _lazyBuilder;
        private Compilation _lazyCompilation;
        private Func<object[], object> _lazyExecutor;
        private Func<ScriptExecutionState, object> _lazyAggrateScriptExecutor;

        internal Script(string code, string path, ScriptOptions options, Type globalsType, Type returnType, ScriptBuilder builder, Script previous)
        {
            _code = code ?? "";
            _path = path ?? "";
            _options = options ?? ScriptOptions.Default;
            _globalsType = globalsType;
            _returnType = returnType ?? typeof(object);
            _previous = previous;

            if (_previous != null && builder != null && _previous._lazyBuilder != builder)
            {
                throw new ArgumentException("Incompatible script builder.");
            }

            _lazyBuilder = builder;
        }

        /// <summary>
        /// A script that will run first when this script is run. 
        /// Any declarations made in the previous script can be referenced in this script.
        /// The end state from running this script includes all declarations made by both scripts.
        /// </summary>
        public Script Previous
        {
            get { return _previous; }
        }

        /// <summary>
        /// The options used by this script.
        /// </summary>
        public ScriptOptions Options
        {
            get { return _options; }
        }

        /// <summary>
        /// The source code of the script.
        /// </summary>
        public string Code
        {
            get { return _code; }
        }

        /// <summary>
        /// The path to the source if it originated from a file.
        /// </summary>
        public string Path
        {
            get { return _path; }
        }

        /// <summary>
        /// The type of an object whose members can be accessed by the script as global variables.
        /// </summary>
        public Type GlobalsType
        {
            get { return _globalsType; }
        }

        /// <summary>
        /// The expected return type of the script.
        /// </summary>
        public Type ReturnType
        {
            get { return _returnType; }
        }

        /// <summary>
        /// The <see cref="ScriptBuilder"/> that will be used to build the script before running.
        /// </summary>
        internal ScriptBuilder Builder
        {
            get
            {
                if (_lazyBuilder == null)
                {
                    ScriptBuilder tmp;
                    if (_previous != null)
                    {
                        tmp = _previous.Builder;
                    }
                    else
                    {
                        tmp = new ScriptBuilder();
                    }

                    Interlocked.CompareExchange(ref _lazyBuilder, tmp, null);
                }

                return _lazyBuilder;
            }
        }

        /// <summary>
        /// Creates a new version of this script with the specified options.
        /// </summary>
        public Script WithOptions(ScriptOptions options)
        {
            return this.With(options: options);
        }

        /// <summary>
        /// Creates a new version of this script with the source code specified.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        public Script WithCode(string code)
        {
            return this.With(code: code ?? "");
        }

        /// <summary>
        /// Creates a new version of this script with the path specified.
        /// The path is optional. It can be used to associate the script code with a file path.
        /// </summary>
        public Script WithPath(string path)
        {
            return this.With(path: path ?? "");
        }

        /// <summary>
        /// Creates a new version of this script with the specified globals type. 
        /// The members of this type can be accessed by the script as global variables.
        /// </summary>
        /// <param name="globalsType">The type that defines members that can be accessed by the script.</param>
        public Script WithGlobalsType(Type globalsType)
        {
            return this.With(globalsType: globalsType);
        }

        /// <summary>
        /// Creates a new version of this script with the specified return type.
        /// The default return type for a script is <see cref="System.Object"/>. 
        /// Specifying a return type may be necessary for proper understanding of some scripts.
        /// </summary>
        public Script WithReturnType(Type returnType)
        {
            return this.With(returnType: returnType);
        }

        /// <summary>
        /// Creates a new version of this script with the previous script specified.
        /// </summary>
        public Script WithPrevious(Script script)
        {
            if (script != null)
            {
                return this.With(previous: script, globalsType: script.GlobalsType);
            }
            else
            {
                return this.With(previous: script);
            }
        }

        /// <summary>
        /// Creates a new verion of this script with the <see cref="ScriptBuilder"/> specified.
        /// </summary>
        internal Script WithBuilder(ScriptBuilder builder)
        {
            return this.With(builder: builder);
        }

        private Script With(
            Optional<string> code = default(Optional<string>),
            Optional<string> path = default(Optional<string>),
            Optional<ScriptOptions> options = default(Optional<ScriptOptions>),
            Optional<Type> globalsType = default(Optional<Type>),
            Optional<Type> returnType = default(Optional<Type>),
            Optional<ScriptBuilder> builder = default(Optional<ScriptBuilder>),
            Optional<Script> previous = default(Optional<Script>))
        {
            var newCode = code.HasValue ? code.Value : _code;
            var newPath = path.HasValue ? path.Value : _path;
            var newOptions = options.HasValue ? options.Value : _options;
            var newGlobalsType = globalsType.HasValue ? globalsType.Value : _globalsType;
            var newReturnType = returnType.HasValue ? returnType.Value : _returnType;
            var newBuilder = builder.HasValue ? builder.Value : _lazyBuilder;
            var newPrevious = previous.HasValue ? previous.Value : _previous;

            if (ReferenceEquals(newCode, _code) &&
                ReferenceEquals(newPath, _path) &&
                newOptions == _options &&
                newGlobalsType == _globalsType &&
                newReturnType == _returnType &&
                newBuilder == _lazyBuilder &&
                newPrevious == this.Previous)
            {
                return this;
            }
            else
            {
                return this.Make(newCode, newPath, newOptions, newGlobalsType, newReturnType, newBuilder, newPrevious);
            }
        }

        /// <summary>
        /// Creates a new instance of a script of this type.
        /// </summary>
        internal abstract Script Make(string code, string path, ScriptOptions options, Type globalsType, Type returnType, ScriptBuilder builder, Script previous);

        /// <summary>
        /// Runs this script.
        /// </summary>
        /// <param name="globals">An object instance whose members can be accessed by the script as global variables, 
        /// or a <see cref="ScriptState"/> instance that was the output from a previously run script.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running the script, including all declared variables and return value.</returns>
        public ScriptState Run(object globals = null)
        {
            var state = globals as ScriptState;
            if (state != null)
            {
                if (state.Script == this)
                {
                    // this state is already the output of running this script.
                    return state;
                }
                else if (this.Previous == null)
                {
                    // if this script is unbound (no previous script) then run this script bound to the state's script
                    return this.WithPrevious(state.Script).Run(state);
                }
                else
                {
                    // attempt to run script forward from the point after the specified state was computed.
                    ScriptExecutionState executionState;
                    object value;
                    if (this.TryRunFrom(state, out executionState, out value))
                    {
                        return new ScriptState(executionState, value, this);
                    }
                    else
                    {
                        throw new InvalidOperationException(ScriptingResources.StartingStateIncompatible);
                    }
                }
            }
            else
            {
                if (_globalsType != null && globals == null)
                {
                    throw new ArgumentNullException(nameof(globals));
                }
                else if (globals != null && _globalsType != null)
                {
                    var runtimeType = globals.GetType();
                    if (!_globalsType.IsAssignableFrom(runtimeType))
                    {
                        throw new ArgumentException(string.Format(ScriptingResources.GlobalsNotAssignable, runtimeType, _globalsType));
                    }
                }

                // make sure we are running from a script with matching globals type
                if (globals != null && _globalsType == null)
                {
                    return this.WithGlobalsType(globals.GetType()).Run(globals);
                }

                // run this script from the start with the specified globals
                var executionState = ScriptExecutionState.Create(globals);
                if (this.Previous == null)
                {
                    // only single submission, so just execute it directly.
                    var executor = this.GetExecutor(CancellationToken.None);
                    var value = executionState.RunSubmission(executor);
                    return new ScriptState(executionState, value, this);
                }
                else
                {
                    // otherwise run the aggregate script.
                    var executor = this.GetAggregateScriptExecutor(CancellationToken.None);
                    var value = executor(executionState);
                    return new ScriptState(executionState, value, this);
                }
            }
        }

        ///<summary>
        /// Continue running script from the point after the intermediate state was produced.
        ///</summary>
        private bool TryRunFrom(ScriptState state, out ScriptExecutionState executionState, out object value)
        {
            if (state.Script == this)
            {
                value = state.ReturnValue;
                executionState = state.ExecutionState.FreezeAndClone();
                return true;
            }
            else if (_previous != null && _previous.TryRunFrom(state, out executionState, out value))
            {
                var executor = this.GetExecutor(CancellationToken.None);
                value = executionState.RunSubmission(executor);
                return true;
            }
            else
            {
                // couldn't find starting point to continue running from.
                value = null;
                executionState = null;
                return false;
            }
        }

        /// <summary>
        /// Get's the <see cref="Compilation"/> that represents the semantics of the script.
        /// </summary>
        public Compilation GetCompilation()
        {
            if (_lazyCompilation == null)
            {
                var compilation = this.CreateCompilation();
                Interlocked.CompareExchange(ref _lazyCompilation, compilation, null);
            }

            return _lazyCompilation;
        }

        /// <summary>
        /// Forces the script through the build step.
        /// If not called directly, the build step will occur on the first call to Run.
        /// </summary>
        public void Build()
        {
            this.GetExecutor(CancellationToken.None);
        }

        /// <summary>
        /// Gets the references that need to be assigned to the compilation.
        /// This can be different than the list of references defined by the <see cref="ScriptOptions"/> instance.
        /// </summary>
        protected ImmutableArray<MetadataReference> GetReferencesForCompilation()
        {
            var references = _options.References;

            if (this.GlobalsType != null)
            {
                var globalsTypeAssembly = MetadataReference.CreateFromAssembly(this.GlobalsType.Assembly);
                if (!references.Contains(globalsTypeAssembly))
                {
                    references = references.Add(globalsTypeAssembly);
                }
            }

            if (_previous == null)
            {
                return references;
            }
            else
            {
                // TODO (tomat): RESOLVED? bound imports should be reused from previous submission instead of passing 
                // them to every submission in the chain. See bug #7802.
                var compilation = _previous.GetCompilation();
                return ImmutableArray.CreateRange(references.Union(compilation.References));
            }
        }

        /// <summary>
        /// Creates a <see cref="Compilation"/> instances based on script members.
        /// </summary>
        protected abstract Compilation CreateCompilation();

        /// <summary>
        /// Gets the executor that will run this portion of the script only. (does not include any previous scripts).
        /// </summary>
        private Func<object[], object> GetExecutor(CancellationToken cancellationToken)
        {
            if (_lazyExecutor == null)
            {
                var compilation = this.GetCompilation();

                var diagnostics = DiagnosticBag.GetInstance();
                try
                {
                    // get compilation diagnostics first.
                    diagnostics.AddRange(compilation.GetParseDiagnostics());
                    if (diagnostics.HasAnyErrors())
                    {
                        CompilationError(diagnostics);
                    }

                    diagnostics.Clear();

                    var executor = this.Builder.Build(this, diagnostics, cancellationToken);

                    if (diagnostics.HasAnyErrors())
                    {
                        CompilationError(diagnostics);
                    }

                    if (executor == null)
                    {
                        executor = (s) => null;
                    }

                    Interlocked.CompareExchange(ref _lazyExecutor, executor, null);
                }
                finally
                {
                    diagnostics.Free();
                }
            }

            return _lazyExecutor;
        }

        private void CompilationError(DiagnosticBag diagnostics)
        {
            var resolvedLocalDiagnostics = diagnostics.AsEnumerable();
            var firstError = resolvedLocalDiagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
            if (firstError != null)
            {
                throw new CompilationErrorException(FormatDiagnostic(firstError, CultureInfo.CurrentCulture),
                    (resolvedLocalDiagnostics.AsImmutable()));
            }
        }

        protected abstract string FormatDiagnostic(Diagnostic diagnostic, CultureInfo culture);

        /// <summary>
        /// Creates a delegate that will execute this script when invoked.
        /// </summary>
        public ScriptRunner CreateDelegate(CancellationToken cancellationToken = default(CancellationToken))
        {
            var executor = this.GetAggregateScriptExecutor(cancellationToken);

            return (globals) =>
            {
                var executionState = ScriptExecutionState.Create(globals);
                return executor(executionState);
            };
        }

        /// <summary>
        /// Creates an executor that while run the entire aggregate script (all submissions).
        /// </summary>
        private Func<ScriptExecutionState, object> GetAggregateScriptExecutor(CancellationToken cancellationToken)
        {
            if (_lazyAggrateScriptExecutor == null)
            {
                Func<ScriptExecutionState, object> aggregateExecutor;

                if (_previous == null)
                {
                    // only one submission, just use the submission's entry point.
                    var executor = this.GetExecutor(cancellationToken);
                    aggregateExecutor = state => state.RunSubmission(executor);
                }
                else
                {
                    // make a function to runs through all submissions in order.
                    var executors = new List<Func<object[], object>>();
                    this.GatherSubmissionExecutors(executors, cancellationToken);

                    aggregateExecutor = state =>
                    {
                        object result = null;
                        foreach (var exec in executors)
                        {
                            result = state.RunSubmission(exec);
                        }

                        return result;
                    };
                }

                Interlocked.CompareExchange(ref _lazyAggrateScriptExecutor, aggregateExecutor, null);
            }

            return _lazyAggrateScriptExecutor;
        }

        private void GatherSubmissionExecutors(List<Func<object[], object>> executors, CancellationToken cancellationToken)
        {
            if (_previous != null)
            {
                _previous.GatherSubmissionExecutors(executors, cancellationToken);
            }

            executors.Add(this.GetExecutor(cancellationToken));
        }
    }
}
