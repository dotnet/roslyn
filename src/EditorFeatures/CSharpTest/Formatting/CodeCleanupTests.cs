// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.CodeCleanup;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting
{
    [UseExportProvider]
    public class CodeCleanupTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeCleanup)]
        public Task FormatDocumentRemoveUsings()
        {
            var code = @"using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
    }
}
";

            var expected = @"using System;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();
    }
}
";
            return AssertCodeCleanupResult(expected, code,
                (FeatureOnOffOptions.IsCodeCleanupRulesConfigured, enabled: true),
                (FeatureOnOffOptions.RemoveUnusedUsings, enabled: true));
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeCleanup)]
        public Task FormatDocumentSortUsings()
        {
            var code = @"using System.Collections.Generic;
using System;
class Program
{
    static void Main(string[] args)
    {
        var list = new List<int>();
        Console.WriteLine(list.Count);
    }
}
";

            var expected = @"using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        var list = new List<int>();
        Console.WriteLine(list.Count);
    }
}
";
            return AssertCodeCleanupResult(expected, code,
                (FeatureOnOffOptions.IsCodeCleanupRulesConfigured, enabled: true),
                (FeatureOnOffOptions.SortUsings, enabled: true));
        }

        [Fact(Skip = "disable the test temporarily until figure out how to set up diagnostic analyzer")]
        [Trait(Traits.Feature, Traits.Features.CodeCleanup)]
        public Task FormatDocumentRemoveUnusedVariable()
        {
            var code = @"class Program
{
    void Method()
    {
        int a;
    }
}
";
            var expected = @"class Program
{
    void Method()
    {
    }
}
";
            return AssertCodeCleanupResult(expected, code,
                (FeatureOnOffOptions.IsCodeCleanupRulesConfigured, enabled:true),
                (FeatureOnOffOptions.RemoveUnusedVariables, enabled: true));

            //workspace.Options = workspace.Options.WithChangedOption(RemoteFeatureOptions.DiagnosticsEnabled, false);
        }

        protected static async Task AssertCodeCleanupResult(string expected, string code, params (PerLanguageOption<bool> option, bool enabled)[] options)
        {
            using (var workspace = TestWorkspace.CreateCSharp(code))
            {
                if (options != null)
                {
                    foreach (var option in options)
                    {
                        workspace.Options = workspace.Options.WithChangedOption(option.option, LanguageNames.CSharp, option.enabled);
                    }
                }


                var hostdoc = workspace.Documents.Single();
                var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);

                var codeCleanupService = document.GetLanguageService<ICodeCleanupService>();
                var newDoc = await codeCleanupService.CleanupDocument(document, new CancellationToken());

                var actual = await newDoc.GetTextAsync();

                Assert.Equal(expected, actual.ToString());
            }
        }

    }
}
