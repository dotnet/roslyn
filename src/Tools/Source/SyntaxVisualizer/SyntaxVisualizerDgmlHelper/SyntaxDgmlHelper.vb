' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualBasic
Imports <xmlns="http://schemas.microsoft.com/vs/2009/dgml">

Public Class SyntaxDgmlOptions
    Public Property ShowTrivia As Boolean = True
    Public Property ShowSpan As Boolean = True
    Public Property ShowErrors As Boolean = True
    Public Property ShowText As Boolean = False
    Public Property ShowGroups As Boolean = False
End Class

Public Module SyntaxDgmlHelper
    Private ReadOnly s_defaultOptions As New SyntaxDgmlOptions
    Private Const s_MAX_LABEL_LENGTH = 30

    'Helpers that return the DGML representation of a SyntaxNode / SyntaxToken / SyntaxTrivia.
    'DGML is an XML-based format for directed graphs that can be rendered by Visual Studio.

#Region "ToDgml"
    <Extension()>
    Public Function ToDgml(nodeOrToken As SyntaxNodeOrToken,
                           Optional options As SyntaxDgmlOptions = Nothing) As XElement
        Dim dgml As XElement = Nothing

        If nodeOrToken.IsNode Then
            dgml = ToDgml(nodeOrToken.AsNode, options)
        Else
            dgml = ToDgml(nodeOrToken.AsToken, options)
        End If

        Return dgml
    End Function

    <Extension()>
    Public Function ToDgml(node As SyntaxNode,
                           Optional options As SyntaxDgmlOptions = Nothing) As XElement
        If options Is Nothing Then
            options = s_defaultOptions
        End If

        Dim dgml = GetDgmlTemplate(options)
        ProcessNode(options, node, dgml)
        Return dgml
    End Function

    <Extension()>
    Public Function ToDgml(token As SyntaxToken,
                           Optional options As SyntaxDgmlOptions = Nothing) As XElement
        If options Is Nothing Then
            options = s_defaultOptions
        End If

        Dim dgml = GetDgmlTemplate(options)
        ProcessToken(options, token, dgml)
        Return dgml
    End Function

    <Extension()>
    Public Function ToDgml(trivia As SyntaxTrivia,
                           Optional options As SyntaxDgmlOptions = Nothing) As XElement
        If options Is Nothing Then
            options = s_defaultOptions
        End If

        Dim dgml = GetDgmlTemplate(options)
        ProcessTrivia(options, trivia, dgml)
        Return dgml
    End Function
#End Region

#Region "Process*"
    Private Sub ProcessNodeOrToken(options As SyntaxDgmlOptions, nodeOrToken As SyntaxNodeOrToken, dgml As XElement,
                                   Optional ByRef count As Integer = 0,
                                   Optional parent As XElement = Nothing,
                                   Optional parentGroup As XElement = Nothing,
                                   Optional properties As HashSet(Of String) = Nothing)
        If nodeOrToken.IsNode Then
            ProcessNode(options, nodeOrToken.AsNode, dgml, count, parent, parentGroup, properties)
        Else
            ProcessToken(options, nodeOrToken.AsToken, dgml, count, parent, parentGroup, properties)
        End If
    End Sub

    Private Sub ProcessNode(options As SyntaxDgmlOptions, node As SyntaxNode, dgml As XElement,
                            Optional ByRef count As Integer = 0,
                            Optional parent As XElement = Nothing,
                            Optional parentGroup As XElement = Nothing,
                            Optional properties As HashSet(Of String) = Nothing)
        count += 1

        Dim current = <Node Id=<%= count %> Label=<%= GetLabelForNode(node) %>/>
        Dim currentID = count, parentID = -1, currentGroupID = -1, parentGroupID = -1
        Initialize(options, dgml, parent, parentGroup, current, properties, currentID, parentID, currentGroupID, parentGroupID)
        AddNodeInfo(options, node, current, dgml, properties)
        Dim currentGroup As XElement = parentGroup

        current.@Category = "0"

        If options.ShowGroups Then
            count += 1
            currentGroup = <Node Group="Expanded" Id=<%= count %> Label=<%= GetLabelForNode(node) %>/>
            AddNodeInfo(options, node, currentGroup, dgml, properties)
            dgml.<Nodes>.First.Add(currentGroup)
            currentGroupID = count
            dgml.<Links>.First.Add(<Link Source=<%= currentGroupID %> Target=<%= currentID %> Category="7"></Link>)
            If parentGroupID <> -1 Then
                dgml.<Links>.First.Add(<Link Source=<%= parentGroupID %> Target=<%= currentGroupID %> Category="7"></Link>)
            End If
        End If

        Dim kind = node.GetKind()

        If (node.IsMissing OrElse node.Span.Length = 0) AndAlso Not kind = "CompilationUnit" Then
            current.@Category = "4"
        End If

        If kind.Contains("Bad") OrElse kind.Contains("Skipped") Then
            current.@Category = "5"
        End If

        If options.ShowErrors AndAlso node.ContainsDiagnostics Then
            AddErrorIcon(current)
        End If

        For Each childSyntaxNode In node.ChildNodesAndTokens()
            ProcessNodeOrToken(options, childSyntaxNode, dgml, count, current, currentGroup, properties)
        Next
    End Sub

    Private Sub ProcessToken(options As SyntaxDgmlOptions, token As SyntaxToken, dgml As XElement,
                             Optional ByRef count As Integer = 0,
                             Optional parent As XElement = Nothing,
                             Optional parentGroup As XElement = Nothing,
                             Optional properties As HashSet(Of String) = Nothing)
        count += 1

        Dim current = <Node Id=<%= count %> Label=<%= GetLabelForToken(token) %>/>
        Initialize(options, dgml, parent, parentGroup, current, properties, count, 0, 0, 0)
        AddTokenInfo(options, token, current, dgml, properties)
        Dim currentGroup As XElement = parentGroup

        current.@Category = "1"

        Dim kind = token.GetKind()

        If (token.IsMissing OrElse token.Span.Length = 0) AndAlso Not kind = "EndOfFileToken" Then
            current.@Category = "4"
        End If

        If kind.Contains("Bad") OrElse kind.Contains("Skipped") Then
            current.@Category = "5"
        End If

        If options.ShowErrors AndAlso token.ContainsDiagnostics Then
            AddErrorIcon(current)
        End If

        If options.ShowTrivia Then
            For Each triviaNode In token.LeadingTrivia
                ProcessTrivia(options, triviaNode, dgml, count, True, current, currentGroup, properties)
            Next
            For Each triviaNode In token.TrailingTrivia
                ProcessTrivia(options, triviaNode, dgml, count, False, current, currentGroup, properties)
            Next
        End If
    End Sub

    Private Sub ProcessTrivia(options As SyntaxDgmlOptions, trivia As SyntaxTrivia, dgml As XElement,
                              Optional ByRef count As Integer = 0,
                              Optional isProcessingLeadingTrivia As Boolean = False,
                              Optional parent As XElement = Nothing,
                              Optional parentGroup As XElement = Nothing,
                              Optional properties As HashSet(Of String) = Nothing)
        count += 1

        Dim current = <Node Id=<%= count %> Label=<%= GetLabelForTrivia(trivia) %>/>
        Initialize(options, dgml, parent, parentGroup, current, properties, count, 0, 0, 0)
        AddTriviaInfo(options, trivia, current, dgml, properties)
        Dim currentGroup As XElement = parentGroup

        If isProcessingLeadingTrivia Then
            current.@Category = "2"
        Else
            current.@Category = "3"
        End If

        Dim kind = trivia.GetKind()

        If kind.Contains("Bad") OrElse kind.Contains("Skipped") Then
            current.@Category = "5"
        End If

        If options.ShowErrors AndAlso trivia.ContainsDiagnostics Then
            AddErrorIcon(current)
        End If

        If options.ShowTrivia Then
            If trivia.HasStructure Then
                ProcessNode(options, trivia.GetStructure, dgml, count, current, currentGroup, properties)
            End If
        End If
    End Sub
#End Region

#Region "GetLabel*"
    Private Function GetLabelForNode(node As SyntaxNode) As String
        Return node.GetKind()
    End Function

    Private Function GetLabelForToken(token As SyntaxToken) As String
        Dim label = token.GetKind()
        Dim text = token.ToString()

        If text.Trim <> String.Empty Then
            If text.Length <= s_MAX_LABEL_LENGTH Then
                label = text
            Else
                label = text.Remove(s_MAX_LABEL_LENGTH) & "..."
            End If
        End If

        Return label
    End Function

    Private Function GetLabelForTrivia(trivia As SyntaxTrivia) As String
        Return trivia.GetKind()
    End Function
#End Region

#Region "Add*"
    Private Sub AddNodeInfo(options As SyntaxDgmlOptions, node As SyntaxNode,
                            current As XElement, dgml As XElement,
                            properties As HashSet(Of String))
        Dim nodeInfo = GetObjectInfo(node)
        AddDgmlProperty("Type", properties, dgml)
        current.@Type = nodeInfo.TypeName
        AddDgmlProperty("Kind", properties, dgml)
        current.@Kind = node.GetKind()

        If options.ShowSpan Then
            AddDgmlProperty("Span", properties, dgml)
            current.@Span = String.Format("{0} Length: {1}",
                                       node.Span.ToString,
                                       node.Span.Length)
            AddDgmlProperty("FullSpan", properties, dgml)
            current.@FullSpan = String.Format("{0} Length: {1}",
                                           node.FullSpan.ToString,
                                           node.FullSpan.Length)
        End If

        For Each field In nodeInfo.PropertyInfos
            Dim name = field.Name
            If Not (name.Contains("Span") OrElse name.Contains("Kind") OrElse name.Contains("Text")) Then
                AddDgmlProperty(name, properties, dgml)
                current.Add(New XAttribute(name, field.Value.ToString))
            End If
        Next

        Dim syntaxTree = node.SyntaxTree
        If syntaxTree IsNot Nothing AndAlso options.ShowErrors Then
            Dim syntaxErrors = syntaxTree.GetDiagnostics(node)
            AddDgmlProperty("Errors", properties, dgml)
            current.@Errors = String.Format("Count: {0}", syntaxErrors.Count)
            For Each syntaxError In syntaxErrors
                current.@Errors &= vbCrLf & syntaxError.ToString(Nothing)
            Next
        End If

        If options.ShowText Then
            AddDgmlProperty("Text", properties, dgml)
            current.@Text = node.ToString()
            AddDgmlProperty("FullText", properties, dgml)
            current.@FullText = node.ToFullString()
        End If
    End Sub

    Private Sub AddTokenInfo(options As SyntaxDgmlOptions, token As SyntaxToken,
                             current As XElement, dgml As XElement,
                             properties As HashSet(Of String))
        Dim tokenInfo = GetObjectInfo(token)
        AddDgmlProperty("Type", properties, dgml)
        current.@Type = tokenInfo.TypeName
        AddDgmlProperty("Kind", properties, dgml)
        current.@Kind = token.GetKind()

        If options.ShowSpan Then
            AddDgmlProperty("Span", properties, dgml)
            current.@Span = String.Format("{0} Length: {1}",
                                       token.Span.ToString,
                                       token.Span.Length)
            AddDgmlProperty("FullSpan", properties, dgml)
            current.@FullSpan = String.Format("{0} Length: {1}",
                                           token.FullSpan.ToString,
                                           token.FullSpan.Length)
        End If

        For Each field In tokenInfo.PropertyInfos
            Dim name = field.Name
            If Not (name.Contains("Span") OrElse name.Contains("Kind") OrElse name.Contains("Text")) Then
                AddDgmlProperty(name, properties, dgml)
                current.Add(New XAttribute(name, field.Value.ToString))
            End If
        Next

        Dim syntaxTree = token.SyntaxTree
        If syntaxTree IsNot Nothing AndAlso options.ShowErrors Then
            Dim syntaxErrors = syntaxTree.GetDiagnostics(token)
            AddDgmlProperty("Errors", properties, dgml)
            current.@Errors = String.Format("Count: {0}", syntaxErrors.Count)
            For Each syntaxError In syntaxErrors
                current.@Errors &= vbCrLf & syntaxError.ToString(Nothing)
            Next
        End If

        If options.ShowText Then
            AddDgmlProperty("Text", properties, dgml)
            current.@Text = token.ToString()
            AddDgmlProperty("FullText", properties, dgml)
            current.@FullText = token.ToFullString()
        End If
    End Sub

    Private Sub AddTriviaInfo(options As SyntaxDgmlOptions, trivia As SyntaxTrivia,
                              current As XElement, dgml As XElement,
                              properties As HashSet(Of String))
        Dim triviaInfo = GetObjectInfo(trivia)
        AddDgmlProperty("Type", properties, dgml)
        current.@Type = triviaInfo.TypeName
        AddDgmlProperty("Kind", properties, dgml)
        current.@Kind = trivia.GetKind()

        If options.ShowSpan Then
            AddDgmlProperty("Span", properties, dgml)
            current.@Span = String.Format("{0} Length: {1}",
                                       trivia.Span.ToString,
                                       trivia.Span.Length)
            AddDgmlProperty("FullSpan", properties, dgml)
            current.@FullSpan = String.Format("{0} Length: {1}",
                                           trivia.FullSpan.ToString,
                                           trivia.FullSpan.Length)
        End If

        For Each field In triviaInfo.PropertyInfos
            Dim name = field.Name
            If Not (name.Contains("Span") OrElse name.Contains("Kind") OrElse name.Contains("Text")) Then
                AddDgmlProperty(name, properties, dgml)
                current.Add(New XAttribute(name, field.Value.ToString))
            End If
        Next

        Dim syntaxTree = trivia.SyntaxTree
        If syntaxTree IsNot Nothing AndAlso options.ShowErrors Then
            Dim syntaxErrors = syntaxTree.GetDiagnostics(trivia)
            AddDgmlProperty("Errors", properties, dgml)
            current.@Errors = String.Format("Count: {0}", syntaxErrors.Count)
            For Each syntaxError In syntaxErrors
                current.@Errors &= vbCrLf & syntaxError.ToString(Nothing)
            Next
        End If

        If options.ShowText Then
            AddDgmlProperty("Text", properties, dgml)
            current.@Text = trivia.ToString()
            AddDgmlProperty("FullText", properties, dgml)
            current.@FullText = trivia.ToFullString()
        End If
    End Sub
#End Region

#Region "Other Helpers"
    Private Sub Initialize(options As SyntaxDgmlOptions,
                           dgml As XElement,
                           parent As XElement,
                           parentGroup As XElement,
                           current As XElement,
                           ByRef properties As HashSet(Of String),
                           currentID As Integer,
                           ByRef parentID As Integer,
                           ByRef currentGroupID As Integer,
                           ByRef parentGroupID As Integer)
        dgml.<Nodes>.First.Add(current)

        parentGroupID = -1 : currentGroupID = -1
        parentID = -1

        If parent IsNot Nothing Then
            parentID = CInt(parent.@Id)
        End If

        If options.ShowGroups Then
            If parentGroup IsNot Nothing Then
                parentGroupID = CInt(parentGroup.@Id)
            End If
            currentGroupID = parentGroupID
        End If

        If parentID <> -1 Then
            dgml.<Links>.First.Add(<Link Source=<%= parentID %> Target=<%= currentID %>></Link>)
        End If

        If options.ShowGroups AndAlso parentGroupID <> -1 Then
            dgml.<Links>.First.Add(<Link Source=<%= parentGroupID %> Target=<%= currentID %> Category="7"></Link>)
        End If

        If properties Is Nothing Then
            properties = New HashSet(Of String)
        End If
    End Sub

    Private Function GetDgmlTemplate(options As SyntaxDgmlOptions) As XElement
        Dim dgml = <DirectedGraph Background="LightGray">
                       <Categories>
                           <Category Id="0" Label="SyntaxNode"/>
                           <Category Id="1" Label="SyntaxToken"/>
                           <Category Id="2" Label="Leading SyntaxTrivia"/>
                           <Category Id="3" Label="Trailing SyntaxTrivia"/>
                           <Category Id="4" Label="Missing / Zero-Width"/>
                           <Category Id="5" Label="Bad / Skipped"/>
                           <Category Id="6" Label="Has Diagnostics"/>
                       </Categories>
                       <Nodes>
                       </Nodes>
                       <Links>
                       </Links>
                       <Properties>
                       </Properties>
                       <Styles>
                           <Style TargetType="Node" GroupLabel="SyntaxNode" ValueLabel="Has category">
                               <Condition Expression="HasCategory('0')"/>
                               <Setter Property="Background" Value="Blue"/>
                               <Setter Property="NodeRadius" Value="5"/>
                           </Style>
                           <Style TargetType="Node" GroupLabel="SyntaxToken" ValueLabel="Has category">
                               <Condition Expression="HasCategory('1')"/>
                               <Setter Property="Background" Value="DarkGreen"/>
                               <Setter Property="FontStyle" Value="Italic"/>
                               <Setter Property="NodeRadius" Value="5"/>
                           </Style>
                           <%= If(options.ShowTrivia,
                               <Style TargetType="Node" GroupLabel="Leading SyntaxTrivia" ValueLabel="Has category">
                                   <Condition Expression="HasCategory('2')"/>
                                   <Setter Property="Background" Value="White"/>
                                   <Setter Property="NodeRadius" Value="5"/>
                               </Style>, Nothing) %>
                           <%= If(options.ShowTrivia,
                               <Style TargetType="Node" GroupLabel="Trailing SyntaxTrivia" ValueLabel="Has category">
                                   <Condition Expression="HasCategory('3')"/>
                                   <Setter Property="Background" Value="DimGray"/>
                                   <Setter Property="NodeRadius" Value="5"/>
                               </Style>, Nothing) %>
                           <Style TargetType="Node" GroupLabel="Missing / Zero-Width" ValueLabel="Has category">
                               <Condition Expression="HasCategory('4')"/>
                               <Setter Property="Background" Value="Black"/>
                               <Setter Property="NodeRadius" Value="5"/>
                           </Style>
                           <Style TargetType="Node" GroupLabel="Bad / Skipped" ValueLabel="Has category">
                               <Condition Expression="HasCategory('5')"/>
                               <Setter Property="Background" Value="Red"/>
                               <Setter Property="FontStyle" Value="Bold"/>
                               <Setter Property="NodeRadius" Value="5"/>
                           </Style>
                           <Style TargetType="Node" GroupLabel="Has Diagnostics" ValueLabel="Has category">
                               <Condition Expression="HasCategory('6')"/>
                               <Setter Property="Icon" Value="CodeSchema_Event"/>
                           </Style>
                       </Styles>
                   </DirectedGraph>

        dgml.AddAnnotation(SaveOptions.OmitDuplicateNamespaces)

        If options.ShowGroups Then
            dgml.<Categories>.First.Add(<Category Id="7" Label="Contains" CanBeDataDriven="False" CanLinkedNodesBeDataDriven="True" IncomingActionLabel="Contained By" IsContainment="True" OutgoingActionLabel="Contains"/>)
        End If
        Return dgml
    End Function

    Private Sub AddDgmlProperty(propertyName As String, properties As HashSet(Of String), dgml As XElement)
        If Not properties.Contains(propertyName) Then
            dgml.<Properties>.First.Add(<Property Id=<%= propertyName %> Label=<%= propertyName %> DataType="System.String"/>)
            properties.Add(propertyName)
        End If
    End Sub

    Private Sub AddErrorIcon(element As XElement)
        element.@Icon = "CodeSchema_Event"
    End Sub
#End Region
End Module
