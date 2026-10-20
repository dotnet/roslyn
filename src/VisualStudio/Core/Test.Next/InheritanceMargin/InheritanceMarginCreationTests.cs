// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.FindUsages;
using Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.UnitTests.InheritanceMargin;

[UseExportProvider]
public sealed class InheritanceMarginCreationTests
{
    [WpfTheory]
    [InlineData(LanguageNames.CSharp, false)]
    [InlineData(LanguageNames.CSharp, true)]
    [InlineData(LanguageNames.VisualBasic, false)]
    [InlineData(LanguageNames.VisualBasic, true)]
    public async Task MarginAndTaggerSurviveDelayedDocumentAssociation(
        string languageName, bool associateBeforeViewCreation)
    {
        using var tempRoot = new TempRoot();
        // Avoid the native metadata-import service, which the VS test environment does not implement.
        var referencePath = tempRoot.CreateFile("mscorlib", "dll").Path;
        File.Copy(typeof(object).Assembly.Location, referencePath, overwrite: true);

        using var environment = new TestEnvironment(
            typeof(InheritanceMarginViewMarginProvider),
            typeof(InheritanceMarginTaggerProvider));

        var project = await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
            "project", languageName, CancellationToken.None);
        project.AddMetadataReference(referencePath, MetadataReferenceProperties.Assembly);
        var contentType = environment.Workspace.Services.GetLanguageServices(languageName)
            .GetRequiredService<IContentTypeLanguageService>().GetDefaultContentType();
        var source = languageName == LanguageNames.CSharp
            ? """
                class Base { }
                class Derived : Base { }
                """
            : """
                Class Base
                End Class
                Class Derived
                    Inherits Base
                End Class
                """;
        var textBuffer = environment.ExportProvider.GetExportedValue<ITextBufferFactoryService>()
            .CreateTextBuffer(source, contentType);

        var globalOptions = environment.ExportProvider.GetExportedValue<IGlobalOptionService>();
        globalOptions.SetGlobalOption(InheritanceMarginOptionsStorage.ShowInheritanceMargin, languageName, true);
        globalOptions.SetGlobalOption(
            InheritanceMarginOptionsStorage.ShowInheritanceMargin,
            languageName == LanguageNames.CSharp ? LanguageNames.VisualBasic : LanguageNames.CSharp,
            false);
        globalOptions.SetGlobalOption(InheritanceMarginOptionsStorage.InheritanceMarginCombinedWithIndicatorMargin, false);

        if (associateBeforeViewCreation)
            AssociateDocument();

        var editorFactory = environment.ExportProvider.GetExportedValue<ITextEditorFactoryService>();
        var textView = editorFactory.CreateTextView(
            textBuffer,
            editorFactory.CreateTextViewRoleSet(
                PredefinedTextViewRoles.Analyzable,
                PredefinedTextViewRoles.Document,
                PredefinedTextViewRoles.Editable,
                PredefinedTextViewRoles.Interactive,
                PredefinedTextViewRoles.Zoomable));
        try
        {
            var textViewHost = new Mock<IWpfTextViewHost>(MockBehavior.Strict);
            textViewHost.SetupGet(host => host.TextView).Returns(textView);

            var marginProvider = environment.ExportProvider.GetExports<IWpfTextViewMarginProvider, NameMetadata>()
                .Single(export => export.Metadata.Name == nameof(InheritanceMarginViewMarginProvider)).Value;
            var taggerProvider = environment.ExportProvider.GetExports<IViewTaggerProvider, NameMetadata>()
                .Single(export => export.Metadata.Name == nameof(InheritanceMarginTaggerProvider)).Value;

            var tagger = taggerProvider.CreateTagger<InheritanceMarginTag>(textView, textBuffer);
            using var taggerLifetime = (IDisposable?)tagger;
            using var margin = marginProvider.CreateMargin(textViewHost.Object, Mock.Of<IWpfTextViewMargin>());

            textView.VisualElement.Measure(new Size(800, 600));
            textView.VisualElement.Arrange(new Rect(0, 0, 800, 600));
            textView.VisualElement.UpdateLayout();
            textView.DisplayTextLineContainingBufferPosition(
                new SnapshotPoint(textBuffer.CurrentSnapshot, 0), 0, ViewRelativePosition.Top);
            Assert.NotNull(textView.TextViewLines);

            var listenerProvider = environment.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
            var waiter = listenerProvider.GetWaiter(FeatureAttribute.InheritanceMargin);
            await waiter.ExpeditedWaitAsync();

            if (!associateBeforeViewCreation)
            {
                AssociateDocument();
                await listenerProvider.WaitAllAsync(
                    environment.Workspace, [FeatureAttribute.Workspace, FeatureAttribute.InheritanceMargin]);
            }

            Assert.NotNull(textBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges());
            Assert.NotNull(margin);
            Assert.True(margin.Enabled);
            Assert.Equal(Visibility.Visible, margin.VisualElement.Visibility);
            Assert.NotNull(tagger);
            Assert.Contains(GetTargets(), target =>
                target.RelationToMember == InheritanceRelationship.DerivedType && target.DisplayName == "Derived");

            globalOptions.SetGlobalOption(InheritanceMarginOptionsStorage.ShowInheritanceMargin, languageName, false);
            await waiter.ExpeditedWaitAsync();
            Assert.Equal(Visibility.Collapsed, margin.VisualElement.Visibility);
            Assert.Empty(GetTargets());

            globalOptions.SetGlobalOption(InheritanceMarginOptionsStorage.ShowInheritanceMargin, languageName, true);
            await waiter.ExpeditedWaitAsync();
            Assert.Equal(Visibility.Visible, margin.VisualElement.Visibility);
            Assert.Contains(GetTargets(), target =>
                target.RelationToMember == InheritanceRelationship.DerivedType && target.DisplayName == "Derived");

            IEnumerable<InheritanceTargetItem> GetTargets()
                => tagger.GetTags(new NormalizedSnapshotSpanCollection(
                    new SnapshotSpan(textBuffer.CurrentSnapshot, 0, textBuffer.CurrentSnapshot.Length)))
                    .SelectMany(tag => tag.Tag.MembersOnLine)
                    .SelectMany(member => member.TargetItems);
        }
        finally
        {
            textView.Close();
        }

        void AssociateDocument()
            => project.AddVirtualDocument(
                textBuffer.AsTextContainer(),
                languageName == LanguageNames.CSharp ? @"Z:\Test.cs" : @"Z:\Test.vb",
                openDocument: true);
    }
}
