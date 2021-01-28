// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;

using Roslyn.SyntaxVisualizer.Control.SymbolDisplay;

using SystemInformation = System.Windows.Forms.SystemInformation;

namespace Roslyn.SyntaxVisualizer.Control
{
    public enum SyntaxCategory
    {
        None,
        SyntaxNode,
        SyntaxToken,
        SyntaxTrivia,
        Operation,
    }

    // A control for visually displaying the contents of a SyntaxTree.
    public partial class SyntaxVisualizerControl : UserControl
    {
        private static readonly string SyntaxNodeTextBrushKey = "SyntaxNodeText.Brush";
        private static readonly string SyntaxTokenTextBrushKey = "SyntaxTokenText.Brush";
        private static readonly string SyntaxTriviaTextBrushKey = "SyntaxTriviaText.Brush";
        private static readonly string OperationTextBrushKey = "OperationText.Brush";
        private static readonly string ErrorSquiggleBrushKey = "ErrorSquiggle.Brush";
        private static readonly string SquiggleStyleKey = "SquiggleStyle";

        // Instances of this class are stored in the Tag field of each item in the treeview.
        private class SyntaxTag
        {
            internal TextSpan Span { get; set; }
            internal TextSpan FullSpan { get; set; }
            internal TreeViewItem? ParentItem { get; set; }
            internal string? Kind { get; set; }
            internal SyntaxNode? SyntaxNode { get; set; }
            internal SyntaxToken SyntaxToken { get; set; }
            internal SyntaxTrivia SyntaxTrivia { get; set; }
            internal SyntaxCategory Category { get; set; }
        }

        #region Private State
        private TreeViewItem? _currentSelection;
        private bool _isNavigatingFromSourceToTree;
        private bool _isNavigatingFromTreeToSource;
        private readonly System.Windows.Forms.PropertyGrid _propertyGrid;
        private static readonly Thickness s_defaultBorderThickness = new Thickness(1);

        /// <remarks>
        /// Every unselected item in the TreeView has a foreground (indicating if it is a node/
        /// token/trivia) and background color (indicating the presence of diagnostics). When an
        /// item is selected (active or inactive), we want to ensure that it looks obviously 
        /// selected while maintaining contrasting colors.
        /// 
        /// To that end, we want to use these custom colors when unselected and the ControlTemplate
        /// colors when selected. Unfortunately, our use of hard-coded color values to instantiate
        /// TreeViewItems in code makes this very difficult to accomplish declaratively. We should
        /// remove the hard-coded color approach in favor of a data class with a DataTemplate in
        /// the future.
        /// 
        /// Instead, we listen for when items are selected/unselected and manually swap colors 
        /// around. With the goal of using custom colors when unselected and the ControlTemplate 
        /// when selected, we handle the colors by:
        ///
        ///   - Background colors: The item's control template hides the specified background color
        /// when it is selected (active or inactive) by overlaying a Border colored by the 
        /// highlight brush. When the item becomes unselected again, the added Border is hidden, 
        /// allowing the originally specified background color to show again, so we don't need any
        /// custom handling.
        /// 
        ///   - Foreground colors: The item's control template does *not* override the specified
        /// foreground when it is selected. To use the control templates correctly themed defaults,
        /// we temporarily clear the specified foreground color and restore it when the item is
        /// unselected. This field is used to save and restore that foreground color.
        /// </remarks>
        private Brush? _currentSelectionUnselectedForeground;
        private ImmutableArray<ClassifiedSpan> classifiedSpans;
        #endregion

        #region Public Properties, Events
        public SyntaxTree? SyntaxTree { get; private set; }
        public SemanticModel? SemanticModel { get; private set; }
        public bool IsLazy { get; private set; }

        public delegate void SyntaxNodeDelegate(SyntaxNode? node);
        public event SyntaxNodeDelegate? SyntaxNodeDirectedGraphRequested;
        public event SyntaxNodeDelegate? SyntaxNodeNavigationToSourceRequested;

        public delegate void SyntaxTokenDelegate(SyntaxToken token);
        public event SyntaxTokenDelegate? SyntaxTokenDirectedGraphRequested;
        public event SyntaxTokenDelegate? SyntaxTokenNavigationToSourceRequested;

        public delegate void SyntaxTriviaDelegate(SyntaxTrivia trivia);
        public event SyntaxTriviaDelegate? SyntaxTriviaDirectedGraphRequested;
        public event SyntaxTriviaDelegate? SyntaxTriviaNavigationToSourceRequested;

        private ClassifiedSpan? _classifiedSpan;
        public ClassifiedSpan? ClassifiedSpan
        {
            set
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                _classifiedSpan = value;

                if (value.HasValue)
                {
                    var color = FontsAndColorsHelper.GetColorForClassification(value.Value);

                    if (!color.HasValue)
                    {
                        _classifiedSpan = null;
                    }

                    if (_classifiedSpan is null)
                    {
                        colorLabel.Visibility = Visibility.Hidden;
                        colorPickerGrid.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        colorLabel.Visibility = Visibility.Visible;
                        colorPickerGrid.Visibility = Visibility.Visible;
                        if (color is not null)
                        {
                            colorPickerButton.Background = new SolidColorBrush(color.Value);
                        }

                        var textValue = _classifiedSpan?.ClassificationType;
                        if (string.IsNullOrEmpty(textValue))
                        {
                            colorKindText.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            colorKindText.Visibility = Visibility.Visible;
                            colorKindText.Content = CultureInfo.CurrentUICulture.TextInfo.ToTitleCase(textValue);
                        }
                    }
                }
            }
            get => _classifiedSpan;
        }
        #endregion

        #region Public Methods
        public SyntaxVisualizerControl()
        {
            InitializeComponent();

            _propertyGrid = new System.Windows.Forms.PropertyGrid
            {
                Dock = System.Windows.Forms.DockStyle.Fill,
                PropertySort = System.Windows.Forms.PropertySort.Alphabetical,
                HelpVisible = false,
                ToolbarVisible = false,
                CommandsVisibleIfAvailable = false
            };
            var tabStopPanel = new TabStopPanel(windowsFormsHost)
            {
                PropertyGrid = _propertyGrid,
                BorderStyle = System.Windows.Forms.BorderStyle.None,
                Padding = System.Windows.Forms.Padding.Empty,
                Margin = System.Windows.Forms.Padding.Empty
            };

            _propertyGrid.SelectedObjectsChanged += (s, e) =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                switch (_propertyGrid.SelectedObject)
                {
                    case SyntaxNode node:
                        ClassifiedSpan = classifiedSpans.FirstOrDefault(s => s.TextSpan.Contains(node.Span));
                        break;

                    case SyntaxToken token:
                        ClassifiedSpan = classifiedSpans.FirstOrDefault(s => s.TextSpan.Contains(token.Span));
                        break;

                    case SyntaxTrivia trivia:
                        ClassifiedSpan = classifiedSpans.FirstOrDefault(s => s.TextSpan.Contains(trivia.Span));
                        break;

                    default:
                        ClassifiedSpan = null;
                        break;
                }
            };

            colorPickerButton.Click += ColorPickerButton_Click;
            windowsFormsHost.Child = tabStopPanel;
        }

        private void ColorPickerButton_Click(object sender, RoutedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var color = ((SolidColorBrush)colorPickerButton.Background).Color;
            var popup = new ColorPickerWindow(color);

            if (popup.ShowDialog() == true)
            {
                if (_classifiedSpan is not null)
                {
                    FontsAndColorsHelper.UpdateClassificationColor(_classifiedSpan.Value, popup.Color);
                }

                colorPickerButton.Background = new SolidColorBrush(popup.Color);
            }

        }

        public void SetPropertyGridColors(IVsUIShell5 shell)
        {
            var backgroundColor = VsColors.GetThemedGDIColor(shell, EnvironmentColors.ToolWindowBackgroundColorKey);
            var foregroundColor = VsColors.GetThemedGDIColor(shell, EnvironmentColors.ToolWindowTextColorKey);
            var lineColor = VsColors.GetThemedGDIColor(shell, EnvironmentColors.ToolWindowContentGridColorKey);
            var disabledColor = VsColors.GetThemedGDIColor(shell, EnvironmentColors.SystemGrayTextColorKey);
            var highlightColor = VsColors.GetThemedGDIColor(shell, EnvironmentColors.SystemHighlightColorKey);
            var highlightTextColor = VsColors.GetThemedGDIColor(shell, EnvironmentColors.SystemHighlightTextColorKey);
            var hyperLinkColor = VsColors.GetThemedGDIColor(shell, EnvironmentColors.ControlLinkTextColorKey);
            var hyperLinkActiveColor = VsColors.GetThemedGDIColor(shell, EnvironmentColors.ControlLinkTextPressedColorKey);

            if (!SystemInformation.HighContrast || !DotNetFrameworkUtilities.IsInstalledFramework471OrAbove())
            {
                _propertyGrid.LineColor = lineColor;
            }

            _propertyGrid.ViewBackColor = backgroundColor;
            _propertyGrid.ViewForeColor = foregroundColor;
            _propertyGrid.ViewBorderColor = lineColor;

            _propertyGrid.HelpBackColor = backgroundColor;
            _propertyGrid.HelpForeColor = foregroundColor;
            _propertyGrid.HelpBorderColor = backgroundColor;

            _propertyGrid.CategoryForeColor = foregroundColor;
            _propertyGrid.CategorySplitterColor = lineColor;

            _propertyGrid.CommandsActiveLinkColor = hyperLinkActiveColor;
            _propertyGrid.CommandsDisabledLinkColor = disabledColor;
            _propertyGrid.CommandsLinkColor = hyperLinkColor;
            _propertyGrid.CommandsForeColor = foregroundColor;
            _propertyGrid.CommandsBackColor = backgroundColor;
            _propertyGrid.CommandsDisabledLinkColor = disabledColor;
            _propertyGrid.CommandsBorderColor = backgroundColor;

            _propertyGrid.SelectedItemWithFocusForeColor = highlightTextColor;
            _propertyGrid.SelectedItemWithFocusBackColor = highlightColor;

            _propertyGrid.DisabledItemForeColor = disabledColor;

            _propertyGrid.CanShowVisualStyleGlyphs = false;
        }

        public void SetTreeViewColors(
            IClassificationTypeRegistryService classificationTypeRegistryService,
            IClassificationFormatMap classificationFormatMap,
            IEditorFormatMap editorFormatMap)
        {
            var syntaxNodeBrush = (SolidColorBrush)FindResource(SyntaxNodeTextBrushKey);
            syntaxNodeBrush.Color = GetForegroundColor(editorFormatMap.GetProperties(classificationFormatMap.GetEditorFormatMapKey(classificationTypeRegistryService.GetClassificationType(PredefinedClassificationTypeNames.Keyword))));

            var syntaxTokenBrush = (SolidColorBrush)FindResource(SyntaxTokenTextBrushKey);
            syntaxTokenBrush.Color = GetForegroundColor(editorFormatMap.GetProperties(classificationFormatMap.GetEditorFormatMapKey(classificationTypeRegistryService.GetClassificationType(PredefinedClassificationTypeNames.Comment))));

            var syntaxTriviaBrush = (SolidColorBrush)FindResource(SyntaxTriviaTextBrushKey);
            syntaxTriviaBrush.Color = GetForegroundColor(editorFormatMap.GetProperties(classificationFormatMap.GetEditorFormatMapKey(classificationTypeRegistryService.GetClassificationType(PredefinedClassificationTypeNames.String))));

            var operationBrush = (SolidColorBrush)FindResource(OperationTextBrushKey);
            operationBrush.Color = GetForegroundColor(editorFormatMap.GetProperties(classificationFormatMap.GetEditorFormatMapKey(classificationTypeRegistryService.GetClassificationType(PredefinedClassificationTypeNames.Number))));

            var errorBrush = (SolidColorBrush)FindResource(ErrorSquiggleBrushKey);
            errorBrush.Color = GetForegroundColor(editorFormatMap.GetProperties(PredefinedErrorTypeNames.SyntaxError));
        }

        private static Color GetForegroundColor(ResourceDictionary resourceDictionary)
        {
            if (resourceDictionary == null)
                return Colors.Transparent;

            if (resourceDictionary.Contains(EditorFormatDefinition.ForegroundColorId))
            {
                var color = (Color)resourceDictionary[EditorFormatDefinition.ForegroundColorId];
                return color;
            }

            if (resourceDictionary.Contains(EditorFormatDefinition.ForegroundBrushId))
            {
                if (resourceDictionary[EditorFormatDefinition.ForegroundBrushId] is SolidColorBrush brush)
                    return brush.Color;
            }

            return Colors.Transparent;
        }

        public void Clear()
        {
            treeView.Items.Clear();
            _propertyGrid.SelectedObject = null;
            typeTextLabel.Visibility = Visibility.Hidden;
            kindTextLabel.Visibility = Visibility.Hidden;
            typeValueLabel.Content = string.Empty;
            kindValueLabel.Content = string.Empty;
        }

        // If lazy is true then treeview items are populated on-demand. In other words, when lazy is true
        // the children for any given item are only populated when the item is selected. If lazy is
        // false then the entire tree is populated at once (and this can result in bad performance when
        // displaying large trees).
        public void DisplaySyntaxTree(SyntaxTree tree, SemanticModel? model = null, bool lazy = true, Workspace? workspace = null)
        {
            if (tree != null)
            {
                IsLazy = lazy;
                SyntaxTree = tree;
                SemanticModel = model;
                AddNode(null, SyntaxTree.GetRoot());

                if (model != null && workspace != null)
                {
                    classifiedSpans = Classifier.GetClassifiedSpans(model, tree.GetRoot().FullSpan, workspace).ToImmutableArray();
                }
                else
                {
                    classifiedSpans = ImmutableArray<ClassifiedSpan>.Empty;
                }
            }
        }

        // If lazy is true then treeview items are populated on-demand. In other words, when lazy is true
        // the children for any given item are only populated when the item is selected. If lazy is
        // false then the entire tree is populated at once (and this can result in bad performance when
        // displaying large trees).
        public void DisplaySyntaxNode(SyntaxNode node, SemanticModel? model = null, bool lazy = true)
        {
            if (node != null)
            {
                IsLazy = lazy;
                SyntaxTree = node.SyntaxTree;
                SemanticModel = model;
                AddNode(null, node);
            }
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose position best matches the supplied position.
        public bool NavigateToBestMatch(int position, string? kind = null,
            SyntaxCategory category = SyntaxCategory.None,
            bool highlightMatch = false)
        {
            TreeViewItem? match = null;

            if (treeView.HasItems && !_isNavigatingFromTreeToSource)
            {
                _isNavigatingFromSourceToTree = true;
                match = NavigateToBestMatch((TreeViewItem)treeView.Items[0], position, kind, category);
                _isNavigatingFromSourceToTree = false;
            }

            if (!highlightMatch || match is null)
                return false;

            match.Background = Brushes.Yellow;
            match.BorderBrush = Brushes.Black;
            match.BorderThickness = s_defaultBorderThickness;

            return true;
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose span best matches the supplied span.
        public bool NavigateToBestMatch(int start, int length, string? kind = null,
            SyntaxCategory category = SyntaxCategory.None,
            bool highlightMatch = false)
        {
            return NavigateToBestMatch(new TextSpan(start, length), kind, category, highlightMatch);
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose span best matches the supplied span.
        public bool NavigateToBestMatch(TextSpan span, string? kind = null,
            SyntaxCategory category = SyntaxCategory.None,
            bool highlightMatch = false)
        {
            TreeViewItem? match = null;

            if (treeView.HasItems && !_isNavigatingFromTreeToSource)
            {
                _isNavigatingFromSourceToTree = true;
                match = NavigateToBestMatch((TreeViewItem)treeView.Items[0], span, kind, category);
                _isNavigatingFromSourceToTree = false;
            }

            if (!highlightMatch || match is null)
                return false;

            match.Background = Brushes.Yellow;
            match.BorderBrush = Brushes.Black;
            match.BorderThickness = s_defaultBorderThickness;

            return true;
        }
        #endregion

        #region Private Helpers - TreeView Navigation
        // Collapse all items in the treeview except for the supplied item. The supplied item
        // is also expanded, selected and scrolled into view.
        private void CollapseEverythingBut(TreeViewItem item)
        {
            if (item != null)
            {
                DeepCollapse((TreeViewItem)treeView.Items[0]);
                ExpandPathTo(item);
                item.IsSelected = true;
                item.BringIntoView();
            }
        }

        // Collapse the supplied treeview item including all its descendants.
        private void DeepCollapse(TreeViewItem item)
        {
            if (item != null)
            {
                item.IsExpanded = false;
                foreach (TreeViewItem child in item.Items)
                {
                    DeepCollapse(child);
                }
            }
        }

        // Ensure that the supplied treeview item and all its ancestors are expanded.
        private void ExpandPathTo(TreeViewItem? item)
        {
            if (item != null)
            {
                item.IsExpanded = true;
                ExpandPathTo(((SyntaxTag)item.Tag).ParentItem);
            }
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose position best matches the supplied position.
        private TreeViewItem? NavigateToBestMatch(TreeViewItem current, int position, string? kind = null,
            SyntaxCategory category = SyntaxCategory.None)
        {
            TreeViewItem? match = null;

            if (current != null)
            {
                var currentTag = (SyntaxTag)current.Tag;
                if (currentTag.FullSpan.Contains(position))
                {
                    CollapseEverythingBut(current);

                    foreach (TreeViewItem item in current.Items)
                    {
                        match = NavigateToBestMatch(item, position, kind, category);
                        if (match != null)
                        {
                            break;
                        }
                    }

                    if (match == null && (kind == null || currentTag.Kind == kind) &&
                       (category == SyntaxCategory.None || category == currentTag.Category))
                    {
                        match = current;
                    }
                }
            }

            return match;
        }

        // Select the SyntaxNode / SyntaxToken / SyntaxTrivia whose span best matches the supplied span.
        private TreeViewItem? NavigateToBestMatch(TreeViewItem current, TextSpan span, string? kind = null,
            SyntaxCategory category = SyntaxCategory.None)
        {
            TreeViewItem? match = null;

            if (current != null)
            {
                var currentTag = (SyntaxTag)current.Tag;
                if (currentTag.FullSpan.Contains(span))
                {
                    if ((currentTag.Span == span || currentTag.FullSpan == span) && (kind == null || currentTag.Kind == kind))
                    {
                        CollapseEverythingBut(current);
                        match = current;
                    }
                    else
                    {
                        CollapseEverythingBut(current);

                        foreach (TreeViewItem item in current.Items)
                        {
                            if (category != SyntaxCategory.Operation && ((SyntaxTag)item.Tag).Category == SyntaxCategory.Operation)
                            {
                                // Do not prefer navigating to IOperation nodes when clicking in source code
                                continue;
                            }

                            match = NavigateToBestMatch(item, span, kind, category);
                            if (match != null)
                            {
                                break;
                            }
                        }

                        if (match == null && (kind == null || currentTag.Kind == kind) &&
                           (category == SyntaxCategory.None || category == currentTag.Category))
                        {
                            match = current;
                        }
                    }
                }
            }

            return match;
        }
        #endregion

        #region Private Helpers - TreeView Population
        // Helpers for populating the treeview.

        private void AddNodeOrToken(TreeViewItem parentItem, SyntaxNodeOrToken nodeOrToken)
        {
            if (nodeOrToken.IsNode)
            {
                AddNode(parentItem, nodeOrToken.AsNode());
            }
            else
            {
                AddToken(parentItem, nodeOrToken.AsToken());
            }
        }

        private void AddOperation(TreeViewItem parentItem, IOperation operation)
        {
            var node = operation.Syntax;
            var kind = operation.Kind.ToString();
            var tag = new SyntaxTag()
            {
                SyntaxNode = node,
                Category = SyntaxCategory.Operation,
                Span = node.Span,
                FullSpan = node.FullSpan,
                Kind = kind,
                ParentItem = parentItem,
            };

            var item = CreateTreeViewItem(tag, tag.Kind + " " + node.Span.ToString(), node.ContainsDiagnostics);
            item.SetResourceReference(ForegroundProperty, OperationTextBrushKey);

            if (SyntaxTree is object && node.ContainsDiagnostics)
            {
                item.ToolTip = string.Empty;
                foreach (var diagnostic in SyntaxTree.GetDiagnostics(node))
                {
                    item.ToolTip += diagnostic.ToString() + "\n";
                }

                item.ToolTip = item.ToolTip.ToString().Trim();
            }

            item.Selected += new RoutedEventHandler((sender, e) =>
            {
                _isNavigatingFromTreeToSource = true;

                typeTextLabel.Visibility = Visibility.Visible;
                kindTextLabel.Visibility = Visibility.Visible;
                typeValueLabel.Content = string.Join(", ", GetOperationInterfaces(operation));
                kindValueLabel.Content = kind;
                _propertyGrid.SelectedObject = operation;

                item.IsExpanded = true;

                if (!_isNavigatingFromSourceToTree && SyntaxNodeNavigationToSourceRequested != null)
                {
                    SyntaxNodeNavigationToSourceRequested(node);
                }

                _isNavigatingFromTreeToSource = false;
                e.Handled = true;
            });

            item.Expanded += new RoutedEventHandler((sender, e) =>
            {
                if (item.Items.Count == 1 && item.Items[0] == null)
                {
                    // Remove placeholder child and populate real children.
                    item.Items.RemoveAt(0);

                    foreach (var child in operation.Children)
                    {
                        AddOperation(item, child);
                    }
                }
            });

            if (parentItem == null)
            {
                treeView.Items.Clear();
                treeView.Items.Add(item);
            }
            else
            {
                parentItem.Items.Add(item);
            }

            if (operation.Children.Any())
            {
                if (IsLazy)
                {
                    // Add placeholder child to indicate that real children need to be populated on expansion.
                    item.Items.Add(null);
                }
                else
                {
                    // Recursively populate all descendants.
                    foreach (var child in operation.Children)
                    {
                        AddOperation(item, child);
                    }
                }
            }

            return;

            // local functions
            static IEnumerable<string> GetOperationInterfaces(IOperation operation)
            {
                var interfaces = new List<Type>();
                foreach (var interfaceType in operation.GetType().GetInterfaces())
                {
                    if (interfaceType == typeof(IOperation)
                        || !interfaceType.IsPublic
                        || !interfaceType.GetInterfaces().Contains(typeof(IOperation)))
                    {
                        continue;
                    }

                    interfaces.Add(interfaceType);
                }

                if (interfaces.Count == 0)
                {
                    interfaces.Add(typeof(IOperation));
                }

                return interfaces.OrderByDescending(x => x.GetInterfaces().Length).Select(x => x.Name);
            }
        }

        private void AddNode(TreeViewItem? parentItem, SyntaxNode? node)
        {
            if (node is null)
            {
                return;
            }

            var kind = node.GetKind();
            var tag = new SyntaxTag()
            {
                SyntaxNode = node,
                Category = SyntaxCategory.SyntaxNode,
                Span = node.Span,
                FullSpan = node.FullSpan,
                Kind = kind,
                ParentItem = parentItem
            };

            var item = CreateTreeViewItem(tag, tag.Kind + " " + node.Span.ToString(), node.ContainsDiagnostics);
            item.SetResourceReference(ForegroundProperty, SyntaxNodeTextBrushKey);

            if (SyntaxTree != null && node.ContainsDiagnostics)
            {
                item.ToolTip = string.Empty;
                foreach (var diagnostic in SyntaxTree.GetDiagnostics(node))
                {
                    item.ToolTip += diagnostic.ToString() + "\n";
                }

                item.ToolTip = item.ToolTip.ToString().Trim();
            }

            item.Selected += new RoutedEventHandler((sender, e) =>
            {
                _isNavigatingFromTreeToSource = true;

                typeTextLabel.Visibility = Visibility.Visible;
                kindTextLabel.Visibility = Visibility.Visible;
                typeValueLabel.Content = node.GetType().Name;
                kindValueLabel.Content = kind;
                _propertyGrid.SelectedObject = node;

                item.IsExpanded = true;

                if (!_isNavigatingFromSourceToTree && SyntaxNodeNavigationToSourceRequested != null)
                {
                    SyntaxNodeNavigationToSourceRequested(node);
                }

                _isNavigatingFromTreeToSource = false;
                e.Handled = true;
            });

            item.Expanded += new RoutedEventHandler((sender, e) =>
            {
                if (item.Items.Count == 1 && item.Items[0] == null)
                {
                    // Remove placeholder child and populate real children.
                    item.Items.RemoveAt(0);

                    if (SemanticModel is not null)
                    {
                        var operation = SemanticModel.GetOperation(node);
                        if (operation is { Parent: null })
                        {
                            AddOperation(item, operation);
                        }
                    }

                    foreach (var child in node.ChildNodesAndTokens())
                    {
                        AddNodeOrToken(item, child);
                    }
                }
            });

            if (parentItem == null)
            {
                treeView.Items.Clear();
                treeView.Items.Add(item);
            }
            else
            {
                parentItem.Items.Add(item);
            }

            if (node.ChildNodesAndTokens().Count > 0)
            {
                if (IsLazy)
                {
                    // Add placeholder child to indicate that real children need to be populated on expansion.
                    item.Items.Add(null);
                }
                else
                {
                    // Recursively populate all descendants.
                    foreach (var child in node.ChildNodesAndTokens())
                    {
                        AddNodeOrToken(item, child);
                    }
                }
            }
        }

        private void AddToken(TreeViewItem parentItem, SyntaxToken token)
        {
            var kind = token.GetKind();
            var tag = new SyntaxTag()
            {
                SyntaxToken = token,
                Category = SyntaxCategory.SyntaxToken,
                Span = token.Span,
                FullSpan = token.FullSpan,
                Kind = kind,
                ParentItem = parentItem
            };

            var item = CreateTreeViewItem(tag, tag.Kind + " " + token.Span.ToString(), token.ContainsDiagnostics);
            item.SetResourceReference(ForegroundProperty, SyntaxTokenTextBrushKey);

            if (SyntaxTree != null && token.ContainsDiagnostics)
            {
                item.ToolTip = string.Empty;
                foreach (var diagnostic in SyntaxTree.GetDiagnostics(token))
                {
                    item.ToolTip += diagnostic.ToString() + "\n";
                }

                item.ToolTip = item.ToolTip.ToString().Trim();
            }

            item.Selected += new RoutedEventHandler((sender, e) =>
            {
                _isNavigatingFromTreeToSource = true;

                typeTextLabel.Visibility = Visibility.Visible;
                kindTextLabel.Visibility = Visibility.Visible;
                typeValueLabel.Content = token.GetType().Name;
                kindValueLabel.Content = kind;
                _propertyGrid.SelectedObject = token;

                item.IsExpanded = true;

                if (!_isNavigatingFromSourceToTree && SyntaxTokenNavigationToSourceRequested != null)
                {
                    SyntaxTokenNavigationToSourceRequested(token);
                }

                _isNavigatingFromTreeToSource = false;
                e.Handled = true;
            });

            item.Expanded += new RoutedEventHandler((sender, e) =>
            {
                if (item.Items.Count == 1 && item.Items[0] == null)
                {
                    // Remove placeholder child and populate real children.
                    item.Items.RemoveAt(0);
                    foreach (var trivia in token.LeadingTrivia)
                    {
                        AddTrivia(item, trivia, true);
                    }

                    foreach (var trivia in token.TrailingTrivia)
                    {
                        AddTrivia(item, trivia, false);
                    }
                }
            });

            if (parentItem == null)
            {
                treeView.Items.Clear();
                treeView.Items.Add(item);
            }
            else
            {
                parentItem.Items.Add(item);
            }

            if (token.HasLeadingTrivia || token.HasTrailingTrivia)
            {
                if (IsLazy)
                {
                    // Add placeholder child to indicate that real children need to be populated on expansion.
                    item.Items.Add(null);
                }
                else
                {
                    // Recursively populate all descendants.
                    foreach (var trivia in token.LeadingTrivia)
                    {
                        AddTrivia(item, trivia, true);
                    }

                    foreach (var trivia in token.TrailingTrivia)
                    {
                        AddTrivia(item, trivia, false);
                    }
                }
            }
        }

        private void AddTrivia(TreeViewItem parentItem, SyntaxTrivia trivia, bool isLeadingTrivia)
        {
            var kind = trivia.GetKind();
            var tag = new SyntaxTag()
            {
                SyntaxTrivia = trivia,
                Category = SyntaxCategory.SyntaxTrivia,
                Span = trivia.Span,
                FullSpan = trivia.FullSpan,
                Kind = kind,
                ParentItem = parentItem
            };

            var item = CreateTreeViewItem(tag, (isLeadingTrivia ? "Lead: " : "Trail: ") + tag.Kind + " " + trivia.Span.ToString(), trivia.ContainsDiagnostics);
            item.SetResourceReference(ForegroundProperty, SyntaxTriviaTextBrushKey);

            if (SyntaxTree != null && trivia.ContainsDiagnostics)
            {
                item.ToolTip = string.Empty;
                foreach (var diagnostic in SyntaxTree.GetDiagnostics(trivia))
                {
                    item.ToolTip += diagnostic.ToString() + "\n";
                }

                item.ToolTip = item.ToolTip.ToString().Trim();
            }

            item.Selected += new RoutedEventHandler((sender, e) =>
            {
                _isNavigatingFromTreeToSource = true;

                typeTextLabel.Visibility = Visibility.Visible;
                kindTextLabel.Visibility = Visibility.Visible;
                typeValueLabel.Content = trivia.GetType().Name;
                kindValueLabel.Content = kind;
                _propertyGrid.SelectedObject = trivia;

                item.IsExpanded = true;

                if (!_isNavigatingFromSourceToTree && SyntaxTriviaNavigationToSourceRequested != null)
                {
                    SyntaxTriviaNavigationToSourceRequested(trivia);
                }

                _isNavigatingFromTreeToSource = false;
                e.Handled = true;
            });

            item.Expanded += new RoutedEventHandler((sender, e) =>
            {
                if (item.Items.Count == 1 && item.Items[0] == null)
                {
                    // Remove placeholder child and populate real children.
                    item.Items.RemoveAt(0);
                    AddNode(item, trivia.GetStructure());
                }
            });

            if (parentItem == null)
            {
                treeView.Items.Clear();
                treeView.Items.Add(item);
                typeTextLabel.Visibility = Visibility.Hidden;
                kindTextLabel.Visibility = Visibility.Hidden;
                typeValueLabel.Content = string.Empty;
                kindValueLabel.Content = string.Empty;
            }
            else
            {
                parentItem.Items.Add(item);
            }

            if (trivia.HasStructure)
            {
                if (IsLazy)
                {
                    // Add placeholder child to indicate that real children need to be populated on expansion.
                    item.Items.Add(null);
                }
                else
                {
                    // Recursively populate all descendants.
                    AddNode(item, trivia.GetStructure());
                }
            }
        }

        private TreeViewItem CreateTreeViewItem(SyntaxTag tag, string text, bool containsDiagnostics)
        {
            var item = new TreeViewItem()
            {
                Tag = tag,
                IsExpanded = false,
                Background = Brushes.Transparent,
            };

            if (containsDiagnostics)
            {
                var textBlock = new TextBlock(new Run(text));

                var brush = new SolidColorBrush();
                BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, new Binding { Source = FindResource(ErrorSquiggleBrushKey), Path = new PropertyPath(SolidColorBrush.ColorProperty.Name) });

                var rectangle = new Rectangle
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Height = 3.0,
                };

                rectangle.SetResourceReference(StyleProperty, SquiggleStyleKey);
                BindingOperations.SetBinding(rectangle, WidthProperty, new Binding { Source = textBlock, Path = new PropertyPath(ActualWidthProperty.Name) });
                item.Header = new Grid { Children = { textBlock, rectangle } };
            }
            else
            {
                item.Header = new TextBlock(new Run(text));
            }

            return item;
        }
        #endregion

        #region Private Helpers - Other
        private void DisplaySymbolInPropertyGrid(ISymbol? symbol)
        {
            if (symbol == null)
            {
                typeTextLabel.Visibility = Visibility.Hidden;
                kindTextLabel.Visibility = Visibility.Hidden;
                typeValueLabel.Content = string.Empty;
                kindValueLabel.Content = string.Empty;

                _propertyGrid.SelectedObject = null;
            }
            else
            {
                typeTextLabel.Visibility = Visibility.Visible;
                kindTextLabel.Visibility = Visibility.Visible;
                typeValueLabel.Content = symbol.GetType().Name;
                kindValueLabel.Content = symbol.Kind.ToString();

                _propertyGrid.SelectedObject = new SymbolPropertyGridAdapter(symbol);
            }
        }

        private static TreeViewItem? FindTreeViewItem(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
            {
                if (source is ContentElement contentElement)
                {
                    source = ContentOperations.GetParent(contentElement);
                }
                else
                {
                    source = VisualTreeHelper.GetParent(source);
                }
            }

            return (TreeViewItem?)source;
        }
        #endregion

        #region Event Handlers
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Restore the regular colors on the newly unselected item
            var previousSelection = _currentSelection;
            if (previousSelection != null)
            {
                previousSelection.Foreground = _currentSelectionUnselectedForeground;
            }

            // Remember the newly selected item's specified foreground color and then clear it to
            // allow the ControlTemplate to correctly set contrasting colors.
            _currentSelection = (TreeViewItem)treeView.SelectedItem;
            if (_currentSelection != null)
            {
                _currentSelectionUnselectedForeground = _currentSelection.Foreground;
                _currentSelection.ClearValue(TreeViewItem.ForegroundProperty);
            }
        }

        private void TreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = FindTreeViewItem((DependencyObject)e.OriginalSource);

            if (item != null)
            {
                item.Focus();
            }
        }

        private void TreeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_currentSelection == null)
            {
                e.Handled = true;
                return;
            }

            var directedSyntaxGraphEnabled =
                (SyntaxNodeDirectedGraphRequested != null) &&
                (SyntaxTokenDirectedGraphRequested != null) &&
                (SyntaxTriviaDirectedGraphRequested != null);

            var symbolDetailsEnabled =
                (SemanticModel != null) &&
                (((SyntaxTag)_currentSelection.Tag).Category == SyntaxCategory.SyntaxNode);

            if ((!directedSyntaxGraphEnabled) && (!symbolDetailsEnabled))
            {
                e.Handled = true;
            }
            else
            {
                directedSyntaxGraphMenuItem.Visibility = directedSyntaxGraphEnabled ? Visibility.Visible : Visibility.Collapsed;
                symbolDetailsMenuItem.Visibility = symbolDetailsEnabled ? Visibility.Visible : Visibility.Collapsed;
                typeSymbolDetailsMenuItem.Visibility = symbolDetailsMenuItem.Visibility;
                convertedTypeSymbolDetailsMenuItem.Visibility = symbolDetailsMenuItem.Visibility;
                aliasSymbolDetailsMenuItem.Visibility = symbolDetailsMenuItem.Visibility;
                constantValueDetailsMenuItem.Visibility = symbolDetailsMenuItem.Visibility;

                if (directedSyntaxGraphEnabled)
                {
                    // The first group is the DGML commands group
                    menuItemSeparator1.Visibility = symbolDetailsMenuItem.Visibility;
                    menuItemSeparator2.Visibility = symbolDetailsMenuItem.Visibility;
                }
                else
                {
                    // The first group is the symbol details group
                    menuItemSeparator1.Visibility = Visibility.Collapsed;
                    menuItemSeparator2.Visibility = symbolDetailsMenuItem.Visibility;
                }
            }
        }

        private void DirectedSyntaxGraphMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSelection != null)
            {
                var currentTag = (SyntaxTag)_currentSelection.Tag;

                if (currentTag.Category == SyntaxCategory.SyntaxNode && SyntaxNodeDirectedGraphRequested != null)
                {
                    SyntaxNodeDirectedGraphRequested(currentTag.SyntaxNode);
                }
                else if (currentTag.Category == SyntaxCategory.SyntaxToken && SyntaxTokenDirectedGraphRequested != null)
                {
                    SyntaxTokenDirectedGraphRequested(currentTag.SyntaxToken);
                }
                else if (currentTag.Category == SyntaxCategory.SyntaxTrivia && SyntaxTriviaDirectedGraphRequested != null)
                {
                    SyntaxTriviaDirectedGraphRequested(currentTag.SyntaxTrivia);
                }
            }
        }

        private void SymbolDetailsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSelection == null)
            {
                e.Handled = true;
                return;
            }

            var currentTag = (SyntaxTag)_currentSelection.Tag;
            if ((SemanticModel != null) && (currentTag.Category == SyntaxCategory.SyntaxNode) && currentTag.SyntaxNode is not null)
            {
                var symbol = SemanticModel.GetSymbolInfo(currentTag.SyntaxNode).Symbol;
                if (symbol == null)
                {
                    symbol = SemanticModel.GetDeclaredSymbol(currentTag.SyntaxNode);
                }

                if (symbol == null)
                {
                    symbol = SemanticModel.GetPreprocessingSymbolInfo(currentTag.SyntaxNode).Symbol;
                }

                DisplaySymbolInPropertyGrid(symbol);
            }
        }

        private void TypeSymbolDetailsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSelection == null)
            {
                e.Handled = true;
                return;
            }

            var currentTag = (SyntaxTag)_currentSelection.Tag;
            if (currentTag.SyntaxNode is null)
            {
                e.Handled = true;
                return;
            }

            if ((SemanticModel != null) && (currentTag.Category == SyntaxCategory.SyntaxNode))
            {
                var symbol = SemanticModel.GetTypeInfo(currentTag.SyntaxNode).Type;
                DisplaySymbolInPropertyGrid(symbol);
            }
        }

        private void ConvertedTypeSymbolDetailsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSelection == null)
            {
                e.Handled = true;
                return;
            }

            var currentTag = (SyntaxTag)_currentSelection.Tag;
            if (currentTag.SyntaxNode is null)
            {
                e.Handled = true;
                return;
            }

            if ((SemanticModel != null) && (currentTag.Category == SyntaxCategory.SyntaxNode))
            {
                var symbol = SemanticModel.GetTypeInfo(currentTag.SyntaxNode).ConvertedType;
                DisplaySymbolInPropertyGrid(symbol);
            }
        }

        private void AliasSymbolDetailsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSelection == null)
            {
                e.Handled = true;
                return;
            }

            var currentTag = (SyntaxTag)_currentSelection.Tag;
            if (currentTag.SyntaxNode is null)
            {
                e.Handled = true;
                return;
            }

            if ((SemanticModel != null) && (currentTag.Category == SyntaxCategory.SyntaxNode))
            {
                var symbol = SemanticModel.GetAliasInfo(currentTag.SyntaxNode);
                DisplaySymbolInPropertyGrid(symbol);
            }
        }

        private void ConstantValueDetailsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSelection == null)
            {
                e.Handled = true;
                return;
            }

            var currentTag = (SyntaxTag)_currentSelection.Tag;
            if (SemanticModel != null &&
                currentTag.Category == SyntaxCategory.SyntaxNode &&
                currentTag.SyntaxNode is not null)
            {
                var value = SemanticModel.GetConstantValue(currentTag.SyntaxNode);
                kindTextLabel.Visibility = Visibility.Hidden;
                kindValueLabel.Content = string.Empty;

                if (!value.HasValue)
                {
                    typeTextLabel.Visibility = Visibility.Hidden;
                    typeValueLabel.Content = string.Empty;
                    _propertyGrid.SelectedObject = null;
                }
                else
                {
                    typeTextLabel.Visibility = Visibility.Visible;
                    typeValueLabel.Content = value.Value?.GetType().Name ?? "<null>";
                    _propertyGrid.SelectedObject = value;
                }
            }
        }
        #endregion
    }
}
