// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;


namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting
{
    public class CodeCleanupTests : FormattingEngineTestBase
    {
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatDocumentRemoveUsings()
        {
            var code = @"using System;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();$$
    }
}
";

            var expected = @"using System;
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine();$$
    }
}
";
            AssertFormatWithView(expected, code, false,
                (FeatureOnOffOptions.IsCodeCleanupRulesConfigured, true),
                (FeatureOnOffOptions.RemoveUnusedUsings, true));
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatDocumentSortUsings()
        {
            var code = @"using System.Collections.Generic;
using System;
class Program
{
    static void Main(string[] args)
    {
        var list = new List<int>();$$
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
        var list = new List<int>();$$
        Console.WriteLine(list.Count);
    }
}
";
            AssertFormatWithView(expected, code, false,
                (FeatureOnOffOptions.IsCodeCleanupRulesConfigured, true),
                (FeatureOnOffOptions.SortUsings, true));
        }

        [WpfFact(Skip = "disable the test temporarily until figure out how to set up diagnostic analyzer")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatDocumentRemoveUnusedVariable()
        {
            var code = @"class Program
{
    void Method($$)
    {
        [|int a = 3;|]
    }
}
";
            var expected = @"class Program
{
    void Method($$)
    {
    }
}
";
            AssertFormatWithView(expected, code, false,
                (FeatureOnOffOptions.IsCodeCleanupRulesConfigured, true),
                (FeatureOnOffOptions.RemoveUnusedVariables, true));

            //workspace.Options = workspace.Options.WithChangedOption(RemoteFeatureOptions.DiagnosticsEnabled, false);
        }

    }
}
