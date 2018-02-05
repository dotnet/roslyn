// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Roslyn.Hosting.Diagnostics.Waiters
{
    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.AutomaticPairCompletion)]
    internal class AutomaticCompletionWaiter : WaiterBase
    {
        public AutomaticCompletionWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.AutomaticPairCompletion, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.AutomaticEndConstructCorrection)]
    internal class AutomaticEndConstructCorrectionWaiter : WaiterBase
    {
        public AutomaticEndConstructCorrectionWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.AutomaticEndConstructCorrection, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.BraceHighlighting)]
    internal class BraceHighlightingWaiter : WaiterBase
    {
        public BraceHighlightingWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.BraceHighlighting, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.Classification)]
    internal class ClassificationWaiter : WaiterBase
    {
        public ClassificationWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.Classification, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.CompletionSet)]
    internal class CompletionSetWaiter : WaiterBase
    {
        public CompletionSetWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.CompletionSet, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.DesignerAttribute)]
    internal class DesignerAttributeWaiter : WaiterBase
    {
        public DesignerAttributeWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.DesignerAttribute, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.DiagnosticService)]
    internal class DiagnosticServiceWaiter : WaiterBase
    {
        public DiagnosticServiceWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.DiagnosticService, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.EncapsulateField)]
    internal class EncapsulateFieldWaiter : WaiterBase
    {
        public EncapsulateFieldWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.EncapsulateField, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.ErrorList)]
    internal class ErrorListWaiter : WaiterBase
    {
        public ErrorListWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.ErrorList, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.ErrorSquiggles)]
    internal class ErrorSquiggleWaiter : WaiterBase
    {
        public ErrorSquiggleWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.ErrorSquiggles, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.EventHookup)]
    internal class EventHookupWaiter : WaiterBase
    {
        public EventHookupWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.EventHookup, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.FindReferences)]
    internal class FindReferencesWaiter : WaiterBase
    {
        public FindReferencesWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.FindReferences, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.GlobalOperation)]
    internal class GlobalOperationWaiter : WaiterBase
    {
        public GlobalOperationWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.GlobalOperation, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.GraphProvider)]
    internal class GraphProviderWaiter : WaiterBase
    {
        public GraphProviderWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.GraphProvider, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.KeywordHighlighting)]
    internal class KeywordHighlightingWaiter : WaiterBase
    {
        public KeywordHighlightingWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.KeywordHighlighting, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.LightBulb)]
    internal class LightBulbWaiter : WaiterBase
    {
        public LightBulbWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.LightBulb, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.LineSeparators)]
    internal class LineSeparatorWaiter : WaiterBase
    {
        public LineSeparatorWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.LineSeparators, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.NavigateTo)]
    internal class NavigateToWaiter : WaiterBase
    {
        public NavigateToWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.NavigateTo, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.NavigationBar)]
    internal class NavigationBarWaiter : WaiterBase
    {
        public NavigationBarWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.NavigationBar, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.Outlining)]
    internal class OutliningWaiter : WaiterBase
    {
        public OutliningWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.Outlining, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.QuickInfo)]
    internal class QuickInfoWaiter : WaiterBase
    {
        public QuickInfoWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.QuickInfo, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.ReferenceHighlighting)]
    internal class ReferenceHighlightingWaiter : WaiterBase
    {
        public ReferenceHighlightingWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.ReferenceHighlighting, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.RemoteHostClient)]
    internal class RemoteHostClientWaiter : WaiterBase
    {
        public RemoteHostClientWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.RemoteHostClient, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.Rename)]
    internal class RenameWaiter : WaiterBase
    {
        public RenameWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.Rename, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.SignatureHelp)]
    internal class SignatureHelpWaiter : WaiterBase
    {
        public SignatureHelpWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.SignatureHelp, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.Snippets)]
    internal class SnippetWaiter : WaiterBase
    {
        public SnippetWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.Snippets, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.SolutionCrawler)]
    internal class SolutionCrawlerWaiter : WaiterBase
    {
        public SolutionCrawlerWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.SolutionCrawler, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.TodoCommentList)]
    internal class TodoCommentListWaiter : WaiterBase
    {
        public TodoCommentListWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.TodoCommentList, provider)
        {
        }
    }

    [Shared]
    [Export(typeof(IAsynchronousOperationListener))]
    [Export(typeof(IAsynchronousOperationWaiter))]
    [Feature(FeatureAttribute.Workspace)]
    internal class WorkspaceWaiter : WaiterBase
    {
        public WorkspaceWaiter(IAsynchronousOperationListenerProvider provider) :
            base(FeatureAttribute.Workspace, provider)
        {
        }
    }
}
