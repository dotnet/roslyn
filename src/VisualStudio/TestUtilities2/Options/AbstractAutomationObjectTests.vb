' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options

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

        Protected MustOverride Function GetNewValue(value As Object, propertyName As String) As Object
        Protected MustOverride Function CreateAutomationObject(workspace As TestWorkspace) As AbstractAutomationObject
        Protected MustOverride Function CreateWorkspace() As TestWorkspace
    End Class
End Namespace
