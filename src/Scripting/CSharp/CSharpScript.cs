// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;

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
        /// <exception cref="ArgumentNullException">Code is null.</exception>
        public static Script<T> Create<T>(string code, ScriptOptions options = null, Type globalsType = null, InteractiveAssemblyLoader assemblyLoader = null)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));
            return Script.CreateInitialScript<T>(CSharpScriptCompiler.Instance, SourceText.From(code, options?.FileEncoding, SourceHashAlgorithms.Default), options, globalsType, assemblyLoader);
        }

        /// <summary>
        /// Create a new C# script.
        /// </summary>
        /// <param name="code">The <see cref="Stream"/> representing the source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// <param name="globalsType">Type of global object.</param>
        /// <param name="assemblyLoader">Custom  assembly loader.</param>
        /// <typeparam name="T">The return type of the script</typeparam>
        /// <exception cref="ArgumentNullException">Stream is null.</exception>
        /// <exception cref="ArgumentException">Stream is not readable or seekable.</exception>
        public static Script<T> Create<T>(Stream code, ScriptOptions options = null, Type globalsType = null, InteractiveAssemblyLoader assemblyLoader = null)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));
            return Script.CreateInitialScript<T>(CSharpScriptCompiler.Instance, SourceText.From(code, options?.FileEncoding), options, globalsType, assemblyLoader);
        }

        /// <summary>
        /// Create a new C# script.
        /// </summary>
        /// <param name="code">The source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// <param name="globalsType">Type of global object.</param>
        /// <param name="assemblyLoader">Custom  assembly loader.</param>
        /// <exception cref="ArgumentNullException">Code is null.</exception>
        public static Script<object> Create(string code, ScriptOptions options = null, Type globalsType = null, InteractiveAssemblyLoader assemblyLoader = null)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));
            return Create<object>(code, options, globalsType, assemblyLoader);
        }

        /// <summary>
        /// Create a new C# script.
        /// </summary>
        /// <param name="code">The <see cref="Stream"/> representing the source code of the script.</param>
        /// <param name="options">The script options.</param>
        /// <param name="globalsType">Type of global object.</param>
        /// <param name="assemblyLoader">Custom  assembly loader.</param>
        /// <exception cref="ArgumentNullException">Stream is null.</exception>
        /// <exception cref="ArgumentException">Stream is not readable or seekable.</exception>
        public static Script<object> Create(Stream code, ScriptOptions options = null, Type globalsType = null, InteractiveAssemblyLoader assemblyLoader = null)
        {
            if (code == null)
                throw new ArgumentNullException(nameof(code));
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

