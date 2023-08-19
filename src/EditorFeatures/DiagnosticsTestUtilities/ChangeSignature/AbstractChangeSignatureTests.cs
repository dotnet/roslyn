// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.ChangeSignature;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature
{
    public abstract class AbstractChangeSignatureTests : AbstractCodeActionTest
    {
        protected override ParseOptions GetScriptOptions()
            => throw new NotSupportedException();

        protected override TestComposition GetComposition()
            => base.GetComposition().AddParts(typeof(TestChangeSignatureOptionsService));

        internal async Task TestChangeSignatureViaCodeActionAsync(
            string markup,
            bool expectedCodeAction = true,
            int[] updatedSignature = null,
            string expectedCode = null,
            int index = 0)
            => await TestChangeSignatureViaCodeActionAsync(
                markup, updatedSignature?.Select(i => new AddedParameterOrExistingIndex(i)).ToArray(),
                expectedCodeAction, expectedCode, index).ConfigureAwait(false);

        internal async Task TestChangeSignatureViaCodeActionAsync(
            string markup,
            AddedParameterOrExistingIndex[] updatedSignature,
            bool expectedCodeAction = true,
            string expectedCode = null,
            int index = 0)
        {
            if (expectedCodeAction)
            {
                var testOptions = new TestParameters();

                using var workspace = CreateWorkspaceFromOptions(markup, testOptions);
                var optionsService = (TestChangeSignatureOptionsService)workspace.Services.GetRequiredService<IChangeSignatureOptionsService>();
                optionsService.UpdatedSignature = updatedSignature;

                var refactoring = await GetCodeRefactoringAsync(workspace, testOptions);
                await TestActionAsync(workspace, expectedCode, refactoring.CodeActions[index].action,
                    conflictSpans: ImmutableArray<TextSpan>.Empty,
                    renameSpans: ImmutableArray<TextSpan>.Empty,
                    warningSpans: ImmutableArray<TextSpan>.Empty,
                    navigationSpans: ImmutableArray<TextSpan>.Empty,
                    parameters: null);
            }
            else
            {
                await TestMissingAsync(markup);
            }
        }

        internal static async Task TestChangeSignatureViaCommandAsync(
            string languageName,
            string markup,
            bool expectedSuccess = true,
            int[] updatedSignature = null,
            string expectedUpdatedInvocationDocumentCode = null,
            ChangeSignatureFailureKind? expectedFailureReason = null,
            int? totalParameters = null,
            bool verifyNoDiagnostics = false,
            ParseOptions parseOptions = null,
            OptionsCollection options = null,
            int expectedSelectedIndex = -1)
            => await TestChangeSignatureViaCommandAsync(languageName, markup,
                updatedSignature?.Select(i => new AddedParameterOrExistingIndex(i)).ToArray(),
                expectedSuccess, expectedUpdatedInvocationDocumentCode,
                expectedFailureReason,
                totalParameters,
                verifyNoDiagnostics,
                parseOptions,
                options,
                expectedSelectedIndex);

        internal static async Task TestChangeSignatureViaCommandAsync(
            string languageName,
            string markup,
            AddedParameterOrExistingIndex[] updatedSignature,
            bool expectedSuccess = true,
            string expectedUpdatedInvocationDocumentCode = null,
            ChangeSignatureFailureKind? expectedFailureReason = null,
            int? totalParameters = null,
            bool verifyNoDiagnostics = false,
            ParseOptions parseOptions = null,
            OptionsCollection options = null,
            int expectedSelectedIndex = -1)
        {
            using (var testState = ChangeSignatureTestState.Create(markup, languageName, parseOptions, options))
            {
                testState.TestChangeSignatureOptionsService.UpdatedSignature = updatedSignature;
                var result = await testState.ChangeSignatureAsync().ConfigureAwait(false);

                if (expectedSuccess)
                {
                    Assert.True(result.Succeeded);
                    Assert.Null(result.ChangeSignatureFailureKind);
                }
                else
                {
                    Assert.False(result.Succeeded);

                    if (expectedFailureReason != null)
                    {
                        Assert.Equal(expectedFailureReason, result.ChangeSignatureFailureKind);
                    }
                }

                // Allow testing of invocation document regardless of success/failure
                if (expectedUpdatedInvocationDocumentCode != null)
                {
                    var updatedInvocationDocument = result.UpdatedSolution.GetDocument(testState.InvocationDocument.Id);
                    var updatedCode = (await updatedInvocationDocument.GetTextAsync()).ToString();
                    Assert.Equal(expectedUpdatedInvocationDocumentCode, updatedCode);
                }

                if (verifyNoDiagnostics)
                {
                    var diagnostics = (await testState.InvocationDocument.GetSemanticModelAsync()).GetDiagnostics();

                    if (diagnostics.Length > 0)
                    {
                        Assert.True(false, CreateDiagnosticsString(diagnostics, updatedSignature, testState.InvocationDocument, totalParameters, (await testState.InvocationDocument.GetTextAsync()).ToString()));
                    }
                }

                if (expectedSelectedIndex != -1)
                {
                    var parameterConfiguration = await testState.GetParameterConfigurationAsync();
                    Assert.Equal(expectedSelectedIndex, parameterConfiguration.SelectedIndex);
                }
            }
        }

        private static string CreateDiagnosticsString(ImmutableArray<Diagnostic> diagnostics, AddedParameterOrExistingIndex[] permutation, Document document, int? totalParameters, string fileContents)
        {
            if (diagnostics.Length == 0)
            {
                return string.Empty;
            }

            return string.Format("{0} diagnostic(s) introduced in signature configuration \"{1}\":\r\n{2}\r\n{3}",
                diagnostics.Length,
                GetSignatureDescriptionString(document, permutation, totalParameters),
                string.Join("\r\n", diagnostics.Select(d => d.GetMessage())),
                fileContents);
        }

        private static string GetSignatureDescriptionString(Document document, AddedParameterOrExistingIndex[] signature, int? totalParameters)
        {
            var existingParametersKept = signature.Where(p => p.IsExisting).Select(p => p.OldIndex).ToArray();
            var removeDescription = string.Empty;
            if (totalParameters.HasValue)
            {
                var removed = new List<int>();
                for (var i = 0; i < totalParameters; i++)
                {
                    if (!existingParametersKept.Contains(i))
                    {
                        removed.Add(i);
                    }
                }

                removeDescription = removed.Any() ? string.Format(", Removed: {{{0}}}", string.Join(", ", removed)) : string.Empty;
            }

            var newParametersString = string.Join(",", signature.Where(p => !p.IsExisting).Select(p => p.GetAddedParameter(document)));
            var addDescription = !newParametersString.IsEmpty() ? string.Format(", Added {{{0}}}", newParametersString) : string.Empty;

            return string.Format("Parameters: <{0}>{1}{2}", string.Join(", ", signature.Select(item => item.ToString())), removeDescription, addDescription);
        }

        /// <summary>
        /// Tests all possible changed signatures to ensure no diagnostics are introduced. Given a
        /// signature 
        ///     Tr M(this T0 t0, T1 t1, ... Tm tm, D1 d1 = v1, ..., Dn dn = vn, params U u)
        /// with s this parameters (0 or 1), m >= 0 simple parameters, n >= 0 parameters with
        /// default values, and p params parameters (0 or 1), there are 
        /// Π s∈{m, n} (Σ k=0 to s (sCk * (s-k)!)) * 2^p - 1 changed signatures to consider.
        /// </summary>
        /// <param name="signaturePartCounts">A four element array containing [s, m, n, p] as 
        /// described above.</param>
        public static IEnumerable<object[]> GetAllSignatureSpecificationsForTheory(int[] signaturePartCounts)
        {
            Assert.Equal(4, signaturePartCounts.Length);
            Assert.True(signaturePartCounts[0] is 0 or 1);
            Assert.True(signaturePartCounts[3] is 0 or 1);

            var totalParameters = signaturePartCounts[0] + signaturePartCounts[1] + signaturePartCounts[2] + signaturePartCounts[3];

            foreach (var signature in GetAllSignatureSpecifications(signaturePartCounts))
            {
                yield return new object[] { totalParameters, signature };
            }
        }

        private static IEnumerable<int[]> GetAllSignatureSpecifications(int[] signaturePartCounts)
        {
            var regularParameterStartIndex = signaturePartCounts[0];
            var defaultValueParameterStartIndex = signaturePartCounts[0] + signaturePartCounts[1];
            var paramParameterIndex = signaturePartCounts[0] + signaturePartCounts[1] + signaturePartCounts[2];

            var regularParameterArrangements = GetPermutedSubsets(regularParameterStartIndex, signaturePartCounts[1]);
            var defaultValueParameterArrangements = GetPermutedSubsets(defaultValueParameterStartIndex, signaturePartCounts[2]);

            var startArray = signaturePartCounts[0] == 0 ? Array.Empty<int>() : [0];

            foreach (var regularParameterPart in regularParameterArrangements)
            {
                foreach (var defaultValueParameterPart in defaultValueParameterArrangements)
                {
                    var p1 = startArray.Concat(regularParameterPart).Concat(defaultValueParameterPart);
                    yield return p1.ToArray();

                    if (signaturePartCounts[3] == 1)
                    {
                        yield return p1.Concat(new[] { paramParameterIndex }).ToArray();
                    }
                }
            }
        }

        private static IEnumerable<IEnumerable<int>> GetPermutedSubsets(int startIndex, int count)
        {
            foreach (var subset in GetSubsets(Enumerable.Range(startIndex, count)))
            {
                foreach (var permutation in GetPermutations(subset))
                {
                    yield return permutation;
                }
            }
        }

        private static IEnumerable<IEnumerable<int>> GetPermutations(IEnumerable<int> list)
        {
            if (!list.Any())
            {
                yield return SpecializedCollections.EmptyEnumerable<int>();
                yield break;
            }

            var index = 0;
            foreach (var element in list)
            {
                var permutationsWithoutElement = GetPermutations(GetListWithoutElementAtIndex(list, index));
                foreach (var perm in permutationsWithoutElement)
                {
                    yield return perm.Concat(element);
                }

                index++;
            }
        }

        private static IEnumerable<int> GetListWithoutElementAtIndex(IEnumerable<int> list, int skippedIndex)
        {
            var index = 0;
            foreach (var x in list)
            {
                if (index != skippedIndex)
                {
                    yield return x;
                }

                index++;
            }
        }

        private static IEnumerable<IEnumerable<int>> GetSubsets(IEnumerable<int> list)
        {
            if (!list.Any())
            {
                return SpecializedCollections.SingletonEnumerable(SpecializedCollections.EmptyEnumerable<int>());
            }

            var firstElement = list.Take(1);

            var withoutFirstElement = GetSubsets(list.Skip(1));
            var withFirstElement = withoutFirstElement.Select(without => firstElement.Concat(without));

            return withoutFirstElement.Concat(withFirstElement);
        }
    }
}
