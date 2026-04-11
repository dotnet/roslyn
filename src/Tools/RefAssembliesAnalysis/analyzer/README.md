| Category | Roslyn producer(s) | Pattern overview |
|---|---|---|
| `state-machine` | `MakeStateMachineTypeName`, `MakeStateMachineStateFieldName`, `MakeIteratorCurrentFieldName`, `MakeIteratorCurrentThreadIdFieldName`, `StateMachineThisParameterProxyName`, `StateMachineParameterProxyFieldName`, `AsyncBuilderFieldName`, 
`MakeAsyncIteratorPromiseOfValueOrEndFieldName`, `MakeAsyncIteratorCombinedTokensFieldName`, `MakeDisposeModeFieldName` | `<Method>d__N`, `<>1__state`, `<>2__current`, `<>l__initialThreadId`, `<>4__this`, `<>3__param`, `<>t__builder`, `<>v__promiseOfValueOrEnd`, 
`<>x__combinedTokens`, `<>w__disposeMode` |
| `async-state-machine-attribute` | paired visible method entries compared after normalizing `AsyncStateMachineAttribute` by blanking its argument list | entries are rebucketed here only when the normalized method signature is identical |
| `iterator-state-machine-attribute` | paired visible method entries compared after normalizing `IteratorStateMachineAttribute` by blanking its argument list | entries are rebucketed here only when the normalized method signature is identical |
| `test-method-attribute` | paired visible method entries compared after normalizing `Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute(...)` by blanking its argument list | entries are rebucketed here only when the normalized method signature is identical |
| `benchmark-attribute` | paired visible method entries compared after normalizing `BenchmarkDotNet.Attributes.BenchmarkAttribute(...)` by blanking its argument list | entries are rebucketed here only when the normalized method signature is identical |
| `awaiter-field` | `AsyncAwaiterFieldName` | `<>u__N` |
| `iterator-finally` | `MakeIteratorFinallyMethodName` | `<>m__FinallyN` |
| `display-class` | `MakeStaticLambdaDisplayClassName`, `MakeLambdaDisplayClassName`, `MakeLambdaDisplayLocalName`, `MakeHoistedLocalFieldName(...LambdaDisplayClass...)` | `<>c`, `<>c__DisplayClass...`, `<>8__localsN` |
| `lambda-method` | `MakeLambdaMethodName` | `<Method>b__N` |
| `local-function` | `MakeLocalFunctionName` | `<Method>g__LocalName|N_M` |
| `lambda-or-dynamic-cache` | `MakeLambdaCacheFieldName`, `MakeCachedFrameInstanceFieldName`, `MakeDynamicCallSiteContainerName`, `MakeDynamicCallSiteFieldName`, `DelegateCacheContainerType`, `DelegateCacheContainerFieldName` | `<>9__...`, `<>9`, `<>o__...`, `<>p__...`, 
`<>O...`, `<N>__TargetMethod` |
| `hoisted-local` | `MakeHoistedLocalFieldName`, `ReusableHoistedLocalFieldName` | `<name>5__N`, `<>7__wrapN` |
| `backing-field` | `MakeBackingFieldName`, `MakePrimaryConstructorParameterFieldName`, `MakeAnonymousTypeBackingFieldName` | `<Property>k__BackingField`, `<param>P`, `<Property>i__Field` |
| `anonymous-type-or-delegate` | `MakeAnonymousTypeOrDelegateTemplateName`, `MakeSynthesizedDelegateName` | `<>f__AnonymousTypeN`, `<>f__AnonymousDelegateN`, `<>A...`, `<>F...` |
| `inline-array-or-readonly-list` | `MakeSynthesizedInlineArrayName`, `MakeSynthesizedReadOnlyListName` | `<>y__InlineArrayN`, `<>z__ReadOnlyArray`, `<>z__ReadOnlyList`, `<>z__ReadOnlySingleElementList` |
| `compiler-generated-other` | | fallback for Roslyn-style generated names not otherwise bucketed |

Pairs whose only differences land in `assembly-file-version`, `assembly-informational-version`, and/or `assembly-metadata` are classified as `ignored-attribute-versioning-only` and excluded from churn totals. `test-method-attribute` and `benchmark-attribute` remain regular diff buckets and still count toward churn.
