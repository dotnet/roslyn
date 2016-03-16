// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.CSharp.Interactive;
using Microsoft.VisualStudio.LanguageServices.Interactive;
using Microsoft.VisualStudio.Shell;

namespace Roslyn.VisualStudio.Test.Utilities.Remoting
{
    internal static class RemotingHelper
    {
        public static InteractiveWindowWrapper CreateCSharpInteractiveWindowWrapper()
        {
            var componentModel = (IComponentModel)(ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel)));
            var vsInteractiveWindowProvider = componentModel.GetService<CSharpVsInteractiveWindowProvider>();
            var vsInteractiveWindow = ExecuteOnUIThread(() => vsInteractiveWindowProvider.Open(0, true));
            return new InteractiveWindowWrapper(vsInteractiveWindow.InteractiveWindow);
        }

        private static T ExecuteOnUIThread<T>(Func<T> action)
            => Application.Current.Dispatcher.Invoke(action);
    }
}
