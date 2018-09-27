// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.CodeCleanUp;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup
{
    internal abstract class CodeCleanUpFixer : ICodeCleanUpFixer
    {
        public abstract Task<bool> FixAsync(ICodeCleanUpScope scope, ICodeCleanUpExecutionContext context, CancellationToken cancellationToken);
    }
}
