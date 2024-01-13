' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Roslyn.Test.Utilities
Imports IVsAsyncFileChangeEx2 = Microsoft.VisualStudio.Shell.IVsAsyncFileChangeEx2

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.Diagnostics)>
    Public Class VisualStudioRuleSetTests
        Implements IDisposable

        Private ReadOnly _tempPath As String

        Public Sub New()
            _tempPath = Path.Combine(TempRoot.Root, Path.GetRandomFileName())
            Directory.CreateDirectory(_tempPath)
        End Sub

        Private Sub Dispose() Implements IDisposable.Dispose
            Directory.Delete(_tempPath, recursive:=True)
        End Sub

        <WpfFact>
        Public Async Function SingleFile() As Task
            Dim ruleSetSource = "<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""New Rule Set3"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA2100"" Action=""Warning"" />
    <Rule Id=""CA2111"" Action=""Warning"" />
    <Rule Id=""CA2119"" Action=""None"" />
    <Rule Id=""CA2104"" Action=""Error"" />
    <Rule Id=""CA2105"" Action=""Warning"" />
  </Rules>
</RuleSet>"

            Using workspace = New EditorTestWorkspace()
                Dim ruleSetPath As String = Path.Combine(_tempPath, "a.ruleset")
                File.WriteAllText(ruleSetPath, ruleSetSource)

                Dim fileChangeService = New MockVsFileChangeEx
                Dim fileChangeWatcher = New FileChangeWatcher(workspace.GetService(Of IAsynchronousOperationListenerProvider)(), Task.FromResult(Of IVsAsyncFileChangeEx2)(fileChangeService))
                Dim ruleSetManager = New VisualStudioRuleSetManager(workspace.ExportProvider.GetExportedValue(Of IThreadingContext), fileChangeWatcher, AsynchronousOperationListenerProvider.NullListener)
                Using visualStudioRuleSet = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                    ' Signing up for file change notifications is lazy, so read the rule set to force it.
                    Dim generalDiagnosticOption = visualStudioRuleSet.Target.Value.GetGeneralDiagnosticOption()

                    Await workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync()
                    Assert.Equal(expected:=1, actual:=fileChangeService.WatchedFileCount)
                End Using

                Await workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync()
                Assert.Equal(expected:=0, actual:=fileChangeService.WatchedFileCount)
            End Using
        End Function

        <WpfFact>
        Public Async Function TwoFiles() As Task
            Dim ruleSetSource = "<?xml version=""1.0"" encoding=""utf-8""?>
        <RuleSet Name=""New Rule Set1"" Description=""Test"" ToolsVersion=""12.0"">
          <Include Path=""file1.ruleset"" Action=""Error"" />
          <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
            <Rule Id=""CA1000"" Action=""Warning"" />
            <Rule Id=""CA1001"" Action=""Warning"" />
            <Rule Id=""CA2111"" Action=""None"" />
          </Rules>
        </RuleSet>"

            Dim includeSource = "<?xml version=""1.0"" encoding=""utf-8""?>
        <RuleSet Name=""New Rule Set2"" Description=""Test"" ToolsVersion=""12.0"">
          <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
            <Rule Id=""CA2100"" Action=""Warning"" />
            <Rule Id=""CA2111"" Action=""Warning"" />
          </Rules>
        </RuleSet>"

            Dim ruleSetPath As String = Path.Combine(_tempPath, "a.ruleset")
            File.WriteAllText(ruleSetPath, ruleSetSource)

            Dim includePath As String = Path.Combine(_tempPath, "file1.ruleset")
            File.WriteAllText(includePath, includeSource)

            Using workspace = New EditorTestWorkspace()
                Dim fileChangeService = New MockVsFileChangeEx
                Dim fileChangeWatcher = New FileChangeWatcher(workspace.GetService(Of IAsynchronousOperationListenerProvider)(), Task.FromResult(Of IVsAsyncFileChangeEx2)(fileChangeService))
                Dim ruleSetManager = New VisualStudioRuleSetManager(workspace.ExportProvider.GetExportedValue(Of IThreadingContext), fileChangeWatcher, AsynchronousOperationListenerProvider.NullListener)
                Using visualStudioRuleSet = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                    ' Signing up for file change notifications is lazy, so read the rule set to force it.
                    Dim generalDiagnosticOption = visualStudioRuleSet.Target.Value.GetGeneralDiagnosticOption()

                    Await workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync()
                    Assert.Equal(expected:=2, actual:=fileChangeService.WatchedFileCount)
                End Using

                Await workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync()
                Assert.Equal(expected:=0, actual:=fileChangeService.WatchedFileCount)
            End Using
        End Function

        <WpfFact>
        Public Async Function IncludeUpdated() As Task
            Dim ruleSetSource = "<?xml version=""1.0"" encoding=""utf-8""?>
        <RuleSet Name=""New Rule Set1"" Description=""Test"" ToolsVersion=""12.0"">
          <Include Path=""file1.ruleset"" Action=""Error"" />
          <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
            <Rule Id=""CA1000"" Action=""Warning"" />
            <Rule Id=""CA1001"" Action=""Warning"" />
            <Rule Id=""CA2111"" Action=""None"" />
          </Rules>
        </RuleSet>"

            Dim includeSource = "<?xml version=""1.0"" encoding=""utf-8""?>
        <RuleSet Name=""New Rule Set2"" Description=""Test"" ToolsVersion=""12.0"">
          <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
            <Rule Id=""CA2100"" Action=""Warning"" />
            <Rule Id=""CA2111"" Action=""Warning"" />
          </Rules>
        </RuleSet>"

            Dim ruleSetPath As String = Path.Combine(_tempPath, "a.ruleset")
            File.WriteAllText(ruleSetPath, ruleSetSource)

            Dim includePath As String = Path.Combine(_tempPath, "file1.ruleset")
            File.WriteAllText(includePath, includeSource)

            Using workspace = New EditorTestWorkspace()
                Dim fileChangeService = New MockVsFileChangeEx
                Dim fileChangeWatcher = New FileChangeWatcher(workspace.GetService(Of IAsynchronousOperationListenerProvider)(), Task.FromResult(Of IVsAsyncFileChangeEx2)(fileChangeService))

                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)
                Dim listener = listenerProvider.GetListener("test")

                Dim ruleSetManager = New VisualStudioRuleSetManager(workspace.ExportProvider.GetExportedValue(Of IThreadingContext), fileChangeWatcher, listener)
                Using ruleSet1 = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)
                    Dim handlerCalled As Boolean = False
                    AddHandler ruleSet1.Target.Value.UpdatedOnDisk, Sub() handlerCalled = True

                    ' Signing up for file change notifications is lazy, so read the rule set to force it.
                    Dim generalDiagnosticOption = ruleSet1.Target.Value.GetGeneralDiagnosticOption()

                    Await workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync()
                    fileChangeService.FireUpdate(includePath)

                    Await listenerProvider.GetWaiter("test").ExpeditedWaitAsync()

                    Assert.True(handlerCalled)
                End Using
            End Using
        End Function

        <WpfFact>
        Public Async Function SameFileRequestedAfterChange() As Task
            Dim ruleSetSource = "<?xml version=""1.0"" encoding=""utf-8""?>
        <RuleSet Name=""New Rule Set3"" Description=""Test"" ToolsVersion=""12.0"">
          <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
            <Rule Id=""CA2100"" Action=""Warning"" />
            <Rule Id=""CA2111"" Action=""Warning"" />
            <Rule Id=""CA2119"" Action=""None"" />
            <Rule Id=""CA2104"" Action=""Error"" />
            <Rule Id=""CA2105"" Action=""Warning"" />
          </Rules>
        </RuleSet>"

            Dim ruleSetPath As String = Path.Combine(_tempPath, "a.ruleset")
            File.WriteAllText(ruleSetPath, ruleSetSource)

            Using workspace = New EditorTestWorkspace()
                Dim fileChangeService = New MockVsFileChangeEx
                Dim fileChangeWatcher = New FileChangeWatcher(workspace.GetService(Of IAsynchronousOperationListenerProvider)(), Task.FromResult(Of IVsAsyncFileChangeEx2)(fileChangeService))

                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)
                Dim listener = listenerProvider.GetListener("test")

                Dim ruleSetManager = New VisualStudioRuleSetManager(workspace.ExportProvider.GetExportedValue(Of IThreadingContext), fileChangeWatcher, listener)
                Using ruleSet1 = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                    ' Signing up for file change notifications is lazy, so read the rule set to force it.
                    Dim generalDiagnosticOption = ruleSet1.Target.Value.GetGeneralDiagnosticOption()
                    Await workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync()
                    fileChangeService.FireUpdate(ruleSetPath)

                    Await listenerProvider.GetWaiter("test").ExpeditedWaitAsync()

                    Using ruleSet2 = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                        ' Signing up for file change notifications is lazy, so read the rule set to force it.
                        generalDiagnosticOption = ruleSet2.Target.Value.GetGeneralDiagnosticOption()

                        Await workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync()
                        Assert.Equal(expected:=1, actual:=fileChangeService.WatchedFileCount)
                        Assert.NotSame(ruleSet1.Target, ruleSet2.Target)
                    End Using
                End Using

                Await workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync()
                Assert.Equal(expected:=0, actual:=fileChangeService.WatchedFileCount)
            End Using
        End Function

        <WpfFact>
        Public Async Function SameFileRequestedMultipleTimes() As Task
            Dim ruleSetSource = "<?xml version=""1.0"" encoding=""utf-8""?>
        <RuleSet Name=""New Rule Set3"" Description=""Test"" ToolsVersion=""12.0"">
          <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
            <Rule Id=""CA2100"" Action=""Warning"" />
            <Rule Id=""CA2111"" Action=""Warning"" />
            <Rule Id=""CA2119"" Action=""None"" />
            <Rule Id=""CA2104"" Action=""Error"" />
            <Rule Id=""CA2105"" Action=""Warning"" />
          </Rules>
        </RuleSet>"

            Dim ruleSetPath As String = Path.Combine(_tempPath, "a.ruleset")
            File.WriteAllText(ruleSetPath, ruleSetSource)

            Using workspace = New EditorTestWorkspace()
                Dim fileChangeService = New MockVsFileChangeEx
                Dim fileChangeWatcher = New FileChangeWatcher(workspace.GetService(Of IAsynchronousOperationListenerProvider)(), Task.FromResult(Of IVsAsyncFileChangeEx2)(fileChangeService))
                Dim ruleSetManager = New VisualStudioRuleSetManager(workspace.ExportProvider.GetExportedValue(Of IThreadingContext), fileChangeWatcher, AsynchronousOperationListenerProvider.NullListener)
                Using ruleSet1 = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                    ' Signing up for file change notifications is lazy, so read the rule set to force it.
                    Dim generalDiagnosticOption = ruleSet1.Target.Value.GetGeneralDiagnosticOption()

                    Using ruleSet2 = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                        Await workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync()
                        Assert.Equal(expected:=1, actual:=fileChangeService.WatchedFileCount)
                        Assert.Same(ruleSet1.Target, ruleSet2.Target)
                    End Using
                End Using

                Await workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync()
                Assert.Equal(expected:=0, actual:=fileChangeService.WatchedFileCount)
            End Using
        End Function

        <WpfFact>
        Public Async Function FileWithError() As Task
            Dim ruleSetSource = "<?xml version=""1.0"" encoding=""utf-8""?>
        <RuleSet Name=""New Rule Set3"" Description=""Test"" ToolsVersion=""12.0"">
          <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
            <Rule Id=""CA2100"" Action=""Warning"" />
            <Rule Id=""CA2111"" Action=""Warning"" />
            <Rule Id=""CA2119"" Action=""None"" />
            <Rule Id=""CA2104"" Action=""Error"" />
            <Rule Id=""CA2105"" Action=""BlahBlahBlah"" />
          </Rules>
        </RuleSet>"

            Dim ruleSetPath As String = Path.Combine(_tempPath, "a.ruleset")
            File.WriteAllText(ruleSetPath, ruleSetSource)

            Using workspace = New EditorTestWorkspace()
                Dim fileChangeService = New MockVsFileChangeEx
                Dim fileChangeWatcher = New FileChangeWatcher(workspace.GetService(Of IAsynchronousOperationListenerProvider)(), Task.FromResult(Of IVsAsyncFileChangeEx2)(fileChangeService))
                Dim ruleSetManager = New VisualStudioRuleSetManager(workspace.ExportProvider.GetExportedValue(Of IThreadingContext), fileChangeWatcher, AsynchronousOperationListenerProvider.NullListener)
                Using ruleSet = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                    Dim generalDiagnosticOption = ruleSet.Target.Value.GetGeneralDiagnosticOption()
                    Await workspace.GetService(Of AsynchronousOperationListenerProvider)().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync()

                    Assert.Equal(expected:=ReportDiagnostic.Default, actual:=generalDiagnosticOption)

                    Dim exception = ruleSet.Target.Value.GetException()
                    Assert.NotNull(exception)
                End Using
            End Using
        End Function
    End Class
End Namespace
