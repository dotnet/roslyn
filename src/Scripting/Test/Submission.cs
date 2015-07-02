// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// Compiled executable submission.
    /// </summary>
    internal sealed class Submission<T>
    {
        private readonly Script<T> _script;
        private readonly Lazy<ScriptState<T>> _lazyResult;

        internal Submission(Script<T> script, Lazy<object> input)
        {
            _script = script;
            _lazyResult = new Lazy<ScriptState<T>>(() => script.Run(input.Value));
        }

        internal ScriptState<T> Run()
        {
            var result = _lazyResult.Value;
            Debug.Assert(result.Script == _script, "Script does not match end state.");
            return result;
        }

        public T Execute()
        {
            return this.Run().ReturnValue.Result;
        }

        public Compilation Compilation
        {
            get { return _script.GetCompilation(); }
        }
    }
}
