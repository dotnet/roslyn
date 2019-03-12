// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Test.Utilities.MinimalImplementations
{
    public static class AntiXssApis
    {
        public const string CSharp = @"
namespace Microsoft.Security.Application
{
    public static class AntiXss
    {
        public static string XmlAttributeEncode(string input)
        {
            return input;
        }

        public static string XmlEncode(string input)
        {
            return input;
        }
    }

    public static class Encoder
    {
        public static string LdapDistinguishedNameEncode(string input)
        {
            return input;
        }

        public static string LdapDistinguishedNameEncode(string input, bool useInitialCharacterRules, bool useFinalCharacterRule)
        {
            return input;
        }

        public static string LdapEncode(string input)
        {
            return input;
        }

        public static string LdapFilterEncode(string input)
        {
            return input;
        }

        public static string XmlAttributeEncode(string input)
        {
            return input;
        }

        public static string XmlEncode(string input)
        {
            return input;
        }
    }
}";
    }
}
