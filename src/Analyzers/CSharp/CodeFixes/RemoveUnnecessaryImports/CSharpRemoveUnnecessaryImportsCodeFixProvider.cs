// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryImports), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddMissingReference)]
    internal class CSharpRemoveUnnecessaryImportsCodeFixProvider : AbstractRemoveUnnecessaryImportsCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpRemoveUnnecessaryImportsCodeFixProvider()
        {
        }

        protected override string GetTitle()
            => CSharpCodeFixesResources.Remove_unnecessary_usings;

        protected override ISyntaxFormatting GetSyntaxFormatting()
            => CSharpSyntaxFormatting.Instance;
    }
}
