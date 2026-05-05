// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable SA1310 // Field names should not contain underscore

namespace Xunit.Harness
{
    using System;
    using System.Runtime.InteropServices;

    internal static class VSConstants
    {
        public const int S_OK = 0;
        public const int S_FALSE = 1;

        public const int E_ACCESSDENIED = -2147024891;

        public static readonly Guid GUID_VSStandardCommandSet97 = typeof(VSStd97CmdID).GUID;

        [Guid("5EFC7975-14BC-11CF-9B2B-00AA00573819")]
        public enum VSStd97CmdID
        {
            Exit = 229,
        }

        public static class DebugEnginesGuids
        {
            /// <summary>The guid of the Debugger Engine for Managed code.</summary>
            public const string ManagedOnly_string = "{449EC4CC-30D2-4032-9256-EE18EB41B62B}";
        }
    }
}
