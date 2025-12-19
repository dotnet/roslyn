// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Xunit;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTests.Razor
{
    public class RazorPredefinedProviderNameTests
    {
        [Theory(Skip = "https://github.com/dotnet/roslyn/issues/58263")]
        [InlineData(typeof(PredefinedCodeFixProviderNames), typeof(RazorPredefinedCodeFixProviderNames))]
        [InlineData(typeof(PredefinedCodeRefactoringProviderNames), typeof(RazorPredefinedCodeRefactoringProviderNames))]
        internal void RoslynNamesExistAndValuesMatchInRazorNamesClass(Type roslynProviderNamesType, Type razorProviderNamesType)
        {
            var roslynProviderNames = GetPredefinedNamesFromFields(roslynProviderNamesType);
            var razorProviderNames = GetPredefinedNamesFromProperties(razorProviderNamesType);

            var failureMessage = new StringBuilder();
            failureMessage.AppendLine($"The following Names were inconsistent between {roslynProviderNamesType.Name} and {razorProviderNamesType.Name}:");
            var passLength = failureMessage.Length;

            // Validates that all names from Roslyn's PredefinedCode*ProviderNames class exist in the RazorPredefinedCode*ProviderNames class
            // and that they have the same value. It does not fail if the Razor class contains names no longer in the Roslyn class as they may
            // need to remain for back-compat.
            foreach (var roslynKvp in roslynProviderNames)
            {
                if (!razorProviderNames.TryGetValue(roslynKvp.Key, out var razorValue))
                {
                    failureMessage.AppendLine($"The Name '{roslynKvp.Key}' does not exist.");
                }

                if (roslynKvp.Value != razorValue)
                {
                    failureMessage.AppendLine($"The Value of '{roslynKvp.Key}' does not match.");
                }
            }

            Assert.True(failureMessage.Length == passLength, failureMessage.ToString());
        }

        private static ImmutableDictionary<string, string> GetPredefinedNamesFromFields(Type namesType)
        {
            return namesType.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public)
                .Where(field => field.FieldType == typeof(string))
                .ToImmutableDictionary(field => field.Name, field => (string)field.GetValue(null)!);
        }

        private static ImmutableDictionary<string, string> GetPredefinedNamesFromProperties(Type namesType)
        {
            return namesType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public)
                .Where(property => property.PropertyType == typeof(string))
                .ToImmutableDictionary(property => property.Name, property => (string)property.GetValue(null)!);
        }
    }
}
