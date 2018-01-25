﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    [Export]
    [Order(After = PredefinedQuickInfoPresenterNames.RoslynQuickInfoPresenter)]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Export(typeof(IQuickInfoSourceProvider))]
    [Name("RoslynQuickInfoProvider")]
    internal partial class QuickInfoCommandHandlerAndSourceProvider :
        ForegroundThreadAffinitizedObject,
        IQuickInfoSourceProvider
    {
        private readonly IIntelliSensePresenter<IQuickInfoPresenterSession, IQuickInfoSession> _presenter;
        private readonly IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> _asyncListeners;
        private readonly IList<Lazy<IQuickInfoProvider, OrderableLanguageMetadata>> _providers;

        [ImportingConstructor]
        public QuickInfoCommandHandlerAndSourceProvider(
            [ImportMany] IEnumerable<Lazy<IQuickInfoProvider, OrderableLanguageMetadata>> providers,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners,
            [ImportMany] IEnumerable<Lazy<IIntelliSensePresenter<IQuickInfoPresenterSession, IQuickInfoSession>, OrderableMetadata>> presenters)
            : this(ExtensionOrderer.Order(presenters).Select(lazy => lazy.Value).FirstOrDefault(),
                   providers, asyncListeners)
        {
        }

        // For testing purposes.
        public QuickInfoCommandHandlerAndSourceProvider(
            IIntelliSensePresenter<IQuickInfoPresenterSession, IQuickInfoSession> presenter,
            [ImportMany] IEnumerable<Lazy<IQuickInfoProvider, OrderableLanguageMetadata>> providers,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _providers = ExtensionOrderer.Order(providers);
            _asyncListeners = asyncListeners;
            _presenter = presenter;
        }

        private bool TryGetController(CommandArgs args, out Controller controller)
        {
            AssertIsForeground();

            // check whether this feature is on.
            if (!args.SubjectBuffer.GetFeatureOnOffOption(InternalFeatureOnOffOptions.QuickInfo))
            {
                controller = null;
                return false;
            }

            // If we don't have a presenter, then there's no point in us even being involved.  Just
            // defer to the next handler in the chain.
            if (_presenter == null)
            {
                controller = null;
                return false;
            }

            // TODO(cyrusn): If there are no presenters then we should not create a controller.
            // Otherwise we'll be affecting the user's typing and they'll have no idea why :)
            controller = Controller.GetInstance(
                args, _presenter,
                new AggregateAsynchronousOperationListener(_asyncListeners, FeatureAttribute.QuickInfo),
                _providers);
            return true;
        }

        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new QuickInfoSource(this, textBuffer);
        }

        internal bool TryHandleEscapeKey(EscapeKeyCommandArgs commandArgs)
        {
            if (!TryGetController(commandArgs, out var controller))
            {
                return false;
            }

            return controller.TryHandleEscapeKey();
        }
    }
}
