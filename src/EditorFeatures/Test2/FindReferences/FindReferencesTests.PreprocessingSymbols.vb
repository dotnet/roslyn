
' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Remote.Testing

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Partial Public Class FindReferencesTests
        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/66009")>
        Public Async Function TestPreprocessingSymbolBasic(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
#define [|PREPROCESSING_SYMBOL|]
#define MORE_PREPROCESSING_SYMBOL

#if [|PREPROCESSING_SYMBOL|]
namespace SimpleNamespace;
#elif true &amp;&amp; (!false || [|PRE$$PROCESSING_SYMBOL|])
namespace AnotherNamespace;
#elif MORE_PREPROCESSING_SYMBOL
namespace MoreSimpleNamespace;
#else
namespace ComplexNamespace;
#endif

// PREPROCESSING_SYMBOL
class PREPROCESSING_SYMBOL
{
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/66009")>
        Public Async Function TestNotPreprocessingSymbol(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
#define PREPROCESSING_SYMBOL
#define MORE_PREPROCESSING_SYMBOL

#if PREPROCESSING_SYMBOL
namespace SimpleNamespace;
#elif true &amp;&amp; (!false || PREPROCESSING_SYMBOL)
namespace AnotherNamespace;
#elif MORE_PREPROCESSING_SYMBOL
namespace MoreSimpleNamespace;
#else
namespace ComplexNamespace;
#endif

// PREPROCESSING_SYMBOL
class {|Definition:PREPROCES$$SING_SYMBOL|}
{
}
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/66009")>
        Public Async Function TestPreprocessingSymbolRegion(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
#define PREPROCESSING_SYMBOL

class Class
{
    #region PREP$$ROCESSING_SYMBOL
    public void Method() { }
    #endregion PREPROCESSING_SYMBOL
}

#if NOT_PREPROCESSING_SYMBOL
#elif PREPROCESSING_SYMBOL
#endif
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/66009")>
        Public Async Function TestPreprocessingSymbolPragmaWarning(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
#define PREPROCESSING_SYMBOL

class Class
{
    #pragma warning disable PREPROCESSING_SYMBOL
    public void Method() { }
    #pragma warning restore PREPROC$$ESSING_SYMBOL
}

#if NOT_PREPROCESSING_SYMBOL
#elif PREPROCESSING_SYMBOL
#endif
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/66009")>
        Public Async Function TestPreprocessingSymbolMultipleDocuments(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
#define [|PREPROCESSING_SYMBOL|]
#undef [|PREPROCESSING_SYMBOL|]
#undef [|PREPROCESSING_SYMBOL|]
#define [|PREPROCESSING_SYMBOL|]

class Class
{
    #region PREPROCESSING_SYMBOL
    public void Method() { }
    #endregion PREPROCESSING_SYMBOL
}

#if NOT_PREPROCESSING_SYMBOL
#elif [|PREPROC$$ESSING_SYMBOL|]
#endif
        </Document>
        <Document>
#if ![|PREPROCESSING_SYMBOL|]
#define [|PREPROCESSING_SYMBOL|]
#elif [|PREPROCESSING_SYMBOL|]
#undef [|PREPROCESSING_SYMBOL|]
#endif
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/66009")>
        Public Async Function TestPreprocessingSymbolMultipleProjects1(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
#if NOT_PREPROCESSING_SYMBOL
#elif [|PREPROCESSING_SYMBO$$L|]
#endif
        </Document>
        <Document>
#if ![|PREPROCESSING_SYMBOL|]
#define [|PREPROCESSING_SYMBOL|]
#elif [|PREPROCESSING_SYMBOL|]
#undef [|PREPROCESSING_SYMBOL|]
#endif
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true">
        <Document>
#define [|PREPROCESSING_SYMBOL|]

#if [|PREPROCESSING_SYMBOL|]
#undef [|PREPROCESSING_SYMBOL|]
#endif
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/66009")>
        Public Async Function TestPreprocessingSymbolMultipleProjects2HoverCSharp(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
#define [|PREPROCESSING_SYMBOL|]

#if [|PREPROCESSING_SYM$$BOL|]
#undef [|PREPROCESSING_SYMBOL|]
#endif
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
#Const [|PREPROCESSING_SYMBOL|] = True
        </Document>
        <Document>
#If DEBUG Then
' Some code
#ElseIf NOT_PREPROCESSING_SYMBOL = [|PREPROCESSING_SYMBOL|] Then
#End If
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/66009")>
        Public Async Function TestPreprocessingSymbolMultipleProjects2HoverVB(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
#define [|PREPROCESSING_SYMBOL|]

#if [|PREPROCESSING_SYMBOL|]
#undef [|PREPROCESSING_SYMBOL|]
#endif
        </Document>
    </Project>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
#Const [|PREPROCESSING_SYMBOL|] = True
        </Document>
        <Document>
#If DEBUG Then
' Some code
#ElseIf NOT_PREPROCESSING_SYMBOL = [|$$PREPROCESSING_SYMBOL|] Then
#End If
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/66009")>
        Public Async Function TestPreprocessingSymbolUsedInSourceGeneratedDocument(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
#if ![|PREPROCESSING_SYMBOL|]
#define [|PREPROCESSING$$_SYMBOL|]
#elif [|PREPROCESSING_SYMBOL|]
#undef [|PREPROCESSING_SYMBOL|]
#endif
        </Document>
        <DocumentFromSourceGenerator>
#if ![|PREPROCESSING_SYMBOL|]
#define [|PREPROCESSING_SYMBOL|]
#elif [|PREPROCESSING_SYMBOL|]
#undef [|PREPROCESSING_SYMBOL|]
#endif
        </DocumentFromSourceGenerator>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/66009")>
        Public Async Function TestPreprocessingSymbolHoverDefine(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
#define [|PREPROC$$ESSING_SYMBOL|]
#undef [|PREPROCESSING_SYMBOL|]
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/66009")>
        Public Async Function TestPreprocessingSymbolHoverUndef(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
#define [|PREPROCESSING_SYMBOL|]
#undef [|PRE$$PROCESSING_SYMBOL|]
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/66009")>
        Public Async Function TestPreprocessingSymbolHoverConst(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
#Const [|PREPROCESS$$ING_SYMBOL|] = True

#If [|PREPROCESSING_SYMBOL|] Then
' Some code
#End If
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/66009")>
        Public Async Function TestPreprocessingSymbolHoverAssignedConst(kind As TestKind, host As TestHost) As Task
            Dim input =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true">
        <Document>
#Const [|PREPROCESSING_SYMBOL|] = True
#Const OTHER = [|PREPROCES$$SING_SYMBOL|]

#If [|PREPROCESSING_SYMBOL|] Then
' Some code
#End If
        </Document>
    </Project>
</Workspace>
            Await TestAPIAndFeature(input, kind, host)
        End Function
    End Class
End Namespace
