// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class OptionsDiagnosticAnalyzer<TLanguageKindEnum> : TestDiagnosticAnalyzer<TLanguageKindEnum> where TLanguageKindEnum : struct
    {
        private readonly AnalyzerOptions _expectedOptions;
        private readonly Dictionary<string, AnalyzerOptions> _mismatchedOptions = new Dictionary<string, AnalyzerOptions>();

        public OptionsDiagnosticAnalyzer(AnalyzerOptions expectedOptions)
        {
            _expectedOptions = expectedOptions;
        }

        protected override void OnAbstractMember(string AbstractMemberName, SyntaxNode node = null, ISymbol symbol = null, [CallerMemberName]string callerName = null)
        {
        }

        protected override void OnOptions(AnalyzerOptions options, [CallerMemberName]string callerName = null)
        {
            if (AreEqual(options, _expectedOptions))
            {
                return;
            }

            if (_mismatchedOptions.ContainsKey(callerName))
            {
                _mismatchedOptions[callerName] = options;
            }
            else
            {
                _mismatchedOptions.Add(callerName, options);
            }
        }

        private bool AreEqual(AnalyzerOptions actual, AnalyzerOptions expected)
        {
            if (actual.AdditionalFiles.Length != expected.AdditionalFiles.Length)
            {
                return false;
            }

            for (int i = 0; i < actual.AdditionalFiles.Length; i++)
            {
                if (actual.AdditionalFiles[i].Path != expected.AdditionalFiles[i].Path)
                {
                    return false;
                }
            }

            return true;
        }

        public void VerifyAnalyzerOptions()
        {
            Assert.True(_mismatchedOptions.Count == 0,
                        _mismatchedOptions.Aggregate("Mismatched calls: ", (s, m) => s + "\r\nfrom : " + m.Key + ", options :" + m.Value));
        }
    }
}
