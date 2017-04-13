// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicEditing : AbstractEditorTest
    {
        private const string TestSource = @"Imports System
Imports MathAlias = System.Math
Namespace Acme
    ''' <summary>innertext
    ''' </summary>
    Public Class Program
        $$Public Shared Sub Main(args As String())
            Console.WriteLine(""Hello World"") 'comment
        End Sub
    End Class
End Namespace";

        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicEditing(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicEditing))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void IndentUnindent()
        {
            SetUpEditor(TestSource);

            VisualStudio.ExecuteCommand("Edit.SelectAll");
            VisualStudio.ExecuteCommand("Edit.InsertTab");
            VisualStudio.Workspace.WaitForAllAsyncOperations();
            VisualStudio.Editor.Verify.TextContains(PrependLines(TestSource, "    "));
            VisualStudio.ExecuteCommand("Edit.TabLeft");
            VisualStudio.Workspace.WaitForAllAsyncOperations();
            VisualStudio.Editor.Verify.TextContains(PrependLines(TestSource, ""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void CommentUncomment()
        {
            SetUpEditor(TestSource);
            VisualStudio.ExecuteCommand("Edit.SelectAll");
            VisualStudio.ExecuteCommand("Edit.CommentSelection");
            VisualStudio.Workspace.WaitForAllAsyncOperations();
            VisualStudio.Editor.Verify.TextContains(PrependLines(TestSource, "'"));
            VisualStudio.ExecuteCommand("Edit.UncommentSelection");
            VisualStudio.Workspace.WaitForAllAsyncOperations();
            VisualStudio.Editor.Verify.TextContains(PrependLines(TestSource, ""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DocCommentGeneration()
        {
            SetUpEditor(TestSource);

            VisualStudio.ExecuteCommand("Edit.BreakLine");
            VisualStudio.ExecuteCommand("Edit.LineUp");
            VisualStudio.Editor.SendKeys("'''");
            VisualStudio.Workspace.WaitForAllAsyncOperations();
            VisualStudio.Editor.Verify.TextContains(@"        ''' <summary>
        ''' 
        ''' </summary>");

            VisualStudio.Editor.SendKeys("comment!", VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"        ''' <summary>
        ''' comment!
        ''' 
        ''' </summary>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void InsertMissingToken()
        {
            SetUpEditor(@"
Imports System.Collections.Generic
Class My$$
End Class");

            VisualStudio.ExecuteCommand("Edit.BreakLine");
            VisualStudio.Editor.SendKeys("Sub Foo", VirtualKey.Enter);
            VisualStudio.Workspace.WaitForAllAsyncOperations();
            VisualStudio.Editor.Verify.TextContains(@"Class My
    Sub Foo()

    End Sub
End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void CaseCorrection()
        {
            SetUpEditor(@"Imports System.Collections.Generic
Class My
    Sub Foo()
        $$
    End Sub
End Class]s");

            VisualStudio.Editor.SendKeys("LONG", VirtualKey.Escape, VirtualKey.Up);
            VisualStudio.Workspace.WaitForAllAsyncOperations();
            VisualStudio.Editor.Verify.TextContains(@"Long");
        }
    }
}