// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.CodeCleanUp;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup
{
    /// <summary>
    /// Roslyn implementations of <see cref="ICodeCleanUpFixer"/> extend this class. Since other extensions could also
    /// be implementing the <see cref="ICodeCleanUpFixer"/> interface, this abstract base class allows Roslyn to operate
    /// on MEF instances of fixers known to be relevant in the context of Roslyn languages.
    /// </summary>
    internal abstract class CodeCleanUpFixer : ICodeCleanUpFixer
    {
        [Obsolete("use one without cancellationtoken", error: true)]
        public virtual Task<bool> FixAsync(ICodeCleanUpScope scope, ICodeCleanUpExecutionContext context, CancellationToken _)
        {
            // cancellation token will be removed in next API update
            return FixAsync(scope, context);
        }

        public abstract Task<bool> FixAsync(ICodeCleanUpScope scope, ICodeCleanUpExecutionContext context);
    }
}
