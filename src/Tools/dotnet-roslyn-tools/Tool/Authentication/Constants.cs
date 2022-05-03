// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.RoslynTools.Authentication
{
    internal class Constants
    {
        public const string SettingsFileName = "settings";
        public const int ErrorCode = 42;
        public const int SuccessCode = 0;
        public const int MaxPopupTries = 3;
        public static readonly string RoslynToolsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".roslyn-tools");
    }
}
