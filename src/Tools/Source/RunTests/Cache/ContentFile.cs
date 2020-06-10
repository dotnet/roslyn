// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        internal string Checksum { get; }
        internal string Content { get; }

        internal ContentFile(string checksum, string content)
        {
            Checksum = checksum;
            Content = content;
        }
    }
}
