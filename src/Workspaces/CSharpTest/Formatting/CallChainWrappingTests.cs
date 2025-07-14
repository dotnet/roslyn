// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions2;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting;

[Trait(Traits.Feature, Traits.Features.Formatting)]
public sealed class CallChainWrappingTests : CSharpFormattingTestBase
{
    private readonly OptionsCollection WrapCallChainsEnabled = new(LanguageNames.CSharp)
    {
        { WrapCallChains, true }
    };

    private readonly OptionsCollection WrapCallChainsDisabled = new(LanguageNames.CSharp)
    {
        { WrapCallChains, false }
    };

    private readonly OptionsCollection WrapAndIndentCallChainsEnabled = new(LanguageNames.CSharp)
    {
        { WrapCallChains, true },
        { IndentWrappedCallChains, true }
    };

    private readonly OptionsCollection IndentWrappedCallChainsOnly = new(LanguageNames.CSharp)
    {
        { WrapCallChains, false },
        { IndentWrappedCallChains, true }
    };

    [Fact]
    public async Task TestBasicMethodCallChain_WrapDisabled()
    {
        await AssertNoFormattingChangesAsync("""
            class C
            {
                void M()
                {
                    var result = data.Where(x => x.Value > 0).OrderBy(x => x.Name).Select(x => x.Value).ToList();
                }
            }
            """, WrapCallChainsDisabled);
    }

    [Fact]
    public async Task TestBasicMethodCallChain_WrapEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = data
                        .Where(x => x.Value > 0)
                        .OrderBy(x => x.Name)
                        .Select(x => x.Value)
                        .ToList();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = data.Where(x => x.Value > 0).OrderBy(x => x.Name).Select(x => x.Value).ToList();
                }
            }
            """, WrapCallChainsEnabled);
    }

    [Fact]
    public async Task TestBasicMethodCallChain_WrapAndIndentEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = data
                        .Where(x => x.Value > 0)
                        .OrderBy(x => x.Name)
                        .Select(x => x.Value)
                        .ToList();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = data.Where(x => x.Value > 0).OrderBy(x => x.Name).Select(x => x.Value).ToList();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestComplexMethodCallChain_WithSubEntity()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var logs = log.Entries.OrderBy(x => x.Time)
                                          .ThenBy(x => x.Thread)
                                          .ThenBy(x => x.LineNum)
                                          .GroupBy(x => x.Group);
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var logs = log.Entries.OrderBy(x => x.Time).ThenBy(x => x.Thread).ThenBy(x => x.LineNum).GroupBy(x => x.Group);
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestComplexMethodCallChain_WithLambda()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var logs = log.Entries.OrderBy(x => x.Time)
                                          .ThenBy(x => x.Thread)
                                          .ThenBy(x => x.LineNum)
                                          .GroupBy(x => {
                                              if (x.Time == DateTime.MinValue)
                                              {
                                                  return " -Start-";
                                              }
                                              else
                                              {
                                                  return $"{x.Time.ToString("yyyy-MM-dd HH:mm:ss,fff")}";
                                              }
                                          });
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var logs = log.Entries.OrderBy(x => x.Time).ThenBy(x => x.Thread).ThenBy(x => x.LineNum).GroupBy(x => {
                        if (x.Time == DateTime.MinValue)
                        {
                            return " -Start-";
                        }
                        else
                        {
                            return $"{x.Time.ToString("yyyy-MM-dd HH:mm:ss,fff")}";
                        }
                    });
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestSimpleMethodCallChain_SimpleIdentifier()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var x = y
                        .GroupBy(i => i.Key.Id, i => i.Key.Version)
                        .Where(i => i.Count() > 1)
                        .ToImmutableArray();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var x = y.GroupBy(i => i.Key.Id, i => i.Key.Version).Where(i => i.Count() > 1).ToImmutableArray();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainWithGenericMethods()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = source
                        .Select<int, string>(x => x.ToString())
                        .Where<string>(x => x.Length > 0)
                        .ToList<string>();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = source.Select<int, string>(x => x.ToString()).Where<string>(x => x.Length > 0).ToList<string>();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainWithNestedCalls()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = data
                        .Where(x => x.GetValue().IsValid())
                        .Select(x => x.GetName().ToUpper())
                        .ToList();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = data.Where(x => x.GetValue().IsValid()).Select(x => x.GetName().ToUpper()).ToList();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainWithStaticMethods()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = Enumerable.Range(1, 10)
                                           .Where(x => x % 2 == 0)
                                           .Select(x => x * 2)
                                           .ToArray();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = Enumerable.Range(1, 10).Where(x => x % 2 == 0).Select(x => x * 2).ToArray();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainWithPropertyAccess()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = data
                        .Where(x => x.IsValid)
                        .Select(x => x.Name.ToUpper())
                        .ToList();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = data.Where(x => x.IsValid).Select(x => x.Name.ToUpper()).ToList();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainWithIndexer()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = data
                        .Where(x => x[0] != null)
                        .Select(x => x[0].ToString())
                        .ToList();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = data.Where(x => x[0] != null).Select(x => x[0].ToString()).ToList();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainWithAsyncMethods()
    {
        await AssertFormatAsync("""
            class C
            {
                async Task M()
                {
                    var result = await data
                        .Where(x => x.IsValid)
                        .SelectAsync(x => x.ProcessAsync())
                        .ToListAsync();
                }
            }
            """, """
            class C
            {
                async Task M()
                {
                    var result = await data.Where(x => x.IsValid).SelectAsync(x => x.ProcessAsync()).ToListAsync();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainWithInlineComments()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = data // Start with data
                        .Where(x => x.IsValid) // Filter valid items
                        .Select(x => x.Name) // Get names
                        .ToList(); // Convert to list
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = data.Where(x => x.IsValid).Select(x => x.Name).ToList();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainWithoutIndentation_WrapOnlyEnabled()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = data
                        .Where(x => x.Value > 0)
                        .OrderBy(x => x.Name)
                        .Select(x => x.Value)
                        .ToList();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = data.Where(x => x.Value > 0).OrderBy(x => x.Name).Select(x => x.Value).ToList();
                }
            }
            """, WrapCallChainsEnabled);
    }

    [Fact]
    public async Task TestShortMethodCallChain()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = data
                        .First()
                        .ToString();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = data.First().ToString();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainWithReturnStatement()
    {
        await AssertFormatAsync("""
            class C
            {
                public List<string> M()
                {
                    return data
                        .Where(x => x.IsValid)
                        .Select(x => x.Name)
                        .ToList();
                }
            }
            """, """
            class C
            {
                public List<string> M()
                {
                    return data.Where(x => x.IsValid).Select(x => x.Name).ToList();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainWithMethodArguments()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    DoSomething(data
                        .Where(x => x.IsValid)
                        .Select(x => x.Name)
                        .ToList());
                }
            }
            """, """
            class C
            {
                void M()
                {
                    DoSomething(data.Where(x => x.IsValid).Select(x => x.Name).ToList());
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainWithConditionalOperator()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = condition ? data
                        .Where(x => x.IsValid)
                        .ToList() : null;
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = condition ? data.Where(x => x.IsValid).ToList() : null;
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainWithMultipleAssignments()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result1 = data1
                        .Where(x => x.IsValid)
                        .ToList();
                    var result2 = data2
                        .Select(x => x.Name)
                        .ToList();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result1 = data1.Where(x => x.IsValid).ToList();
                    var result2 = data2.Select(x => x.Name).ToList();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainPreservesExistingWrapping_WrapDisabled()
    {
        await AssertNoFormattingChangesAsync("""
            class C
            {
                void M()
                {
                    var result = data
                        .Where(x => x.Value > 0)
                        .OrderBy(x => x.Name)
                        .Select(x => x.Value)
                        .ToList();
                }
            }
            """, WrapCallChainsDisabled);
    }

    [Fact]
    public async Task TestIndentOnlyWithoutWrapping_HasNoEffect()
    {
        await AssertNoFormattingChangesAsync("""
            class C
            {
                void M()
                {
                    var result = data.Where(x => x.Value > 0).OrderBy(x => x.Name).Select(x => x.Value).ToList();
                }
            }
            """, IndentWrappedCallChainsOnly);
    }

    [Fact]
    public async Task TestEditorConfigIntegration_WrapOnly()
    {
        var editorConfig = """
            [*.cs]
            csharp_wrap_call_chains = true
            """;

        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = data
                        .Where(x => x.Value > 0)
                        .OrderBy(x => x.Name)
                        .ToList();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = data.Where(x => x.Value > 0).OrderBy(x => x.Name).ToList();
                }
            }
            """, WrapCallChainsEnabled);
    }

    [Fact]
    public async Task TestEditorConfigIntegration_WrapAndIndent()
    {
        var editorConfig = """
            [*.cs]
            csharp_wrap_call_chains = true
            csharp_indent_wrapped_call_chains = true
            """;

        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = data
                        .Where(x => x.Value > 0)
                        .OrderBy(x => x.Name)
                        .ToList();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = data.Where(x => x.Value > 0).OrderBy(x => x.Name).ToList();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainWithNullConditionalOperator()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = data?
                        .Where(x => x.IsValid)
                        .Select(x => x.Name)
                        .ToList();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = data?.Where(x => x.IsValid).Select(x => x.Name).ToList();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainWithLinqQuery()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = (from x in data
                                  where x.IsValid
                                  select x.Name)
                        .Distinct()
                        .ToList();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = (from x in data
                                  where x.IsValid
                                  select x.Name).Distinct().ToList();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainWithCasting()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = data
                        .Cast<string>()
                        .Where(x => x.Length > 0)
                        .ToList();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = data.Cast<string>().Where(x => x.Length > 0).ToList();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainWithComplexExpression()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = GetData()
                        .Where(x => x.Id > 0 && x.Name != null)
                        .Select(x => new { x.Id, UpperName = x.Name.ToUpper() })
                        .OrderBy(x => x.UpperName)
                        .ToList();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = GetData().Where(x => x.Id > 0 && x.Name != null).Select(x => new { x.Id, UpperName = x.Name.ToUpper() }).OrderBy(x => x.UpperName).ToList();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }

    [Fact]
    public async Task TestMethodCallChainWithComplexBaseExpression()
    {
        await AssertFormatAsync("""
            class C
            {
                void M()
                {
                    var result = obj.GetService<IDataService>()
                                    .GetData()
                                    .Where(x => x.IsValid)
                                    .ToList();
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var result = obj.GetService<IDataService>().GetData().Where(x => x.IsValid).ToList();
                }
            }
            """, WrapAndIndentCallChainsEnabled);
    }
} 