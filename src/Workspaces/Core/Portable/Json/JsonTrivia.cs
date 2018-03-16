//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System.Collections.Immutable;
//using System.Diagnostics;
//using Microsoft.CodeAnalysis.VirtualChars;

//namespace Microsoft.CodeAnalysis.Json
//{
//    /// <summary>
//    /// Trivia on a <see cref="JsonToken"/>.
//    /// </summary>
//    internal struct JsonTrivia
//    {
//        public readonly JsonKind Kind;
//        public readonly ImmutableArray<VirtualChar> VirtualChars;

//        /// <summary>
//        /// A place for diagnostics to be stored during parsing.  Not intended to be accessed 
//        /// directly.  These will be collected and aggregated into <see cref="JsonTree.Diagnostics"/>
//        /// </summary> 
//        internal readonly ImmutableArray<JsonDiagnostic> Diagnostics;

//        public JsonTrivia(JsonKind kind, ImmutableArray<VirtualChar> virtualChars)
//            : this(kind, virtualChars, ImmutableArray<JsonDiagnostic>.Empty)
//        {
//        }


//        public JsonTrivia(JsonKind kind, ImmutableArray<VirtualChar> virtualChars, ImmutableArray<JsonDiagnostic> diagnostics)
//        {
//            Debug.Assert(virtualChars.Length > 0);
//            Kind = kind;
//            VirtualChars = virtualChars;
//            Diagnostics = diagnostics;
//        }
//    }
//}
