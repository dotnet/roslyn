// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpCompleteStatement : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpCompleteStatement(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpCompleteStatement))
        {
        }

        [WpfFact]
        public void UndoRestoresCaretPosition1()
        {
            SetUpEditor(@"
public class Test
{
    private object f;

    public void Method()
    {
        f.ToString($$)
    }
}
");

            VisualStudio.Editor.SendKeys(';');
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString();$$", assertCaretPosition: true);

            VisualStudio.Editor.Undo();
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString($$)", assertCaretPosition: true);
        }

        [WpfFact]
        [WorkItem(43400, "https://github.com/dotnet/roslyn/issues/43400")]
        public void UndoRestoresCaretPosition2()
        {
            SetUpEditor(@"
public class Test
{
    private object f;

    public void Method()
    {
        Method(condition ? whenTrue $$)
    }
}
");

            VisualStudio.Editor.SendKeys(';');
            VisualStudio.Editor.Verify.CurrentLineText("Method(condition ? whenTrue );$$", assertCaretPosition: true);

            VisualStudio.Editor.Undo();
            VisualStudio.Editor.Verify.CurrentLineText("Method(condition ? whenTrue $$)", assertCaretPosition: true);
        }

        [WpfFact]
        public void UndoRestoresFormatBeforeRestoringCaretPosition()
        {
            SetUpEditor(@"
public class Test
{
    private object f;

    public void Method()
    {
        f.ToString($$ )
    }
}
");

            VisualStudio.Editor.SendKeys(';');
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString();$$", assertCaretPosition: true);

            VisualStudio.Editor.Undo();
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString( );$$", assertCaretPosition: true);

            VisualStudio.Editor.Undo();
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString($$ )", assertCaretPosition: true);
        }
    }
}
