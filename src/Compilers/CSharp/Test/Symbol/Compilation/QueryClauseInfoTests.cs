// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
                    new SymbolInfo(obj),
                    new SymbolInfo(obj)),
                new QueryClauseInfo(
                    new SymbolInfo(obj),
                    new SymbolInfo(obj)));

            EqualityTesting.AssertEqual(
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(obj, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(obj, int32), CandidateReason.Inaccessible)),
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(obj, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(obj, int32), CandidateReason.Inaccessible)));

            EqualityTesting.AssertEqual(
                new QueryClauseInfo(
                    new SymbolInfo(int32),
                    new SymbolInfo(int32)),
                new QueryClauseInfo(
                    new SymbolInfo(int32),
                    new SymbolInfo(int32)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)),
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(obj, int32), CandidateReason.Inaccessible)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(int32),
                    new SymbolInfo(obj)),
                new QueryClauseInfo(
                    new SymbolInfo(int32),
                    new SymbolInfo(int32)));

            EqualityTesting.AssertEqual(
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)),
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(obj),
                    new SymbolInfo(int32)),
                new QueryClauseInfo(
                    new SymbolInfo(int32),
                    new SymbolInfo(int32)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(obj, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)),
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, obj), CandidateReason.Inaccessible),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)),
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(obj, int32), CandidateReason.Inaccessible)),
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, obj), CandidateReason.Inaccessible)),
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Ambiguous),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)),
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)));

            EqualityTesting.AssertNotEqual(
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Ambiguous)),
                new QueryClauseInfo(
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible),
                    new SymbolInfo(ImmutableArray.Create<ISymbol>(int32, int32), CandidateReason.Inaccessible)));
        }
    }
}
