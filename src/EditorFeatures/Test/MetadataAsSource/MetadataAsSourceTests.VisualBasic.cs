// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    <AttributeUsage(AttributeTargets.Class Or AttributeTargets.Struct Or AttributeTargets.Enum Or AttributeTargets.Constructor Or AttributeTargets.Method Or AttributeTargets.Property Or AttributeTargets.Field Or AttributeTargets.Event Or AttributeTargets.Interface Or AttributeTargets.Delegate, Inherited:=False)> <ComVisible(True)>
    Public NotInheritable Class [|ObsoleteAttribute|]
        Inherits Attribute

        Public Sub New()
        Public Sub New(message As String)
        Public Sub New(message As String, [error] As Boolean)

        Public ReadOnly Property Message As String
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

            [Fact, WorkItem(26605, "https://github.com/dotnet/roslyn/issues/26605")]
            public async Task TestValueTuple()
            {
                using var context = TestContext.Create(LanguageNames.VisualBasic);
                await context.GenerateAndVerifySourceAsync("System.ValueTuple",
@$"#Region ""{FeaturesResources.Assembly} System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51""
' System.ValueTuple.dll
#End Region

Imports System.Collections

Namespace System
    Public Structure [|ValueTuple|]
        Implements IEquatable(Of ValueTuple), IStructuralEquatable, IStructuralComparable, IComparable, IComparable(Of ValueTuple), ITupleInternal

        Public Shared Function Create() As ValueTuple
        Public Shared Function Create(Of T1)(item1 As T1) As ValueTuple(Of T1)
        Public Shared Function Create(Of T1, T2)(item1 As T1, item2 As T2) As (T1, T2)
        Public Shared Function Create(Of T1, T2, T3)(item1 As T1, item2 As T2, item3 As T3) As (T1, T2, T3)
        Public Shared Function Create(Of T1, T2, T3, T4)(item1 As T1, item2 As T2, item3 As T3, item4 As T4) As (T1, T2, T3, T4)
        Public Shared Function Create(Of T1, T2, T3, T4, T5)(item1 As T1, item2 As T2, item3 As T3, item4 As T4, item5 As T5) As (T1, T2, T3, T4, T5)
        Public Shared Function Create(Of T1, T2, T3, T4, T5, T6)(item1 As T1, item2 As T2, item3 As T3, item4 As T4, item5 As T5, item6 As T6) As (T1, T2, T3, T4, T5, T6)
        Public Shared Function Create(Of T1, T2, T3, T4, T5, T6, T7)(item1 As T1, item2 As T2, item3 As T3, item4 As T4, item5 As T5, item6 As T6, item7 As T7) As (T1, T2, T3, T4, T5, T6, T7)
        Public Shared Function Create(Of T1, T2, T3, T4, T5, T6, T7, T8)(item1 As T1, item2 As T2, item3 As T3, item4 As T4, item5 As T5, item6 As T6, item7 As T7, item8 As T8) As (T1, T2, T3, T4, T5, T6, T7, T8)
        Public Overrides Function Equals(obj As Object) As Boolean
        Public Function Equals(other As ValueTuple) As Boolean
        Public Function CompareTo(other As ValueTuple) As Integer
        Public Overrides Function GetHashCode() As Integer
        Public Overrides Function ToString() As String
    End Structure
End Namespace");
            }
        }
    }
}
