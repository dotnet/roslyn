'******************************************************************************
'* ResourceEditorView.vb
'*
'* Copyright (C) 1999-2003 Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Option Strict On
Option Explicit On
Option Compare Binary

Imports System
Imports System.Collections.Specialized
Imports System.Diagnostics
Imports EnvDTE
Imports Microsoft.VisualStudio.TemplateWizard

Namespace Microsoft.VisualStudio.Editors.ResourceEditor

    ''' <summary>
    ''' This class will set ProjectItem.Properties values for an item being added to the project
    ''' with the special consideration of whether or not the item being added is a localized .resx
    ''' file-name. If it is a localized .resx file (e.g. foo.en.resx), this class will skip setting
    ''' any CustomTool-related properties [CustomTool is the project-property name for a generator]
    ''' </summary>
    ''' <remarks></remarks>
    Public Class ResxItemWizard
        Implements IWizard

        Private propertiesToSet As StringDictionary

        ''' <summary>
        ''' Do nothing
        ''' </summary>
        ''' <param name="projectItem"></param>
        ''' <remarks></remarks>
        Public Sub BeforeOpeningFile(ByVal projectItem As EnvDTE.ProjectItem) Implements IWizard.BeforeOpeningFile
        End Sub

        ''' <summary>
        ''' Do nothing
        ''' </summary>
        ''' <param name="project"></param>
        ''' <remarks></remarks>
        Public Sub ProjectFinishedGenerating(ByVal project As EnvDTE.Project) Implements IWizard.ProjectFinishedGenerating
        End Sub

        ''' <summary>
        ''' set the CustomTool property on this .resx file if it is NOT a localized file-name (determined
        ''' by looking to see whether the text between the next-to-last and last '.' characters is a valid
        ''' CultureInfo string).
        ''' </summary>
        ''' <param name="projectItem"></param>
        ''' <remarks></remarks>
        Public Sub ProjectItemFinishedGenerating(ByVal projectItem As EnvDTE.ProjectItem) Implements IWizard.ProjectItemFinishedGenerating

            Debug.Assert(projectItem IsNot Nothing, "Null projectItem?")
            If (projectItem IsNot Nothing AndAlso propertiesToSet IsNot Nothing) Then

                Dim fileName As String = projectItem.FileNames(1)
                Debug.Assert(fileName IsNot Nothing AndAlso fileName.Length > 0, "bogus ProjectItem.FileNames(1) value?")

                Dim isLocalizedResxFile As Boolean = ResourceEditorView.IsLocalizedResXFile(fileName)
                Dim itemProperties As Properties = projectItem.Properties

                Debug.Assert(itemProperties IsNot Nothing, "null projectItem.Properties?")
                If (itemProperties IsNot Nothing) Then

                    For Each propertyEntry As System.Collections.DictionaryEntry In propertiesToSet

                        Dim name As String = TryCast(propertyEntry.Key, String)
                        Dim value As String = TryCast(propertyEntry.Value, String)

                        Debug.Assert(name IsNot Nothing, "how did we put a null value for name in a StringDictionary")
                        Debug.Assert(value IsNot Nothing, "how did we put a null value for value in a StringDictionary")

                        ' if this property is trying to set any of the CustomTool properties and it
                        '   is a localized resx file, then skip this property because we don't want
                        '   to have folks doing localized resources within VS generate the same class
                        '   multiple times. [of course, the logic is written as: if this is not a customtool
                        '   related property or this is not a localizable resx file, then set the property]
                        '
                        If ((Not name.ToUpperInvariant().Contains("CUSTOMTOOL") OrElse (Not isLocalizedResxFile))) Then

                            Dim itemProperty As [Property] = Nothing

                            Try
                                itemProperty = itemProperties.Item(name)
                            Catch ex As ArgumentException
                                ' ignore if no such property
                                Debug.Fail("error getting property named '" & CStr(name) & "': " & ex.Message)
                            End Try

                            If (itemProperty IsNot Nothing) Then
                                Try
                                    itemProperty.Value = value
                                Catch ex As Exception
                                    Debug.Fail("Setting property " & name & " to value " & value & " threw: " & ex.Message)
                                End Try
                            End If
                        End If
                    Next
                End If

            End If

        End Sub

        ''' <summary>
        ''' Do nothing
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub RunFinished() Implements IWizard.RunFinished
        End Sub

        ''' <summary>
        ''' Called when this wizard is started, giving us a chance to get the replacementsDictionary
        ''' values we care about and store them.
        ''' </summary>
        ''' <param name="automationObject"></param>
        ''' <param name="replacementsDictionary"></param>
        ''' <param name="runKind"></param>
        ''' <param name="customParams"></param>
        ''' <remarks></remarks>
        Public Sub RunStarted(ByVal automationObject As Object, ByVal replacementsDictionary As System.Collections.Generic.Dictionary(Of String, String), ByVal runKind As WizardRunKind, ByVal customParams() As Object) Implements IWizard.RunStarted

            ' we can't do any work if the dictionary is nothing...
            '
            Debug.Assert(replacementsDictionary IsNot Nothing, "Null dictionary param?")
            If (replacementsDictionary IsNot Nothing) Then

                If (replacementsDictionary.ContainsKey("$itemproperties$")) Then

                    propertiesToSet = New StringDictionary()

                    Dim propertyNames As String = replacementsDictionary("$itemproperties$")

                    For Each propertyName As String In propertyNames.Split(New Char() {";"c, ","c})

                        Dim trimmedPropertyName As String = propertyName.Trim()

                        If (trimmedPropertyName.Length > 0) Then

                            Dim macroName As String = "$" & trimmedPropertyName & "$"

                            If (replacementsDictionary.ContainsKey(macroName) AndAlso Not propertiesToSet.ContainsKey(trimmedPropertyName)) Then
                                propertiesToSet.Add(trimmedPropertyName, replacementsDictionary(macroName))
                            End If
                        End If
                    Next
                Else
                    ' if the dictionary does not have $itemproperties$ then there's nothing
                    '   for us to set, so we clear out our string-dictionary
                    '
                    propertiesToSet = Nothing
                End If

            End If

        End Sub

        ''' <summary>
        ''' Do nothing and simply return true
        ''' </summary>
        ''' <param name="filePath"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function ShouldAddProjectItem(ByVal filePath As String) As Boolean Implements IWizard.ShouldAddProjectItem
            Return True
        End Function
    End Class

End Namespace
