// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class OptionsDiagnosticAnalyzer<TLanguageKindEnum> : TestDiagnosticAnalyzer<TLanguageKindEnum> where TLanguageKindEnum : struct
    {
        private AnalyzerOptions expectedOptions;
        private Dictionary<string, AnalyzerOptions> mismatchedOptions = new Dictionary<string, AnalyzerOptions>();

        public OptionsDiagnosticAnalyzer(AnalyzerOptions expectedOptions)
        {
            this.expectedOptions = expectedOptions;
        }

        protected override void OnAbstractMember(string AbstractMemberName, SyntaxNode node = null, ISymbol symbol = null, [CallerMemberName]string callerName = null)
        {
        }
    

        protected override void OnOptions(AnalyzerOptions options, [CallerMemberName]string callerName = null)
        {
            if (AreEqual(options, this.expectedOptions))
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

        private bool AreEqual(AnalyzerOptions actual, AnalyzerOptions expected)
        {
            if (actual.AdditionalStreams.Length != expected.AdditionalStreams.Length)
            {
                return false;
            }

            for (int i =0; i < actual.AdditionalStreams.Length; i++)
            {
                if (actual.AdditionalStreams[i].Path != expected.AdditionalStreams[i].Path)
                {
                    return false;
                }
            }

            return true;
        }

        public void VerifyAnalyzerOptions()
        {
            Assert.True(mismatchedOptions.Count == 0, 
                        mismatchedOptions.Aggregate("Mismatched calls: ", (s, m) => s + "\r\nfrom : " + m.Key + ", options :" + m.Value));
        }
    }
}
