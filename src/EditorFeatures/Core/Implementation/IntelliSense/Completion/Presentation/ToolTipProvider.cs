// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using VSCompletion = Microsoft.VisualStudio.Language.Intellisense.Completion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    [Export(typeof(IUIElementProvider<VSCompletion, ICompletionSession>))]
    [Name("RoslynToolTipProvider")]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal class ToolTipProvider : IUIElementProvider<VSCompletion, ICompletionSession>
    {
        private readonly ClassificationTypeMap _typeMap;

        // The textblock containing "..." that will be displayed until the actual completion 
        // description has been computed.
        private readonly TextBlock _defaultTextBlock;

        [ImportingConstructor]
        public ToolTipProvider(ClassificationTypeMap typeMap)
        {
            _typeMap = typeMap;
            _defaultTextBlock = SymbolDisplayPartExtensions.ToTextBlock(
                new[] { new SymbolDisplayPart(SymbolDisplayPartKind.Text, symbol: null, text: "...") },
                typeMap);
        }

        public UIElement GetUIElement(VSCompletion itemToRender, ICompletionSession context, UIElementType elementType)
        {
            var item = itemToRender as CustomCommitCompletion;
            if (item == null)
            {
                return null;
            }

            return new CancellableContentControl(this, item.CompletionItem);
        }

        private class CancellableContentControl : ContentControl
        {
            private readonly ForegroundThreadAffinitizedObject _foregroundObject = new ForegroundThreadAffinitizedObject();
            private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
            private readonly ToolTipProvider _toolTipProvider;

            public CancellableContentControl(ToolTipProvider toolTipProvider, CompletionItem completionItem)
            {
                Debug.Assert(_foregroundObject.IsForeground());
                _toolTipProvider = toolTipProvider;

                // Set our content to be "..." initially.
                this.Content = toolTipProvider._defaultTextBlock;

                // Kick off the task to produce the new content.  When it completes, call back on 
                // the UI thread to update the display.
                var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
                completionItem.GetDescriptionAsync(_cancellationTokenSource.Token)
                              .ContinueWith(ProcessDescription, _cancellationTokenSource.Token,
                                            TaskContinuationOptions.OnlyOnRanToCompletion, scheduler);

                // If we get unloaded (i.e. the user scrolls down in the completion list and VS 
                // dismisses the existing tooltip), then cancel the work we're doing
                this.Unloaded += (s, e) => _cancellationTokenSource.Cancel();
            }

            private void ProcessDescription(Task<ImmutableArray<SymbolDisplayPart>> obj)
            {
                Debug.Assert(_foregroundObject.IsForeground());

                // If we were canceled, or didn't run all the way to completion, then don't bother
                // updating the UI.
                if (_cancellationTokenSource.IsCancellationRequested ||
                    obj.Status != TaskStatus.RanToCompletion)
                {
                    return;
                }

                var descriptionParts = obj.Result;
                this.Content = descriptionParts.ToTextBlock(_toolTipProvider._typeMap);
            }
        }
    }
}