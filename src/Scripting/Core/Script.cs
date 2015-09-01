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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// A class that represents a script that you can run.
    /// 
    /// Create a script using a language specific script class such as CSharpScript or VisualBasicScript.
    /// </summary>
    public abstract class Script
    {
        internal readonly ScriptCompiler Compiler;

        private ScriptBuilder _lazyBuilder;
        private Compilation _lazyCompilation;
        
        internal Script(ScriptCompiler compiler, string code, ScriptOptions options, Type globalsType, ScriptBuilder builder, Script previous)
        {
            Compiler = compiler;
            Code = code ?? "";
            Options = options ?? ScriptOptions.Default;
            GlobalsType = globalsType;
            Previous = previous;

            if (Previous != null && builder != null && Previous._lazyBuilder != builder)
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
        public Script Previous { get; }

        /// <summary>
        /// The options used by this script.
        /// </summary>
        public ScriptOptions Options { get; }

        /// <summary>
        /// The source code of the script.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// The type of an object whose members can be accessed by the script as global variables.
        /// </summary>
        public Type GlobalsType { get; }

        /// <summary>
        /// The expected return type of the script.
        /// </summary>
        public abstract Type ReturnType { get; }

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
                    if (Previous != null)
                    {
                        tmp = Previous.Builder;
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

        internal ScriptBuilder LazyBuilder => _lazyBuilder;

        /// <summary>
        /// Creates a new version of this script with the specified options.
        /// </summary>
        public Script WithOptions(ScriptOptions options) => WithOptionsInternal(options);
        internal abstract Script WithOptionsInternal(ScriptOptions options);

        /// <summary>
        /// Creates a new version of this script with the source code specified.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        public Script WithCode(string code) => WithCodeInternal(code);
        internal abstract Script WithCodeInternal(string code);

        /// <summary>
        /// Creates a new version of this script with the specified globals type. 
        /// The members of this type can be accessed by the script as global variables.
        /// </summary>
        /// <param name="globalsType">The type that defines members that can be accessed by the script.</param>
        public Script WithGlobalsType(Type globalsType) => this.WithGlobalsTypeInternal(globalsType);
        internal abstract Script WithGlobalsTypeInternal(Type globalsType);
        
        /// <summary>
        /// Continues the script with given code snippet.
        /// </summary>
        public Script<object> ContinueWith(string code, ScriptOptions options = null) =>
            ContinueWith<object>(code, options);

        /// <summary>
        /// Continues the script with given code snippet.
        /// </summary>
        public Script<TResult> ContinueWith<TResult>(string code, ScriptOptions options = null) => 
            new Script<TResult>(this.Compiler, code, options ?? Options, GlobalsType, _lazyBuilder, this);

        /// <summary>
        /// Get's the <see cref="Compilation"/> that represents the semantics of the script.
        /// </summary>
        public Compilation GetCompilation()
        {
            if (_lazyCompilation == null)
            {
                var compilation = Compiler.CreateSubmission(this);
                Interlocked.CompareExchange(ref _lazyCompilation, compilation, null);
            }

            return _lazyCompilation;
        }

        /// <summary>
        /// Runs the script from the beginning and returns the result of the last code snippet.
        /// </summary>
        /// <param name="globals">
        /// An instance of <see cref="Script.GlobalsType"/> holding on values of global variables accessible from the script.
        /// Must be specified if and only if the script was created with a <see cref="Script.GlobalsType"/>.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the last code snippet.</returns>
        public Task<object> EvaluateAsync(object globals = null, CancellationToken cancellationToken = default(CancellationToken)) =>
            CommonEvaluateAsync(globals, cancellationToken);

        internal abstract Task<object> CommonEvaluateAsync(object globals, CancellationToken cancellationToken);
       
        /// <summary>
        /// Runs the script from the beginning.
        /// </summary>
        /// <param name="globals">
        /// An instance of <see cref="Script.GlobalsType"/> holding on values for global variables accessible from the script.
        /// Must be specified if and only if the script was created with <see cref="Script.GlobalsType"/>.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running the script, including all declared variables and return value.</returns>
        public Task<ScriptState> RunAsync(object globals = null, CancellationToken cancellationToken = default(CancellationToken)) =>
            CommonRunAsync(globals, cancellationToken);

        internal abstract Task<ScriptState> CommonRunAsync(object globals, CancellationToken cancellationToken);

        /// <summary>
        /// Continue script execution from the specified state.
        /// </summary>
        /// <param name="previousState">
        /// Previous state of the script execution.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running the script, including all declared variables and return value.</returns>
        public Task<ScriptState> ContinueAsync(ScriptState previousState, CancellationToken cancellationToken = default(CancellationToken)) =>
            CommonContinueAsync(previousState, cancellationToken);

        internal abstract Task<ScriptState> CommonContinueAsync(ScriptState previousState, CancellationToken cancellationToken);

        /// <summary>
        /// Forces the script through the build step.
        /// If not called directly, the build step will occur on the first call to Run.
        /// </summary>
        public void Build(CancellationToken cancellationToken = default(CancellationToken)) =>
            CommonBuild(cancellationToken);

        internal abstract void CommonBuild(CancellationToken cancellationToken);
        internal abstract Func<object[], Task> CommonGetExecutor(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the references that need to be assigned to the compilation.
        /// This can be different than the list of references defined by the <see cref="ScriptOptions"/> instance.
        /// </summary>
        internal ImmutableArray<MetadataReference> GetReferencesForCompilation()
        {
            var references = this.Options.References;

            var previous = this.Previous;
            if (previous != null)
            {
                // TODO (tomat): RESOLVED? bound imports should be reused from previous submission instead of passing 
                // them to every submission in the chain. See bug #7802.
                var compilation = previous.GetCompilation();
                return ImmutableArray.CreateRange(references.Union(compilation.References));
            }

            var corLib = MetadataReference.CreateFromAssemblyInternal(typeof(object).GetTypeInfo().Assembly);
            references = references.Add(corLib);

            if (this.GlobalsType != null)
            {
                var globalsTypeAssembly = MetadataReference.CreateFromAssemblyInternal(this.GlobalsType.GetTypeInfo().Assembly);
                references = references.Add(globalsTypeAssembly);
            }

            return references;
        }
    }

    public sealed class Script<T> : Script
    {
        private ImmutableArray<Func<object[], Task>> _lazyPrecedingExecutors;
        private Func<object[], Task<T>> _lazyExecutor;

        internal Script(ScriptCompiler compiler, string code, ScriptOptions options, Type globalsType, ScriptBuilder builder, Script previous)
            : base(compiler, code, options, globalsType, builder, previous)
        {
        }

        public override Type ReturnType => typeof(T);

        public new Script<T> WithOptions(ScriptOptions options)
        {
            return (options == this.Options) ?
                this :
                new Script<T>(this.Compiler, this.Code, options, this.GlobalsType, this.LazyBuilder, this.Previous);
        }

        public new Script<T> WithCode(string code)
        {
            if (code == null)
            {
                code = "";
            }

            return (code == this.Code) ?
                this :
                new Script<T>(this.Compiler, code, this.Options, this.GlobalsType, this.LazyBuilder, this.Previous);
        }

        public new Script<T> WithGlobalsType(Type globalsType)
        {
            return (globalsType == this.GlobalsType) ?
                this :
                new Script<T>(this.Compiler, this.Code, this.Options, globalsType, this.LazyBuilder, this.Previous);
        }

        internal override Script WithOptionsInternal(ScriptOptions options) => WithOptions(options);
        internal override Script WithCodeInternal(string code) => WithCode(code);
        internal override Script WithGlobalsTypeInternal(Type globalsType) => WithGlobalsType(globalsType);

        /// <exception cref="CompilationErrorException">Compilation has errors.</exception>
        internal override void CommonBuild(CancellationToken cancellationToken)
        {
            GetPrecedingExecutors(cancellationToken);
            GetExecutor(cancellationToken);
        }

        internal override Func<object[], Task> CommonGetExecutor(CancellationToken cancellationToken)
            => GetExecutor(cancellationToken);

        internal override Task<object> CommonEvaluateAsync(object globals, CancellationToken cancellationToken) =>
            EvaluateAsync(globals, cancellationToken).CastAsync<T, object>();

        internal override Task<ScriptState> CommonRunAsync(object globals, CancellationToken cancellationToken) =>
            RunAsync(globals, cancellationToken).CastAsync<ScriptState<T>, ScriptState>();

        internal override Task<ScriptState> CommonContinueAsync(ScriptState previousState, CancellationToken cancellationToken) =>
            ContinueAsync(previousState, cancellationToken).CastAsync<ScriptState<T>, ScriptState>();

        /// <exception cref="CompilationErrorException">Compilation has errors.</exception>
        private Func<object[], Task<T>> GetExecutor(CancellationToken cancellationToken)
        {
            if (_lazyExecutor == null)
            {
                Interlocked.CompareExchange(ref _lazyExecutor, Builder.CreateExecutor<T>(Compiler, GetCompilation(), cancellationToken), null);
            }

            return _lazyExecutor;
        }

        /// <exception cref="CompilationErrorException">Compilation has errors.</exception>
        private ImmutableArray<Func<object[], Task>> GetPrecedingExecutors(CancellationToken cancellationToken)
        {
            if (_lazyPrecedingExecutors.IsDefault)
            {
                var preceding = TryGetPrecedingExecutors(null, cancellationToken);
                Debug.Assert(!preceding.IsDefault);
                InterlockedOperations.Initialize(ref _lazyPrecedingExecutors, preceding);
            }

            return _lazyPrecedingExecutors;
        }

        /// <exception cref="CompilationErrorException">Compilation has errors.</exception>
        private ImmutableArray<Func<object[], Task>> TryGetPrecedingExecutors(Script lastExecutedScriptInChainOpt, CancellationToken cancellationToken)
        {
            Script script = Previous;
            if (script == lastExecutedScriptInChainOpt)
            {
                return ImmutableArray<Func<object[], Task>>.Empty;
            }

            var scriptsReversed = ArrayBuilder<Script>.GetInstance();

            while (script != null && script != lastExecutedScriptInChainOpt)
            {
                scriptsReversed.Add(script);
                script = script.Previous;
            }

            if (lastExecutedScriptInChainOpt != null && script != lastExecutedScriptInChainOpt)
            {
                scriptsReversed.Free();
                return default(ImmutableArray<Func<object[], Task>>);
            }

            var executors = ArrayBuilder<Func<object[], Task>>.GetInstance(scriptsReversed.Count);

            // We need to build executors in the order in which they are chained,
            // so that assemblies created for the submissions are loaded in the correct order.
            for (int i = scriptsReversed.Count - 1; i >= 0; i--)
            {
                executors.Add(scriptsReversed[i].CommonGetExecutor(cancellationToken));
            }

            return executors.ToImmutableAndFree();
        }

        /// <summary>
        /// Runs the script from the beginning and returns the result of the last code snippet.
        /// </summary>
        /// <param name="globals">
        /// An instance of <see cref="Script.GlobalsType"/> holding on values of global variables accessible from the script.
        /// Must be specified if and only if the script was created with a <see cref="Script.GlobalsType"/>.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the last code snippet.</returns>
        public new Task<T> EvaluateAsync(object globals = null, CancellationToken cancellationToken = default(CancellationToken)) =>
            RunAsync(globals, cancellationToken).GetEvaluationResultAsync();

        /// <summary>
        /// Runs the script from the beginning.
        /// </summary>
        /// <param name="globals">
        /// An instance of <see cref="Script.GlobalsType"/> holding on values for global variables accessible from the script.
        /// Must be specified if and only if the script was created with <see cref="Script.GlobalsType"/>.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running the script, including all declared variables and return value.</returns>
        /// <exception cref="CompilationErrorException">Compilation has errors.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="globals"/> doesn't match <see cref="Script.GlobalsType"/>.</exception>
        public new Task<ScriptState<T>> RunAsync(object globals = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // The following validation and executor contruction may throw;
            // do so synchronously so that the exception is not wrapped in the task.

            ValidateGlobals(globals, GlobalsType);

            var executionState = ScriptExecutionState.Create(globals);
            var precedingExecutors = GetPrecedingExecutors(cancellationToken);
            var currentExecutor = GetExecutor(cancellationToken);

            return RunSubmissionsAsync(executionState, precedingExecutors, currentExecutor, cancellationToken);
        }

        /// <summary>
        /// Creates a delegate that will run this script from the beginning when invoked.
        /// </summary>
        /// <remarks>
        /// The delegate doesn't hold on this script or its compilation.
        /// </remarks>
        public ScriptRunner<T> CreateDelegate(CancellationToken cancellationToken = default(CancellationToken))
        {
            var precedingExecutors = GetPrecedingExecutors(cancellationToken);
            var currentExecutor = GetExecutor(cancellationToken);
            var globalsType = GlobalsType;

            return (globals, token) =>
            {
                ValidateGlobals(globals, globalsType);
                return ScriptExecutionState.Create(globals).RunSubmissionsAsync<T>(precedingExecutors, currentExecutor, token);
            };
        }

        /// <summary>
        /// Continue script execution from the specified state.
        /// </summary>
        /// <param name="previousState">
        /// Previous state of the script execution.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running the script, including all declared variables and return value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="previousState"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="previousState"/> is not a previous execution state of this script.</exception>
        public new Task<ScriptState<T>> ContinueAsync(ScriptState previousState, CancellationToken cancellationToken = default(CancellationToken))
        {
            // The following validation and executor contruction may throw;
            // do so synchronously so that the exception is not wrapped in the task.

            if (previousState == null)
            {
                throw new ArgumentNullException(nameof(previousState));
            }

            if (previousState.Script == this)
            {
                // this state is already the output of running this script.
                return Task.FromResult((ScriptState<T>)previousState);
            }

            var precedingExecutors = TryGetPrecedingExecutors(previousState.Script, cancellationToken);
            if (precedingExecutors.IsDefault)
            {
                throw new ArgumentException(ScriptingResources.StartingStateIncompatible, nameof(previousState));
            }

            var currentExecutor = GetExecutor(cancellationToken);
            ScriptExecutionState newExecutionState = previousState.ExecutionState.FreezeAndClone();

            return RunSubmissionsAsync(newExecutionState, precedingExecutors, currentExecutor, cancellationToken);
        }

        private async Task<ScriptState<T>> RunSubmissionsAsync(ScriptExecutionState executionState, ImmutableArray<Func<object[], Task>> precedingExecutors, Func<object[], Task> currentExecutor, CancellationToken cancellationToken)
        {
            var result = await executionState.RunSubmissionsAsync<T>(precedingExecutors, currentExecutor, cancellationToken).ConfigureAwait(continueOnCapturedContext: true);
            return new ScriptState<T>(executionState, result, this);
        }

        private static void ValidateGlobals(object globals, Type globalsType)
        {
            if (globalsType != null)
            {
                if (globals == null)
                {
                    throw new ArgumentException(ScriptingResources.ScriptRequiresGlobalVariables, nameof(globals));
                }

                var runtimeType = globals.GetType().GetTypeInfo();
                var globalsTypeInfo = globalsType.GetTypeInfo();

                if (!globalsTypeInfo.IsAssignableFrom(runtimeType))
                {
                    throw new ArgumentException(string.Format(ScriptingResources.GlobalsNotAssignable, runtimeType, globalsTypeInfo), nameof(globals));
                }
            }
            else if (globals != null)
            {
                throw new ArgumentException(ScriptingResources.GlobalVariablesWithoutGlobalType, nameof(globals));
            }
        }
    }
}
