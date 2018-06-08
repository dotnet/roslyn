// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Symbols
{
    internal static class SymbolKeyType
    {
        public const char Alias = 'A';
        public const char BodyLevel = 'B';
        public const char ConstructedMethod = 'C';
        public const char NamedType = 'D';
        public const char ErrorType = 'E';
        public const char Field = 'F';
        public const char DynamicType = 'I';
        public const char Method = 'M';
        public const char Namespace = 'N';
        public const char PointerType = 'O';
        public const char Parameter = 'P';
        public const char Property = 'Q';
        public const char ArrayType = 'R';
        public const char Assembly = 'S';
        public const char TupleType = 'T';
        public const char Module = 'U';
        public const char Event = 'V';
        public const char AnonymousType = 'W';
        public const char ReducedExtensionMethod = 'X';
        public const char TypeParameter = 'Y';
        public const char AnonymousFunctionOrDelegate = 'Z';

        // Not to be confused with ArrayType.  This indicates an array of elements in the stream.
        public const char Array = '%';
        public const char Reference = '#';
        public const char Null = '!';
        public const char TypeParameterOrdinal = '@';
    }
}
