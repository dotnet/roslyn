// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Testing
{
    internal static class ProjectExtensions
    {
        private static readonly Func<Project, IEnumerable<TextDocument>> s_analyzerConfigDocuments;

        static ProjectExtensions()
        {
            var analyzerConfigDocumentType = typeof(Project).GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.AnalyzerConfigDocument");
            if (analyzerConfigDocumentType is { })
            {
                var analyzerConfigDocumentsProperty = typeof(Project).GetProperty(nameof(AnalyzerConfigDocuments), typeof(IEnumerable<>).MakeGenericType(analyzerConfigDocumentType));
                if (analyzerConfigDocumentsProperty is { GetMethod: { } getMethod })
                {
                    s_analyzerConfigDocuments = (Func<Project, IEnumerable<TextDocument>>)getMethod.CreateDelegate(typeof(Func<Project, IEnumerable<TextDocument>>), target: null);
                }
                else
                {
                    s_analyzerConfigDocuments = project => Enumerable.Empty<TextDocument>();
                }
            }
            else
            {
                s_analyzerConfigDocuments = project => Enumerable.Empty<TextDocument>();
            }
        }

        public static IEnumerable<TextDocument> AnalyzerConfigDocuments(this Project project)
            => s_analyzerConfigDocuments(project);
    }
}
