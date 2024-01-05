// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive;
using Microsoft.CodeAnalysis.Editor.Implementation.Notification;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.Notification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Fakes;
using Microsoft.CodeAnalysis.UnitTests.Remote;
using Microsoft.VisualStudio.InteractiveWindow;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    public static class EditorTestCompositions
    {
        public static readonly TestComposition Editor = TestComposition.Empty
            .AddAssemblies(
                // Microsoft.VisualStudio.Platform.VSEditor.dll:
                Assembly.LoadFrom("Microsoft.VisualStudio.Platform.VSEditor.dll"),

                // Microsoft.VisualStudio.Text.Logic.dll:
                //   Must include this because several editor options are actually stored as exported information 
                //   on this DLL.  Including most importantly, the tab size information.
                typeof(VisualStudio.Text.Editor.DefaultOptions).Assembly,

                // Microsoft.VisualStudio.Text.UI.dll:
                //   Include this DLL to get several more EditorOptions including WordWrapStyle.
                typeof(VisualStudio.Text.Editor.WordWrapStyle).Assembly,

                // Microsoft.VisualStudio.Text.UI.Wpf.dll:
                //   Include this DLL to get more EditorOptions values.
                typeof(VisualStudio.Text.Editor.HighlightCurrentLineOption).Assembly,

                // BasicUndo.dll:
                //   Include this DLL to satisfy ITextUndoHistoryRegistry
                typeof(BasicUndo.IBasicUndoHistory).Assembly,

                // Microsoft.VisualStudio.Language.StandardClassification.dll:
                typeof(VisualStudio.Language.StandardClassification.PredefinedClassificationTypeNames).Assembly,

                // Microsoft.VisualStudio.Language
                typeof(VisualStudio.Language.Intellisense.AsyncCompletion.IAsyncCompletionBroker).Assembly,

                // Microsoft.VisualStudio.CoreUtility
                typeof(VisualStudio.Utilities.IFeatureServiceFactory).Assembly)
            .AddParts(
                typeof(TestSerializerService.Factory),
                typeof(TestExportJoinableTaskContext),
                typeof(StubStreamingFindUsagesPresenter), // actual implementation is in VS layer
                typeof(EditorNotificationServiceFactory), // TODO: use mock INotificationService instead (https://github.com/dotnet/roslyn/issues/46045)
                typeof(TestObscuringTipManager));         // TODO: https://devdiv.visualstudio.com/DevDiv/_workitems?id=544569

        public static readonly TestComposition EditorFeatures = FeaturesTestCompositions.Features
            .AddParts(typeof(TestGlobalOperationNotificationService))
            .Add(Editor)
            .AddAssemblies(
                typeof(TextEditorResources).Assembly,
                typeof(EditorFeaturesResources).Assembly,
                typeof(CSharp.CSharpEditorResources).Assembly,
                typeof(VisualBasic.VBEditorResources).Assembly,
                typeof(LanguageServerProtocolResources).Assembly);

        public static readonly TestComposition EditorFeaturesWpf = EditorFeatures
            .AddAssemblies(
                typeof(EditorFeaturesWpfResources).Assembly);

        public static readonly TestComposition InteractiveWindow = EditorFeaturesWpf
            .AddAssemblies(
                typeof(IInteractiveWindow).Assembly)
            .AddParts(
                typeof(TestInteractiveWindowEditorFactoryService));

        public static readonly TestComposition LanguageServerProtocol = FeaturesTestCompositions.Features
            .AddAssemblies(typeof(LanguageServerProtocolResources).Assembly);

        public static readonly TestComposition LanguageServerProtocolEditorFeatures = EditorFeatures
            .AddAssemblies(typeof(LanguageServerProtocolResources).Assembly);
    }
}
