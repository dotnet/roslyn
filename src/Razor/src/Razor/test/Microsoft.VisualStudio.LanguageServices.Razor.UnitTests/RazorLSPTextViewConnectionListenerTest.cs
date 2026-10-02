// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.VisualStudio;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Razor.LanguageClient;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor;

public class RazorLSPTextViewConnectionListenerTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [UITheory]
    [InlineData(false, false, false, false)]
    [InlineData(false, false, false, true)]
    [InlineData(false, false, true, false)]
    [InlineData(false, false, true, true)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, false, true)]
    [InlineData(false, true, true, false)]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, false, true)]
    [InlineData(true, false, true, false)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    [InlineData(true, true, true, true)]
    public void SubjectBuffersConnected_AdaptersAreOptional(bool hasViewAdapter, bool hasBufferAdapter, bool isRemoteClient, bool isRazorBuffer)
    {
        var textBuffer = VsMocks.CreateTextBuffer(isRazorBuffer ? VsMocks.ContentTypes.RazorLSP : VsMocks.ContentTypes.NonRazor);
        var dataBuffer = isRazorBuffer ? textBuffer : VsMocks.CreateTextBuffer(VsMocks.ContentTypes.RazorLSP);
        var textViewModel = StrictMock.Of<ITextViewModel>(m => m.DataBuffer == dataBuffer);
        var textView = StrictMock.Of<ITextView>(v =>
            v.TextBuffer == textBuffer &&
            v.TextViewModel == textViewModel);

        var languageServiceId = RazorConstants.RazorLanguageServiceGuid;
        var vsBuffer = new StrictMock<IVsTextBuffer>();
        vsBuffer.Setup(b => b.SetLanguageServiceID(ref languageServiceId))
            .Returns(VSConstants.S_OK);

        var next = StrictMock.Of<IOleCommandTarget>();
        var vsTextView = new StrictMock<IVsTextView>();
        vsTextView.Setup(v => v.AddCommandFilter(It.IsAny<IOleCommandTarget>(), out next))
            .Returns(VSConstants.S_OK);

        var editorAdaptersFactory = new StrictMock<IVsEditorAdaptersFactoryService>();
        editorAdaptersFactory.Setup(f => f.GetBufferAdapter(dataBuffer))
            .Returns(hasBufferAdapter ? vsBuffer.Object : null);
        editorAdaptersFactory.Setup(f => f.GetViewAdapter(textView))
            .Returns(hasViewAdapter ? vsTextView.Object : null);

        var editorFeatureDetector = new StrictMock<ILspEditorFeatureDetector>();
        editorFeatureDetector.Setup(d => d.IsRemoteClient())
            .Returns(isRemoteClient);

        var viewOptions = CreateEditorOptions();
        var bufferOptions = CreateEditorOptions();
        var editorOptionsFactory = new StrictMock<IEditorOptionsFactoryService>();
        editorOptionsFactory.Setup(f => f.GetOptions(textView))
            .Returns(viewOptions.Object);
        editorOptionsFactory.Setup(f => f.GetOptions(textBuffer))
            .Returns(bufferOptions.Object);

        var spaceSettings = new ClientSpaceSettings(IndentWithTabs: true, 8);
        var completionSettings = new ClientCompletionSettings(true, true);
        var editorSettingsManager = new StrictMock<IClientSettingsManager>();
        editorSettingsManager.Setup(m => m.Update(spaceSettings));
        editorSettingsManager.Setup(m => m.Update(completionSettings));

        var textManager = new StrictMock<IVsTextManager4>();
        textManager.Setup(m => m.GetUserPreferences4(null, It.IsAny<LANGPREFERENCES3[]>(), null))
            .Callback((VIEWPREFERENCES3[] _, LANGPREFERENCES3[] languagePreferences, FONTCOLORPREFERENCES2[] _) =>
            {
                languagePreferences[0].fInsertTabs = 1;
                languagePreferences[0].uTabSize = 8;
                languagePreferences[0].fAutoListMembers = 1;
                languagePreferences[0].fAutoListParams = 1;
            })
            .Returns(VSConstants.S_OK);

        var serviceProvider = VsMocks.CreateServiceProvider(b => b.AddService<SVsTextManager>(textManager.Object));
        var listener = new RazorLSPTextViewConnectionListener(
            serviceProvider,
            editorAdaptersFactory.Object,
            editorFeatureDetector.Object,
            editorOptionsFactory.Object,
            editorSettingsManager.Object,
            JoinableTaskContext,
            interceptedCommands: []);

        listener.SubjectBuffersConnected(textView, ConnectionReason.TextViewLifetime, [dataBuffer]);

        vsTextView.Verify(v => v.AddCommandFilter(It.IsAny<IOleCommandTarget>(), out next), hasViewAdapter ? Times.Once() : Times.Never());
        vsBuffer.Verify(b => b.SetLanguageServiceID(ref languageServiceId), hasBufferAdapter && !isRemoteClient ? Times.Once() : Times.Never());
        editorAdaptersFactory.Verify(f => f.GetBufferAdapter(dataBuffer), isRemoteClient ? Times.Never() : Times.Once());
        editorFeatureDetector.Verify(d => d.IsRemoteClient(), Times.Once());
        editorOptionsFactory.Verify(f => f.GetOptions(textView), isRazorBuffer ? Times.Once() : Times.Never());
        editorOptionsFactory.Verify(f => f.GetOptions(textBuffer), isRazorBuffer ? Times.Once() : Times.Never());
        viewOptions.Verify(o => o.SetOptionValue(DefaultTextViewOptions.WordWrapStyleName, WordWrapStyles.None), isRazorBuffer ? Times.Once() : Times.Never());

        if (isRazorBuffer)
        {
            Assert.Equal(
                RazorLSPConstants.RoslynLanguageServerName,
                textBuffer.Properties[RazorLSPConstants.WebToolsWrapWithTagServerNameProperty]);
        }
        else
        {
            Assert.False(textBuffer.Properties.ContainsProperty(RazorLSPConstants.WebToolsWrapWithTagServerNameProperty));
        }

        var optionChanged = new EditorOptionChangedEventArgs(DefaultOptions.TabSizeOptionId.Name);
        viewOptions.Raise(o => o.OptionChanged += null, optionChanged);

        listener.SubjectBuffersDisconnected(textView, ConnectionReason.TextViewLifetime, [dataBuffer]);

        viewOptions.Raise(o => o.OptionChanged += null, optionChanged);

        var updates = isRazorBuffer ? Times.Exactly(2) : Times.Never();
        editorSettingsManager.Verify(m => m.Update(spaceSettings), updates);
        editorSettingsManager.Verify(m => m.Update(completionSettings), updates);
        viewOptions.Verify(o => o.SetOptionValue(DefaultOptions.TabSizeOptionId, 8), updates);
        viewOptions.Verify(o => o.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, false), updates);
        bufferOptions.Verify(o => o.SetOptionValue(DefaultOptions.TabSizeOptionId, 8), updates);
        bufferOptions.Verify(o => o.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, false), updates);
    }

    private static StrictMock<IEditorOptions> CreateEditorOptions()
    {
        var options = new StrictMock<IEditorOptions>();
        options.Setup(o => o.SetOptionValue(It.IsAny<string>(), It.IsAny<bool>()));
        options.Setup(o => o.SetOptionValue(It.IsAny<string>(), It.IsAny<int>()));
        options.Setup(o => o.SetOptionValue(It.IsAny<string>(), It.IsAny<WordWrapStyles>()));
        options.Setup(o => o.SetOptionValue(It.IsAny<EditorOptionKey<bool>>(), It.IsAny<bool>()));
        options.Setup(o => o.SetOptionValue(It.IsAny<EditorOptionKey<int>>(), It.IsAny<int>()));
        return options;
    }
}
