﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

#pragma warning disable CS0618 // IQuickInfo* is obsolete, tracked by https://github.com/dotnet/roslyn/issues/24094
namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo.Presentation
{
    internal partial class QuickInfoPresenter
    {
        private class QuickInfoSource : ForegroundThreadAffinitizedObject, IQuickInfoSource
        {
            public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
            {
                AssertIsForeground();
                if (!session.Properties.TryGetProperty<QuickInfoPresenterSession>(s_augmentSessionKey, out var presenterSession))
                {
                    applicableToSpan = session.ApplicableToSpan;
                    return;
                }

                session.Properties.RemoveProperty(s_augmentSessionKey);
                presenterSession.AugmentQuickInfoSession(quickInfoContent, out applicableToSpan);
            }

            public void Dispose()
            {
            }
        }
    }
}
#pragma warning restore CS0618 // IQuickInfo* is obsolete, tracked by https://github.com/dotnet/roslyn/issues/24094
