// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class ThrowingDiagnosticAnalyzer<TLanguageKindEnum> : TestDiagnosticAnalyzer<TLanguageKindEnum> where TLanguageKindEnum : struct
    {
        public class DeliberateException : Exception
        {
            public override string Message
            {
                get { return "If this goes unhandled, our diagnostics engine is susceptible to malicious analyzers"; }
            }
        }

        private readonly List<string> _throwOnList = new List<string>();

        public bool Thrown { get; private set; }

        public void ThrowOn(string method)
        {
            _throwOnList.Add(method);
        }

        protected override void OnAbstractMember(string abstractMemberName, SyntaxNode node, ISymbol symbol, string methodName)
        {
            if (_throwOnList.Contains(methodName))
            {
                Thrown = true;
                throw new DeliberateException();
            }
        }

        public static void VerifyAnalyzerEngineIsSafeAgainstExceptions(Func<DiagnosticAnalyzer, IEnumerable<Diagnostic>> runAnalysis)
        {
            var handled = new bool?[AllAnalyzerMemberNames.Length];
            for (int i = 0; i < AllAnalyzerMemberNames.Length; i++)
            {
                var member = AllAnalyzerMemberNames[i];
                var analyzer = new ThrowingDiagnosticAnalyzer<TLanguageKindEnum>();
                analyzer.ThrowOn(member);
                try
                {
                    var diagnostics = runAnalysis(analyzer).Distinct();
                    handled[i] = analyzer.Thrown ? true : (bool?)null;
                    if (analyzer.Thrown)
                    {
                        Assert.True(diagnostics.Any(AnalyzerExecutor.IsAnalyzerExceptionDiagnostic));
                    }
                    else
                    {
                        Assert.False(diagnostics.Any(AnalyzerExecutor.IsAnalyzerExceptionDiagnostic));
                    }
                }
                catch (DeliberateException)
                {
                    handled[i] = false;
                }
            }

            var membersHandled = AllAnalyzerMemberNames.Zip(handled, (m, h) => new { Member = m, Handled = h });
            Assert.True(!handled.Any(h => h == false) && handled.Any(h => true), Environment.NewLine +
                "  Exceptions thrown by analyzers in these members were *NOT* handled:" + Environment.NewLine + string.Join(Environment.NewLine, membersHandled.Where(mh => mh.Handled == false).Select(mh => mh.Member)) + Environment.NewLine + Environment.NewLine +
                "  Exceptions thrown from these members were handled gracefully:" + Environment.NewLine + string.Join(Environment.NewLine, membersHandled.Where(mh => mh.Handled == true).Select(mh => mh.Member)) + Environment.NewLine + Environment.NewLine +
                "  These members were not called/accessed by analyzer engine:" + Environment.NewLine + string.Join(Environment.NewLine, membersHandled.Where(mh => mh.Handled == null).Select(mh => mh.Member)));
        }
    }
}
