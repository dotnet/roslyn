// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal abstract class VSTypeScriptAsynchronousTaggerProvider<TTag> : AsynchronousViewTaggerProvider<TTag>
        where TTag : ITag
    {
        protected VSTypeScriptAsynchronousTaggerProvider(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider asyncListenerProvider,
#pragma warning disable IDE0060 // TODO: use global options
            VSTypeScriptGlobalOptions globalOptions)
#pragma warning restore IDE0060
            : base(threadingContext, asyncListenerProvider.GetListener(FeatureAttribute.Classification))
        {
        }
    }
}
