// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[assembly: MyAttr()]

namespace CSharpProject
{
    /// <summary>
    /// This is a C# class
    /// </summary>
    public class CSharpClass
    {
    }
}

[System.Diagnostics.Conditional("EnableMyAttribute")]
public class MyAttr : Attribute
{
}

