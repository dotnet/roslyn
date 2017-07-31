﻿Option Strict Off
' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnnecessaryImports
    Partial Public Class RemoveUnnecessaryImportsTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInDocument() As Task
            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
{|FixAllInDocument:Imports System
Imports System.Collections.Generic
|}
Class Program
    Public x As Int32
End Class]]>
                                </Document>
                                <Document><![CDATA[
Imports System
Imports System.Collections.Generic

Class Program2
    Public x As Int32
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Imports System
Imports System.Collections.Generic

Class Program3
    Public x As Int32
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document>
                                       <![CDATA[Class Program
    Public x As Int32
End Class]]>
                                   </Document>
                                   <Document><![CDATA[
Imports System
Imports System.Collections.Generic

Class Program2
    Public x As Int32
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Imports System
Imports System.Collections.Generic

Class Program3
    Public x As Int32
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected, ignoreTrivia:=False, fixAllActionEquivalenceKey:=Nothing)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInProject() As Task
            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
{|FixAllInProject:Imports System
Imports System.Collections.Generic
|}
Class Program
    Public x As Int32
End Class]]>
                                </Document>
                                <Document><![CDATA[
Imports System
Imports System.Collections.Generic

Class Program2
    Public x As Int32
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Imports System
Imports System.Collections.Generic

Class Program3
    Public x As Int32
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document>
                                       <![CDATA[Class Program
    Public x As Int32
End Class]]>
                                   </Document>
                                   <Document>
                                       <![CDATA[Class Program2
    Public x As Int32
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document><![CDATA[
Imports System
Imports System.Collections.Generic

Class Program3
    Public x As Int32
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected, ignoreTrivia:=False, fixAllActionEquivalenceKey:=Nothing)
        End Function

        <Fact>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllInSolution() As Task
            Dim input = <Workspace>
                            <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                <Document><![CDATA[
{|FixAllInSolution:Imports System
Imports System.Collections.Generic
|}
Class Program
    Public x As Int32
End Class]]>
                                </Document>
                                <Document><![CDATA[
Imports System
Imports System.Collections.Generic

Class Program2
    Public x As Int32
End Class]]>
                                </Document>
                            </Project>
                            <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                <ProjectReference>Assembly1</ProjectReference>
                                <Document><![CDATA[
Imports System
Imports System.Collections.Generic

Class Program3
    Public x As Int32
End Class]]>
                                </Document>
                            </Project>
                        </Workspace>.ToString()

            Dim expected = <Workspace>
                               <Project Language="Visual Basic" AssemblyName="Assembly1" CommonReferences="true">
                                   <Document>
                                       <![CDATA[Class Program
    Public x As Int32
End Class]]>
                                   </Document>
                                   <Document>
                                       <![CDATA[Class Program2
    Public x As Int32
End Class]]>
                                   </Document>
                               </Project>
                               <Project Language="Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                                   <ProjectReference>Assembly1</ProjectReference>
                                   <Document>
                                       <![CDATA[Class Program3
    Public x As Int32
End Class]]>
                                   </Document>
                               </Project>
                           </Workspace>.ToString()

            Await TestInRegularAndScriptAsync(input, expected, ignoreTrivia:=False, fixAllActionEquivalenceKey:=Nothing)
        End Function
    End Class
End Namespace
