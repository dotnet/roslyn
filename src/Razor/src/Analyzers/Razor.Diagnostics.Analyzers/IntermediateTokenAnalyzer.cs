// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Razor.Diagnostics.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class IntermediateTokenAnalyzer : DiagnosticAnalyzer
{
    private const string Title = "IntermediateToken with Kind CSharp";
    private const string MessageFormat = "Avoid directly creating an IntermediateToken with Kind 'CSharp'. Instead, lower to an appropriate intermediate node and emit the C# in the output writer.";
    private const string BaselineFileName = "IntermediateTokenBaseline.txt";

    internal static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.RawIntermediateTokenCreation,
        Title,
        MessageFormat,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
        context.RegisterOperationAction(AnalyzeMethodInvocation, OperationKind.Invocation);
    }

    private void AnalyzeObjectCreation(OperationAnalysisContext context)
    {
        var objectCreation = (IObjectCreationOperation)context.Operation;

        // Check if the type is IntermediateToken
        if (objectCreation.Type?.Name != "IntermediateToken")
        {
            return;
        }

        // Resolve the TokenKind.CSharp enum value
        var tokenKindType = context.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Razor.Language.Intermediate.TokenKind");
        if (tokenKindType == null)
        {
            return; // TokenKind enum not found
        }

        var csharpEnumMember = tokenKindType.GetMembers("CSharp").FirstOrDefault() as IFieldSymbol;
        if (csharpEnumMember == null)
        {
            return; // TokenKind.CSharp enum member not found
        }

        // Check if the Kind property is set to TokenKind.CSharp via an initializer
        var kindInitializer = objectCreation.Initializer?.Initializers
            .OfType<ISimpleAssignmentOperation>()
            .FirstOrDefault(init =>
                init.Target is IPropertyReferenceOperation property &&
                property.Property.Name == "Kind" &&
                init.Value.ConstantValue.HasValue &&
                init.Value.ConstantValue.Value == csharpEnumMember.ConstantValue);

        // If the initializer doesnt set Kind to TokenKind.CSharp, return
        if (kindInitializer == null)
        {
            return;
        }

        // Report the diagnostic if not suppressed
        ReportIfNotSuppressed(context, objectCreation.Syntax.GetLocation());
    }

    private void AnalyzeMethodInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;

        // Check if the method is IntermediateToken.CreateCSharpToken
        if (invocation.TargetMethod.Name != "CreateCSharpToken" ||
            invocation.TargetMethod.ContainingType.Name != "IntermediateToken")
        {
            return;
        }

        ReportIfNotSuppressed(context, invocation.Syntax.GetLocation());
    }

    private void ReportIfNotSuppressed(OperationAnalysisContext context, Location location)
    {
        // If we're a test project, ignore.
        if (context.Compilation.AssemblyName?.Contains(".Test") == true)
        {
            return;
        }

        // Check if the warning is suppressed in the baseline file
        // Note: code for regenerating the baseline from scratch can be found here: https://gist.github.com/chsienki/d06b2a3ee583191cacf80da79f6fc540
        var additionalFiles = context.Options.AdditionalFiles;
        var baselineFile = additionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path) == BaselineFileName);
        if (baselineFile != null)
        {
            var baselineDirectory = Path.GetDirectoryName(baselineFile.Path);
            var locationFilePath = location.SourceTree?.FilePath;

            var relativePath = locationFilePath?.Substring(baselineDirectory?.Length + 1 ?? 0).Replace("\\", "/");
            var linePosition = location.GetLineSpan().StartLinePosition;
            var locationString = $"{relativePath}:{linePosition.Line + 1}:{linePosition.Character + 1}";

            var baselineContent = baselineFile.GetText(context.CancellationToken)?.ToString();
            if (baselineContent != null && baselineContent.Contains(locationString))
            {
                return;
            }
        }

        // Report the diagnostic
        context.ReportDiagnostic(Diagnostic.Create(Rule, location));
    }
}
