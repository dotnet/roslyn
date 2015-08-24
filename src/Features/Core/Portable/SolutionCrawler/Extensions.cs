// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal static class Extensions
    {
        public static string ToBase64(this string data)
        {
            // Write out the message in base64, since it may contain 
            // user code that can't be represented in xml. (see
            // http://vstfdevdiv:8080/web/wi.aspx?pcguid=22f9acc9-569a-41ff-b6ac-fac1b6370209&id=578059)
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
        }

        public static string DecodeBase64(this string data)
        {
            var bytes = Convert.FromBase64String(data);
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }
    }
}
