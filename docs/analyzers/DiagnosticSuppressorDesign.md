# Feature Request

https://github.com/dotnet/roslyn/issues/30172: _Programmatic suppression of warnings_

Provide an ability for platform/library authors to author simple, context-aware compiler extensions to programmatically suppress specific instances of reported analyzer and/or compiler diagnostics, which are always known to be false positives in the context of the platform/library.

# Programmatically Suppressible Diagnostic

An analyzer/compiler diagnostic would be considered a candidate for programmatic suppression if **all** of the following conditions hold:
1. _Not an error by default_: Diagnostic's [DefaultSeverity](http://source.roslyn.io/#Microsoft.CodeAnalysis/Diagnostic/Diagnostic.cs,8e27a878b4d6e40c) is *not* [DiagnosticSeverity.Error](http://source.roslyn.io/#Microsoft.CodeAnalysis/Diagnostic/DiagnosticSeverity.cs,f771032fb5a00c1c).
2. _Must be configurable_: Diagnostic is *not* tagged with [WellKnownDiagnosticTags.NotConfigurable](http://source.roslyn.io/#Microsoft.CodeAnalysis/Diagnostic/WellKnownDiagnosticTags.cs,207e57dd0b96bd4b) custom tag, indicating that its severity is configurable.
3. _No existing source suppression_: Diagnostic is *not* already suppressed in source via pragma/suppress message attribute.

# Core Design

1. _For platform/library authors:_ Expose a new "DiagnosticSuppressor" public API for authoring such compiler extensions. API contract for the suppressors (detailed API with doc comments in the [last section](https://gist.github.com/mavasani/fcac17a9581b5c54cef8a689eeec954a#detailed-api-proposal-with-documentation-comments-from-the-draft-pr)):
   1. Declaratively provide all the analyzer and/or compiler diagnostic IDs that can be suppressed by it.
   2. For each such suppressible diagnostic ID, provide a unique suppression ID and justification. These are required to enable proper diagnosis and configuration of each suppression (covered in detail [later](https://gist.github.com/mavasani/fcac17a9581b5c54cef8a689eeec954a#example-experience-with-screenshots)).
   3. Register callbacks for reported analyzer and/or compiler diagnostics with these suppressible diagnostic IDs. The callback can analyze the syntax and/or semantics of the diagnostic location and report a suppression for it.
   4. DiagnosticSuppressors would *not* be able to register any another analysis callbacks, and hence cannot report any new diagnostics.
2. _For end users:_
   1. Seamless development experience when targeting such a platform/library, whereby the users do not see false positives from analyzers/compiler in their development context.
   2. End users do not need to manually add and/or maintain source suppressions through pragmas/SuppressMessageAttributes for such context-specific false positives.
   3. End users still have the ultimate control over suppressors (specifics covered in [later section](https://gist.github.com/mavasani/fcac17a9581b5c54cef8a689eeec954a#example-experience-with-screenshots)):
      1. Audit: Diagnostic suppressions are logged as "Info" diagnostics on the command line, so the end users can audit each suppression in the verbose build log or msbuild binlog. Additionally, they are also logged in the [/errorlog SARIF file](https://github.com/dotnet/roslyn/blob/master/docs/compilers/Error%20Log%20Format.md) as suppressed diagnostics.
      2. Configuration: Each diagnostic suppression has an associated suppression ID. This is clearly indicated in the logged "Info" diagnostics for suppressions, and the end users can disable the bucket of suppressions under any specific suppression ID with simple command line argument or ruleset entries.
 3. _For analyzer driver:_
    1. Order of execution between analyzers/compiler and suppressors: For command line builds, all diagnostic suppressors will run **after** all the analyzer and compiler diagnostics have been computed. For live analysis in Visual Studio, diagnostic suppressors may be invoked with a subset of the full set of reported diagnostics, as an optimization for supporting incremental and partial analysis scenarios.
    2. Order of execution between individual suppressors: Execution of diagnostic suppressors would be independent of other suppressors. Diagnostic suppressions reported by one suppressor would not affect the input set of reported diagnostics passed to any other suppressor. Each diagnostic might be programmatically suppressed by more then one suppressor, so the analyzer driver will union all the reported suppressions for each diagnostic. Command line compiler will log one suppression diagnostic per-suppression for each programmatically suppressed diagnostic.
 
# Development Experience Example

Let us consider the core example for UnityEngine covered in https://github.com/dotnet/roslyn/issues/30172:

```csharp
using UnityEngine;

class BulletMovement : MonoBehaviour
{
    [SerializeField] private float speed;

    private void Update()
    {
         this.transform.position *= speed;
    }
}
```

Serialization frameworks and tools embedding .NET Core or Mono often set the value of fields or call methods outside of user code. Such code is not available for analysis to the compiler and analyzers, leading to false reports such as following:
1. [CS0649](https://docs.microsoft.com/en-us/dotnet/csharp/misc/cs0649): `Field 'speed' is never assigned to, and will always have its default value 0`
2. [IDE0044 dotnet_style_readonly_field](https://docs.microsoft.com/en-us/visualstudio/ide/editorconfig-code-style-settings-reference?view=vs-2019): Make field 'speed' readonly

## New experience with screenshots:
1. _For platform/library authors:_ UnityEngine can selectively suppress instances of such diagnostics by writing a simple [SerializedFieldDiagnosticSuppressor](https://gist.github.com/mavasani/fcac17a9581b5c54cef8a689eeec954a#file-serializedfielddiagnosticsuppressor-cs) extension based on the DiagnosticSuppressor API and packaging it along with the Unity framework SDK/library.
2. _For end users:_
   1. Users do not see above false positives in the IDE live analysis or command line build. For example, see the below screenshots with and without the SerializedFieldDiagnosticSuppressor:
      1. Without the suppressor: See image [here](https://gist.github.com/mavasani/fcac17a9581b5c54cef8a689eeec954a#file-without_suppressor-png)
      2. With the suppressor: See image [here](https://gist.github.com/mavasani/fcac17a9581b5c54cef8a689eeec954a#file-with_suppressor-png)
   2. Audit suppressions:
      1. Command line build: End users will see "Info" severity suppression diagnostics in command line builds, see image [here](https://gist.github.com/mavasani/fcac17a9581b5c54cef8a689eeec954a#file-suppression_info_diagnostic-png)
      `info SPR0001: Diagnostic 'CS0649' was programmatically suppressed by a DiagnosticSuppressor with suppresion ID 'SPR1001' and justification 'Field symbols marked with SerializedFieldAttribute are implicitly assigned by UnityEngine at runtime and hence do not have its default value'`
      These diagnostics will *always* be logged in the [msbuild binlog](https://github.com/Microsoft/msbuild/blob/master/documentation/wiki/Binary-Log.md). They would not be visible in the console output for regular msbuild invocations, but increasing the msbuild verbosity to detailed or diagnostic will emit these diagnostics.
      2. Visual Studio IDE: Users can see the original suppressed diagnostic in the Visual Studio Error List when they change the default filter for the "Suppression State" column in the error list to include "Suppressed" diagnostics, see image [here](https://gist.github.com/mavasani/fcac17a9581b5c54cef8a689eeec954a#file-view_suppressed_diagnostic_errorlist-png). In future, we also plan to add a new error list column, possibly named "Suppression Source", which will display the suppression ID and justification associated with each programmatic suppression.
   3. Disable suppressors: End users can disable specific suppression IDs from an analyzer package using the following two mechanisms:
      1. `/nowarn:<%suppression_id%>`: See image [here](https://gist.github.com/mavasani/fcac17a9581b5c54cef8a689eeec954a#file-disable_suppressor_nowarn-png) for disabling the suppression ID from the project property page, which in turn generates the nowarn command line argument.
      2. Ruleset entry: See image [here](https://gist.github.com/mavasani/fcac17a9581b5c54cef8a689eeec954a#file-disable_suppressor_rulesetentry-png) for disabling the suppression ID using a [CodeAnalysis ruleset](https://docs.microsoft.com/en-us/visualstudio/code-quality/using-rule-sets-to-group-code-analysis-rules?view=vs-2019)
   
# Open Questions:
1. _Should the DiagnsoticSuppressor feature be Opt-in OR on-by-default:_ This was brought up few times in the past, especially with the concern with _on-by-default_ behavior being that an end user would not see any indication in the command line that diagnostic suppressors were executed as part of their build. I think we have following options:
   1. Opt-in:
      1. Keep the feature behind a new, _permanent_ feature flag: This way the command line arguments will always indicate if diagnostic suppressors were involved in the build.
      2. Add a new command line switch to enable suppressors
   2. On-By-default: No command line argument indicating suppressors are involved in a build, but users can look at binlogs and/or verbose msbuild output logs to determine the same.
   
   The [draft PR](https://gist.github.com/mavasani/fcac17a9581b5c54cef8a689eeec954a#draft-pr-implementing-the-above-proposal) for this feature currently puts the feature being a feature flag. If we decide to take the _on-by-default_ route, I can revert the changes adding the feature flag. Otherwise, if we decide that we should make it opt-in with a new command line compiler switch, I would like to propose that we keep the feature flag for the initial PR, and then have a follow-up PR to add the command line switch.

   **Update:** We have decided to keep the feature being a temporary feature flag for initial release, which will be removed once we are confident about the feature's performance, user experience, etc.
   
2. _Message for the "Info" suppression diagnostic_: I have chosen the below message format (with 3 format arguments), but any suggested changes are welcome:
   `Diagnostic 'CS0649' was programmatically suppressed by a DiagnosticSuppressor with suppresion ID 'SPR1001' and justification 'Field symbols marked with SerializedFieldAttribute are implicitly assigned by UnityEngine at runtime and hence do not have its default value'`

3. _What should be the diagnostic ID for the "Info" suppression diagnostic?_: We have following options:
   1. Use a CSxxxx/BCxxxx diagnostic ID for the suppression diagnostic so it is evident that the diagnostic is coming from the core compiler.
   2. Use a distinct diagnostic prefix, such as 'SPR0001', similar to the way we report 'AD0001' diagnostics for analyzer exceptions. This provides a separate bucketing/categorization for these special suppression diagnostics.
   The [draft PR](https://gist.github.com/mavasani/fcac17a9581b5c54cef8a689eeec954a#draft-pr-implementing-the-above-proposal) chooses the second approach with 'SPR0001' as the diagnostic ID.

   **Update:** We have decided the diagnostic ID would be `SP0001`.
   
# Draft PR implementing the above proposal:

https://github.com/dotnet/roslyn/pull/36067

# Detailed API proposal with documentation comments from the Draft PR

```csharp
namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// The base type for diagnostic suppressors that can programmatically suppress analyzer and/or compiler non-error diagnostics.
    /// </summary>
    public abstract class DiagnosticSuppressor : DiagnosticAnalyzer
    {
        // Disallow suppressors from reporting diagnostics or registering analysis actions.
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;

        public sealed override void Initialize(AnalysisContext context) { }

        /// <summary>
        /// Returns a set of descriptors for the suppressions that this suppressor is capable of producing.
        /// </summary>
        public abstract ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; }

        /// <summary>
        /// Suppress analyzer and/or compiler non-error diagnostics reported for the compilation.
        /// This may be a subset of the full set of reported diagnostics, as an optimization for
        /// supporting incremental and partial analysis scenarios.
        /// A diagnostic is considered suppressible by a DiagnosticSuppressor if *all* of the following conditions are met:
        ///     1. Diagnostic is not already suppressed in source via pragma/suppress message attribute.
        ///     2. Diagnostic's <see cref="Diagnostic.DefaultSeverity"/> is not <see cref="DiagnosticSeverity.Error"/>.
        ///     3. Diagnostic is not tagged with <see cref="WellKnownDiagnosticTags.NotConfigurable"/> custom tag.
        /// </summary>
        public abstract void ReportSuppressions(SuppressionAnalysisContext context);
    }

    /// <summary>
    /// Provides a description about a programmatic suppression of a <see cref="Diagnostic"/> by a <see cref="DiagnosticSuppressor"/>.
    /// </summary>
    public sealed class SuppressionDescriptor : IEquatable<SuppressionDescriptor>
    {
        /// <summary>
        /// An unique identifier for the suppression.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Identifier of the suppressed diagnostic, i.e. <see cref="Diagnostic.Id"/>.
        /// </summary>
        public string SuppressedDiagnosticId { get; }

        /// <summary>
        /// A localizable description about the suppression.
        /// </summary>
        public LocalizableString Description { get; }
    }

    /// <summary>
    /// Context for suppressing analyzer and/or compiler non-error diagnostics reported for the compilation.
    /// </summary>
    public struct SuppressionAnalysisContext
    {
        /// <summary>
        /// Suppressible analyzer and/or compiler non-error diagnostics reported for the compilation.
        /// This may be a subset of the full set of reported diagnostics, as an optimization for
        /// supporting incremental and partial analysis scenarios.
        /// A diagnostic is considered suppressible by a DiagnosticSuppressor if *all* of the following conditions are met:
        ///     1. Diagnostic is not already suppressed in source via pragma/suppress message attribute.
        ///     2. Diagnostic's <see cref="Diagnostic.DefaultSeverity"/> is not <see cref="DiagnosticSeverity.Error"/>.
        ///     3. Diagnostic is not tagged with <see cref="WellKnownDiagnosticTags.NotConfigurable"/> custom tag.
        /// </summary>
        public ImmutableArray<Diagnostic> ReportedDiagnostics { get; }

        /// <summary>
        /// Report a <see cref="Suppression"/> for a reported diagnostic.
        /// </summary>
        public void ReportSuppression(Suppression suppression);

        /// <summary>
        /// Gets a <see cref="SemanticModel"/> for the given <see cref="SyntaxTree"/>, which is shared across all analyzers.
        /// </summary>
        public SemanticModel GetSemanticModel(SyntaxTree syntaxTree);

        /// <summary>
        /// <see cref="CodeAnalysis.Compilation"/> for the context.
        /// </summary>
        public Compilation Compilation { get; }

        /// <summary>
        /// Options specified for the analysis.
        /// </summary>
        public AnalyzerOptions Options { get; }

        /// <summary>
        /// Token to check for requested cancellation of the analysis.
        /// </summary>
        public CancellationToken CancellationToken { get; }
    }

    /// <summary>
    /// Programmatic suppression of a <see cref="Diagnostic"/> by a <see cref="DiagnosticSuppressor"/>.
    /// </summary>
    public struct Suppression
    {
        /// <summary>
        /// Creates a suppression of a <see cref="Diagnostic"/> with the given <see cref="SuppressionDescriptor"/>.
        /// </summary>
        /// <param name="descriptor">
        /// Descriptor for the suppression, which must be from <see cref="DiagnosticSuppressor.SupportedSuppressions"/>
        /// for the <see cref="DiagnosticSuppressor"/> creating this suppression.
        /// </param>
        /// <param name="suppressedDiagnostic">
        /// <see cref="Diagnostic"/> to be suppressed, which must be from <see cref="SuppressionAnalysisContext.ReportedDiagnostics"/>
        /// for the suppression context in which this suppression is being created.</param>
        public static Suppression Create(SuppressionDescriptor descriptor, Diagnostic suppressedDiagnostic);

        /// <summary>
        /// Descriptor for this suppression.
        /// </summary>
        public SuppressionDescriptor Descriptor { get; }

        /// <summary>
        /// Diagnostic suppressed by this suppression.
        /// </summary>
        public Diagnostic SuppressedDiagnostic { get; }
    }
}
```
