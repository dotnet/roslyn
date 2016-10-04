// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Structure
{
    internal static class BlockTypes
    {
        // Basic types.
        //public const string Structural = nameof(Structural);
        //public const string Nonstructural = nameof(Nonstructural);

        public const string Nonstructural = nameof(Nonstructural);

        // Trivia
        public const string Comment = nameof(Comment);
        // public const string Region = nameof(Region);
        public const string PreprocessorRegion = nameof(PreprocessorRegion);

        // Top level declarations.
        public const string Imports = nameof(Imports);
        public const string Namespace = nameof(Namespace);
        public const string Class = nameof(Class);
        public const string Enum = nameof(Enum);
        public const string Interface = nameof(Interface);
        public const string Module = nameof(Module);
        public const string Structure = nameof(Structure);

        // Type level declarations.
        public const string Accessor = nameof(Accessor);
        public const string Constructor = nameof(Constructor);
        public const string Destructor = nameof(Destructor);
        public const string Event = nameof(Event);
        public const string Indexer = nameof(Indexer);
        public const string Method = nameof(Method);
        public const string Operator = nameof(Operator);
        public const string Property = nameof(Property);

        // Statements
        public const string Case = nameof(Case);
        public const string Conditional = nameof(Conditional);
        public const string LocalFunction = nameof(LocalFunction);
        public const string Lock = nameof(Lock);
        public const string Loop = nameof(Loop);
        public const string Standalone = nameof(Standalone);
        public const string Switch = nameof(Switch);
        public const string TryCatchFinally = nameof(TryCatchFinally);
        public const string Using = nameof(Using);
        public const string With = nameof(With);

        // Expressions
        public const string AnonymousMethod = nameof(AnonymousMethod);
        public const string Xml = nameof(Xml);

        // public const string Other = nameof(Other);
    }
}