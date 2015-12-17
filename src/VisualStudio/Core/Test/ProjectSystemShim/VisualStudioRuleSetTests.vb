' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    Public Class VisualStudioRuleSetTests

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
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

            Dim tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
            Directory.CreateDirectory(tempPath)

            Dim ruleSetPath As String = Path.Combine(tempPath, "a.ruleset")
            File.WriteAllText(ruleSetPath, ruleSetSource)

            Dim fileChangeService = New MockVsFileChangeEx
            Using ruleSetManager = New VisualStudioRuleSetManager(fileChangeService, New TestForegroundNotificationService(), AggregateAsynchronousOperationListener.CreateEmptyListener())
                Dim visualStudioRuleSet = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                ' Signing up for file change notifications is lazy, so read the rule set to force it.
                Dim generalDiagnosticOption = visualStudioRuleSet.GetGeneralDiagnosticOption()

                Assert.Equal(expected:=1, actual:=fileChangeService.WatchedFileCount)
            End Using

            Assert.Equal(expected:=0, actual:=fileChangeService.WatchedFileCount)
            GC.Collect()
            GC.WaitForPendingFinalizers()
            Directory.Delete(tempPath, recursive:=True)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
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

            Dim tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
            Directory.CreateDirectory(tempPath)

            Dim ruleSetPath As String = Path.Combine(tempPath, "a.ruleset")
            File.WriteAllText(ruleSetPath, ruleSetSource)

            Dim includePath As String = Path.Combine(tempPath, "file1.ruleset")
            File.WriteAllText(includePath, includeSource)

            Dim fileChangeService = New MockVsFileChangeEx
            Using ruleSetManager = New VisualStudioRuleSetManager(fileChangeService, New TestForegroundNotificationService(), AggregateAsynchronousOperationListener.CreateEmptyListener())
                Dim visualStudioRuleSet = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                ' Signing up for file change notifications is lazy, so read the rule set to force it.
                Dim generalDiagnosticOption = visualStudioRuleSet.GetGeneralDiagnosticOption()

                Assert.Equal(expected:=2, actual:=fileChangeService.WatchedFileCount)
            End Using

            Assert.Equal(expected:=0, actual:=fileChangeService.WatchedFileCount)

            GC.Collect()
            GC.WaitForPendingFinalizers()

            Directory.Delete(tempPath, recursive:=True)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
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

            Dim tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
            Directory.CreateDirectory(tempPath)

            Dim ruleSetPath As String = Path.Combine(tempPath, "a.ruleset")
            File.WriteAllText(ruleSetPath, ruleSetSource)

            Dim includePath As String = Path.Combine(tempPath, "file1.ruleset")
            File.WriteAllText(includePath, includeSource)

            Dim fileChangeService = New MockVsFileChangeEx
            Using ruleSetManager = New VisualStudioRuleSetManager(fileChangeService, New TestForegroundNotificationService(), AggregateAsynchronousOperationListener.CreateEmptyListener())
                Dim ruleSet1 = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)
                Dim handlerCalled As Boolean = False
                Dim handler = Sub(sender As Object, e As EventArgs)
                                  handlerCalled = True
                              End Sub
                AddHandler ruleSet1.UpdatedOnDisk, handler

                ' Signing up for file change notifications is lazy, so read the rule set to force it.
                Dim generalDiagnosticOption = ruleSet1.GetGeneralDiagnosticOption()

                fileChangeService.FireUpdate(includePath)
                Assert.True(handlerCalled)

                RemoveHandler ruleSet1.UpdatedOnDisk, handler
            End Using
            GC.Collect()
            GC.WaitForPendingFinalizers()

            Directory.Delete(tempPath, recursive:=True)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
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

            Dim tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
            Directory.CreateDirectory(tempPath)

            Dim ruleSetPath As String = Path.Combine(tempPath, "a.ruleset")
            File.WriteAllText(ruleSetPath, ruleSetSource)

            Dim fileChangeService = New MockVsFileChangeEx
            Using ruleSetManager = New VisualStudioRuleSetManager(fileChangeService, New TestForegroundNotificationService(), AggregateAsynchronousOperationListener.CreateEmptyListener())
                Dim ruleSet1 = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                ' Signing up for file change notifications is lazy, so read the rule set to force it.
                Dim generalDiagnosticOption = ruleSet1.GetGeneralDiagnosticOption()
                fileChangeService.FireUpdate(ruleSetPath)

                Dim ruleSet2 = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                ' Signing up for file change notifications is lazy, so read the rule set to force it.
                generalDiagnosticOption = ruleSet2.GetGeneralDiagnosticOption()

                Assert.Equal(expected:=1, actual:=fileChangeService.WatchedFileCount)
                Assert.False(Object.ReferenceEquals(ruleSet1, ruleSet2))
            End Using

            Assert.Equal(expected:=0, actual:=fileChangeService.WatchedFileCount)
            GC.Collect()
            GC.WaitForPendingFinalizers()

            Directory.Delete(tempPath, recursive:=True)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
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

            Dim tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
            Directory.CreateDirectory(tempPath)

            Dim ruleSetPath As String = Path.Combine(tempPath, "a.ruleset")
            File.WriteAllText(ruleSetPath, ruleSetSource)

            Dim fileChangeService = New MockVsFileChangeEx
            Using ruleSetManager = New VisualStudioRuleSetManager(fileChangeService, New TestForegroundNotificationService(), AggregateAsynchronousOperationListener.CreateEmptyListener())
                Dim ruleSet1 = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                ' Signing up for file change notifications is lazy, so read the rule set to force it.
                Dim generalDiagnosticOption = ruleSet1.GetGeneralDiagnosticOption()

                Dim ruleSet2 = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                Assert.Equal(expected:=1, actual:=fileChangeService.WatchedFileCount)
                Assert.Same(ruleSet1, ruleSet2)
            End Using

            Assert.Equal(expected:=0, actual:=fileChangeService.WatchedFileCount)

            Directory.Delete(tempPath, recursive:=True)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
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

            Dim tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
            Directory.CreateDirectory(tempPath)

            Dim ruleSetPath As String = Path.Combine(tempPath, "a.ruleset")
            File.WriteAllText(ruleSetPath, ruleSetSource)

            Dim fileChangeService = New MockVsFileChangeEx
            Using ruleSetManager = New VisualStudioRuleSetManager(fileChangeService, New TestForegroundNotificationService(), AggregateAsynchronousOperationListener.CreateEmptyListener())
                Dim ruleSet = ruleSetManager.GetOrCreateRuleSet(ruleSetPath)

                Dim generalDiagnosticOption = ruleSet.GetGeneralDiagnosticOption()

                Assert.Equal(expected:=ReportDiagnostic.Default, actual:=generalDiagnosticOption)

                Dim exception = ruleSet.GetException()
                Assert.NotNull(exception)
            End Using

            Directory.Delete(tempPath, recursive:=True)
        End Sub

    End Class
End Namespace

