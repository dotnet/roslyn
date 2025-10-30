// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;
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
        internal readonly ScriptBuilder Builder;

        private Compilation _lazyCompilation;

        internal Script(ScriptCompiler compiler, ScriptBuilder builder, SourceText sourceText, ScriptOptions options, Type globalsTypeOpt, Script previousOpt)
        {
            Debug.Assert(sourceText != null);
            Debug.Assert(options != null);
            Debug.Assert(compiler != null);
            Debug.Assert(builder != null);

            Compiler = compiler;
            Builder = builder;
            Previous = previousOpt;
            SourceText = sourceText;
            Options = options;
            GlobalsType = globalsTypeOpt;
        }

        internal static Script<T> CreateInitialScript<T>(ScriptCompiler compiler, SourceText sourceText, ScriptOptions optionsOpt, Type globalsTypeOpt, InteractiveAssemblyLoader assemblyLoaderOpt)
        {
            return new Script<T>(compiler, new ScriptBuilder(assemblyLoaderOpt ?? new InteractiveAssemblyLoader()), sourceText, optionsOpt ?? ScriptOptions.Default, globalsTypeOpt, previousOpt: null);
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
        public string Code => SourceText.ToString();

        /// <summary>
        /// The <see cref="SourceText"/> of the script.
        /// </summary>
        internal SourceText SourceText { get; }

        /// <summary>
        /// The type of an object whose members can be accessed by the script as global variables.
        /// </summary>
        public Type GlobalsType { get; }

        /// <summary>
        /// The expected return type of the script.
        /// </summary>
        public abstract Type ReturnType { get; }

        /// <summary>
        /// Creates a new version of this script with the specified options.
        /// </summary>
        public Script WithOptions(ScriptOptions options) => WithOptionsInternal(options);
        internal abstract Script WithOptionsInternal(ScriptOptions options);

        /// <summary>
        /// Continues the script with given code snippet.
        /// </summary>
        public Script<object> ContinueWith(string code, ScriptOptions options = null)
            => ContinueWith<object>(code, options);

        /// <summary>
        /// Continues the script with given <see cref="Stream"/> representing code.
        /// </summary>
        /// <exception cref="ArgumentNullException">Stream is null.</exception>
        /// <exception cref="ArgumentException">Stream is not readable or seekable.</exception>
        public Script<object> ContinueWith(Stream code, ScriptOptions options = null)
            => ContinueWith<object>(code, options);

        /// <summary>
        /// Continues the script with given code snippet.
        /// </summary>
        public Script<TResult> ContinueWith<TResult>(string code, ScriptOptions options = null)
        {
            options = options ?? InheritOptions(Options);
            return new Script<TResult>(Compiler, Builder, SourceText.From(code ?? "", options.FileEncoding), options, GlobalsType, this);
        }

        /// <summary>
        /// Continues the script with given <see cref="Stream"/> representing code.
        /// </summary>
        /// <exception cref="ArgumentNullException">Stream is null.</exception>
        /// <exception cref="ArgumentException">Stream is not readable or seekable.</exception>
        public Script<TResult> ContinueWith<TResult>(Stream code, ScriptOptions options = null)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));
            options = options ?? InheritOptions(Options);
            return new Script<TResult>(Compiler, Builder, SourceText.From(code, options.FileEncoding), options, GlobalsType, this);
        }

        private static ScriptOptions InheritOptions(ScriptOptions previous)
        {
            // don't inherit references or imports, they have already been applied:
            return previous.
                WithReferences(ImmutableArray<MetadataReference>.Empty).
                WithImports(ImmutableArray<string>.Empty);
        }

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
        internal Task<object> EvaluateAsync(object globals = null, CancellationToken cancellationToken = default(CancellationToken))
            => CommonEvaluateAsync(globals, cancellationToken);

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
        public Task<ScriptState> RunAsync(object globals, CancellationToken cancellationToken)
            => CommonRunAsync(globals, null, cancellationToken);

        /// <summary>
        /// Runs the script from the beginning.
        /// </summary>
        /// <param name="globals">
        /// An instance of <see cref="Script.GlobalsType"/> holding on values for global variables accessible from the script.
        /// Must be specified if and only if the script was created with <see cref="Script.GlobalsType"/>.
        /// </param>
        /// <param name="catchException">
        /// If specified, any exception thrown by the script top-level code is passed to <paramref name="catchException"/>.
        /// If it returns true the exception is caught and stored on the resulting <see cref="ScriptState"/>, otherwise the exception is propagated to the caller.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running the script, including all declared variables and return value.</returns>
        public Task<ScriptState> RunAsync(object globals = null, Func<Exception, bool> catchException = null, CancellationToken cancellationToken = default(CancellationToken))
            => CommonRunAsync(globals, catchException, cancellationToken);

        internal abstract Task<ScriptState> CommonRunAsync(object globals, Func<Exception, bool> catchException, CancellationToken cancellationToken);

        /// <summary>
        /// Run the script from the specified state.
        /// </summary>
        /// <param name="previousState">
        /// Previous state of the script execution.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running the script, including all declared variables and return value.</returns>
        public Task<ScriptState> RunFromAsync(ScriptState previousState, CancellationToken cancellationToken)
            => CommonRunFromAsync(previousState, null, cancellationToken);

        /// <summary>
        /// Run the script from the specified state.
        /// </summary>
        /// <param name="previousState">
        /// Previous state of the script execution.
        /// </param>
        /// <param name="catchException">
        /// If specified, any exception thrown by the script top-level code is passed to <paramref name="catchException"/>.
        /// If it returns true the exception is caught and stored on the resulting <see cref="ScriptState"/>, otherwise the exception is propagated to the caller.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running the script, including all declared variables and return value.</returns>
        public Task<ScriptState> RunFromAsync(ScriptState previousState, Func<Exception, bool> catchException = null, CancellationToken cancellationToken = default(CancellationToken))
            => CommonRunFromAsync(previousState, catchException, cancellationToken);

        internal abstract Task<ScriptState> CommonRunFromAsync(ScriptState previousState, Func<Exception, bool> catchException, CancellationToken cancellationToken);

        /// <summary>
        /// Forces the script through the compilation step.
        /// If not called directly, the compilation step will occur on the first call to Run.
        /// </summary>
        public ImmutableArray<Diagnostic> Compile(CancellationToken cancellationToken = default(CancellationToken))
            => CommonCompile(cancellationToken);

        internal abstract ImmutableArray<Diagnostic> CommonCompile(CancellationToken cancellationToken);
        internal abstract Func<object[], Task> CommonGetExecutor(CancellationToken cancellationToken);

        // Apply recursive alias <host> to the host assembly reference, so that we hide its namespaces and global types behind it.
        internal static readonly MetadataReferenceProperties HostAssemblyReferenceProperties =
            MetadataReferenceProperties.Assembly.WithAliases(ImmutableArray.Create("<host>")).WithRecursiveAliases(true);

        /// <summary>
        /// Gets the references that need to be assigned to the compilation.
        /// This can be different than the list of references defined by the <see cref="ScriptOptions"/> instance.
        /// </summary>
        internal ImmutableArray<MetadataReference> GetReferencesForCompilation(
            CommonMessageProvider messageProvider,
            DiagnosticBag diagnostics,
            MetadataReference languageRuntimeReferenceOpt = null)
        {
            var resolver = Options.MetadataResolver;
            var references = ArrayBuilder<MetadataReference>.GetInstance();
            try
            {
                if (Previous == null)
                {
                    var corLib = CreateFromAssembly(typeof(object).GetTypeInfo().Assembly, default, Options.CreateFromFileFunc);
                    references.Add(corLib);

                    if (GlobalsType != null)
                    {
                        var globalsAssembly = GlobalsType.GetTypeInfo().Assembly;

                        // If the assembly doesn't have metadata (it's an in-memory or dynamic assembly),
                        // the host has to add reference to the metadata where globals type is located explicitly.
                        if (MetadataReference.HasMetadata(globalsAssembly))
                        {
                            references.Add(CreateFromAssembly(globalsAssembly, HostAssemblyReferenceProperties, Options.CreateFromFileFunc));
                        }
                    }

                    if (languageRuntimeReferenceOpt != null)
                    {
                        references.Add(languageRuntimeReferenceOpt);
                    }
                }

                // add new references:
                foreach (var reference in Options.MetadataReferences)
                {
                    if (reference is UnresolvedMetadataReference unresolved)
                    {
                        var resolved = resolver.ResolveReference(unresolved.Reference, null, unresolved.Properties);
                        if (resolved.IsDefault)
                        {
                            diagnostics.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_MetadataFileNotFound, Location.None, unresolved.Reference));
                        }
                        else
                        {
                            references.AddRange(resolved);
                        }
                    }
                    else
                    {
                        references.Add(reference);
                    }
                }

                return references.ToImmutable();
            }
            finally
            {
                references.Free();
            }
        }

        // TODO: remove
        internal bool HasReturnValue()
        {
            return GetCompilation().HasSubmissionResult();
        }

        internal static MetadataImageReference CreateFromAssembly(
            Assembly assembly,
            MetadataReferenceProperties properties,
            Func<string, PEStreamOptions, MetadataReferenceProperties, MetadataImageReference> createFromFileFunc)
        {
            // The file is locked by the CLR assembly loader, so we can create a lazily read metadata, 
            // which might also lock the file until the reference is GC'd.
            var filePath = MetadataReference.GetAssemblyFilePath(assembly, properties);
            return createFromFileFunc(filePath, PEStreamOptions.Default, properties);
        }

        /// <summary>
        /// <see cref="MetadataReference.CreateFromFile(string, PEStreamOptions, MetadataReferenceProperties, DocumentationProvider)"/>
        /// </summary>
        /// <remarks>
        /// This API exists as the default for reading <see cref="MetadataReference"/> from files. It is handy 
        /// to have this separate method when trying to track down unit tests that aren't going through the 
        /// proper helpers in ScriptTestBase to hook loading from file. Can set a breakpoint here, debug the 
        /// tests and fix any hits.
        /// </remarks>
        internal static MetadataImageReference CreateFromFile(
            string filePath,
            PEStreamOptions options,
            MetadataReferenceProperties properties) =>
            MetadataReference.CreateFromFile(filePath, options, properties);
    }

    public sealed class Script<T> : Script
    {
        private ImmutableArray<Func<object[], Task>> _lazyPrecedingExecutors;
        private Func<object[], Task<T>> _lazyExecutor;

        internal Script(ScriptCompiler compiler, ScriptBuilder builder, SourceText sourceText, ScriptOptions options, Type globalsTypeOpt, Script previousOpt)
            : base(compiler, builder, sourceText, options, globalsTypeOpt, previousOpt)
        {
        }

        public override Type ReturnType => typeof(T);

        public new Script<T> WithOptions(ScriptOptions options)
        {
            return (options == Options) ? this : new Script<T>(Compiler, Builder, SourceText, options, GlobalsType, Previous);
        }

        internal override Script WithOptionsInternal(ScriptOptions options) => WithOptions(options);

        internal override ImmutableArray<Diagnostic> CommonCompile(CancellationToken cancellationToken)
        {
            // TODO: avoid throwing exception, report all diagnostics https://github.com/dotnet/roslyn/issues/5949
            try
            {
                GetPrecedingExecutors(cancellationToken);
                GetExecutor(cancellationToken);

                return ImmutableArray.CreateRange(GetCompilation().GetDiagnostics(cancellationToken).Where(d => d.Severity == DiagnosticSeverity.Warning));
            }
            catch (CompilationErrorException e)
            {
                return ImmutableArray.CreateRange(e.Diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning));
            }
        }

        internal override Func<object[], Task> CommonGetExecutor(CancellationToken cancellationToken)
            => GetExecutor(cancellationToken);

        internal override Task<object> CommonEvaluateAsync(object globals, CancellationToken cancellationToken)
            => EvaluateAsync(globals, cancellationToken).CastAsync<T, object>();

        internal override Task<ScriptState> CommonRunAsync(object globals, Func<Exception, bool> catchException, CancellationToken cancellationToken)
            => RunAsync(globals, catchException, cancellationToken).CastAsync<ScriptState<T>, ScriptState>();

        internal override Task<ScriptState> CommonRunFromAsync(ScriptState previousState, Func<Exception, bool> catchException, CancellationToken cancellationToken)
            => RunFromAsync(previousState, catchException, cancellationToken).CastAsync<ScriptState<T>, ScriptState>();

        /// <exception cref="CompilationErrorException">Compilation has errors.</exception>
        private Func<object[], Task<T>> GetExecutor(CancellationToken cancellationToken)
        {
            if (_lazyExecutor == null)
            {
                Interlocked.CompareExchange(ref _lazyExecutor, Builder.CreateExecutor<T>(Compiler, GetCompilation(), Options.EmitDebugInformation, cancellationToken), null);
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
        internal new Task<T> EvaluateAsync(object globals = null, CancellationToken cancellationToken = default(CancellationToken))
            => RunAsync(globals, cancellationToken).GetEvaluationResultAsync();

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
        public new Task<ScriptState<T>> RunAsync(object globals, CancellationToken cancellationToken)
            => RunAsync(globals, null, cancellationToken);

        /// <summary>
        /// Runs the script from the beginning.
        /// </summary>
        /// <param name="globals">
        /// An instance of <see cref="Script.GlobalsType"/> holding on values for global variables accessible from the script.
        /// Must be specified if and only if the script was created with <see cref="Script.GlobalsType"/>.
        /// </param>
        /// <param name="catchException">
        /// If specified, any exception thrown by the script top-level code is passed to <paramref name="catchException"/>.
        /// If it returns true the exception is caught and stored on the resulting <see cref="ScriptState"/>, otherwise the exception is propagated to the caller.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running the script, including all declared variables and return value.</returns>
        /// <exception cref="CompilationErrorException">Compilation has errors.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="globals"/> doesn't match <see cref="Script.GlobalsType"/>.</exception>
        public new Task<ScriptState<T>> RunAsync(object globals = null, Func<Exception, bool> catchException = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // The following validation and executor construction may throw;
            // do so synchronously so that the exception is not wrapped in the task.

            ValidateGlobals(globals, GlobalsType);

            var executionState = ScriptExecutionState.Create(globals);
            var precedingExecutors = GetPrecedingExecutors(cancellationToken);
            var currentExecutor = GetExecutor(cancellationToken);

            return RunSubmissionsAsync(executionState, precedingExecutors, currentExecutor, catchException, cancellationToken);
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
                return ScriptExecutionState.Create(globals).RunSubmissionsAsync<T>(precedingExecutors, currentExecutor, null, null, token);
            };
        }

        /// <summary>
        /// Run the script from the specified state.
        /// </summary>
        /// <param name="previousState">
        /// Previous state of the script execution.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running the script, including all declared variables and return value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="previousState"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="previousState"/> is not a previous execution state of this script.</exception>
        public new Task<ScriptState<T>> RunFromAsync(ScriptState previousState, CancellationToken cancellationToken)
            => RunFromAsync(previousState, null, cancellationToken);

        /// <summary>
        /// Run the script from the specified state.
        /// </summary>
        /// <param name="previousState">
        /// Previous state of the script execution.
        /// </param>
        /// <param name="catchException">
        /// If specified, any exception thrown by the script top-level code is passed to <paramref name="catchException"/>.
        /// If it returns true the exception is caught and stored on the resulting <see cref="ScriptState"/>, otherwise the exception is propagated to the caller.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="ScriptState"/> that represents the state after running the script, including all declared variables and return value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="previousState"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="previousState"/> is not a previous execution state of this script.</exception>
        public new Task<ScriptState<T>> RunFromAsync(ScriptState previousState, Func<Exception, bool> catchException = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // The following validation and executor construction may throw;
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

            return RunSubmissionsAsync(newExecutionState, precedingExecutors, currentExecutor, catchException, cancellationToken);
        }

        private async Task<ScriptState<T>> RunSubmissionsAsync(
            ScriptExecutionState executionState,
            ImmutableArray<Func<object[], Task>> precedingExecutors,
            Func<object[], Task> currentExecutor,
            Func<Exception, bool> catchExceptionOpt,
            CancellationToken cancellationToken)
        {
            var exceptionOpt = (catchExceptionOpt != null) ? new StrongBox<Exception>() : null;
            T result = await executionState.RunSubmissionsAsync<T>(precedingExecutors, currentExecutor, exceptionOpt, catchExceptionOpt, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            return new ScriptState<T>(executionState, this, result, exceptionOpt?.Value);
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
