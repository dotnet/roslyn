using System.Collections.Generic;
using Microsoft.CodeAnalysis.FxCopDiagnosticFixers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Performance
{
    /// <summary>
    /// CA1813: Avoid unsealed attributes
    /// </summary>
    public abstract class CA1813CodeFixProviderBase : CodeFixProviderBase
    {
        public sealed override IEnumerable<string> GetFixableDiagnosticIds()
        {
            return SpecializedCollections.SingletonEnumerable(CA1813DiagnosticAnalyzer.RuleId);
        }

        protected sealed override string GetCodeFixDescription(string ruleId)
        {
            return FxCopFixersResources.AvoidUnsealedAttributesCodeFix;
        }
    }
}