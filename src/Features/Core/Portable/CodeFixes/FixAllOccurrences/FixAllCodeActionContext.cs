// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// FixAll context with some additional information specifically for <see cref="FixAllCodeAction"/>.
    /// </summary>
    internal static partial class FixAllCodeActionContext 
    {

#if false

        public IEnumerable<Diagnostic> OriginalDiagnostics
        {
            get { return _originalFixDiagnostics; }
        }

        public FixAllProvider FixAllProvider
        {
            get { return _fixAllProviderInfo.FixAllProvider; }
        }

        public IEnumerable<FixAllScope> SupportedScopes
        {
            get { return _fixAllProviderInfo.SupportedScopes; }
        }
#endif
    }
}
