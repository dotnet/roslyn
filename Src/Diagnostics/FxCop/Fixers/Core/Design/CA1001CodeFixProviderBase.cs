using System.Collections.Generic;
using Microsoft.CodeAnalysis.FxCopDiagnosticFixers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1001: Types that own disposable fields should be disposable
    /// </summary>
    public abstract class CA1001CodeFixProviderBase : CodeFixProviderBase
    {
        protected const string NotImplementedExceptionName = "System.NotImplementedException";
        protected const string IDisposableName = "System.IDisposable";

        public sealed override IEnumerable<string> GetFixableDiagnosticIds()
        {
            return SpecializedCollections.SingletonEnumerable(CA1001DiagnosticAnalyzer.RuleId);
        }

        protected sealed override string GetCodeFixDescription(string ruleId)
        {
            return FxCopFixersResources.ImplementIDisposableInterface;
        }
    }
}