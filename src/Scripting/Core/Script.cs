// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// A delegate that will run a script when invoked.
    /// </summary>
    /// <param name="globals">An object instance whose members can be accessed by the script as global variables.</param>
    public delegate Task<T> ScriptRunner<T>(object globals = null);

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
        private readonly Script _previous;

        private ScriptBuilder _lazyBuilder;
        private Compilation _lazyCompilation;

        internal Script(string code, string path, ScriptOptions options, Type globalsType, ScriptBuilder builder, Script previous)
        {
            _code = code ?? "";
            _path = path ?? "";
            _options = options ?? ScriptOptions.Default;
            _globalsType = globalsType;
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
        public abstract Type ReturnType
        {
            get;
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

        internal ScriptBuilder LazyBuilder
        {
            get { return _lazyBuilder; }
        }

        /// <summary>
        /// Creates a new version of this script with the specified options.
        /// </summary>
        public Script WithOptions(ScriptOptions options)
        {
            return this.WithOptionsInternal(options);
        }

        /// <summary>
        /// Creates a new version of this script with the source code specified.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        public Script WithCode(string code)
        {
            return this.WithCodeInternal(code);
        }

        /// <summary>
        /// Creates a new version of this script with the path specified.
        /// The path is optional. It can be used to associate the script code with a file path.
        /// </summary>
        public Script WithPath(string path)
        {
            return this.WithPathInternal(path);
        }

        /// <summary>
        /// Creates a new version of this script with the specified globals type. 
        /// The members of this type can be accessed by the script as global variables.
        /// </summary>
        /// <param name="globalsType">The type that defines members that can be accessed by the script.</param>
        public Script WithGlobalsType(Type globalsType)
        {
            return this.WithGlobalsTypeInternal(globalsType);
        }

        /// <summary>
        /// Creates a new version of this script with the previous script specified.
        /// </summary>
        public Script WithPrevious(Script previous)
        {
            return this.WithPreviousInternal(previous);
        }

        internal abstract Script WithOptionsInternal(ScriptOptions options);
        internal abstract Script WithCodeInternal(string code);
        internal abstract Script WithPathInternal(string path);
        internal abstract Script WithGlobalsTypeInternal(Type globalsType);
        internal abstract Script WithPreviousInternal(Script previous);

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
        /// Creates a <see cref="Compilation"/> instances based on script members.
        /// </summary>
        protected abstract Compilation CreateCompilation();

        /// <summary>
        /// Runs this script.
        /// </summary>
        /// <param name="globals">An object instance whose members can be accessed by the script as global variables, 
        /// or a <see cref="ScriptState"/> instance that was the output from a previously run script.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running the script, including all declared variables and return value.</returns>
        public ScriptState RunAsync(object globals = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RunInternalAsync(globals, cancellationToken);
        }

        protected abstract ScriptState RunInternalAsync(object globals, CancellationToken cancellationToken);

        /// <summary>
        /// Forces the script through the build step.
        /// If not called directly, the build step will occur on the first call to Run.
        /// </summary>
        public void Build()
        {
            this.GetExecutorInternal(CancellationToken.None);
        }

        internal abstract Func<object[], Task> GetExecutorInternal(CancellationToken cancellationToken);

        internal void GatherSubmissionExecutors(ArrayBuilder<Func<object[], Task>> executors, CancellationToken cancellationToken)
        {
            var previous = this.Previous;
            if (previous != null)
            {
                previous.GatherSubmissionExecutors(executors, cancellationToken);
            }
            executors.Add(this.GetExecutorInternal(cancellationToken));
        }

        ///<summary>
        /// Continue running script from the point after the intermediate state was produced.
        ///</summary>
        internal bool TryRunFrom(ScriptState state, out ScriptExecutionState executionState, out object value)
        {
            if (state.Script == this)
            {
                value = state.ReturnValue;
                executionState = state.ExecutionState.FreezeAndClone();
                return true;
            }

            var previous = this.Previous;
            if (previous != null && previous.TryRunFrom(state, out executionState, out value))
            {
                value = this.RunSubmission(executionState);
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

        internal abstract object RunSubmission(ScriptExecutionState executionState);
    }

    public abstract class Script<T> : Script
    {
        private Func<object[], Task<T>> _lazyExecutor;
        private Func<ScriptExecutionState, Task<T>> _lazyAggrateScriptExecutor;

        internal Script(string code, string path, ScriptOptions options, Type globalsType, ScriptBuilder builder, Script previous) :
            base(code, path, options, globalsType, builder, previous)
        {
        }

        public sealed override Type ReturnType
        {
            get { return typeof(T); }
        }

        protected sealed override ScriptState RunInternalAsync(object globals, CancellationToken cancellationToken)
        {
            return RunAsync(globals, cancellationToken);
        }

        public new ScriptState<T> RunAsync(object globals = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = globals as ScriptState;
            if (state != null)
            {
                if (state.Script == this)
                {
                    // this state is already the output of running this script.
                    return (ScriptState<T>)state;
                }
                else if (this.Previous == null)
                {
                    // if this script is unbound (no previous script) then run this script bound to the state's script
                    return (ScriptState<T>)this.WithPrevious(state.Script).RunAsync(state, cancellationToken);
                }
                else
                {
                    // attempt to run script forward from the point after the specified state was computed.
                    ScriptExecutionState executionState;
                    object value;
                    if (this.TryRunFrom(state, out executionState, out value))
                    {
                        return new ScriptState<T>(executionState, (Task<T>)value, this);
                    }
                    else
                    {
                        throw new InvalidOperationException(ScriptingResources.StartingStateIncompatible);
                    }
                }
            }
            else
            {
                var globalsType = this.GlobalsType;
                if (globalsType != null)
                {
                    if (globals == null)
                    {
                        throw new ArgumentNullException(nameof(globals));
                    }
                    else
                    {
                        var runtimeType = globals.GetType().GetTypeInfo();
                        var globalsTypeInfo = globalsType.GetTypeInfo();

                        if (!globalsTypeInfo.IsAssignableFrom(runtimeType))
                        {
                            throw new ArgumentException(string.Format(ScriptingResources.GlobalsNotAssignable, runtimeType, globalsTypeInfo));
                        }
                    }
                }
                else if (globals != null)
                {
                    // make sure we are running from a script with matching globals type
                    return (ScriptState<T>)this.WithGlobalsType(globals.GetType()).RunAsync(globals, cancellationToken);
                }

                // run this script from the start with the specified globals
                var executionState = ScriptExecutionState.Create(globals);
                if (this.Previous == null)
                {
                    // only single submission, so just execute it directly.
                    var executor = this.GetExecutor(CancellationToken.None);
                    var value = executionState.RunSubmission(executor);
                    return new ScriptState<T>(executionState, value, this);
                }
                else
                {
                    // otherwise run the aggregate script.
                    var executor = this.GetAggregateScriptExecutor(CancellationToken.None);
                    var value = executor(executionState);
                    return new ScriptState<T>(executionState, value, this);
                }
            }
        }

        internal sealed override object RunSubmission(ScriptExecutionState executionState)
        {
            var executor = this.GetExecutor(CancellationToken.None);
            return executionState.RunSubmission(executor);
        }

        public new Script<T> WithOptions(ScriptOptions options)
        {
            return (options == this.Options) ?
                this :
                this.Make(this.Code, this.Path, options, this.GlobalsType, this.LazyBuilder, this.Previous);
        }

        public new Script<T> WithCode(string code)
        {
            if (code == null)
            {
                code = "";
            }
            return (code == this.Code) ?
                this :
                this.Make(code, this.Path, this.Options, this.GlobalsType, this.LazyBuilder, this.Previous);
        }

        public new Script<T> WithPath(string path)
        {
            if (path == null)
            {
                path = "";
            }
            return (path == this.Path) ?
                this :
                this.Make(this.Code, path, this.Options, this.GlobalsType, this.LazyBuilder, this.Previous);
        }

        public new Script<T> WithGlobalsType(Type globalsType)
        {
            return (globalsType == this.GlobalsType) ?
                this :
                this.Make(this.Code, this.Path, this.Options, globalsType, this.LazyBuilder, this.Previous);
        }

        public new Script<T> WithPrevious(Script previous)
        {
            var globalsType = (previous == null) ? this.GlobalsType : previous.GlobalsType;
            return (globalsType == this.GlobalsType) && (previous == this.Previous) ?
                this :
                this.Make(this.Code, this.Path, this.Options, globalsType, this.LazyBuilder, previous);
        }

        internal sealed override Script WithOptionsInternal(ScriptOptions options)
        {
            return this.WithOptions(options);
        }

        internal sealed override Script WithCodeInternal(string code)
        {
            return this.WithCode(code);
        }

        internal sealed override Script WithPathInternal(string path)
        {
            return this.WithPath(path);
        }

        internal sealed override Script WithGlobalsTypeInternal(Type globalsType)
        {
            return this.WithGlobalsType(globalsType);
        }

        internal sealed override Script WithPreviousInternal(Script previous)
        {
            return this.WithPrevious(previous);
        }

        /// <summary>
        /// Creates a new version of this script with the <see cref="ScriptBuilder"/> specified.
        /// </summary>
        internal Script<T> WithBuilder(ScriptBuilder builder)
        {
            return (builder == this.LazyBuilder) ?
                this :
                this.Make(this.Code, this.Path, this.Options, this.GlobalsType, builder, this.Previous);
        }

        /// <summary>
        /// Creates a new instance of a script of this type.
        /// </summary>
        internal abstract Script<T> Make(string code, string path, ScriptOptions options, Type globalsType, ScriptBuilder builder, Script previous);

        /// <summary>
        /// Gets the references that need to be assigned to the compilation.
        /// This can be different than the list of references defined by the <see cref="ScriptOptions"/> instance.
        /// </summary>
        protected ImmutableArray<MetadataReference> GetReferencesForCompilation()
        {
            var references = this.Options.References;

            if (this.GlobalsType != null)
            {
                var globalsTypeAssembly = MetadataReference.CreateFromAssemblyInternal(this.GlobalsType.GetTypeInfo().Assembly);
                if (!references.Contains(globalsTypeAssembly))
                {
                    references = references.Add(globalsTypeAssembly);
                }
            }

            var previous = this.Previous;
            if (previous == null)
            {
                return references;
            }
            else
            {
                // TODO (tomat): RESOLVED? bound imports should be reused from previous submission instead of passing 
                // them to every submission in the chain. See bug #7802.
                var compilation = previous.GetCompilation();
                return ImmutableArray.CreateRange(references.Union(compilation.References));
            }
        }

        /// <summary>
        /// Gets the executor that will run this portion of the script only. (does not include any previous scripts).
        /// </summary>
        private Func<object[], Task<T>> GetExecutor(CancellationToken cancellationToken)
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

                    var executor = this.Builder.Build<T>(this, diagnostics, cancellationToken);

                    // emit can fail due to compilation errors or because there is nothing to emit:
                    if (diagnostics.HasAnyErrors())
                    {
                        CompilationError(diagnostics);
                    }

                    if (executor == null)
                    {
                        executor = (s) => Task.FromResult(default(T));
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

        internal sealed override Func<object[], Task> GetExecutorInternal(CancellationToken cancellationToken)
        {
            return this.GetExecutor(cancellationToken);
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
        public ScriptRunner<T> CreateDelegate(CancellationToken cancellationToken = default(CancellationToken))
        {
            var executor = this.GetAggregateScriptExecutor(cancellationToken);
            return globals => executor(ScriptExecutionState.Create(globals));
        }

        /// <summary>
        /// Creates an executor that while run the entire aggregate script (all submissions).
        /// </summary>
        private Func<ScriptExecutionState, Task<T>> GetAggregateScriptExecutor(CancellationToken cancellationToken)
        {
            if (_lazyAggrateScriptExecutor == null)
            {
                var builder = ArrayBuilder<Func<object[], Task>>.GetInstance();
                this.GatherSubmissionExecutors(builder, cancellationToken);
                var executors = builder.ToImmutableAndFree();
                Debug.Assert(executors.Length > 0);

                // make a function to run through all submissions in order.
                Func<ScriptExecutionState, Task<T>> aggregateExecutor = state =>
                {
                    Task result = null;
                    foreach (var executor in executors)
                    {
                        result = state.RunSubmission(executor);
                        Debug.Assert(result != null);
                    }
                    return (Task<T>)result;
                };

                Interlocked.CompareExchange(ref _lazyAggrateScriptExecutor, aggregateExecutor, null);
            }

            return _lazyAggrateScriptExecutor;
        }
    }
}
