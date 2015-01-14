// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
