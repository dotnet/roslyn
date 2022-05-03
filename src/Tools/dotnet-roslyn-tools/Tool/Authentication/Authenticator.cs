// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.RoslynTools.Authentication.PopUps;

namespace Microsoft.RoslynTools.Authentication
{
    internal class Authenticator
    {
        internal static Task<int> UpdateAsync(bool clearSettings, ILogger logger)
        {
            // If clear was passed, then clear the options (no popup)
            if (clearSettings)
            {
                var defaultSettings = new LocalSettings();
                defaultSettings.SaveSettingsFile(logger);
                return Task.FromResult(Constants.SuccessCode);
            }
            else
            {
                var initEditorPopUp = new AuthenticateEditorPopUp("authenticate-settings/authenticate-todo", logger);

                var uxManager = new UxManager("git", logger);
                return Task.FromResult(uxManager.PopUp(initEditorPopUp));
            }
        }
    }
}
