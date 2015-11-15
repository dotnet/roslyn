// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.CSharp.Scripting
{
    /// <summary>
    /// A factory for creating and running C# scripts.
    /// </summary>
    public static class CSharpScript
    {
        /// <summary>
        /// Create a new C# script.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// <param name="globalsType">Type of global object.</param>
        /// <param name="assemblyLoader">Custom  assembly loader.</param>
        /// <typeparam name="T">The return type of the script</typeparam>
        public static Script<T> Create<T>(string code, ScriptOptions options = null, Type globalsType = null, InteractiveAssemblyLoader assemblyLoader = null)
        {
            return Script.CreateInitialScript<T>(CSharpScriptCompiler.Instance, code, options, globalsType, assemblyLoader);
        }

        /// <summary>
        /// Create a new C# script.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// <param name="globalsType">Type of global object.</param>
        /// <param name="assemblyLoader">Custom  assembly loader.</param>
        public static Script<object> Create(string code, ScriptOptions options = null, Type globalsType = null, InteractiveAssemblyLoader assemblyLoader = null)
        {
            return Create<object>(code, options, globalsType, assemblyLoader);
        }

        /// <summary>
        /// Run a C# script.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// <param name="globals">An object instance whose members can be accessed by the script as global variables.</param>
        /// <param name="globalsType">Type of global object, <paramref name="globals"/>.GetType() is used if not specified.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <typeparam name="T">The return type of the submission</typeparam>
        /// <exception cref="CompilationErrorException">Specified code has errors.</exception>
        public static Task<ScriptState<T>> RunAsync<T>(string code, ScriptOptions options = null, object globals = null, Type globalsType = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Create<T>(code, options, globalsType ?? globals?.GetType()).RunAsync(globals, cancellationToken);
        }

        /// <summary>
        /// Run a C# script.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// <param name="globals">An object instance whose members can be accessed by the script as global variables.</param>
        /// <param name="globalsType">Type of global object, <paramref name="globals"/>.GetType() is used if not specified.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="CompilationErrorException">Specified code has errors.</exception>
        public static Task<ScriptState<object>> RunAsync(string code, ScriptOptions options = null, object globals = null, Type globalsType = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RunAsync<object>(code, options, globals, globalsType, cancellationToken);
        }

        /// <summary>
        /// Run a C# script and return its resulting value.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// <param name="globals">An object instance whose members can be accessed by the script as global variables.</param>
        /// <param name="globalsType">Type of global object, <paramref name="globals"/>.GetType() is used if not specified.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <typeparam name="T">The return type of the submission</typeparam>
        /// <return>Returns the value returned by running the script.</return>
        /// <exception cref="CompilationErrorException">Specified code has errors.</exception>
        public static Task<T> EvaluateAsync<T>(string code, ScriptOptions options = null, object globals = null, Type globalsType = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RunAsync<T>(code, options, globals, globalsType, cancellationToken).GetEvaluationResultAsync();
        }

        /// <summary>
        /// Run a C# script and return its resulting value.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// <param name="globals">An object instance whose members can be accessed by the script as global variables.</param>
        /// <param name="globalsType">Type of global object, <paramref name="globals"/>.GetType() is used if not specified.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <return>Returns the value returned by running the script.</return>
        /// <exception cref="CompilationErrorException">Specified code has errors.</exception>
        public static Task<object> EvaluateAsync(string code, ScriptOptions options = null, object globals = null, Type globalsType = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return EvaluateAsync<object>(code, options, globals, globalsType, cancellationToken);
        }
    }
}

