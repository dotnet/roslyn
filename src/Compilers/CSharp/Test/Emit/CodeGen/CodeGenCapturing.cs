
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using System.Collections;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenCapturing : CSharpTestBase
    {
        private class CaptureContext
        {
            // Stores a mapping from scope index (0-based count of scopes
            // from `this` to most nested scope)
            public readonly List<IList<string>> VariablesByScope = new List<IList<string>>();

            private CaptureContext() { }

            public CaptureContext(int MaxVariables)
            {
                // Fields are shared among methods, so we also share them in the
                // capture context when cloning
                var fieldsBuilder = ImmutableArray.CreateBuilder<string>(MaxVariables);
                for (int i = 0; i < MaxVariables; i++)
                {
                    fieldsBuilder.Add($"field_{i}");
                }
                VariablesByScope.Add(fieldsBuilder.MoveToImmutable());
            }

            public void Add(int depth, string varName)
            {
                if (VariablesByScope.Count <= depth ||
                    VariablesByScope[depth] == null)
                {
                    VariablesByScope.Insert(depth, new List<string>() { varName });
                }
                else
                {
                    VariablesByScope[depth].Add(varName);
                }
            }

            public CaptureContext Clone()
            {
                var fields = VariablesByScope[0];
                var newCtx = new CaptureContext();
                newCtx.VariablesByScope.Add(fields);
                newCtx.VariablesByScope.AddRange(
                    this.VariablesByScope
                    .Skip(1)
                    .Select(list => list == null ? null : new List<string>(list)));
                newCtx.CaptureNameIndex = this.CaptureNameIndex;
                return newCtx;
            }

            public int CaptureNameIndex = 0;
        }

        private static string MakeLocalFunc(int nameIndex, string captureExpression)
            => $@"int Local_{nameIndex}() => {captureExpression};";

        private static string MakeCaptureExpression(IList<int> varsToCapture, CaptureContext ctx)
        {
            var varNames = new List<string>();
            for (int varDepth = 0; varDepth < varsToCapture.Count; varDepth++)
            {
                var variablesByScope = ctx.VariablesByScope;
                // Do we have any variables in this scope depth?
                // If not, initialize an empty list
                if (variablesByScope.Count <= varDepth)
                {
                    variablesByScope.Add(new List<string>());
                }

                var varsAtCurrentDepth = variablesByScope[varDepth];

                int numToCapture = varsToCapture[varDepth];
                int numVarsAvailable = variablesByScope[varDepth].Count;
                // If we have enough variables to capture in the context
                // just add them
                if (numVarsAvailable >= numToCapture)
                {
                    // Capture the last variables added since if there are more
                    // vars in the context than the max vars to capture we'll never
                    // have code coverage of the newest vars added to the context.
                    varNames.AddRange(varsAtCurrentDepth
                        .Skip(numVarsAvailable - numToCapture)
                        .Take(numToCapture));
                }
                else
                {
                    // Not enough variables in the context -- add more
                    for (int i = 0; i < numToCapture - numVarsAvailable; i++)
                    {
                        varsAtCurrentDepth.Add($"captureVar_{ctx.CaptureNameIndex++}");
                    }

                    varNames.AddRange(varsAtCurrentDepth);
                }
            }

            return varNames.Count == 0 ? "0" : string.Join(" + ", varNames);
        }

        /// <summary>
        /// Generates all combinations of distributing a sum to a list of subsets.
        /// This is equivalent to the "stars and bars" combinatorics construction.
        /// </summary>
        private static IEnumerable<IList<int>> GenerateAllSetCombinations(int sum, int numSubsets)
        {
            Assert.True(numSubsets > 0);
            return GenerateAll(sum, 0, ImmutableList<int>.Empty);

            IEnumerable<ImmutableList<int>> GenerateAll(
                int remainingSum,
                int setIndex, // 0-based index of subset we're generating
                ImmutableList<int> setsSoFar)
            {
                for (int i = 0; i <= remainingSum; i++)
                {
                    var newSets = setsSoFar.Add(i);
                    if (setIndex == numSubsets - 1)
                    {
                        yield return newSets;
                    }
                    else
                    {
                        foreach (var captures in GenerateAll(remainingSum - i,
                                                             setIndex + 1,
                                                             newSets))
                        {
                            yield return captures;
                        }
                    }
                }
            }
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30212")]
        public void GenerateAllTest()
        {
            Assert.Equal(new[]
            {
                ImmutableList<int>.Empty.Add(0),
                ImmutableList<int>.Empty.Add(1),
                ImmutableList<int>.Empty.Add(2),
                ImmutableList<int>.Empty.Add(3)
            }, GenerateAllSetCombinations(3, 1));
            Assert.Equal(new[]
            {
                ImmutableList<int>.Empty.Add(0).Add(0),
                ImmutableList<int>.Empty.Add(0).Add(1),
                ImmutableList<int>.Empty.Add(0).Add(2),
                ImmutableList<int>.Empty.Add(0).Add(3),
                ImmutableList<int>.Empty.Add(1).Add(0),
                ImmutableList<int>.Empty.Add(1).Add(1),
                ImmutableList<int>.Empty.Add(1).Add(2),
                ImmutableList<int>.Empty.Add(2).Add(0),
                ImmutableList<int>.Empty.Add(2).Add(1),
                ImmutableList<int>.Empty.Add(3).Add(0)
            }, GenerateAllSetCombinations(3, 2));
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30212")]
        public void ExpressionGeneratorTest01()
        {
            var ctx = new CaptureContext(1);
            int[] captures = { 1 }; // Capture 1 var at the 0 depth
            var expr = MakeCaptureExpression(captures, ctx);
            Assert.Equal("field_0", expr);
            VerifyContext(new[]
            {
                new[] { "field_0"}
            }, ctx.VariablesByScope);

            ctx = new CaptureContext(3);
            captures = new[] { 3 }; // Capture 3 vars at 0 depth
            expr = MakeCaptureExpression(captures, ctx);
            Assert.Equal("field_0 + field_1 + field_2", expr);
            VerifyContext(new[]
            {
                new[] { "field_0", "field_1", "field_2" }
            }, ctx.VariablesByScope);

            ctx = new CaptureContext(3);
            captures = new[] { 1, 1, 1 }; // Capture 1 var at each of 3 depths
            expr = MakeCaptureExpression(captures, ctx);
            Assert.Equal("field_2 + captureVar_0 + captureVar_1", expr);
            VerifyContext(new[]
            {
                new[] { "field_0", "field_1", "field_2"},
                new[] { "captureVar_0"},
                new[] { "captureVar_1"}
            }, ctx.VariablesByScope);

            void VerifyContext(IList<IEnumerable<string>> expectedCtx, List<IList<string>> actualCtx)
            {
                Assert.Equal(expectedCtx.Count, ctx.VariablesByScope.Count);
                for (int depth = 0; depth < expectedCtx.Count; depth++)
                {
                    AssertEx.Equal(expectedCtx[depth], ctx.VariablesByScope[depth]);
                }
            }
        }
        private struct LayoutEnumerator : IEnumerator<(int depth, int localFuncIndex)>
        {
            private readonly IList<int> _layout;
            private (int depth, int localFuncIndex) _current;

            public LayoutEnumerator(IList<int> layout)
            {
                _layout = layout;
                _current = (-1, -1);
            }

            public (int depth, int localFuncIndex) Current => _current;

            object IEnumerator.Current => throw new NotImplementedException();

            public void Dispose() => throw new NotImplementedException();

            public bool MoveNext()
            {
                if (_current.depth < 0)
                {
                    return FindNonEmptyDepth(0, _layout, out _current);
                }
                else
                {
                    int newIndex = _current.localFuncIndex + 1;
                    if (newIndex == _layout[_current.depth])
                    {
                        return FindNonEmptyDepth(_current.depth + 1, _layout, out _current);
                    }

                    _current = (_current.depth, newIndex);
                    return true;
                }

                bool FindNonEmptyDepth(int startingDepth, IList<int> layout, out (int depth, int localFuncIndex) newCurrent)
                {
                    for (int depth = startingDepth; depth < layout.Count; depth++)
                    {
                        if (layout[depth] > 0)
                        {
                            newCurrent = (depth, 0);
                            return true;
                        }
                    }
                    newCurrent = (layout.Count, 0);
                    return false;
                }
            }

            public void Reset() => throw new NotImplementedException();
        }

        private class MethodInfo
        {
            public MethodInfo(int MaxCaptures)
            {
                LocalFuncs = new List<IList<string>>();
                CaptureContext = new CaptureContext(MaxCaptures);
            }

            private MethodInfo() { }

            public List<IList<string>> LocalFuncs { get; private set; }

            public CaptureContext CaptureContext { get; private set; }

            public int TotalLocalFuncs { get; set; }

            public MethodInfo Clone()
            {
                return new MethodInfo
                {
                    LocalFuncs = this.LocalFuncs
                        .Select(x => x == null
                                     ? null
                                     : (IList<string>)new List<string>(x)).ToList(),
                    CaptureContext = this.CaptureContext.Clone()
                };
            }
        }

        private static IEnumerable<MethodInfo> MakeMethodsWithLayout(IList<int> localFuncLayout)
        {
            const int MaxCaptures = 3;

            var enumerator = new LayoutEnumerator(localFuncLayout);
            if (!enumerator.MoveNext())
            {
                return Array.Empty<MethodInfo>();
            }

            var methods = new List<MethodInfo>();
            DfsLayout(enumerator, new MethodInfo(MaxCaptures), 0);
            return methods;

            // Note that the enumerator is a struct, so every new var
            // is a copy
            void DfsLayout(LayoutEnumerator e, MethodInfo methodSoFar, int localFuncNameIndex)
            {
                var (depth, localFuncIndex) = e.Current;

                bool isLastFunc = !e.MoveNext();

                foreach (var captureCombo in GenerateAllSetCombinations(MaxCaptures, depth + 2))
                {
                    var copy = methodSoFar.Clone();
                    var expr = MakeCaptureExpression(captureCombo, copy.CaptureContext);
                    if (depth >= copy.LocalFuncs.Count)
                    {
                        copy.LocalFuncs.AddRange(Enumerable.Repeat<List<string>>(null, depth - copy.LocalFuncs.Count));
                        copy.LocalFuncs.Insert(depth, new List<string>());
                    }
                    string localFuncName = $"Local_{localFuncNameIndex}";
                    copy.LocalFuncs[depth].Add($"int {localFuncName}() => {expr};");
                    copy.CaptureContext.Add(depth + 1, $"{localFuncName}()");

                    if (!isLastFunc)
                    {
                        DfsLayout(e, copy, localFuncNameIndex + 1);
                    }
                    else
                    {
                        copy.TotalLocalFuncs = localFuncNameIndex + 1;
                        methods.Add(copy);
                    }
                }
            }
        }

        private static IEnumerable<MethodInfo> MakeAllMethods()
        {
            const int MaxDepth = 3;
            const int MaxLocalFuncs = 3;

            // Set combinations indicate what depth we will place local functions
            // at. For instance, { 0, 1 } indicates 0 local functions at method
            // depth and 1 local function at one nested scope below method level.
            foreach (var localFuncLayout in GenerateAllSetCombinations(MaxLocalFuncs, MaxDepth))
            {
                // Given a local function map, we need to generate capture
                // expressions for each local func at each depth
                foreach (var method in MakeMethodsWithLayout(localFuncLayout))
                    yield return method;
            }
        }

        private void SerializeMethod(MethodInfo methodInfo, StringBuilder builder, int methodIndex)
        {
            int totalLocalFuncs = methodInfo.TotalLocalFuncs;

            var methodText = new StringBuilder();
            var localFuncs = methodInfo.LocalFuncs;
            for (int depth = 0; depth < localFuncs.Count; depth++)
            {
                if (depth > 0)
                {
                    methodText.Append(' ', 4 * (depth + 1));
                    methodText.AppendLine("{");
                }

                var captureVars = methodInfo.CaptureContext.VariablesByScope;
                if (captureVars.Count > (depth + 1) &&
                    captureVars[depth + 1] != null)
                {
                    foreach (var captureVar in captureVars[depth + 1])
                    {
                        if (captureVar.EndsWith("()"))
                        {
                            continue;
                        }

                        methodText.Append(' ', 4 * (depth + 2));
                        methodText.AppendLine($"int {captureVar} = 0;");
                    }
                }

                if (localFuncs[depth] != null)
                {
                    foreach (var localFunc in localFuncs[depth])
                    {
                        methodText.Append(' ', 4 * (depth + 2));
                        methodText.AppendLine(localFunc);
                    }
                }
            }

            var localFuncCalls = string.Join(" + ",
                Enumerable.Range(0, totalLocalFuncs).Select(f => $"Local_{f}()"));
            methodText.AppendLine($"Console.WriteLine({localFuncCalls});");

            for (int depth = localFuncs.Count - 1; depth > 0; depth--)
            {
                methodText.Append(' ', 4 * (depth + 1));
                methodText.AppendLine("}");
            }

            builder.Append($@"
    public void M_{methodIndex}()
    {{
{methodText.ToString()}
    }}");

        }

        /// <summary>
        /// This test exercises the C# local function capturing analysis by generating
        /// all possible combinations of capturing within a certain complexity. The
        /// generating functions use a maximum number of variables captured per local function,
        /// a maximum number of local functions, and a maximum scope depth to decide the
        /// limits of the combinations.
        /// </summary>
        [ConditionalFact(typeof(WindowsOnly), typeof(NoIOperationValidation), Reason = "https://github.com/dotnet/roslyn/issues/30212")]
        public void AllCaptureTests()
        {
            var methods = MakeAllMethods().ToList();

            var fields = methods.First().CaptureContext.VariablesByScope[0];

            const int PartitionSize = 500;
            const string ClassFmt = @"
using System;
public class C
{{
    {0}";
            StringBuilder GetClassStart()
                => new StringBuilder(string.Format(ClassFmt,
                    string.Join("\r\n", fields.Select(f => $"public int {f} = 0;"))));

            Parallel.ForEach(Partitioner.Create(0, methods.Count, PartitionSize), (range, state) =>
            {
                var methodsText = GetClassStart();

                for (int methodIndex = range.Item1; methodIndex < range.Item2; methodIndex++)
                {
                    var methodInfo = methods[methodIndex];

                    if (methodInfo.TotalLocalFuncs == 0)
                    {
                        continue;
                    }

                    SerializeMethod(methodInfo, methodsText, methodIndex);
                }

                methodsText.AppendLine("\r\n}");
                CreateCompilation(methodsText.ToString()).VerifyEmitDiagnostics();
            });
        }
    }
}
