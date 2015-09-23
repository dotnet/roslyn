// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Testing
{
    internal class ProjectTreeFormatException : FormatException
    {
        public ProjectTreeFormatException(string message, ProjectTreeFormatError errorId, int position)
            : base(message)
        {
            ErrorId = errorId;
            Position = position;
        }

        public ProjectTreeFormatError ErrorId
        {
            get;
        }

        public int Position
        {
            get;
        }
    }
}
