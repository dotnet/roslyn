﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
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

        private string GetText(AbstractOptionPreviewViewModel viewModel)
        {
            return viewModel.TextViewHost.TextView.TextBuffer.CurrentSnapshot.GetText().ToString();
        }

        public OptionViewModelTests()
        {
            WpfTestCase.RequireWpfFact("Tests create WPF ViewModels and updates previews with them");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Options)]
        public void TestCheckBox()
        {
            using (var workspace = TestWorkspace.CreateCSharp(""))
            {
                var serviceProvider = new MockServiceProvider(workspace.ExportProvider);
                using (var viewModel = new SpacingViewModel(workspace.Options, serviceProvider))
                {
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
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Options)]
        public void TestOptionLoading()
        {
            using (var workspace = TestWorkspace.CreateCSharp(""))
            {
                var optionSet = workspace.Options.WithChangedOption(CSharpFormattingOptions.SpacingAfterMethodDeclarationName, true);

                var serviceProvider = new MockServiceProvider(workspace.ExportProvider);
                using (var viewModel = new SpacingViewModel(optionSet, serviceProvider))
                {
                    // Use the first item's preview.
                    var checkbox = viewModel.Items.OfType<CheckBoxOptionViewModel>().Where(c => c.Option == CSharpFormattingOptions.SpacingAfterMethodDeclarationName).First();
                    Assert.True(checkbox.IsChecked);
                }
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Options)]
        public void TestOptionSaving()
        {
            using (var workspace = TestWorkspace.CreateCSharp(""))
            {
                var serviceProvider = new MockServiceProvider(workspace.ExportProvider);
                using (var viewModel = new SpacingViewModel(workspace.Options, serviceProvider))
                {
                    // Use the first item's preview.
                    var checkbox = viewModel.Items.OfType<CheckBoxOptionViewModel>().Where(c => c.Option == CSharpFormattingOptions.SpacingAfterMethodDeclarationName).First();
                    var initial = checkbox.IsChecked;
                    checkbox.IsChecked = !checkbox.IsChecked;

                    var changedOptions = viewModel.ApplyChangedOptions(workspace.Options);
                    Assert.NotEqual(changedOptions.GetOption(CSharpFormattingOptions.SpacingAfterMethodDeclarationName), initial);
                }
            }
        }
    }
}
