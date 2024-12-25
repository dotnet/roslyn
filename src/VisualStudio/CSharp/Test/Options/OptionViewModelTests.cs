// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.Options
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.Options)]
    public class OptionViewModelTests
    {
        private class MockServiceProvider : IServiceProvider
        {
            private readonly MockComponentModel _componentModel;

            public MockServiceProvider(ExportProvider exportProvider)
            {
                _componentModel = new MockComponentModel(exportProvider);
            }

            public object GetService(Type serviceType)
            {
                return _componentModel;
            }
        }

        private static string GetText(AbstractOptionPreviewViewModel viewModel)
        {
            return viewModel.TextViewHost.TextView.TextBuffer.CurrentSnapshot.GetText().ToString();
        }

        public OptionViewModelTests()
        {
            WpfTestRunner.RequireWpfFact("Tests create WPF ViewModels and updates previews with them");
        }

        [WpfFact]
        public void TestCheckBox()
        {
            using var workspace = EditorTestWorkspace.CreateCSharp("");
            var serviceProvider = new MockServiceProvider(workspace.ExportProvider);
            var optionStore = new OptionStore(workspace.GlobalOptions);
            using var viewModel = new SpacingViewModel(optionStore, serviceProvider);
            // Use the first item's preview.
            var checkbox = viewModel.Items.OfType<CheckBoxOptionViewModel>().First();
            viewModel.SetOptionAndUpdatePreview(checkbox.IsChecked, checkbox.Option, checkbox.GetPreview());

            // Get a checkbox and toggle it
            var originalPreview = GetText(viewModel);

            checkbox.IsChecked = !checkbox.IsChecked;
            var modifiedPreview = GetText(viewModel);
            Assert.NotEqual(modifiedPreview, originalPreview);

            // Switch it back
            checkbox.IsChecked = !checkbox.IsChecked;
            Assert.Equal(originalPreview, viewModel.TextViewHost.TextView.TextBuffer.CurrentSnapshot.GetText().ToString());
        }

        [WpfFact]
        public void TestOptionLoading()
        {
            using var workspace = EditorTestWorkspace.CreateCSharp("");
            var optionStore = new OptionStore(workspace.GlobalOptions);
            workspace.GlobalOptions.SetGlobalOption(CSharpFormattingOptions2.SpacingAfterMethodDeclarationName, true);

            var serviceProvider = new MockServiceProvider(workspace.ExportProvider);
            using var viewModel = new SpacingViewModel(optionStore, serviceProvider);
            // Use the first item's preview.
            var checkbox = viewModel.Items.OfType<CheckBoxOptionViewModel>().Where(c => c.Option == CSharpFormattingOptions2.SpacingAfterMethodDeclarationName).First();
            Assert.True(checkbox.IsChecked);
        }

        [WpfFact]
        public void TestOptionSaving()
        {
            using var workspace = EditorTestWorkspace.CreateCSharp("");
            var serviceProvider = new MockServiceProvider(workspace.ExportProvider);
            var optionStore = new OptionStore(workspace.GlobalOptions);
            using var viewModel = new SpacingViewModel(optionStore, serviceProvider);
            // Use the first item's preview.
            var checkbox = viewModel.Items.OfType<CheckBoxOptionViewModel>().Where(c => c.Option == CSharpFormattingOptions2.SpacingAfterMethodDeclarationName).First();
            var initial = checkbox.IsChecked;
            checkbox.IsChecked = !checkbox.IsChecked;

            Assert.NotEqual(optionStore.GetOption(CSharpFormattingOptions2.SpacingAfterMethodDeclarationName), initial);
        }
    }
}
