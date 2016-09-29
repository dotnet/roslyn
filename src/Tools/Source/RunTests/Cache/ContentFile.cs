// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunTests.Cache
{
    /// <summary>
    /// Represents a set of content file and the checksum of that conent.
    /// </summary>
    internal sealed class ContentFile
    {
        internal static readonly ContentFile Empty = new ContentFile(string.Empty, string.Empty);

        internal string Checksum { get; }
        internal string Content { get; }
        internal bool IsEmpty => this == Empty;

        internal ContentFile(string checksum, string content)
        {
            Checksum = checksum;
            Content = content;
        }
    }
}
