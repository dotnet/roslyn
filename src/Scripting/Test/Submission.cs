// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// Compiled executable submission.
    /// </summary>
    internal sealed class Submission<T>
    {
        private readonly Script _script;
        private readonly Lazy<ScriptState> _lazyResult;

        internal Submission(Script script, Lazy<object> input)
        {
            _script = script;
            _lazyResult = new Lazy<ScriptState>(() => script.Run(input.Value));
        }

        internal ScriptState Run()
        {
            var result = _lazyResult.Value;
            Debug.Assert(result.Script == _script, "Script does not match end state.");
            return result;
        }

        public T Execute()
        {
            return (T)this.Run().ReturnValue;
        }

        public Compilation Compilation
        {
            get { return _script.GetCompilation(); }
        }
    }
}
