// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.CodeAnalysis.Simplification
{
    internal class SpecialTypeAnnotation
    {
        public const string Kind = "SpecialType";

        private static readonly ConcurrentDictionary<SpecialType, string> s_fromSpecialTypes = new ConcurrentDictionary<SpecialType, string>();
        private static readonly ConcurrentDictionary<string, SpecialType> s_toSpecialTypes = new ConcurrentDictionary<string, SpecialType>();

        public static SyntaxAnnotation Create(SpecialType specialType)
        {
            return new SyntaxAnnotation(Kind, s_fromSpecialTypes.GetOrAdd(specialType, s_createFromSpecialTypes));
        }

        public static SpecialType GetSpecialType(SyntaxAnnotation annotation)
        {
            return s_toSpecialTypes.GetOrAdd(annotation.Data, s_createToSpecialTypes);
        }

        private static Func<SpecialType, string> s_createFromSpecialTypes =
            arg => arg.ToString();

        private static Func<string, SpecialType> s_createToSpecialTypes =
            arg => (SpecialType)Enum.Parse(typeof(SpecialType), arg);
    }
}
