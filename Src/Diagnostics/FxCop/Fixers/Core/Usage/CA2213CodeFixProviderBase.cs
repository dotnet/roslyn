using System.Collections.Generic;
using Microsoft.CodeAnalysis.FxCopDiagnosticFixers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Usage
{
    /// <summary>
    /// CA2213: Disposable fields should be disposed
    /// </summary>
    public abstract class CA2213CodeFixProviderBase : CodeFixProviderBase
    {
        public sealed override IEnumerable<string> GetFixableDiagnosticIds()
        {
            return SpecializedCollections.SingletonEnumerable(CA2213DiagnosticAnalyzer.RuleId);
        }

        protected sealed override string GetCodeFixDescription(string ruleId)
        {
            return FxCopFixersResources.DisposableFieldsShouldBeDisposed;
        }
    }
}