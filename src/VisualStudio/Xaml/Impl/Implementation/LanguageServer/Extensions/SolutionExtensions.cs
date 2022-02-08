// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Xaml;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Extensions
{
    internal static class SolutionExtensions
    {
        public static IEnumerable<Project> GetXamlProjects(this Solution solution)
            => solution.Projects.Where(p => p.Language == StringConstants.XamlLanguageName);
    }
}
