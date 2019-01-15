' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports IVsAsyncFileChangeEx = Microsoft.VisualStudio.Shell.IVsAsyncFileChangeEx
Imports Microsoft.VisualStudio.Shell.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub SingleFile()
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

            Dim fileChangeService = New MockVsFileChangeEx
            Dim fileChangeWatcher = New FileChangeWatcher(Task.FromResult(Of IVsAsyncFileChangeEx)(fileChangeService))
            Dim ruleSetManager = New VisualStudioRuleSetManager(fileChangeWatcher, New TestForegroundNotificationService(), AsynchronousOperationListenerProvider.NullListener)
            Using visualStudioRuleSet = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                ' Signing up for file change notifications is lazy, so read the rule set to force it.
                Dim generalDiagnosticOption = visualStudioRuleSet.Target.Value.GetGeneralDiagnosticOption()

                fileChangeWatcher.WaitForQueue_TestOnly()
                Assert.Equal(expected:=1, actual:=fileChangeService.WatchedFileCount)
            End Using

            fileChangeWatcher.WaitForQueue_TestOnly()
            Assert.Equal(expected:=0, actual:=fileChangeService.WatchedFileCount)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TwoFiles()
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

            Dim fileChangeService = New MockVsFileChangeEx
            Dim fileChangeWatcher = New FileChangeWatcher(Task.FromResult(Of IVsAsyncFileChangeEx)(fileChangeService))
            Dim ruleSetManager = New VisualStudioRuleSetManager(fileChangeWatcher, New TestForegroundNotificationService(), AsynchronousOperationListenerProvider.NullListener)
            Using visualStudioRuleSet = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                ' Signing up for file change notifications is lazy, so read the rule set to force it.
                Dim generalDiagnosticOption = visualStudioRuleSet.Target.Value.GetGeneralDiagnosticOption()

                fileChangeWatcher.WaitForQueue_TestOnly()
                Assert.Equal(expected:=2, actual:=fileChangeService.WatchedFileCount)
            End Using

            fileChangeWatcher.WaitForQueue_TestOnly()
            Assert.Equal(expected:=0, actual:=fileChangeService.WatchedFileCount)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub IncludeUpdated()
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

            Dim fileChangeService = New MockVsFileChangeEx
            Dim fileChangeWatcher = New FileChangeWatcher(Task.FromResult(Of IVsAsyncFileChangeEx)(fileChangeService))
            Dim ruleSetManager = New VisualStudioRuleSetManager(fileChangeWatcher, New TestForegroundNotificationService(), AsynchronousOperationListenerProvider.NullListener)
            Using ruleSet1 = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)
                Dim handlerCalled As Boolean = False
                AddHandler ruleSet1.Target.Value.UpdatedOnDisk, Sub() handlerCalled = True

                ' Signing up for file change notifications is lazy, so read the rule set to force it.
                Dim generalDiagnosticOption = ruleSet1.Target.Value.GetGeneralDiagnosticOption()

                fileChangeWatcher.WaitForQueue_TestOnly()
                fileChangeService.FireUpdate(includePath)
                Assert.True(handlerCalled)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub SameFileRequestedAfterChange()
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

            Dim fileChangeService = New MockVsFileChangeEx
            Dim fileChangeWatcher = New FileChangeWatcher(Task.FromResult(Of IVsAsyncFileChangeEx)(fileChangeService))
            Dim ruleSetManager = New VisualStudioRuleSetManager(fileChangeWatcher, New TestForegroundNotificationService(), AsynchronousOperationListenerProvider.NullListener)
            Using ruleSet1 = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                ' Signing up for file change notifications is lazy, so read the rule set to force it.
                Dim generalDiagnosticOption = ruleSet1.Target.Value.GetGeneralDiagnosticOption()
                fileChangeWatcher.WaitForQueue_TestOnly()
                fileChangeService.FireUpdate(ruleSetPath)

                Using ruleSet2 = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                    ' Signing up for file change notifications is lazy, so read the rule set to force it.
                    generalDiagnosticOption = ruleSet2.Target.Value.GetGeneralDiagnosticOption()

                    fileChangeWatcher.WaitForQueue_TestOnly()
                    Assert.Equal(expected:=1, actual:=fileChangeService.WatchedFileCount)
                    Assert.NotSame(ruleSet1.Target, ruleSet2.Target)
                End Using
            End Using

            fileChangeWatcher.WaitForQueue_TestOnly()
            Assert.Equal(expected:=0, actual:=fileChangeService.WatchedFileCount)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub SameFileRequestedMultipleTimes()
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

            Dim fileChangeService = New MockVsFileChangeEx
            Dim fileChangeWatcher = New FileChangeWatcher(Task.FromResult(Of IVsAsyncFileChangeEx)(fileChangeService))
            Dim ruleSetManager = New VisualStudioRuleSetManager(fileChangeWatcher, New TestForegroundNotificationService(), AsynchronousOperationListenerProvider.NullListener)
            Using ruleSet1 = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                ' Signing up for file change notifications is lazy, so read the rule set to force it.
                Dim generalDiagnosticOption = ruleSet1.Target.Value.GetGeneralDiagnosticOption()

                Using ruleSet2 = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                    fileChangeWatcher.WaitForQueue_TestOnly()
                    Assert.Equal(expected:=1, actual:=fileChangeService.WatchedFileCount)
                    Assert.Same(ruleSet1.Target, ruleSet2.Target)
                End Using
            End Using

            fileChangeWatcher.WaitForQueue_TestOnly()
            Assert.Equal(expected:=0, actual:=fileChangeService.WatchedFileCount)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub FileWithError()
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

            Dim fileChangeService = New MockVsFileChangeEx
            Dim fileChangeWatcher = New FileChangeWatcher(Task.FromResult(Of IVsAsyncFileChangeEx)(fileChangeService))
            Dim ruleSetManager = New VisualStudioRuleSetManager(fileChangeWatcher, New TestForegroundNotificationService(), AsynchronousOperationListenerProvider.NullListener)
            Using ruleSet = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                Dim generalDiagnosticOption = ruleSet.Target.Value.GetGeneralDiagnosticOption()
                fileChangeWatcher.WaitForQueue_TestOnly()

                Assert.Equal(expected:=ReportDiagnostic.Default, actual:=generalDiagnosticOption)

                Dim exception = ruleSet.Target.Value.GetException()
                Assert.NotNull(exception)
            End Using
        End Sub

    End Class
End Namespace

