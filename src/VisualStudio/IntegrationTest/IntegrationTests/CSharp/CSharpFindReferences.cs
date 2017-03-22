// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpFindReferences : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpFindReferences(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpFindReferences))
        {
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/17634"),
         Trait(Traits.Feature, Traits.Features.FindReferences)]
        public void FindReferencesToCtor()
        {
            SetUpEditor(@"
class Program
{
}$$
");

            VisualStudio.Instance.SolutionExplorer.AddFile(ProjectName, "File2.cs");
            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "File2.cs");

            SetUpEditor(@"
class SomeOtherClass
{
    void M()
    {
        Program p = new Progr$$am();
    }
}
");

            SendKeys(Shift(VirtualKey.F12));

            const string programReferencesCaption = "'Program' references";
            var results = VisualStudio.Instance.FindReferencesWindow.GetContents(programReferencesCaption);

            var activeWindowCaption = VisualStudio.Instance.Shell.GetActiveWindowCaption();
            Assert.Equal(expected: programReferencesCaption, actual: activeWindowCaption);

            Assert.Collection(
                results,
                new Action<Reference>[]
                {
                    reference =>
                    {
                        Assert.Equal(expected: "class Program", actual: reference.Code);
                        Assert.Equal(expected: 1, actual: reference.Line);
                        Assert.Equal(expected: 6, actual: reference.Column);
                    },
                    reference =>
                    {
                        Assert.Equal(expected: "Program p = new Program();", actual: reference.Code);
                        Assert.Equal(expected: 5, actual: reference.Line);
                        Assert.Equal(expected: 24, actual: reference.Column);
                    }
                });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public void FindReferencesToLocals()
        {
            SetUpEditor(@"
class Program
{
    static void Main()
    {
        int local = 1;
        Console.WriteLine(local$$);
    }
}
");

            SendKeys(Shift(VirtualKey.F12));

            const string localReferencesCaption = "'local' references";
            var results = VisualStudio.Instance.FindReferencesWindow.GetContents(localReferencesCaption);

            var activeWindowCaption = VisualStudio.Instance.Shell.GetActiveWindowCaption();
            Assert.Equal(expected: localReferencesCaption, actual: activeWindowCaption);

            Assert.Collection(
                results,
                new Action<Reference>[]
                {
                    reference =>
                    {
                        Assert.Equal(expected: "int local = 1;", actual: reference.Code);
                        Assert.Equal(expected: 5, actual: reference.Line);
                        Assert.Equal(expected: 12, actual: reference.Column);
                    },
                    reference =>
                    {
                        Assert.Equal(expected: "Console.WriteLine(local);", actual: reference.Code);
                        Assert.Equal(expected: 6, actual: reference.Line);
                        Assert.Equal(expected: 26, actual: reference.Column);
                    }
                });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.FindReferences)]
        public void FindReferencesToString()
        {
            SetUpEditor(@"
class Program
{
    static void Main()
    {
         string local = ""1""$$;
    }
}
");

            SendKeys(Shift(VirtualKey.F12));

            const string findReferencesCaption = "'\"1\"' references";
            var results = VisualStudio.Instance.FindReferencesWindow.GetContents(findReferencesCaption);

            var activeWindowCaption = VisualStudio.Instance.Shell.GetActiveWindowCaption();
            Assert.Equal(expected: findReferencesCaption, actual: activeWindowCaption);

            Assert.Collection(
                results,
                new Action<Reference>[]
                {
                    reference =>
                    {
                        Assert.Equal(expected: "string local = \"1\";", actual: reference.Code);
                        Assert.Equal(expected: 5, actual: reference.Line);
                        Assert.Equal(expected: 24, actual: reference.Column);
                    }
                });
        }
    }
}
