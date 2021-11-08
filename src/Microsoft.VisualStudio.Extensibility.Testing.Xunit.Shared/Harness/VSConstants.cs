// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable disable

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
    }
}
