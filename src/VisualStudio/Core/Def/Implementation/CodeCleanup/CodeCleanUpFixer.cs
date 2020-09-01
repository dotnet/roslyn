// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public abstract Task<bool> FixAsync(ICodeCleanUpScope scope, ICodeCleanUpExecutionContext context);
    }
}
