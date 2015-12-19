' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.GeneratedCodeRecognition
Imports Microsoft.CodeAnalysis.GenerateType
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Notification
Imports Microsoft.CodeAnalysis.ProjectManagement
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.VisualStudio.LanguageServices.Implementation.GenerateType
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.GenerateType
    Public Class GenerateTypeViewModelTests
        Private Shared s_assembly1_Name As String = "Assembly1"
        Private Shared s_test1_Name As String = "Test1"
        Private Shared s_submit_failed_unexpectedly As String = "Submit failed unexpectedly."
        Private Shared s_submit_passed_unexpectedly As String = "Submit passed unexpectedly. Submit should fail here"

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeExistingFileCSharp() As Task
            Dim documentContentMarkup = <Text><![CDATA[
class Program
{
    static void Main(string[] args)
    {
        A.B.Foo$$ bar;
    }
}

namespace A
{
    namespace B
    {

    }
}"]]></Text>
            Dim viewModel = Await GetViewModelAsync(documentContentMarkup, "C#")

            ' Test the default values
            Assert.Equal(0, viewModel.AccessSelectIndex)
            Assert.Equal(0, viewModel.KindSelectIndex)
            Assert.Equal("Foo", viewModel.TypeName)

            Assert.Equal("Foo.cs", viewModel.FileName)

            Assert.Equal(s_assembly1_Name, viewModel.SelectedProject.Name)
            Assert.Equal(s_test1_Name + ".cs", viewModel.SelectedDocument.Name)

            Assert.Equal(True, viewModel.IsExistingFile)

            ' Set the Radio to new file
            viewModel.IsNewFile = True
            Assert.Equal(True, viewModel.IsNewFile)
            Assert.Equal(False, viewModel.IsExistingFile)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeExistingFileVisualBasic() As Task
            Dim documentContentMarkup = <Text><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x As A.B.Foo$$ = Nothing
    End Sub
End Module

Namespace A
    Namespace B
    End Namespace
End Namespace"]]></Text>
            Dim viewModel = Await GetViewModelAsync(documentContentMarkup, "Visual Basic")

            ' Test the default values
            Assert.Equal(0, viewModel.AccessSelectIndex)
            Assert.Equal(0, viewModel.KindSelectIndex)
            Assert.Equal("Foo", viewModel.TypeName)

            Assert.Equal("Foo.vb", viewModel.FileName)

            Assert.Equal(s_assembly1_Name, viewModel.SelectedProject.Name)
            Assert.Equal(s_test1_Name + ".vb", viewModel.SelectedDocument.Name)

            Assert.Equal(True, viewModel.IsExistingFile)

            ' Set the Radio to new file
            viewModel.IsNewFile = True
            Assert.Equal(True, viewModel.IsNewFile)
            Assert.Equal(False, viewModel.IsExistingFile)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeNewFileBothLanguage() As Task
            Dim documentContentMarkup = <Text><![CDATA[
class Program
{
    static void Main(string[] args)
    {
        A.B.Foo$$ bar;
    }
}

namespace A
{
    namespace B
    {

    }
}"]]></Text>
            Dim viewModel = Await GetViewModelAsync(documentContentMarkup, "C#", projectRootFilePath:="C:\OuterFolder\InnerFolder\")

            viewModel.IsNewFile = True

            ' Feed a filename and check if the change is effective
            viewModel.FileName = "Wow"

            viewModel.UpdateFileNameExtension()
            Assert.True(viewModel.TrySubmit(), s_submit_failed_unexpectedly)
            Assert.Equal("Wow.cs", viewModel.FileName)

            viewModel.FileName = "Foo\Bar\Woow"

            viewModel.UpdateFileNameExtension()
            Assert.True(viewModel.TrySubmit(), s_submit_failed_unexpectedly)
            Assert.Equal("Woow.cs", viewModel.FileName)
            Assert.Equal(2, viewModel.Folders.Count)
            Assert.Equal("Foo", viewModel.Folders(0))
            Assert.Equal("Bar", viewModel.Folders(1))

            viewModel.FileName = "\    name has space \  Foo      \Bar\      Woow"

            viewModel.UpdateFileNameExtension()
            Assert.True(viewModel.TrySubmit(), s_submit_failed_unexpectedly)
            Assert.Equal("Woow.cs", viewModel.FileName)
            Assert.Equal(3, viewModel.Folders.Count)
            Assert.Equal("name has space", viewModel.Folders(0))
            Assert.Equal("Foo", viewModel.Folders(1))
            Assert.Equal("Bar", viewModel.Folders(2))

            ' Set it to invalid identifier
            viewModel.FileName = "w?d"
            viewModel.UpdateFileNameExtension()
            Assert.False(viewModel.TrySubmit(), s_submit_passed_unexpectedly)

            viewModel.FileName = "wow\w?d"
            viewModel.UpdateFileNameExtension()
            Assert.False(viewModel.TrySubmit(), s_submit_passed_unexpectedly)

            viewModel.FileName = "w?d\wdd"
            viewModel.UpdateFileNameExtension()
            Assert.False(viewModel.TrySubmit(), s_submit_passed_unexpectedly)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeProjectChangeAndDependencyBothLanguage() As Task
            Dim workspaceXml = <Workspace>
                                   <Project Language="C#" AssemblyName="CS1" CommonReferences="true">
                                       <Document FilePath="Test1.cs">
class Program
{
    static void Main(string[] args)
    {
        A.B.Foo$$ bar;
    }
}

namespace A
{
    namespace B
    {

    }
}
                                       </Document>
                                       <Document FilePath="Test4.cs"></Document>
                                   </Project>
                                   <Project Language="C#" AssemblyName="CS2" CommonReferences="true">
                                       <ProjectReference>CS1</ProjectReference>
                                   </Project>
                                   <Project Language="C#" AssemblyName="CS3" CommonReferences="true">
                                       <ProjectReference>CS2</ProjectReference>
                                   </Project>
                                   <Project Language="Visual Basic" AssemblyName="VB1" CommonReferences="true">
                                       <Document FilePath="Test2.vb"></Document>
                                       <Document FilePath="Test3.vb"></Document>
                                   </Project>
                               </Workspace>

            Dim viewModel = Await GetViewModelAsync(workspaceXml, "")

            ' Only 2 Projects can be selected because CS2 and CS3 will introduce cyclic dependency
            Assert.Equal(2, viewModel.ProjectList.Count)
            Assert.Equal(2, viewModel.DocumentList.Count)

            viewModel.DocumentSelectIndex = 1

            Dim projectToSelect = viewModel.ProjectList.Where(Function(p) p.Name = "VB1").Single().Project

            Dim monitor = New PropertyChangedTestMonitor(viewModel)
            monitor.AddExpectation(Function() viewModel.DocumentList)

            ' Check to see if the values are reset when there is a change in the project selection
            viewModel.SelectedProject = projectToSelect
            Assert.Equal(2, viewModel.DocumentList.Count())
            Assert.Equal(0, viewModel.DocumentSelectIndex)
            Assert.Equal(1, viewModel.ProjectSelectIndex)

            monitor.VerifyExpectations()
            monitor.Detach()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeDisableExistingFileForEmptyProject() As Task
            Dim workspaceXml = <Workspace>
                                   <Project Language="C#" AssemblyName="CS1" CommonReferences="true">
                                       <Document FilePath="Test1.cs">
class Program
{
    static void Main(string[] args)
    {
        A.B.Foo$$ bar;
    }
}

namespace A
{
    namespace B
    {

    }
}
                                       </Document>
                                       <Document FilePath="Test4.cs"></Document>
                                   </Project>
                                   <Project Language="C#" AssemblyName="CS2" CommonReferences="true"/>
                               </Workspace>

            Dim viewModel = Await GetViewModelAsync(workspaceXml, "")

            ' Select the project CS2 which has no documents.
            Dim projectToSelect = viewModel.ProjectList.Where(Function(p) p.Name = "CS2").Single().Project
            viewModel.SelectedProject = projectToSelect


            ' Check if the option for Existing File is disabled
            Assert.Equal(0, viewModel.DocumentList.Count())
            Assert.Equal(False, viewModel.IsExistingFileEnabled)

            ' Select the project CS1 which has documents
            projectToSelect = viewModel.ProjectList.Where(Function(p) p.Name = "CS1").Single().Project
            viewModel.SelectedProject = projectToSelect

            ' Check if the option for Existing File is enabled
            Assert.Equal(2, viewModel.DocumentList.Count())
            Assert.Equal(True, viewModel.IsExistingFileEnabled)
        End Function

        <WorkItem(858815)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeAllowPublicAccessOnlyForGenerationIntoOtherProject() As Task
            Dim workspaceXml = <Workspace>
                                   <Project Language="C#" AssemblyName="CS1" CommonReferences="true">
                                       <Document FilePath="Test1.cs">
class Program
{
    static void Main(string[] args)
    {
        A.B.Foo$$ bar;
    }
}

namespace A
{
    namespace B
    {

    }
}
                                       </Document>
                                       <Document FilePath="Test4.cs"></Document>
                                   </Project>
                                   <Project Language="C#" AssemblyName="CS2" CommonReferences="true"/>
                               </Workspace>

            Dim viewModel = Await GetViewModelAsync(workspaceXml, "")

            viewModel.SelectedAccessibilityString = "Default"

            ' Check if the AccessKind List is enabled
            Assert.Equal(True, viewModel.IsAccessListEnabled)

            ' Select the project CS2 which has no documents.
            Dim projectToSelect = viewModel.ProjectList.Where(Function(p) p.Name = "CS2").Single().Project
            viewModel.SelectedProject = projectToSelect

            ' Check if access kind is set to Public and the AccessKind is set to be disabled
            Assert.Equal(2, viewModel.AccessSelectIndex)
            Assert.Equal(False, viewModel.IsAccessListEnabled)

            ' Switch back to the initial document
            projectToSelect = viewModel.ProjectList.Where(Function(p) p.Name = "CS1").Single().Project
            viewModel.SelectedProject = projectToSelect

            ' Check if AccessKind list is enabled again
            Assert.Equal(True, viewModel.IsAccessListEnabled)
        End Function

        <WorkItem(858815)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeAllowClassTypeKindForAttribute_CSharp() As Task
            Dim documentContentMarkup = <Text><![CDATA[
[Foo$$]
class Program
{
    static void Main(string[] args)
    {
    }
}]]></Text>
            Dim viewModel = Await GetViewModelAsync(documentContentMarkup, LanguageNames.CSharp, typeKindvalue:=TypeKindOptions.Attribute, isAttribute:=True)

            ' Check if only class is present
            Assert.Equal(1, viewModel.KindList.Count)
            Assert.Equal("class", viewModel.KindList(0))

            Assert.Equal("FooAttribute", viewModel.TypeName)
        End Function

        <WorkItem(858815)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeAllowClassTypeKindForAttribute_VisualBasic() As Task
            Dim documentContentMarkup = <Text><![CDATA[
<Blah$$>
Class C
End Class]]></Text>
            Dim viewModel = Await GetViewModelAsync(documentContentMarkup, LanguageNames.VisualBasic, typeKindvalue:=TypeKindOptions.Attribute, isAttribute:=True)

            ' Check if only class is present
            Assert.Equal(1, viewModel.KindList.Count)
            Assert.Equal("Class", viewModel.KindList(0))

            Assert.Equal("BlahAttribute", viewModel.TypeName)
        End Function

        <WorkItem(861544)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeWithCapsAttribute_VisualBasic() As Task
            Dim documentContentMarkup = <Text><![CDATA[
<FooAttribute$$>
Public class CCC
End class]]></Text>
            Dim viewModel = Await GetViewModelAsync(documentContentMarkup, LanguageNames.VisualBasic, typeKindvalue:=TypeKindOptions.Class, isPublicOnlyAccessibility:=False, isAttribute:=True)

            Assert.Equal("FooAttribute", viewModel.TypeName)
        End Function

        <WorkItem(861544)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeWithoutCapsAttribute_VisualBasic() As Task
            Dim documentContentMarkup = <Text><![CDATA[
<Fooattribute$$>
Public class CCC
End class]]></Text>
            Dim viewModel = Await GetViewModelAsync(documentContentMarkup, LanguageNames.VisualBasic, typeKindvalue:=TypeKindOptions.Class, isPublicOnlyAccessibility:=False, isAttribute:=True)

            Assert.Equal("FooattributeAttribute", viewModel.TypeName)
        End Function

        <WorkItem(861544)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeWithCapsAttribute_CSharp() As Task
            Dim documentContentMarkup = <Text><![CDATA[
[FooAttribute$$]
public class CCC
{
}]]></Text>
            Dim viewModel = Await GetViewModelAsync(documentContentMarkup, LanguageNames.CSharp, typeKindvalue:=TypeKindOptions.Class, isPublicOnlyAccessibility:=False, isAttribute:=True)

            Assert.Equal("FooAttribute", viewModel.TypeName)
        End Function

        <WorkItem(861544)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeWithoutCapsAttribute_CSharp() As Task
            Dim documentContentMarkup = <Text><![CDATA[
[Fooattribute$$]
public class CCC
{
}]]></Text>
            Dim viewModel = Await GetViewModelAsync(documentContentMarkup, LanguageNames.CSharp, typeKindvalue:=TypeKindOptions.Class, isPublicOnlyAccessibility:=False, isAttribute:=True)

            Assert.Equal("FooattributeAttribute", viewModel.TypeName)
        End Function


        <WorkItem(861462)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeCheckOnlyPublic_CSharp_1() As Task
            Dim documentContentMarkup = <Text><![CDATA[
public class C : $$D
{
}]]></Text>
            Dim viewModel = Await GetViewModelAsync(documentContentMarkup, LanguageNames.CSharp, typeKindvalue:=TypeKindOptions.BaseList)

            ' Check if interface, class is present
            Assert.Equal(2, viewModel.KindList.Count)
            Assert.Equal("class", viewModel.KindList(0))
            Assert.Equal("interface", viewModel.KindList(1))

            ' Check if all Accessibility are present
            Assert.Equal(3, viewModel.AccessList.Count)
        End Function

        <WorkItem(861462)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeCheckOnlyPublic_CSharp_2() As Task
            Dim documentContentMarkup = <Text><![CDATA[
public interface CCC : $$DDD
{
}]]></Text>
            Dim viewModel = Await GetViewModelAsync(documentContentMarkup, LanguageNames.CSharp, typeKindvalue:=TypeKindOptions.Interface, isPublicOnlyAccessibility:=True)

            ' Check if interface, class is present
            Assert.Equal(1, viewModel.KindList.Count)
            Assert.Equal("interface", viewModel.KindList(0))

            Assert.Equal(1, viewModel.AccessList.Count)
            Assert.Equal("public", viewModel.AccessList(0))
        End Function

        <WorkItem(861462)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeCheckOnlyPublic_VisualBasic_1() As Task
            Dim documentContentMarkup = <Text><![CDATA[
Public Class C
    Implements $$D
End Class]]></Text>
            Dim viewModel = Await GetViewModelAsync(documentContentMarkup, LanguageNames.VisualBasic, typeKindvalue:=TypeKindOptions.Interface, isPublicOnlyAccessibility:=False)

            ' Check if only Interface is present
            Assert.Equal(1, viewModel.KindList.Count)
            Assert.Equal("Interface", viewModel.KindList(0))

            Assert.Equal(3, viewModel.AccessList.Count)
        End Function

        <WorkItem(861462)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeCheckOnlyPublic_VisualBasic_2() As Task
            Dim documentContentMarkup = <Text><![CDATA[
Public Class CC
    Inherits $$DD
End Class]]></Text>
            Dim viewModel = Await GetViewModelAsync(documentContentMarkup, LanguageNames.VisualBasic, typeKindvalue:=TypeKindOptions.Class, isPublicOnlyAccessibility:=True)

            ' Check if only class is present
            Assert.Equal(1, viewModel.KindList.Count)
            Assert.Equal("Class", viewModel.KindList(0))

            ' Check if only Public is present
            Assert.Equal(1, viewModel.AccessList.Count)
            Assert.Equal("Public", viewModel.AccessList(0))
        End Function

        <WorkItem(861462)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeCheckOnlyPublic_VisualBasic_3() As Task
            Dim documentContentMarkup = <Text><![CDATA[
Public Interface CCC
    Inherits $$DDD
End Interface]]></Text>
            Dim viewModel = Await GetViewModelAsync(documentContentMarkup, LanguageNames.VisualBasic, typeKindvalue:=TypeKindOptions.Interface, isPublicOnlyAccessibility:=True)

            ' Check if only class is present
            Assert.Equal(1, viewModel.KindList.Count)
            Assert.Equal("Interface", viewModel.KindList(0))

            ' Check if only Public is present
            Assert.Equal(1, viewModel.AccessList.Count)
            Assert.Equal("Public", viewModel.AccessList(0))
        End Function

        <WorkItem(861362)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeWithModuleOption() As Task
            Dim workspaceXml = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="VB1" CommonReferences="true">
                                       <Document FilePath="Test1.vb">
Module Program
    Sub Main(args As String())
        Dim s as A.$$B.C
    End Sub
End Module

Namespace A
End Namespace                                       </Document>
                                   </Project>
                                   <Project Language="C#" AssemblyName="CS1" CommonReferences="true"/>
                               </Workspace>

            Dim viewModel = Await GetViewModelAsync(workspaceXml, "", typeKindvalue:=TypeKindOptions.Class Or TypeKindOptions.Structure Or TypeKindOptions.Module)

            ' Check if Module is present in addition to the normal options
            Assert.Equal(3, viewModel.KindList.Count)

            ' Select the project CS2 which has no documents.
            Dim projectToSelect = viewModel.ProjectList.Where(Function(p) p.Name = "CS1").Single().Project
            viewModel.SelectedProject = projectToSelect

            ' C# does not have Module
            Assert.Equal(2, viewModel.KindList.Count)

            ' Switch back to the initial document
            projectToSelect = viewModel.ProjectList.Where(Function(p) p.Name = "VB1").Single().Project
            viewModel.SelectedProject = projectToSelect

            Assert.Equal(3, viewModel.KindList.Count)
        End Function

        <WorkItem(858826)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeFileExtensionUpdate() As Task
            Dim workspaceXml = <Workspace>
                                   <Project Language="C#" AssemblyName="CS1" CommonReferences="true">
                                       <Document FilePath="Test1.cs">
class Program
{
    static void Main(string[] args)
    {
        Foo$$ bar;
    }
}
                                       </Document>
                                       <Document FilePath="Test4.cs"></Document>
                                   </Project>
                                   <Project Language="Visual Basic" AssemblyName="VB1" CommonReferences="true">
                                       <Document FilePath="Test2.vb"></Document>
                                   </Project>
                               </Workspace>

            Dim viewModel = Await GetViewModelAsync(workspaceXml, "")

            ' Assert the current display
            Assert.Equal(viewModel.FileName, "Foo.cs")

            ' Select the project CS2 which has no documents.
            Dim projectToSelect = viewModel.ProjectList.Where(Function(p) p.Name = "VB1").Single().Project
            viewModel.SelectedProject = projectToSelect

            ' Assert the new current display
            Assert.Equal(viewModel.FileName, "Foo.vb")

            ' Switch back to the initial document
            projectToSelect = viewModel.ProjectList.Where(Function(p) p.Name = "CS1").Single().Project
            viewModel.SelectedProject = projectToSelect

            ' Assert the display is back to the way it was before
            Assert.Equal(viewModel.FileName, "Foo.cs")

            ' Set the name with vb extension
            viewModel.FileName = "Foo.vb"

            ' On focus change,we trigger this method
            viewModel.UpdateFileNameExtension()

            ' Assert that the filename changes accordingly
            Assert.Equal(viewModel.FileName, "Foo.cs")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeExcludeGeneratedDocumentsFromList() As Task
            Dim workspaceXml = <Workspace>
                                   <Project Language="C#" AssemblyName="CS1" CommonReferences="true">
                                       <Document FilePath="Test1.cs">$$</Document>
                                       <Document FilePath="Test2.cs"></Document>
                                       <Document FilePath="TemporaryGeneratedFile_test.cs"></Document>
                                       <Document FilePath="AssemblyInfo.cs"></Document>
                                       <Document FilePath="Test3.cs"></Document>
                                   </Project>
                               </Workspace>

            Dim viewModel = Await GetViewModelAsync(workspaceXml, LanguageNames.CSharp)

            Dim expectedDocuments = {"Test1.cs", "Test2.cs", "AssemblyInfo.cs", "Test3.cs"}
            Assert.Equal(expectedDocuments, viewModel.DocumentList.Select(Function(d) d.Document.Name).ToArray())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeIntoGeneratedDocument() As Task
            Dim workspaceXml = <Workspace>
                                   <Project Language="C#" AssemblyName="CS1" CommonReferences="true">
                                       <Document FilePath="Test.generated.cs">
class Program
{
    static void Main(string[] args)
    {
        Foo$$ bar;
    }
}
                                       </Document>
                                       <Document FilePath="Test2.cs"></Document>
                                   </Project>
                               </Workspace>

            Dim viewModel = Await GetViewModelAsync(workspaceXml, LanguageNames.CSharp)

            ' Test the default values
            Assert.Equal(0, viewModel.AccessSelectIndex)
            Assert.Equal(0, viewModel.KindSelectIndex)
            Assert.Equal("Foo", viewModel.TypeName)
            Assert.Equal("Foo.cs", viewModel.FileName)
            Assert.Equal("Test.generated.cs", viewModel.SelectedDocument.Name)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeNewFileNameOptions() As Task
            Dim workspaceXml = <Workspace>
                                   <Project Language="C#" AssemblyName="CS1" CommonReferences="true" FilePath="C:\A\B\CS1.csproj">
                                       <Document FilePath="C:\A\B\CDE\F\Test1.cs">
class Program
{
    static void Main(string[] args)
    {
        Foo$$ bar;
    }
}
                                       </Document>
                                       <Document FilePath="Test4.cs"></Document>
                                       <Document FilePath="C:\A\B\ExistingFile.cs"></Document>
                                   </Project>
                                   <Project Language="Visual Basic" AssemblyName="VB1" CommonReferences="true">
                                       <Document FilePath="Test2.vb"></Document>
                                   </Project>
                               </Workspace>
            Dim projectFolder = PopulateProjectFolders(New List(Of String)(), "\outer\", "\outer\inner\")

            Dim viewModel = Await GetViewModelAsync(workspaceXml, "", projectFolders:=projectFolder)

            viewModel.IsNewFile = True

            ' Assert the current display
            Assert.Equal(viewModel.FileName, "Foo.cs")

            ' Set the folder to \outer\
            viewModel.FileName = viewModel.ProjectFolders(0)
            Assert.False(viewModel.TrySubmit(), s_submit_passed_unexpectedly)

            ' Set the Filename to \\something.cs
            viewModel.FileName = "\\ExistingFile.cs"
            viewModel.UpdateFileNameExtension()
            Assert.False(viewModel.TrySubmit(), s_submit_passed_unexpectedly)

            ' Set the Filename to an existing file
            viewModel.FileName = "..\..\ExistingFile.cs"
            viewModel.UpdateFileNameExtension()
            Assert.False(viewModel.TrySubmit(), s_submit_passed_unexpectedly)

            ' Set the Filename to empty
            viewModel.FileName = "  "
            viewModel.UpdateFileNameExtension()
            Assert.False(viewModel.TrySubmit(), s_submit_passed_unexpectedly)

            ' Set the Filename with more than permissible characters
            viewModel.FileName = "sjkygjksdfygujysdkgkufsdfrgujdfyhgjksuydfujkgysdjkfuygjkusydfjusyfkjsdfygjusydfgjkuysdkfjugyksdfjkusydfgjkusdfyjgukysdjfyjkusydfgjuysdfgjuysdfjgsdjfugjusdfygjuysdfjugyjdufgsgdfvsgdvgtsdvfgsvdfgsdgfgdsvfgsdvfgsvdfgsdfsjkygjksdfygujysdkgkufsdfrgujdfyhgjksuydfujkgysdjkfuygjkusydfjusyfkjsdfygjusydfgjkuysdkfjugyksdfjkusydfgjkusdfyjgukysdjfyjkusydfgjuysdfgjuysdfjgsdjfugjusdfygjuysdfjugyjdufgsgdfvsgdvgtsdvfgsvdfgsdgfgdsvfgsdvfgsvdfgsdf.cs"
            Assert.False(viewModel.TrySubmit(), s_submit_passed_unexpectedly)

            ' Set the Filename with keywords
            viewModel.FileName = "com1\foo.cs"
            Assert.False(viewModel.TrySubmit(), s_submit_passed_unexpectedly)

            ' Set the Filename with ".."
            viewModel.FileName = "..\..\foo.cs"
            viewModel.UpdateFileNameExtension()
            Assert.True(viewModel.TrySubmit(), s_submit_failed_unexpectedly)

            ' Set the Filename with ".."
            viewModel.FileName = "..\.\..\.\foo.cs"
            viewModel.UpdateFileNameExtension()
            Assert.True(viewModel.TrySubmit(), s_submit_failed_unexpectedly)
        End Function

        <WorkItem(898452)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateTypeIntoNewFileWithInvalidIdentifierFolderName() As Task
            Dim documentContentMarkupCSharp = <Text><![CDATA[
class Program
{
    static void Main(string[] args)
    {
        A.B.Foo$$ bar;
    }
}

namespace A
{
    namespace B
    {

    }
}"]]></Text>
            Dim viewModel = Await GetViewModelAsync(documentContentMarkupCSharp, "C#", projectRootFilePath:="C:\OuterFolder\InnerFolder\")

            viewModel.IsNewFile = True
            Dim foldersAreInvalid = "Folders are not valid identifiers"
            viewModel.FileName = "123\456\Wow.cs"

            viewModel.UpdateFileNameExtension()
            Assert.True(viewModel.TrySubmit(), s_submit_failed_unexpectedly)
            Assert.False(viewModel.AreFoldersValidIdentifiers, foldersAreInvalid)

            viewModel.FileName = "@@@@\######\Woow.cs"

            viewModel.UpdateFileNameExtension()
            Assert.True(viewModel.TrySubmit(), s_submit_failed_unexpectedly)
            Assert.False(viewModel.AreFoldersValidIdentifiers, foldersAreInvalid)

            viewModel.FileName = "....a\.....b\Wow.cs"

            viewModel.UpdateFileNameExtension()
            Assert.True(viewModel.TrySubmit(), s_submit_failed_unexpectedly)
            Assert.False(viewModel.AreFoldersValidIdentifiers, foldersAreInvalid)

            Dim documentContentMarkupVB = <Text><![CDATA[
class Program
{
    static void Main(string[] args)
    {
        A.B.Foo$$ bar;
    }
}

namespace A
{
    namespace B
    {

    }
}"]]></Text>
            viewModel = Await GetViewModelAsync(documentContentMarkupVB, "Visual Basic", projectRootFilePath:="C:\OuterFolder1\InnerFolder1\")

            viewModel.IsNewFile = True
            viewModel.FileName = "123\456\Wow.vb"

            viewModel.UpdateFileNameExtension()
            Assert.True(viewModel.TrySubmit(), s_submit_failed_unexpectedly)
            Assert.False(viewModel.AreFoldersValidIdentifiers, foldersAreInvalid)

            viewModel.FileName = "@@@@\######\Woow.vb"

            viewModel.UpdateFileNameExtension()
            Assert.True(viewModel.TrySubmit(), s_submit_failed_unexpectedly)
            Assert.False(viewModel.AreFoldersValidIdentifiers, foldersAreInvalid)

            viewModel.FileName = "....a\.....b\Wow.vb"

            viewModel.UpdateFileNameExtension()
            Assert.True(viewModel.TrySubmit(), s_submit_failed_unexpectedly)
            Assert.False(viewModel.AreFoldersValidIdentifiers, foldersAreInvalid)
        End Function

        <WorkItem(898563)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)>
        Public Async Function TestGenerateType_DontGenerateIntoExistingFile() As Task
            ' Get a Temp Folder Path
            Dim projectRootFolder = Path.GetTempPath()

            ' Get a random filename
            Dim randomFileName = Path.GetRandomFileName()

            ' Get the final combined path of the file
            Dim pathString = Path.Combine(projectRootFolder, randomFileName)

            ' Create the file
            Dim fs = File.Create(pathString)
            Dim bytearray = New Byte() {0, 1}
            fs.Write(bytearray, 0, bytearray.Length)
            fs.Close()

            Dim documentContentMarkupCSharp = <Text><![CDATA[
class Program
{
    static void Main(string[] args)
    {
        A.B.Foo$$ bar;
    }
}

namespace A
{
    namespace B
    {

    }
}"]]></Text>
            Dim viewModel = Await GetViewModelAsync(documentContentMarkupCSharp, "C#", projectRootFilePath:=projectRootFolder)

            viewModel.IsNewFile = True
            viewModel.FileName = randomFileName

            Assert.False(viewModel.TrySubmit(), s_submit_passed_unexpectedly)

            ' Cleanup
            File.Delete(pathString)
        End Function

        Private Function PopulateProjectFolders(list As List(Of String), ParamArray values As String()) As List(Of String)
            list.AddRange(values)
            Return list
        End Function

        Private Function GetOneProjectWorkspace(
            documentContent As XElement,
            languageName As String,
            projectName As String,
            documentName As String,
            projectRootFilePath As String) As XElement
            Dim documentNameWithExtension = documentName + If(languageName = "C#", ".cs", ".vb")
            If projectRootFilePath Is Nothing Then
                Return <Workspace>
                           <Project Language=<%= languageName %> AssemblyName=<%= projectName %> CommonReferences="true">
                               <Document FilePath=<%= documentNameWithExtension %>><%= documentContent.NormalizedValue.Replace(vbCrLf, vbLf) %></Document>
                           </Project>
                       </Workspace>
            Else
                Dim projectFilePath As String = projectRootFilePath + projectName + If(languageName = "C#", ".csproj", ".vbproj")
                Dim documentFilePath As String = projectRootFilePath + documentNameWithExtension
                Return <Workspace>
                           <Project Language=<%= languageName %> AssemblyName=<%= projectName %> CommonReferences="true" FilePath=<%= projectFilePath %>>
                               <Document FilePath=<%= documentFilePath %>><%= documentContent.NormalizedValue.Replace(vbCrLf, vbLf) %></Document>
                           </Project>
                       </Workspace>
            End If
        End Function

        Private Async Function GetViewModelAsync(
            content As XElement,
            languageName As String,
            Optional isNewFile As Boolean = False,
            Optional accessSelectString As String = "",
            Optional kindSelectString As String = "",
            Optional projectName As String = "Assembly1",
            Optional documentName As String = "Test1",
            Optional typeKindvalue As TypeKindOptions = TypeKindOptions.AllOptions,
            Optional isPublicOnlyAccessibility As Boolean = False,
            Optional isAttribute As Boolean = False,
            Optional projectFolders As List(Of String) = Nothing,
            Optional projectRootFilePath As String = Nothing) As Tasks.Task(Of GenerateTypeDialogViewModel)

            Dim workspaceXml = If(content.Name.LocalName = "Workspace", content, GetOneProjectWorkspace(content, languageName, projectName, documentName, projectRootFilePath))
            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceXml)
                Dim testDoc = workspace.Documents.SingleOrDefault(Function(d) d.CursorPosition.HasValue)
                Assert.NotNull(testDoc)
                Dim document = workspace.CurrentSolution.GetDocument(testDoc.Id)

                Dim token = (Await document.GetSyntaxTreeAsync()).GetTouchingWord(testDoc.CursorPosition.Value, document.Project.LanguageServices.GetService(Of ISyntaxFactsService)(), CancellationToken.None)
                Dim typeName = token.ToString()

                Dim testProjectManagementService As IProjectManagementService = Nothing

                If projectFolders IsNot Nothing Then
                    testProjectManagementService = New TestProjectManagementService(projectFolders)
                End If

                Dim syntaxFactsService = document.Project.LanguageServices.GetService(Of ISyntaxFactsService)()

                Return New GenerateTypeDialogViewModel(
                    document,
                    New TestNotificationService(),
                    testProjectManagementService,
                    syntaxFactsService,
                    workspace.Services.GetService(Of IGeneratedCodeRecognitionService)(),
                    New GenerateTypeDialogOptions(isPublicOnlyAccessibility, typeKindvalue, isAttribute),
                    typeName,
                    If(document.Project.Language = LanguageNames.CSharp, ".cs", ".vb"),
                    isNewFile,
                    accessSelectString,
                    kindSelectString)
            End Using
        End Function
    End Class

    Friend Class TestProjectManagementService
        Implements IProjectManagementService

        Private _projectFolders As List(Of String)

        Public Sub New(projectFolders As List(Of String))
            Me._projectFolders = projectFolders
        End Sub

        Public Function GetDefaultNamespace(project As Project, workspace As Workspace) As String Implements IProjectManagementService.GetDefaultNamespace
            Return ""
        End Function

        Public Function GetFolders(projectId As ProjectId, workspace As Workspace) As IList(Of String) Implements IProjectManagementService.GetFolders
            Return Me._projectFolders
        End Function
    End Class
End Namespace

