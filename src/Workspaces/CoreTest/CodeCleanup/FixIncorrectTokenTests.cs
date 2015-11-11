// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeCleanup.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.CodeCleanup
{
    public class FixIncorrectTokensTests
    {
        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_WithMatchingIf()
        {
            var code = @"
Module Program
    Sub Main(args As String())
        [|If args IsNot Nothing Then
            System.Console.WriteLine(args)
        endif|]
    End Sub
End Module";

            var expected = @"
Module Program
    Sub Main(args As String())
        If args IsNot Nothing Then
            System.Console.WriteLine(args)
        End If
    End Sub
End Module";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_WithMatchingIf_Directive()
        {
            var code = @"[|
#If c = 0 Then
#Endif|]";

            var expected = @"
#If c = 0 Then
#End If";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_WithoutMatchingIf()
        {
            var code = @"
Module Program
    Sub Main(args As String())
        [|EndIf|]
    End Sub
End Module";

            var expected = @"
Module Program
    Sub Main(args As String())
        End If
    End Sub
End Module";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_WithoutMatchingIf_Directive()
        {
            var code = @"[|
Class X
End Class

#Endif|]";

            var expected = @"
Class X
End Class

#End If";
            Verify(code, expected);
        }

        [Fact(Skip = "889521")]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_SameLineAsIf()
        {
            var code = @"
Module Program
    Sub Main(args As String())
        If args IsNot Nothing Then [|EndIf|]        
    End Sub
End Module";

            var expected = @"
Module Program
    Sub Main(args As String())
        If args IsNot Nothing Then
        End If
    End Sub
End Module";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_SameLineAsIf_Invalid()
        {
            var code = @"
Module Program
    Sub Main(args As String())
        If args IsNot Nothing [|EndIf|]
    End Sub
End Module";

            var expected = @"
Module Program
    Sub Main(args As String())
        If args IsNot Nothing EndIf
    End Sub
End Module";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_SameLineAsIf_Directive()
        {
            var code = @"[|
#If c = 0 Then #Endif|]";

            var expected = @"
#If c = 0 Then #Endif";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_WithLeadingTrivia()
        {
            var code = @"
Module Program
    Sub Main(args As String())
        [|If args IsNot Nothing Then
            System.Console.WriteLine(args)
' Dummy Endif
        EndIf|]
    End Sub
End Module";

            var expected = @"
Module Program
    Sub Main(args As String())
        If args IsNot Nothing Then
            System.Console.WriteLine(args)
            ' Dummy Endif
        End If
    End Sub
End Module";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_WithLeadingTrivia_Directive()
        {
            var code = @"[|
#If c = 0 Then
'#Endif
#Endif
|]";

            var expected = @"
#If c = 0 Then
'#Endif
#End If
";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_InvocationExpressionArgument()
        {
            var code = @"
Module Program
    Sub Main(args As String())
        [|If args IsNot Nothing Then
            System.Console.WriteLine(args)
        InvocationExpression EndIf|]
    End Sub
End Module";

            var expected = @"
Module Program
    Sub Main(args As String())
        If args IsNot Nothing Then
            System.Console.WriteLine(args)
            InvocationExpression EndIf
    End Sub
End Module";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_InvalidDirectiveCases()
        {
            var code = @"[|
' BadDirective cases
#If c = 0 Then
#InvocationExpression #Endif

#If c = 0 Then
InvocationExpression# #Endif

#If c = 0 Then
InvocationExpression #Endif


' Missing EndIfDirective cases
#If c = 0 Then
#InvocationExpression
#Endif

#If c = 0 Then
InvocationExpression#
#Endif

#If c = 0 Then
InvocationExpression
#Endif
|]";

            var expected = @"
' BadDirective cases
#If c = 0 Then
#InvocationExpression #Endif

#If c = 0 Then
InvocationExpression# #Endif

#If c = 0 Then
InvocationExpression #Endif


' Missing EndIfDirective cases
#If c = 0 Then
#InvocationExpression
#End If

#If c = 0 Then
InvocationExpression#
#End If

#If c = 0 Then
InvocationExpression
#End If
";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_WithTrailingTrivia()
        {
            var code = @"
Module Program
    Sub Main(args As String())
        [|If args IsNot Nothing Then
            System.Console.WriteLine(args)
        EndIf ' Dummy EndIf|]
    End Sub
End Module";

            var expected = @"
Module Program
    Sub Main(args As String())
        If args IsNot Nothing Then
            System.Console.WriteLine(args)
        End If ' Dummy EndIf
    End Sub
End Module";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_WithTrailingTrivia_Directive()
        {
            var code = @"[|
#If c = 0 Then
#Endif '#Endif
|]";

            var expected = @"
#If c = 0 Then
#End If '#Endif
";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_WithIdentifierTokenTrailingTrivia()
        {
            var code = @"
Module Program
    Sub Main(args As String())
        [|If args IsNot Nothing Then
            System.Console.WriteLine(args)
        EndIf IdentifierToken|]
    End Sub
End Module";

            var expected = @"
Module Program
    Sub Main(args As String())
        If args IsNot Nothing Then
            System.Console.WriteLine(args)
        End If IdentifierToken
    End Sub
End Module";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_InvalidDirectiveCases_02()
        {
            var code = @"[|
' BadDirective cases
#If c = 0 Then
#Endif #IdentifierToken

#If c = 0 Then
#Endif IdentifierToken#

#If c = 0 Then
#Endif IdentifierToken


' Missing EndIfDirective cases
#If c = 0 Then
#Endif
#IdentifierToken

#If c = 0 Then
#Endif
IdentifierToken#

#If c = 0 Then
#Endif
IdentifierToken
|]";

            var expected = @"
' BadDirective cases
#If c = 0 Then
#End If #IdentifierToken

#If c = 0 Then
#End If IdentifierToken#

#If c = 0 Then
#End If IdentifierToken


' Missing EndIfDirective cases
#If c = 0 Then
#End If
#IdentifierToken

#If c = 0 Then
#End If
IdentifierToken#

#If c = 0 Then
#End If
IdentifierToken
";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_WithLeadingAndTrailingTrivia()
        {
            var code = @"
Module Program
    Sub Main(args As String())
        [|If args IsNot Nothing Then
            System.Console.WriteLine(args)
' Dummy EndIf
EndIf
' Dummy EndIf|]
    End Sub
End Module";

            var expected = @"
Module Program
    Sub Main(args As String())
        If args IsNot Nothing Then
            System.Console.WriteLine(args)
            ' Dummy EndIf
        End If
        ' Dummy EndIf
    End Sub
End Module";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_WithLeadingAndTrailingTrivia_Directive()
        {
            var code = @"[|
#If c = 0 Then
'#Endif
#Endif '#Endif
|]";

            var expected = @"
#If c = 0 Then
'#Endif
#End If '#Endif
";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_WithLeadingAndTrailingInvocationExpressions()
        {
            var code = @"
Module Program
    Sub Main(args As String())
        [|If args IsNot Nothing Then
            System.Console.WriteLine(args)
IdentifierToken
EndIf
IdentifierToken|]
    End Sub
End Module";

            var expected = @"
Module Program
    Sub Main(args As String())
        If args IsNot Nothing Then
            System.Console.WriteLine(args)
            IdentifierToken
        End If
        IdentifierToken
    End Sub
End Module";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(17313, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixEndIfKeyword_WithLeadingAndTrailingInvocationExpressions_Directive()
        {
            var code = @"[|
' BadDirective cases
#If c = 0 Then
#InvalidTrivia #Endif #InvalidTrivia

#If c = 0 Then
InvalidTrivia #Endif InvalidTrivia

#If c = 0 Then
InvalidTrivia# #Endif InvalidTrivia#


' Missing EndIfDirective cases
#If c = 0 Then
#InvalidTrivia
#Endif #InvalidTrivia

#If c = 0 Then
InvalidTrivia
#Endif InvalidTrivia

#If c = 0 Then
InvalidTrivia#
#Endif InvalidTrivia#
|]";

            var expected = @"
' BadDirective cases
#If c = 0 Then
#InvalidTrivia #Endif #InvalidTrivia

#If c = 0 Then
InvalidTrivia #Endif InvalidTrivia

#If c = 0 Then
InvalidTrivia# #Endif InvalidTrivia#


' Missing EndIfDirective cases
#If c = 0 Then
#InvalidTrivia
#End If #InvalidTrivia

#If c = 0 Then
InvalidTrivia
#End If InvalidTrivia

#If c = 0 Then
InvalidTrivia#
#End If InvalidTrivia#
";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(5722, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixPrimitiveTypeKeywords_ValidCases()
        {
            var code = @"[|
Imports SystemAlias = System
Imports SystemInt16Alias = System.Short
Imports SystemUInt16Alias = System.ushort
Imports SystemInt32Alias = System.INTEGER
Imports SystemUInt32Alias = System.UInteger
Imports SystemInt64Alias = System.Long
Imports SystemUInt64Alias = System.uLong
Imports SystemDateTimeAlias = System.Date

Module Program
    Sub Main(args As String())
        Dim a1 As System.Short = 0
        Dim b1 As SystemAlias.SHORT = a1
        Dim c1 As SystemInt16Alias = b1

        Dim a2 As System.UShort = 0
        Dim b2 As SystemAlias.USHORT = a2
        Dim c2 As SystemUInt16Alias = b2

        Dim a3 As System.Integer = 0
        Dim b3 As SystemAlias.INTEGER = a3
        Dim c3 As SystemInt32Alias = b3

        Dim a4 As System.UInteger = 0
        Dim b4 As SystemAlias.UINTEGER = a4
        Dim c4 As SystemUInt32Alias = b4

        Dim a5 As System.Long = 0
        Dim b5 As SystemAlias.LONG = a5
        Dim c5 As SystemInt64Alias = b5

        Dim a6 As System.ULong = 0
        Dim b6 As SystemAlias.ULONG = 0
        Dim c6 As SystemUInt64Alias = 0

        Dim a7 As System.Date = Nothing
        Dim b7 As SystemAlias.DATE = Nothing
        Dim c7 As SystemDateTimeAlias = Nothing
    End Sub
End Module
|]";

            var expected = @"
Imports SystemAlias = System
Imports SystemInt16Alias = System.Int16
Imports SystemUInt16Alias = System.UInt16
Imports SystemInt32Alias = System.Int32
Imports SystemUInt32Alias = System.UInt32
Imports SystemInt64Alias = System.Int64
Imports SystemUInt64Alias = System.UInt64
Imports SystemDateTimeAlias = System.DateTime

Module Program
    Sub Main(args As String())
        Dim a1 As System.Int16 = 0
        Dim b1 As SystemAlias.Int16 = a1
        Dim c1 As SystemInt16Alias = b1

        Dim a2 As System.UInt16 = 0
        Dim b2 As SystemAlias.UInt16 = a2
        Dim c2 As SystemUInt16Alias = b2

        Dim a3 As System.Int32 = 0
        Dim b3 As SystemAlias.Int32 = a3
        Dim c3 As SystemInt32Alias = b3

        Dim a4 As System.UInt32 = 0
        Dim b4 As SystemAlias.UInt32 = a4
        Dim c4 As SystemUInt32Alias = b4

        Dim a5 As System.Int64 = 0
        Dim b5 As SystemAlias.Int64 = a5
        Dim c5 As SystemInt64Alias = b5

        Dim a6 As System.UInt64 = 0
        Dim b6 As SystemAlias.UInt64 = 0
        Dim c6 As SystemUInt64Alias = 0

        Dim a7 As System.DateTime = Nothing
        Dim b7 As SystemAlias.DateTime = Nothing
        Dim c7 As SystemDateTimeAlias = Nothing
    End Sub
End Module
";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(5722, "DevDiv_Projects/Roslyn")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixPrimitiveTypeKeywords_InvalidCases()
        {
            // With a user defined type named System
            // No fixups as System binds to type not a namespace.
            var code = @"
Imports SystemAlias = System
Imports SystemInt16Alias = System.Short
Imports SystemUInt16Alias = System.ushort
Imports SystemInt32Alias = System.INTEGER
Imports SystemUInt32Alias = System.UInteger
Imports SystemInt64Alias = System.Long
Imports SystemUInt64Alias = System.uLong
Imports SystemDateTimeAlias = System.Date

Class System
End Class

Module Program
    Sub Main(args As String())
        Dim a1 As System.Short = 0
        Dim b1 As SystemAlias.SHORT = a1
        Dim c1 As SystemInt16Alias = b1
        Dim d1 As System.System.Short = 0
        Dim e1 As Short = 0

        Dim a2 As System.UShort = 0
        Dim b2 As SystemAlias.USHORT = a2
        Dim c2 As SystemUInt16Alias = b2
        Dim d2 As System.System.UShort = 0
        Dim e2 As UShort = 0

        Dim a3 As System.Integer = 0
        Dim b3 As SystemAlias.INTEGER = a3
        Dim c3 As SystemInt32Alias = b3
        Dim d3 As System.System.Integer = 0
        Dim e3 As Integer = 0

        Dim a4 As System.UInteger = 0
        Dim b4 As SystemAlias.UINTEGER = a4
        Dim c4 As SystemUInt32Alias = b4
        Dim d4 As System.System.UInteger = 0
        Dim e4 As UInteger = 0

        Dim a5 As System.Long = 0
        Dim b5 As SystemAlias.LONG = a5
        Dim c5 As SystemInt64Alias = b5
        Dim d5 As System.System.Long = 0
        Dim e5 As Long = 0

        Dim a6 As System.ULong = 0
        Dim b6 As SystemAlias.ULONG = 0
        Dim c6 As SystemUInt64Alias = 0
        Dim d6 As System.System.ULong = 0
        Dim e6 As ULong = 0

        Dim a7 As System.Date = Nothing
        Dim b7 As SystemAlias.DATE = Nothing
        Dim c7 As SystemDateTimeAlias = Nothing
        Dim d7 As System.System.Date = 0
        Dim e7 As Date = 0
    End Sub
End Module
";

            Verify(@"[|" + code + @"|]", expectedResult: code);

            // No Fixes in trivia
            code = @"
Imports SystemAlias = System
'Imports SystemInt16Alias = System.Short

Module Program
    Sub Main(args As String())
        ' Dim a1 As System.Short = 0
        ' Dim b1 As SystemAlias.SHORT = a1
    End Sub
End Module
";

            Verify(@"[|" + code + @"|]", expectedResult: code);
        }

        [Fact]
        [WorkItem(606015, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixFullWidthSingleQuotes()
        {
            var code = @"[|
‘ｆｕｌｌｗｉｄｔｈ 1　
’ｆｕｌｌｗｉｄｔｈ 2
‘‘ｆｕｌｌｗｉｄｔｈ 3
’'ｆｕｌｌｗｉｄｔｈ 4
'‘ｆｕｌｌｗｉｄｔｈ 5
‘’ｆｕｌｌｗｉｄｔｈ 6
‘’‘’ｆｕｌｌｗｉｄｔｈ 7
'‘’‘’ｆｕｌｌｗｉｄｔｈ 8|]";

            var expected = @"
'ｆｕｌｌｗｉｄｔｈ 1　
'ｆｕｌｌｗｉｄｔｈ 2
'‘ｆｕｌｌｗｉｄｔｈ 3
''ｆｕｌｌｗｉｄｔｈ 4
'‘ｆｕｌｌｗｉｄｔｈ 5
'’ｆｕｌｌｗｉｄｔｈ 6
'’‘’ｆｕｌｌｗｉｄｔｈ 7
'‘’‘’ｆｕｌｌｗｉｄｔｈ 8";
            Verify(code, expected);
        }

        [Fact]
        [WorkItem(707135, "DevDiv")]
        [Trait(Traits.Feature, Traits.Features.FixIncorrectTokens)]
        public void FixFullWidthSingleQuotes2()
        {
            var savedCulture = System.Threading.Thread.CurrentThread.CurrentCulture;

            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture =
                    System.Globalization.CultureInfo.CreateSpecificCulture("zh-CN");

                var code = @"[|‘’ｆｕｌｌｗｉｄｔｈ 1|]";

                var expected = @"'’ｆｕｌｌｗｉｄｔｈ 1";
                Verify(code, expected);
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = savedCulture;
            }
        }

        private static string FixLineEndings(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }

        private static void Verify(string codeWithMarker, string expectedResult)
        {
            codeWithMarker = FixLineEndings(codeWithMarker);
            expectedResult = FixLineEndings(expectedResult);

            var codeWithoutMarker = default(string);
            var textSpans = (IList<TextSpan>)new List<TextSpan>();
            MarkupTestFile.GetSpans(codeWithMarker, out codeWithoutMarker, out textSpans);

            var document = CreateDocument(codeWithoutMarker, LanguageNames.VisualBasic);
            var codeCleanups = CodeCleaner.GetDefaultProviders(document).Where(p => p.Name == PredefinedCodeCleanupProviderNames.FixIncorrectTokens || p.Name == PredefinedCodeCleanupProviderNames.Format);

            var cleanDocument = CodeCleaner.CleanupAsync(document, textSpans[0], codeCleanups).Result;

            Assert.Equal(expectedResult, cleanDocument.GetSyntaxRootAsync().Result.ToFullString());
        }

        private static Document CreateDocument(string code, string language)
        {
            var solution = new AdhocWorkspace().CurrentSolution;
            var projectId = ProjectId.CreateNewId();
            var project = solution.AddProject(projectId, "Project", "Project.dll", language).GetProject(projectId);

            return project.AddMetadataReference(TestReferences.NetFx.v4_0_30319.mscorlib)
                          .AddDocument("Document", SourceText.From(code));
        }
    }
}