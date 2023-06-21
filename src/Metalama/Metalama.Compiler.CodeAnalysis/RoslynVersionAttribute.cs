// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

using System;

namespace Metalama.Compiler;

[AttributeUsage(AttributeTargets.Assembly)]
class RoslynVersionAttribute : Attribute
{
    public string RoslynVersion { get; }

    public RoslynVersionAttribute(string roslynVersion)
    {
        RoslynVersion = roslynVersion;
    }
}
