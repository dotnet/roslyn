Introduction
============
The C# and Visual Basic compilers support a `/reportanalyzer` switch on
the command line to report additional analyzer information, such as execution time.

The output contains the total wall clock time spent in executing the analyzers and
the relative execution times per-analyzer.
Note that the elapsed time may be less than analyzer execution time because
analyzers can run concurrently. One should use this data only for comparative analysis to
identify any outlier analyzer which takes signinficantly more time than other analyzers.

MSBuild command
=============
Use the following msbuild command line to get the analyzer performance report:

```
msbuild.exe /v:d /p:reportanalyzer=true <%project_file%>
```

Output Format
=============

```
Total analyzer execution time: XYZ seconds.
NOTE: Elapsed time may be less than analyzer execution time because analyzers can run concurrently.
```
Time (s)  | % |  Analyzer
----------|---|----------------------
xyz1      | X |  Analyzer Assembly Identity
xyz2      | Y |    -    DiagnosticAnalyzer1
xyz3      | Z |    -    DiagnosticAnalyzer2


Example
=============

Following is an example output from a project built with analyzers:

-------------------------------------------------------------------------------------------------------
```
Total analyzer execution time: 0.503 seconds.
NOTE: Elapsed time may be less than analyzer execution time because analyzers can run concurrently.

Time (s)    %   Analyzer
   0.242   48   Roslyn.Diagnostics.CSharp.Analyzers, Version=1.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
   0.240   47      Roslyn.Diagnostics.Analyzers.CSharpCodeActionCreateAnalyzer
   0.002   <1      Roslyn.Diagnostics.Analyzers.CSharp.CSharpSpecializedEnumerableCreationAnalyzer
  <0.001   <1      Roslyn.Diagnostics.Analyzers.CSharp.CSharpDiagnosticDescriptorAccessAnalyzer
  <0.001   <1      Roslyn.Diagnostics.Analyzers.CSharpSymbolDeclaredEventAnalyzer

   0.137   27   System.Runtime.Analyzers, Version=1.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
   0.045    8      System.Runtime.Analyzers.AttributeStringLiteralsShouldParseCorrectlyAnalyzer
   0.024    4      System.Runtime.Analyzers.SpecifyStringComparisonAnalyzer
   0.022    4      System.Runtime.Analyzers.SpecifyIFormatProviderAnalyzer
   0.020    3      System.Runtime.Analyzers.ProvideCorrectArgumentsToFormattingMethodsAnalyzer
   0.009    1      System.Runtime.Analyzers.CallGCSuppressFinalizeCorrectlyAnalyzer
   0.007    1      System.Runtime.Analyzers.DisposableTypesShouldDeclareFinalizerAnalyzer
   0.003   <1      System.Runtime.Analyzers.NormalizeStringsToUppercaseAnalyzer
   0.003   <1      System.Runtime.Analyzers.InstantiateArgumentExceptionsCorrectlyAnalyzer
   0.002   <1      System.Runtime.Analyzers.TestForNaNCorrectlyAnalyzer
   0.001   <1      System.Runtime.Analyzers.DoNotUseEnumerableMethodsOnIndexableCollectionsInsteadUseTheCollectionDirectlyAnalyzer
  <0.001   <1      System.Runtime.Analyzers.SpecifyCultureInfoAnalyzer
  <0.001   <1      System.Runtime.Analyzers.DoNotLockOnObjectsWithWeakIdentityAnalyzer
  <0.001   <1      System.Runtime.Analyzers.TestForEmptyStringsUsingStringLengthAnalyzer
  <0.001   <1      System.Runtime.Analyzers.AvoidUnsealedAttributesAnalyzer

   0.085   16   Roslyn.Diagnostics.Analyzers, Version=1.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
   0.085   16      Roslyn.Diagnostics.Analyzers.DeclarePublicAPIAnalyzer

   0.023    4   System.Runtime.CSharp.Analyzers, Version=1.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
   0.014    2      System.Runtime.Analyzers.CSharpAvoidZeroLengthArrayAllocationsAnalyzer
   0.008    1      System.Runtime.Analyzers.CSharpInitializeStaticFieldsInlineAnalyzer
   0.001   <1      System.Runtime.Analyzers.CSharpDoNotRaiseReservedExceptionTypesAnalyzer
  <0.001   <1      System.Runtime.Analyzers.CSharpUseOrdinalStringComparisonAnalyzer
  <0.001   <1      System.Runtime.Analyzers.CSharpDoNotUseTimersThatPreventPowerStateChangesAnalyzer
  <0.001   <1      System.Runtime.Analyzers.CSharpDisposeMethodsShouldCallBaseClassDisposeAnalyzer

   0.006    1   System.Runtime.InteropServices.CSharp.Analyzers, Version=1.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
   0.006    1      System.Runtime.InteropServices.Analyzers.CSharpAlwaysConsumeTheValueReturnedByMethodsMarkedWithPreserveSigAttributeAnalyzer
  <0.001   <1      System.Runtime.InteropServices.Analyzers.CSharpMarkBooleanPInvokeArgumentsWithMarshalAsAnalyzer
  <0.001   <1      System.Runtime.InteropServices.Analyzers.CSharpUseManagedEquivalentsOfWin32ApiAnalyzer

   0.004   <1   System.Runtime.InteropServices.Analyzers, Version=1.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
   0.004   <1      System.Runtime.InteropServices.Analyzers.PInvokeDiagnosticAnalyzer

   0.004   <1   System.Collections.Immutable.Analyzers, Version=1.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
   0.004   <1      System.Collections.Immutable.Analyzers.DoNotCallToImmutableArrayOnAnImmutableArrayValueAnalyzer

   0.001   <1   System.Threading.Tasks.Analyzers, Version=1.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
   0.001   <1      System.Threading.Tasks.Analyzers.DoNotCreateTasksWithoutPassingATaskSchedulerAnalyzer

   0.001   <1   XmlDocumentationComments.CSharp.Analyzers, Version=1.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35
   0.001   <1      XmlDocumentationComments.Analyzers.CSharpAvoidUsingCrefTagsWithAPrefixAnalyzer
```
