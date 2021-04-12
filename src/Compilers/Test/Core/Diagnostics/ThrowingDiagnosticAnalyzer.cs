// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class ThrowingDiagnosticAnalyzer<TLanguageKindEnum> : TestDiagnosticAnalyzer<TLanguageKindEnum> where TLanguageKindEnum : struct
    {
        [Serializable]
        public class DeliberateException : Exception
        {
            public DeliberateException()
            {
            }

            protected DeliberateException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            {
                throw new NotImplementedException();
            }

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
            VerifyAnalyzerEngineIsSafeAgainstExceptionsAsync(runAnalysis).Wait();
        }

        public static async Task VerifyAnalyzerEngineIsSafeAgainstExceptionsAsync(Func<DiagnosticAnalyzer, IEnumerable<Diagnostic>> runAnalysis)
        {
            await VerifyAnalyzerEngineIsSafeAgainstExceptionsAsync(a => Task.FromResult(runAnalysis(a)));
        }

        public static async Task VerifyAnalyzerEngineIsSafeAgainstExceptionsAsync(Func<DiagnosticAnalyzer, Task<IEnumerable<Diagnostic>>> runAnalysis)
        {
            var handled = new bool?[AllAnalyzerMemberNames.Length];
            for (int i = 0; i < AllAnalyzerMemberNames.Length; i++)
            {
                var member = AllAnalyzerMemberNames[i];
                var analyzer = new ThrowingDiagnosticAnalyzer<TLanguageKindEnum>();
                analyzer.ThrowOn(member);
                try
                {
                    var diagnostics = (await runAnalysis(analyzer)).Distinct();
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
