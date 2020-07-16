// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive;
using Microsoft.CodeAnalysis.Editor.Implementation.Notification;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.InteractiveWindow;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    public static class EditorTestCompositions
    {
        public static readonly TestComposition EditorFeatures = FeaturesTestCompositions.Features.WithAdditionalParts(
            MinimalTestExportProvider.GetEditorAssemblies().Concat(new[]
            {
                typeof(EditorFeaturesResources).Assembly,
                typeof(CSharp.CSharpEditorResources).Assembly,
                typeof(VisualBasic.VBEditorResources).Assembly,
            }),
            new[]
            {
                typeof(TestExportJoinableTaskContext),
                // TODO: use mock INotificationService instead (https://github.com/dotnet/roslyn/issues/46045)
                typeof(EditorNotificationServiceFactory),
            },
            vsMef: true);

        public static readonly TestComposition EditorFeaturesWpf = EditorFeatures.WithAdditionalParts(
            new[]
            {
                typeof(EditorFeaturesWpfResources).Assembly,
                typeof(CSharp.CSharpEditorWpfResources).Assembly
            },
            new[]
            {
                typeof(TestWaitIndicator)
            },
            vsMef: true);

        public static readonly TestComposition EditorFeaturesInteractiveWindow = EditorFeaturesWpf.WithAdditionalParts(
            new[]
            {
                typeof(IInteractiveWindow).Assembly
            },
            new[]
            {
                typeof(TestInteractiveWindowEditorFactoryService)
            },
            vsMef: true);
    }
}
