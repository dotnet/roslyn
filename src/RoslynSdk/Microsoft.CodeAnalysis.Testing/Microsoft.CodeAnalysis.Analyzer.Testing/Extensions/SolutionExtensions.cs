// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing
{
    internal static class SolutionExtensions
    {
        private static readonly Func<Solution, DocumentId, string, SourceText, IEnumerable<string>?, string?, Solution> s_addAnalyzerConfigDocument;

        static SolutionExtensions()
        {
            var methodInfo = typeof(Solution).GetMethod(nameof(AddAnalyzerConfigDocument), new[] { typeof(DocumentId), typeof(string), typeof(SourceText), typeof(IEnumerable<string>), typeof(string) });
            if (methodInfo is { })
            {
                s_addAnalyzerConfigDocument = (Func<Solution, DocumentId, string, SourceText, IEnumerable<string>?, string?, Solution>)methodInfo.CreateDelegate(typeof(Func<Solution, DocumentId, string, SourceText, IEnumerable<string>, string, Solution>), target: null);
            }
            else
            {
                s_addAnalyzerConfigDocument = (solution, documentId, name, text, folders, filePath) => throw new NotSupportedException();
            }
        }

        public static Solution AddAnalyzerConfigDocument(this Solution solution, DocumentId documentId, string name, SourceText text, IEnumerable<string>? folders = null, string? filePath = null)
        {
            return s_addAnalyzerConfigDocument(solution, documentId, name, text, folders, filePath);
        }
    }
}
