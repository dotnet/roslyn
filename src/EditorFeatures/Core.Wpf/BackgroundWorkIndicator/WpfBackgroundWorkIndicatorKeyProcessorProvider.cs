//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//using System;
//using System.ComponentModel.Composition;
//using System.Windows.Input;
//using Microsoft.CodeAnalysis.Host.Mef;
//using Microsoft.VisualStudio.Text.Editor;
//using Microsoft.VisualStudio.Utilities;

//namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator
//{
//    [Export(typeof(IKeyProcessorProvider))]
//    [TextViewRole(PredefinedTextViewRoles.Interactive)]
//    [ContentType(ContentTypeNames.RoslynContentType)]
//    [Name(nameof(WpfBackgroundWorkIndicatorKeyProcessorProvider))]
//    internal sealed class WpfBackgroundWorkIndicatorKeyProcessorProvider : IKeyProcessorProvider
//    {
//        private readonly WpfBackgroundWorkIndicatorFactory _factory;

//        [ImportingConstructor]
//        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
//        public WpfBackgroundWorkIndicatorKeyProcessorProvider(
//            WpfBackgroundWorkIndicatorFactory factory)
//        {
//            _factory = factory;
//        }

//        public KeyProcessor GetAssociatedProcessor(IWpfTextView wpfTextView)
//        {
//            return wpfTextView.Properties.GetOrCreateSingletonProperty(() => new WpfBackgroundWorkIndicatorKeyProcessor(_factory));
//        }

//        private class WpfBackgroundWorkIndicatorKeyProcessor : KeyProcessor
//        {
//            private readonly WpfBackgroundWorkIndicatorFactory _factory;

//            public WpfBackgroundWorkIndicatorKeyProcessor(WpfBackgroundWorkIndicatorFactory factory)
//            {
//                _factory = factory;
//            }

//            public override void KeyDown(KeyEventArgs args)
//            {
//                if (args.SystemKey == Key.Escape)
//                    _factory.OnEscapeKeyPressed();
//            }
//        }
//    }
//}
