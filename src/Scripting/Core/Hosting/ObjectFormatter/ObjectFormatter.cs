// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// Object pretty printer.
    /// </summary>
    public abstract class ObjectFormatter
    {
        public abstract string FormatObject(object obj, PrintOptions options);

        public abstract string FormatRaisedException(Exception e);
    }
}