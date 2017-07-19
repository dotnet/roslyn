// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpFormatting : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpFormatting(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpFormatting))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void AlignOpenBraceWithMethodDeclaration()
        {
            using (var telemetry = VisualStudio.EnableTestTelemetryChannel())
            {
                SetUpEditor(@"
$$class C
{
    void Main()
     {
    }
}");

                VisualStudio.Editor.FormatDocument();
                VisualStudio.Editor.Verify.TextContains(@"
class C
{
    void Main()
    {
    }
}");
                telemetry.VerifyFired("vs/ide/vbcs/commandhandler/formatcommand");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatOnSemicolon()
        {
            SetUpEditor(@"
public class C
{
    void Foo()
    {
        var x =        from a             in       new List<int>()
    where x % 2 = 0
                      select x   ;$$
    }
}");

            VisualStudio.Editor.SendKeys(VirtualKey.Backspace, ";");
            VisualStudio.Editor.Verify.TextContains(@"
public class C
{
    void Foo()
    {
        var x = from a in new List<int>()
                where x % 2 = 0
                select x;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatSelection()
        {
            SetUpEditor(@"
public class C {
    public void M( ) {$$
        }
}");

            VisualStudio.Editor.SelectTextInCurrentDocument("public void M( ) {");
            VisualStudio.Editor.FormatSelection();
            VisualStudio.Editor.Verify.TextContains(@"
public class C {
    public void M()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void PasteCodeWithLambdaBody()
        {
            SetUpEditor(@"
using System;
class Program
{
    static void Main()
    {
        Action a = () =>
        {
            using (null)
            {
                $$
            }
        };
    }
}");
            VisualStudio.Editor.Paste(@"        Action b = () =>
        {

            };");

            VisualStudio.Editor.Verify.TextContains(@"
using System;
class Program
{
    static void Main()
    {
        Action a = () =>
        {
            using (null)
            {
                Action b = () =>
                {

                };
            }
        };
    }
}");
            // Undo should only undo the formatting
            VisualStudio.Editor.Undo();
            VisualStudio.Editor.Verify.TextContains(@"
using System;
class Program
{
    static void Main()
    {
        Action a = () =>
        {
            using (null)
            {
                        Action b = () =>
        {

            };
            }
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void PasteCodeWithLambdaBody2()
        {
            SetUpEditor(@"
using System;
class Program
{
    static void Main()
    {
        Action a = () =>
        {
            using (null)
            {
                $$
            }
        };
    }
}");
            VisualStudio.Editor.Paste(@"        Action<int> b = n =>
        {
            Console.Writeline(n);
        };");

            VisualStudio.Editor.Verify.TextContains(@"
using System;
class Program
{
    static void Main()
    {
        Action a = () =>
        {
            using (null)
            {
                Action<int> b = n =>
                {
                    Console.Writeline(n);
                };
            }
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void PasteCodeWithLambdaBody3()
        {
            SetUpEditor(@"
using System;
class Program
{
    static void Main()
    {
        Action a = () =>
        {
            using (null)
            {
                $$
            }
        };
    }
}");
            VisualStudio.Editor.Paste(@"        D d = delegate(int x)
{
    return 2 * x;
};");

            VisualStudio.Editor.Verify.TextContains(@"
using System;
class Program
{
    static void Main()
    {
        Action a = () =>
        {
            using (null)
            {
                D d = delegate (int x)
                {
                    return 2 * x;
                };
            }
        };
    }
}");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18065"),
         Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ShiftEnterWithIntelliSenseAndBraceMatching()
        {
            SetUpEditor(@"
class Program
{
    object M(object bar)
    {
        return M$$
    }
}");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudio.Editor.SendKeys("(ba", new KeyPress(VirtualKey.Enter, ShiftState.Shift), "// comment");
            VisualStudio.Editor.Verify.TextContains(@"
class Program
{
    object M(object bar)
    {
        return M(bar);
        // comment
    }
}");
        }
    }
}
