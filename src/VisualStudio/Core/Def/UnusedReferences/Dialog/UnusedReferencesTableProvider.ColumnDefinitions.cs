// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.UnusedReferences;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences.Dialog;

internal partial class UnusedReferencesTableProvider
{
    internal class ReferenceImageMonikers
    {
        // Change this to use KnownMonikers.NuGetNoColor once we are able to move to Microsoft.VisualStudio.ImageCatalog v16.9
        public static ImageMoniker Package => new() { Guid = KnownImageIds.ImageCatalogGuid, Id = 3902 };
        public static ImageMoniker Project => KnownMonikers.Application;
        public static ImageMoniker Assembly => KnownMonikers.Reference;
    }

    internal static class UnusedReferencesTableKeyNames
    {
        public const string SolutionName = "solutionname";
        public const string ProjectName = "projectname";
        public const string Language = "language";
        public const string ReferenceType = "referencetype";
        public const string ReferenceName = "referencename";
        public const string UpdateAction = "updateaction";
    }

    internal static class UnusedReferencesColumnDefinitions
    {
        private const string Prefix = "unusedreferences.";

        public const string SolutionName = Prefix + UnusedReferencesTableKeyNames.SolutionName;
        public const string ProjectName = Prefix + UnusedReferencesTableKeyNames.ProjectName;
        public const string ReferenceType = Prefix + UnusedReferencesTableKeyNames.ReferenceType;
        public const string ReferenceName = Prefix + UnusedReferencesTableKeyNames.ReferenceName;
        public const string UpdateAction = Prefix + UnusedReferencesTableKeyNames.UpdateAction;

        public static readonly ImmutableArray<string> ColumnNames = [SolutionName, ProjectName, ReferenceType, ReferenceName, UpdateAction];
    }

    /// <summary>
    /// Creates an element to display within the TableControl comprised of both an image and text string.
    /// </summary>
    internal static FrameworkElement CreateGridElement(ImageMoniker imageMoniker, string text, bool isBold)
    {
        var stackPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var block = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center
        };
        block.Inlines.Add(new Run(text)
        {
            FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal
        });

        if (!imageMoniker.IsNullImage())
        {
            // If we have an image and text, then create some space between them.
            block.Margin = new Thickness(5.0, 0.0, 0.0, 0.0);

            var image = new CrispImage
            {
                VerticalAlignment = VerticalAlignment.Center,
                Moniker = imageMoniker,
                Width = 16.0,
                Height = 16.0
            };

            stackPanel.Children.Add(image);
        }

        // Always add the textblock last so it can follow the image.
        stackPanel.Children.Add(block);

        return stackPanel;
    }

    private static ImageMoniker GetReferenceTypeImageMoniker(ReferenceType referenceType)
    {
        return referenceType switch
        {
            ReferenceType.Package => ReferenceImageMonikers.Package,
            ReferenceType.Project => ReferenceImageMonikers.Project,
            ReferenceType.Assembly => ReferenceImageMonikers.Assembly,
            _ => throw ExceptionUtilities.UnexpectedValue(referenceType)
        };
    }

    [Export(typeof(ITableColumnDefinition))]
    [Name(UnusedReferencesColumnDefinitions.SolutionName)]
    internal class SolutionNameColumnDefinition : TableColumnDefinitionBase
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SolutionNameColumnDefinition()
        {
        }

        public override string Name => UnusedReferencesColumnDefinitions.SolutionName;

        public override bool TryCreateColumnContent(ITableEntryHandle entry, bool singleColumnView, out FrameworkElement? content)
        {
            if (entry.TryGetValue(UnusedReferencesTableKeyNames.SolutionName, out string name))
            {
                content = CreateGridElement(KnownMonikers.Solution, name, isBold: false);
                return true;
            }

            content = null;
            return false;
        }

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string content)
        {
            return entry.TryGetValue(UnusedReferencesTableKeyNames.SolutionName, out content);
        }

        public override IEntryBucket? CreateBucketForEntry(ITableEntryHandle entry)
        {
            return entry.TryGetValue(UnusedReferencesTableKeyNames.SolutionName, out string name)
                ? new ImageEntryBucket(KnownMonikers.Solution, name)
                : null;
        }
    }

    [Export(typeof(ITableColumnDefinition))]
    [Name(UnusedReferencesColumnDefinitions.ProjectName)]
    internal class ProjectNameColumnDefinition : TableColumnDefinitionBase
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ProjectNameColumnDefinition()
        {
        }

        public override string Name => UnusedReferencesColumnDefinitions.ProjectName;

        public override bool TryCreateColumnContent(ITableEntryHandle entry, bool singleColumnView, out FrameworkElement? content)
        {
            if (entry.TryGetValue(UnusedReferencesTableKeyNames.ProjectName, out string name))
            {
                content = CreateGridElement(GetImageMoniker(entry), name, isBold: false);
                return true;
            }

            content = null;
            return false;
        }

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string content)
        {
            return entry.TryGetValue(UnusedReferencesTableKeyNames.ProjectName, out content);
        }

        public override IEntryBucket? CreateBucketForEntry(ITableEntryHandle entry)
        {
            return entry.TryGetValue(UnusedReferencesTableKeyNames.ProjectName, out string name)
                ? new ImageEntryBucket(GetImageMoniker(entry), name)
                : null;
        }

        private static ImageMoniker GetImageMoniker(ITableEntryHandle entry)
        {
            return entry.TryGetValue(UnusedReferencesTableKeyNames.Language, out string languageName) && languageName == LanguageNames.VisualBasic
                ? KnownMonikers.VBProjectNode
                : KnownMonikers.CSProjectNode;
        }
    }

    [Export(typeof(ITableColumnDefinition))]
    [Name(UnusedReferencesColumnDefinitions.ReferenceType)]
    internal class ReferenceTypeColumnDefinition : TableColumnDefinitionBase
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ReferenceTypeColumnDefinition()
        {
        }

        public override string Name => UnusedReferencesColumnDefinitions.ReferenceType;

        public override bool TryCreateColumnContent(ITableEntryHandle entry, bool singleColumnView, out FrameworkElement? content)
        {
            if (entry.TryGetValue<ReferenceType>(UnusedReferencesTableKeyNames.ReferenceType, out var referenceType))
            {
                content = CreateGridElement(GetReferenceTypeImageMoniker(referenceType), GetText(referenceType), isBold: false);
                return true;
            }

            content = null;
            return false;
        }

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string? content)
        {
            content = entry.TryGetValue<ReferenceType>(UnusedReferencesTableKeyNames.ReferenceType, out var referenceType)
                ? GetText(referenceType)
                : null;
            return content != null;
        }

        public override IEntryBucket? CreateBucketForEntry(ITableEntryHandle entry)
        {
            return entry.TryGetValue<ReferenceType>(UnusedReferencesTableKeyNames.ReferenceType, out var referenceType)
                ? new ImageEntryBucket(GetReferenceTypeImageMoniker(referenceType), GetText(referenceType))
                : null;
        }

        private static string GetText(ReferenceType referenceType)
        {
            return referenceType switch
            {
                ReferenceType.Package => ServicesVSResources.Packages,
                ReferenceType.Project => ServicesVSResources.Projects,
                ReferenceType.Assembly => ServicesVSResources.Assemblies,
                _ => throw ExceptionUtilities.UnexpectedValue(referenceType)
            };
        }
    }

    [Export(typeof(ITableColumnDefinition))]
    [Name(UnusedReferencesColumnDefinitions.ReferenceName)]
    internal class ReferenceNameColumnDefinition : TableColumnDefinitionBase
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ReferenceNameColumnDefinition()
        {
        }

        public override string Name => UnusedReferencesColumnDefinitions.ReferenceName;
        public override string DisplayName => ServicesVSResources.Reference;
        public override bool IsFilterable => false;
        public override double MinWidth => 200;

        public override bool TryCreateColumnContent(ITableEntryHandle entry, bool singleColumnView, out FrameworkElement? content)
        {
            content = CreateGridElement(GetImageMoniker(entry), GetText(entry), isBold: false);
            return true;
        }

        public override bool TryCreateStringContent(ITableEntryHandle entry, bool truncatedText, bool singleColumnView, out string content)
        {
            return entry.TryGetValue(UnusedReferencesTableKeyNames.ReferenceName, out content);
        }

        private static ImageMoniker GetImageMoniker(ITableEntryHandle entry)
        {
            return entry.TryGetValue(UnusedReferencesTableKeyNames.ReferenceType, out ReferenceType referenceType)
                ? GetReferenceTypeImageMoniker(referenceType)
                : default;
        }

        private static string GetText(ITableEntryHandle entry)
        {
            return entry.TryGetValue(UnusedReferencesTableKeyNames.ReferenceName, out string text)
                ? text
                : string.Empty;
        }
    }

    [Export(typeof(ITableColumnDefinition))]
    [Name(UnusedReferencesColumnDefinitions.UpdateAction)]
    internal class UpdateActionColumnDefinition : TableColumnDefinitionBase
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UpdateActionColumnDefinition()
        {
        }

        public override string Name => UnusedReferencesColumnDefinitions.UpdateAction;
        public override string DisplayName => ServicesVSResources.Action;
        public override bool IsFilterable => false;
        public override bool IsSortable => false;
        public override double MinWidth => 100;

        public override bool TryCreateColumnContent(ITableEntryHandle entry, bool singleColumnView, out FrameworkElement? content)
        {
            var combobox = new ComboBox
            {
                IsEditable = false,
                ItemsSource = new[] { ServicesVSResources.Keep, ServicesVSResources.Remove }
            };

            combobox.SetValue(AutomationProperties.NameProperty, ServicesVSResources.Action);

            if (entry.TryGetValue(UnusedReferencesTableKeyNames.UpdateAction, out UpdateAction action))
            {
                combobox.SelectedItem = action switch
                {
                    UpdateAction.Remove => ServicesVSResources.Remove,
                    _ => ServicesVSResources.Keep
                };
            }

            combobox.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                var action = combobox.SelectedIndex switch
                {
                    0 => UpdateAction.TreatAsUsed,
                    1 => UpdateAction.Remove,
                    _ => throw ExceptionUtilities.UnexpectedValue(combobox.SelectedIndex)
                };

                entry.TrySetValue(UnusedReferencesTableKeyNames.UpdateAction, action);
            };

            content = combobox;
            return true;
        }
    }

    /// <summary>
    /// Used for columns that will be grouped on. Displays an image and text string.
    /// </summary>
    internal class ImageEntryBucket : StringEntryBucket
    {
        public readonly ImageMoniker ImageMoniker;

        public ImageEntryBucket(ImageMoniker imageMoniker, string name, object? tooltip = null, StringComparer? comparer = null, bool expandedByDefault = true)
            : base(name, tooltip, comparer, expandedByDefault)
        {
            ImageMoniker = imageMoniker;
        }

        public override bool TryCreateColumnContent(out FrameworkElement content)
        {
            content = CreateGridElement(ImageMoniker, Name, isBold: true);
            return true;
        }
    }
}
