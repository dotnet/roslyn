// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.NET.Sdk.Razor.SourceGenerators.Diagnostics;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    internal static class RazorDiagnostics
    {
        public static readonly DiagnosticDescriptor InvalidRazorLangVersionDescriptor =
            CreateDescriptor(
                DiagnosticIds.InvalidRazorLangVersionRuleId,
                nameof(RazorSourceGeneratorResources.InvalidRazorLangTitle),
                nameof(RazorSourceGeneratorResources.InvalidRazorLangMessage),
                DiagnosticSeverity.Error);

        public static readonly DiagnosticDescriptor InvalidRazorWarningLevelDescriptor =
            CreateDescriptor(
                DiagnosticIds.InvalidRazorWarningLevelRuleId,
                nameof(RazorSourceGeneratorResources.InvalidRazorWarningLevelTitle),
                nameof(RazorSourceGeneratorResources.InvalidRazorWarningLevelMessage),
                DiagnosticSeverity.Error);

        public static readonly DiagnosticDescriptor ReComputingTagHelpersDescriptor =
            CreateDescriptor(
                DiagnosticIds.ReComputingTagHelpersRuleId,
                nameof(RazorSourceGeneratorResources.RecomputingTagHelpersTitle),
                nameof(RazorSourceGeneratorResources.RecomputingTagHelpersMessage),
                DiagnosticSeverity.Info);

        public static readonly DiagnosticDescriptor TargetPathNotProvided =
            CreateDescriptor(
                DiagnosticIds.TargetPathNotProvidedRuleId,
                nameof(RazorSourceGeneratorResources.TargetPathNotProvidedTitle),
                nameof(RazorSourceGeneratorResources.TargetPathNotProvidedMessage),
                DiagnosticSeverity.Warning);

        public static readonly DiagnosticDescriptor GeneratedOutputFullPathNotProvided =
            CreateDescriptor(
                DiagnosticIds.GeneratedOutputFullPathNotProvidedRuleId,
                nameof(RazorSourceGeneratorResources.GeneratedOutputFullPathNotProvidedTitle),
                nameof(RazorSourceGeneratorResources.GeneratedOutputFullPathNotProvidedMessage),
                DiagnosticSeverity.Warning);

        public static readonly DiagnosticDescriptor CurrentCompilationReferenceNotFoundDescriptor =
            CreateDescriptor(
                DiagnosticIds.CurrentCompilationReferenceNotFoundId,
                nameof(RazorSourceGeneratorResources.CurrentCompilationReferenceNotFoundTitle),
                nameof(RazorSourceGeneratorResources.CurrentCompilationReferenceNotFoundMessage),
                DiagnosticSeverity.Warning);

        public static readonly DiagnosticDescriptor SkippingGeneratedFileWriteDescriptor =
            CreateDescriptor(
                DiagnosticIds.SkippingGeneratedFileWriteId,
                nameof(RazorSourceGeneratorResources.SkippingGeneratedFileWriteTitle),
                nameof(RazorSourceGeneratorResources.SkippingGeneratedFileWriteMessage),
                DiagnosticSeverity.Warning);

        public static readonly DiagnosticDescriptor SourceTextNotFoundDescriptor =
            CreateDescriptor(
                DiagnosticIds.SourceTextNotFoundId,
                nameof(RazorSourceGeneratorResources.SourceTextNotFoundTitle),
                nameof(RazorSourceGeneratorResources.SourceTextNotFoundMessage),
                DiagnosticSeverity.Error);

        public static readonly DiagnosticDescriptor UnexpectedProjectItemReadCallDescriptor =
            CreateDescriptor(
                DiagnosticIds.UnexpectedProjectItemReadCallId,
                nameof(RazorSourceGeneratorResources.UnexpectedProjectItemReadCallTitle),
                nameof(RazorSourceGeneratorResources.UnexpectedProjectItemReadCallMessage),
                DiagnosticSeverity.Error);

        public static readonly DiagnosticDescriptor InvalidRazorContextComputedDescriptor =
            CreateDescriptor(
                DiagnosticIds.InvalidRazorContextComputedId,
                nameof(RazorSourceGeneratorResources.InvalidRazorContextComputedTitle),
                nameof(RazorSourceGeneratorResources.InvalidRazorContextComputedMessage),
                DiagnosticSeverity.Info);

        public static readonly DiagnosticDescriptor MetadataReferenceNotProvidedDescriptor =
            CreateDescriptor(
                DiagnosticIds.MetadataReferenceNotProvidedId,
                nameof(RazorSourceGeneratorResources.MetadataReferenceNotProvidedTitle),
                nameof(RazorSourceGeneratorResources.MetadataReferenceNotProvidedMessage),
                DiagnosticSeverity.Info);

        public static Diagnostic AsDiagnostic(this RazorDiagnostic razorDiagnostic)
        {
            var descriptor = new DiagnosticDescriptor(
                razorDiagnostic.Id,
                razorDiagnostic.GetMessage(CultureInfo.CurrentCulture),
                razorDiagnostic.GetMessage(CultureInfo.CurrentCulture),
                "Razor",
                razorDiagnostic.Severity switch
                {
                    RazorDiagnosticSeverity.Error => DiagnosticSeverity.Error,
                    RazorDiagnosticSeverity.Warning => DiagnosticSeverity.Warning,
                    _ => DiagnosticSeverity.Hidden,
                },
                isEnabledByDefault: true);

            var span = razorDiagnostic.Span;

            Location location;
            if (span == SourceSpan.Undefined)
            {
                // TextSpan.Empty
                location = Location.None;
            }
            else
            {
                var linePosition = new LinePositionSpan(
                    new LinePosition(span.LineIndex, span.CharacterIndex),
                    new LinePosition(span.LineIndex, span.CharacterIndex + span.Length));

                location = Location.Create(
                   span.FilePath,
                   span.AsTextSpan(),
                   linePosition);
            }

            return Diagnostic.Create(descriptor, location);
        }

        private static DiagnosticDescriptor CreateDescriptor(string id, string titleResourceName, string messageResourceName, DiagnosticSeverity defaultSeverity)
        {
            return new DiagnosticDescriptor(
                id,
                new LocalizableResourceString(titleResourceName, RazorSourceGeneratorResources.ResourceManager, typeof(RazorSourceGeneratorResources)),
                new LocalizableResourceString(messageResourceName, RazorSourceGeneratorResources.ResourceManager, typeof(RazorSourceGeneratorResources)),
                "RazorSourceGenerator",
                defaultSeverity,
                isEnabledByDefault: true);
        }
    }
}
