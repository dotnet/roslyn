// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using System;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp
{
    public class QueryClauseInfoTests : CSharpTestBase
    {
        [Fact]
        public void Equality()
        {
            var c = (Compilation)CreateCompilation("");
            var obj = c.GetSpecialType(SpecialType.System_Object);
            var int32 = c.GetSpecialType(SpecialType.System_Int32);

            EqualityTesting.AssertEqual(default(QueryClauseInfo), default(QueryClauseInfo));

            EqualityTesting.AssertEqual(
                new QueryClauseInfo(
                    new SymbolInfo(obj, ImmutableArray.Create<ISymbol>(obj, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(obj, ImmutableArray.Create<ISymbol>(obj, int32), CandidateReason.Inaccessible)),
                new QueryClauseInfo(
                    new SymbolInfo(obj, ImmutableArray.Create<ISymbol>(obj, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(obj, ImmutableArray.Create<ISymbol>(obj, int32), CandidateReason.Inaccessible)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)),
                new QueryClauseInfo(
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(obj, int32), CandidateReason.Inaccessible)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(obj, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)),
                new QueryClauseInfo(
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(obj, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)),
                new QueryClauseInfo(
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(obj, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)),
                new QueryClauseInfo(
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, obj), CandidateReason.Inaccessible),
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)),
                new QueryClauseInfo(
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(obj, int32), CandidateReason.Inaccessible)),
                new QueryClauseInfo(
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, obj), CandidateReason.Inaccessible)),
                new QueryClauseInfo(
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Ambiguous),
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)),
                new QueryClauseInfo(
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Ambiguous)),
                new QueryClauseInfo(
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(int32, ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)));
        }
    }
}
