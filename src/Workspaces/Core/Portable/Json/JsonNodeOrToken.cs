//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System.Diagnostics;

//namespace Microsoft.CodeAnalysis.Json
//{
//    internal struct JsonNodeOrToken
//    {
//        public readonly JsonNode Node;
//        public readonly JsonToken Token;

//        private JsonNodeOrToken(JsonNode node) : this()
//        {
//            Debug.Assert(node != null);
//            Node = node;
//        }

//        private JsonNodeOrToken(JsonToken token) : this()
//        {
//            Debug.Assert(token.Kind != JsonKind.None);
//            Token = token;
//        }

//        public bool IsNode => Node != null;

//        public static implicit operator JsonNodeOrToken(JsonNode node)
//            => new JsonNodeOrToken(node);

//        public static implicit operator JsonNodeOrToken(JsonToken token)
//            => new JsonNodeOrToken(token);
//    }
//}
