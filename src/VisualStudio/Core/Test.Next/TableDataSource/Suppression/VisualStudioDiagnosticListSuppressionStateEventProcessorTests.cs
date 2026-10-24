// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;
using Microsoft.VisualStudio.Shell.TableControl;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.TableDataSource.Suppression;

public sealed class VisualStudioDiagnosticListSuppressionStateEventProcessorTests
{
    [WpfFact]
    public void RefreshesSelectionWhenEntriesChange()
    {
        IEnumerable<ITableEntryHandle> selectedEntries = [];
        var tableControl = new Mock<IWpfTableControl>(MockBehavior.Strict);
        tableControl.SetupGet(control => control.SelectedEntries).Returns(() => selectedEntries);
        var suppressionStateService = new Mock<IVisualStudioDiagnosticListSuppressionStateService>(MockBehavior.Strict);
        suppressionStateService
            .Setup(service => service.RefreshSelection(It.IsAny<IEnumerable<ITableEntryHandle>>()));

        _ = new VisualStudioDiagnosticListSuppressionStateEventProcessor(tableControl.Object, suppressionStateService.Object);

        suppressionStateService.Verify(service => service.RefreshSelection(selectedEntries), Times.Once);

        selectedEntries = [Mock.Of<ITableEntryHandle>()];
        tableControl.Raise(
            control => control.EntriesChanged += null,
            new Mock<EntriesChangedEventArgs>(MockBehavior.Loose, 0).Object);

        suppressionStateService.Verify(service => service.RefreshSelection(selectedEntries), Times.Once);
    }

    [WpfFact]
    public void ForwardsSelectionChanges()
    {
        var tableControl = new Mock<IWpfTableControl>(MockBehavior.Strict);
        tableControl.SetupGet(control => control.SelectedEntries).Returns([]);
        var suppressionStateService = new Mock<IVisualStudioDiagnosticListSuppressionStateService>(MockBehavior.Strict);
        suppressionStateService
            .Setup(service => service.RefreshSelection(It.IsAny<IEnumerable<ITableEntryHandle>>()));

        var processor = new VisualStudioDiagnosticListSuppressionStateEventProcessor(
            tableControl.Object,
            suppressionStateService.Object);
        var eventArgs = new TableSelectionChangedEventArgs(
            new SelectionChangedEventArgs(
                Selector.SelectionChangedEvent,
                removedItems: Array.Empty<object>(),
                addedItems: Array.Empty<object>()));
        suppressionStateService.Setup(service => service.ProcessSelectionChanged(eventArgs));

        processor.PostprocessSelectionChanged(eventArgs);

        suppressionStateService.Verify(service => service.ProcessSelectionChanged(eventArgs), Times.Once);
    }
}
