// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal static class ExceptionUtilities
    {
        internal static Exception UnexpectedValue(object o)
        {
            string output = string.Format("Unexpected value '{0}' of type '{1}'", o, (o != null) ? o.GetType().FullName : "<unknown>");
            Debug.Assert(false, output);

            // We do not throw from here because we don't want all Watson reports to be bucketed to this call.
            return new InvalidOperationException(output);
        }

        internal static Exception Unreachable
        {
            get { return new InvalidOperationException("This program location is thought to be unreachable."); }
        }
    }
}
