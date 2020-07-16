// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 1) csc /target:library LongTypeFormInSignature.cs
// 2) open in a binary editor and replace "Attribute" with "String\0\0\0" and "DateTime" with "Double\0\0" (the only occurrence should be in the #String heap).

public class C
{
    public static System.Attribute RT()
    {
		return null;
    }

    public static System.DateTime VT()
    {
        return default(System.DateTime);
    }
}
