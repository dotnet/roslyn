// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.MetadataAsSource
{
    public partial class MetadataAsSourceTests
    {
        [UseExportProvider]
        public class VisualBasic
        {
            [Fact, WorkItem(530123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530123"), Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
            public async Task TestGenerateTypeInModule()
            {
                var metadataSource = @"
Module M
    Public Class D
    End Class
End Module";
                await GenerateAndVerifySourceAsync(metadataSource, "M+D", LanguageNames.VisualBasic, $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Friend Module M
    Public Class [|D|]
        Public Sub New()
    End Class
End Module");
            }

            // This test depends on the version of mscorlib used by the TestWorkspace and may 
            // change in the future
            [WorkItem(530526, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530526")]
            [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
            public async Task BracketedIdentifierSimplificationTest()
            {
                var expected = $@"#Region ""{FeaturesResources.Assembly} mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089""
' mscorlib.v4_6_1038_0.dll
#End Region

Imports System.Runtime.InteropServices

Namespace System
    <__DynamicallyInvokableAttribute> <AttributeUsage(AttributeTargets.Class Or AttributeTargets.Struct Or AttributeTargets.Enum Or AttributeTargets.Constructor Or AttributeTargets.Method Or AttributeTargets.Property Or AttributeTargets.Field Or AttributeTargets.Event Or AttributeTargets.Interface Or AttributeTargets.Delegate, Inherited:=False)> <ComVisible(True)>
    Public NotInheritable Class [|ObsoleteAttribute|]
        Inherits Attribute

        <__DynamicallyInvokableAttribute>
        Public Sub New()
        <__DynamicallyInvokableAttribute>
        Public Sub New(message As String)
        <__DynamicallyInvokableAttribute>
        Public Sub New(message As String, [error] As Boolean)

        <__DynamicallyInvokableAttribute>
        Public ReadOnly Property Message As String
        <__DynamicallyInvokableAttribute>
        Public ReadOnly Property IsError As Boolean
    End Class
End Namespace";

                using var context = TestContext.Create(LanguageNames.VisualBasic);
                await context.GenerateAndVerifySourceAsync("System.ObsoleteAttribute", expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
            public void ExtractXMLFromDocComment()
            {
                var docCommentText = @"''' <summary>
''' I am the very model of a modern major general.
''' </summary>";

                var expectedXMLFragment = @" <summary>
 I am the very model of a modern major general.
 </summary>";

                var extractedXMLFragment = DocumentationCommentUtilities.ExtractXMLFragment(docCommentText, "'''");

                Assert.Equal(expectedXMLFragment, extractedXMLFragment);
            }
        }
    }
}
