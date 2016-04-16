// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio
{
    public static class VsInteractiveWindowExtensions
    {
        public static IWpfTextViewHost GetTextViewHost(this IInteractiveWindow window)
        {
            var cmdFilter = VsInteractiveWindowEditorFactoryService.GetCommandFilter(window);
            if (cmdFilter != null)
            {
                return cmdFilter.TextViewHost;
            }
            return null;
        }

        public static void SetLanguage(this IInteractiveWindow window, Guid languageServiceGuid, IContentType contentType)
        {
            VsInteractiveWindowEditorFactoryService.GetDispatcher(window).CheckAccess();

            var commandFilter = VsInteractiveWindowEditorFactoryService.GetCommandFilter(window);
            window.Properties[typeof(IContentType)] = contentType;
            commandFilter.firstLanguageServiceCommandFilter = null;
            var provider = commandFilter._oleCommandTargetProviders.OfContentType(contentType, commandFilter._contentTypeRegistry);
            if (provider != null)
            {
                var targetFilter = commandFilter.firstLanguageServiceCommandFilter ?? commandFilter.EditorServicesCommandFilter;
                var target = provider.GetCommandTarget(window.TextView, targetFilter);
                if (target != null)
                {
                    commandFilter.firstLanguageServiceCommandFilter = target;
                }
            }

            if (window.CurrentLanguageBuffer != null)
            {
                window.CurrentLanguageBuffer.ChangeContentType(contentType, null);
            }

            VsInteractiveWindowEditorFactoryService.SetEditorOptions(window.TextView.Options, languageServiceGuid);
        }
    }
}
