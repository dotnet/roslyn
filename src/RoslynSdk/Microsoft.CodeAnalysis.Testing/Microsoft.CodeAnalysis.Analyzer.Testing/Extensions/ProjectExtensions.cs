// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Testing.Lightup;

namespace Microsoft.CodeAnalysis.Testing
{
    internal static class ProjectExtensions
    {
        private static readonly Func<Project, IEnumerable<TextDocument>> s_analyzerConfigDocuments =
            LightupHelpers.CreatePropertyAccessor<Project, IEnumerable<TextDocument>>(
                typeof(Project),
                nameof(AnalyzerConfigDocuments),
                defaultValue: Enumerable.Empty<TextDocument>());

        public static IEnumerable<TextDocument> AnalyzerConfigDocuments(this Project project)
            => s_analyzerConfigDocuments(project);
    }
}
