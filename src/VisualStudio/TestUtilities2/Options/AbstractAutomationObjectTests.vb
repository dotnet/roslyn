' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports System.Windows
Imports System.Windows.Controls
Imports Microsoft.CodeAnalysis.ColorSchemes
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Public MustInherit Class AbstractAutomationObjectTests
        Protected MustOverride ReadOnly Property SkippedOptionsForOptionChangedTest As ImmutableArray(Of String)
        Protected MustOverride ReadOnly Property SkippedOptionsForProperStorageTest As ImmutableArray(Of String)

        <Fact>
        Public Sub TestEnsureProperStorageLocation()
            Using workspace As TestWorkspace = CreateWorkspace()
                AddHandler workspace.GlobalOptions.OptionChanged, Sub(sender, e)

                                                                      If SkippedOptionsForProperStorageTest.Contains(e.Option.Name) Then
                                                                          Return
                                                                      End If

                                                                      Assert.True(e.Option.StorageLocations.Any(
                                                                                  Function(s) TypeOf s Is RoamingProfileStorageLocation OrElse TypeOf s Is LocalUserProfileStorageLocation OrElse TypeOf s Is FeatureFlagStorageLocation),
                                                                                  $"Option '{e.Option.Name}' doesn't have proper storage location for persistence.")
                                                                  End Sub

                Dim automationObject As AbstractAutomationObject = CreateAutomationObject(workspace)
                Dim automationObjectType = automationObject.GetType()

                For Each [property] In automationObjectType.GetProperties()
                    Assert.True([property].CanRead, $"'{[property].Name}' must have a getter.")
                    Assert.True([property].CanWrite, $"'{[property].Name}' must have a setter.")

                    If SkippedOptionsForOptionChangedTest.Contains([property].Name) Then
                        Continue For
                    End If

                    Dim defaultValue = [property].GetValue(automationObject, Nothing)
                    Dim newValue As Object = GetNewValue(defaultValue, [property].Name)

                    Assert.True(newValue IsNot defaultValue, $"'{[property].Name}' must have a different newValue.")
                    Assert.Raises(Of OptionChangedEventArgs)(attach:=Sub(h) AddHandler workspace.GlobalOptions.OptionChanged, h,
                                                             detach:=Sub(h) RemoveHandler workspace.GlobalOptions.OptionChanged, h,
                                                             testCode:=Sub() [property].SetValue(automationObject, newValue))

                    Dim retrievedNewValue = [property].GetValue(automationObject, Nothing)
                    Assert.True(retrievedNewValue.Equals(newValue), $"'{[property].Name}' didn't retrieve the new value correctly.")
                Next
            End Using
        End Sub

        ' TODO: https://github.com/dotnet/roslyn/issues/62384
        Protected Shared Sub VerifySingleChangeWhenOptionChangeInUI(automationObject As AbstractAutomationObject, changeUIControl As Action, optionService As Object, optionStore As OptionStore, optionForAssertMessage As String)
            Dim automationValuesBeforeChange = GetAutomationDictionary(automationObject)

            changeUIControl()

            ' Following simulates the SaveSettingsToStorage call.
            ' Save the changes that were accumulated in the option store.
            Dim oldOptions As New SolutionOptionSet(DirectCast(optionService, ILegacyWorkspaceOptionService))
            Dim newOptions = DirectCast(optionStore.GetOptions(), SolutionOptionSet)

            ' Must log the option change before setting the new option values via s_optionService,
            ' otherwise oldOptions and newOptions would be identical and nothing will be logged.
            OptionLogger.Log(oldOptions, newOptions)
            DirectCast(optionService, ILegacyWorkspaceOptionService).SetOptions(newOptions, newOptions.GetChangedOptions())

            Dim automationValuesAfterChange = GetAutomationDictionary(automationObject)

            Assert.Equal(automationValuesBeforeChange.Count, automationValuesAfterChange.Count)
            AssertExactlyOneChange(automationValuesBeforeChange, automationValuesAfterChange, optionForAssertMessage)
        End Sub

        Private Shared Function GetAutomationDictionary(automationObject As AbstractAutomationObject) As ImmutableDictionary(Of String, Object)
            Dim automationObjectValues = ImmutableDictionary.CreateBuilder(Of String, Object)()
            For Each [property] In automationObject.GetType().GetProperties()
                automationObjectValues.Add([property].Name, [property].GetValue(automationObject, Nothing))
            Next

            Return automationObjectValues.ToImmutable()
        End Function

        Private Shared Sub AssertExactlyOneChange(dictionary1 As ImmutableDictionary(Of String, Object), dictionary2 As ImmutableDictionary(Of String, Object), optionForAssertMessage As String)
            Dim seenChange = False
            Dim changedKey As String = Nothing
            For Each key In dictionary1.Keys
                If Not dictionary1(key).Equals(dictionary2(key)) Then
                    If seenChange Then
                        If key <> "ShowSnippets" AndAlso changedKey <> "ShowSnippets" AndAlso
                            key <> "EnterKeyBehavior" AndAlso changedKey <> "EnterKeyBehavior" Then
                            Assert.False(True, $"Two values ('{key}' and '{changedKey}') have changed in automation object after changing '{optionForAssertMessage}'!")
                        End If
                    End If

                    changedKey = key
                    seenChange = True
                End If
            Next

            Assert.True(seenChange, $"No change was found in automation object after changing '{optionForAssertMessage}'.")
        End Sub

        <WpfFact>
        Public Sub TestOptionsInUIShouldBeInAutomationObject()
            Using workspace = CreateWorkspace(EditorTestCompositions.LanguageServerProtocol.AddExcludedPartTypes(GetType(ColorSchemeApplier)).AddParts(GetType(TestColorSchemeApplier)))
                Dim optionStore As New OptionStore(workspace.Options, Enumerable.Empty(Of IOption)())
                Dim optionService = workspace.Services.GetRequiredService(Of ILegacyWorkspaceOptionService)()
                Dim automationObject = CreateAutomationObject(workspace)

                Dim pageControls As IEnumerable(Of AbstractOptionPageControl) = CreatePageControls(optionStore, workspace)
                For Each pageControl In pageControls
                    Dim radioButtonGroups = New Dictionary(Of String, List(Of RadioButton))()
                    For Each bindingExpression In pageControl.GetTestAccessor().BindingExpressions
                        Dim target = bindingExpression.Target
                        Dim optionForAssertMessage = DirectCast(target, FrameworkElement).Name

                        If TypeOf target Is CheckBox Then
                            Dim checkBox = DirectCast(target, CheckBox)
                            VerifySingleChangeWhenOptionChangeInUI(automationObject, Sub() checkBox.IsChecked = Not checkBox.IsChecked, optionService, optionStore, optionForAssertMessage)
                        ElseIf TypeOf target Is ComboBox Then
                            Dim comboBox = DirectCast(target, ComboBox)
                            VerifySingleChangeWhenOptionChangeInUI(automationObject, Sub() comboBox.SelectedIndex = If(comboBox.SelectedIndex = 0, 1, 0), optionService, optionStore, optionForAssertMessage)
                        ElseIf TypeOf target Is RadioButton Then
                            Dim radioButton = DirectCast(target, RadioButton)
                            Dim list As List(Of RadioButton) = Nothing
                            If radioButtonGroups.TryGetValue(radioButton.GroupName, list) Then
                                list.Add(radioButton)
                            Else
                                radioButtonGroups.Add(radioButton.GroupName, New List(Of RadioButton) From {radioButton})
                            End If

                            Continue For
                        End If
                    Next

                    For Each radioButtonGroup In radioButtonGroups
                        Dim groupName = radioButtonGroup.Key
                        Dim radioButtons = radioButtonGroup.Value
                        ' There is no point in having a single radio button in a group.
                        Assert.True(radioButtons.Count > 1, $"Expected radio button group '{groupName}' to have more than one radio button. Found {radioButtons.Count}.")
                        Dim selectedRadioButton = radioButtons.SingleOrDefault(Function(r) r.IsChecked.HasValue AndAlso r.IsChecked.Value)
                        For Each radioButton In radioButtons
                            ' We test selecting every radio button in the group.
                            ' We skip the already selected one till we are sure we tested other radio buttons.
                            If radioButton Is selectedRadioButton Then
                                Continue For
                            End If

                            Assert.False(radioButton.IsChecked)
                            VerifySingleChangeWhenOptionChangeInUI(automationObject, Sub() radioButton.IsChecked = True, optionService, optionStore, optionForAssertMessage:=radioButton.Name)
                        Next

                        ' TODO: Consider asserting a non-null selectedRadioButton if https://github.com/dotnet/roslyn/issues/62363 is fixed.

                        If selectedRadioButton IsNot Nothing Then
                            ' Now that we tested other radio buttons in the group, the initially selected radio button is now not selected.
                            Assert.False(selectedRadioButton.IsChecked)
                            VerifySingleChangeWhenOptionChangeInUI(automationObject, Sub() selectedRadioButton.IsChecked = True, optionService, optionStore, optionForAssertMessage:=selectedRadioButton.Name)
                        End If
                    Next
                Next

                ' Above checks that all options are in AutomationObjects.
                ' TODO: check that all automation object members are in options.
            End Using
        End Sub

        Protected MustOverride Function GetNewValue(value As Object, propertyName As String) As Object
        Protected MustOverride Function CreateAutomationObject(workspace As TestWorkspace) As AbstractAutomationObject
        Protected MustOverride Function CreateWorkspace(Optional composition As TestComposition = Nothing) As TestWorkspace
        Protected MustOverride Function CreatePageControls(optionStore As OptionStore, workspace As TestWorkspace) As IEnumerable(Of AbstractOptionPageControl)

        <Export(GetType(IColorSchemeApplier)), [Shared], PartNotDiscoverable>
        Private Class TestColorSchemeApplier
            Implements IColorSchemeApplier

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function IsSupportedThemeAsync(cancellationToken As CancellationToken) As Task(Of Boolean) Implements IColorSchemeApplier.IsSupportedThemeAsync
                Return Task.FromResult(True)
            End Function

            Public Function IsThemeCustomizedAsync(cancellationToken As CancellationToken) As Task(Of Boolean) Implements IColorSchemeApplier.IsThemeCustomizedAsync
                Return Task.FromResult(True)
            End Function
        End Class
    End Class
End Namespace
