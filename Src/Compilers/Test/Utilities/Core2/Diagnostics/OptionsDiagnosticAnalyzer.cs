using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class OptionsDiagnosticAnalyzer<TSyntaxKind> : TestDiagnosticAnalyzer<TSyntaxKind>
    {
        private AnalyzerOptions expectedOptions;
        private Dictionary<string, AnalyzerOptions> mismatchedOptions = new Dictionary<string, AnalyzerOptions>();

        public OptionsDiagnosticAnalyzer(AnalyzerOptions expectedOptions)
        {
            this.expectedOptions = expectedOptions;
        }

        protected override void OnInterfaceMember(SyntaxNode node = null, ISymbol symbol = null, [CallerMemberName]string callerName = null)
        {
        }
    

        protected override void OnOptions(AnalyzerOptions options, [CallerMemberName]string callerName = null)
        {
            if (options == this.expectedOptions)
            {
                return;
            }

            if (mismatchedOptions.ContainsKey(callerName))
            {
                mismatchedOptions[callerName] = options;
            }
            else
            {
                mismatchedOptions.Add(callerName, options);
            }
        }

        public void VerifyAnalyzerOptions()
        {
            Assert.True(mismatchedOptions.Count == 0, 
                        mismatchedOptions.Aggregate("Mismatched calls: ", (s, m) => s + "\r\nfrom : " + m.Key + ", options :" + m.Value));
        }
    }
}
