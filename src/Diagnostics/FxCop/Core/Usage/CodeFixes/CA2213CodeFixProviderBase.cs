// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    /// <summary>
    /// CA2213: Disposable fields should be disposed
    /// </summary>
    public abstract class CA2213CodeFixProviderBase : CodeFixProviderBase
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CA2213DiagnosticAnalyzer.RuleId); }
        }

        protected sealed override string GetCodeFixDescription(Diagnostic diagnostic)
        {
            return FxCopFixersResources.DisposableFieldsShouldBeDisposed;
        }
    }
}
