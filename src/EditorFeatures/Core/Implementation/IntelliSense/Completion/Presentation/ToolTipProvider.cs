// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using VSCompletion = Microsoft.VisualStudio.Language.Intellisense.Completion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    [Export(typeof(IUIElementProvider<VSCompletion, ICompletionSession>))]
    [Name("RoslynToolTipProvider")]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal class ToolTipProvider : IUIElementProvider<VSCompletion, ICompletionSession>
    {
        private readonly ClassificationTypeMap _typeMap;

        [ImportingConstructor]
        public ToolTipProvider(ClassificationTypeMap typeMap)
        {
            _typeMap = typeMap;
        }

        public UIElement GetUIElement(VSCompletion itemToRender, ICompletionSession context, UIElementType elementType)
        {
            if (!(itemToRender is CustomCommitCompletion))
            {
                return null;
            }

            var item = (CustomCommitCompletion)itemToRender;
            var descriptionParts = item.CompletionItem.GetDescriptionAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            return descriptionParts.ToTextBlock(_typeMap);
        }
    }
}
