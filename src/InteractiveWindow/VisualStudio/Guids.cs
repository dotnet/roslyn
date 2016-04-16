// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    public static class Guids
    {
        // vsct guids:
        // This GUID identifies the VsInteractiveWindow type. We need to pass it to VS in a string form.
        public const string InteractiveToolWindowIdString = "2D0A56AA-9527-4B78-B6E6-EBE6E05DA749";
        public const string InteractiveWindowPackageIdString = "F5199A4E-6A60-4F79-82E9-FC92A41C4610";
        public const string InteractiveCommandSetIdString = "00B8868B-F9F5-4970-A048-410B05508506";

        public static readonly Guid InteractiveToolWindowId = new Guid(InteractiveToolWindowIdString);
        public static readonly Guid InteractiveWindowPackageId = new Guid(InteractiveWindowPackageIdString);
        public static readonly Guid InteractiveCommandSetId = new Guid(InteractiveCommandSetIdString);
    }
}
