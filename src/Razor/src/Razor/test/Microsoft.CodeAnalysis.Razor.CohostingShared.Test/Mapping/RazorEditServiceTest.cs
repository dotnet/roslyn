// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.ProjectSystem;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Mapping;

public class RazorEditServiceTest(ITestOutputHelper testOutput) : CohostEndpointTestBase(testOutput)
{
    private IRazorEditService? _razorEditService;

    protected override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _razorEditService = OOPExportProvider.GetExportedValue<IRazorEditService>();
    }

    [Fact]
    public Task SimpleEdits_Apply()
        => TestAsync(
            csharpSource: """
                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
            razorSource: """
                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
            newCSharpSource: """
                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }
                }
                """,
            expectedRazorSource: """
                @code {
                    public int NewCounter { get; set; } 
                }
                """);

    [Fact]
    public Task MappedAndUnmappedEdits_Apply()
        => TestAsync(
            csharpSource: """
                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}

                    void UnmappedMethod()
                    {
                    }
                }
                """,
            razorSource: """
                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
            newCSharpSource: """
                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }

                    void Method()
                    {
                    }
                }
                """,
            expectedRazorSource: """
                @code {
                    public int NewCounter { get; set; } 

                    void Method()
                    {
                    }
                }
                """);

    [Fact]
    public Task GenerateMethod_ExistingCodeBlock_DoesNotDuplicate()
        => TestAsync(
            csharpSource: """
                class MyComponent : ComponentBase
                {
                    {|map1:private void M()
                    {
                        NewMethod();
                    }|}
                }
                """,
            razorSource: """
                @code {
                    {|map1:private void M()
                    {
                        NewMethod();
                    }|}
                }
                """,
            newCSharpSource: """
                using System;

                class MyComponent : ComponentBase
                {
                    private void M()
                    {
                        NewMethod();
                    }

                    private void NewMethod()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            expectedRazorSource: """
                @using System
                @code {
                    private void M()
                    {
                        NewMethod();
                    }

                    private void NewMethod()
                    {
                        throw new NotImplementedException();
                    }
                }
                """);

    [Fact]
    public Task GenerateMethod_WithBlankLineInBody()
        => TestAsync(
            csharpSource: """
                class MyComponent : ComponentBase
                {
                    {|map1:private void M()
                    {
                        NewMethod();
                    }|}
                }
                """,
            razorSource: """
                @code {
                    {|map1:private void M()
                    {
                        NewMethod();
                    }|}
                }
                """,
            newCSharpSource: """
                using System;

                class MyComponent : ComponentBase
                {
                    private void M()
                    {
                        NewMethod();
                    }

                    private void NewMethod()
                    {
                        var x = 1;

                        throw new NotImplementedException();
                    }
                }
                """,
            expectedRazorSource: """
                @using System
                @code {
                    private void M()
                    {
                        NewMethod();
                    }

                    private void NewMethod()
                    {
                        var x = 1;

                        throw new NotImplementedException();
                    }
                }
                """);

    [Fact]
    public Task GenerateMethod_ExistingCodeBlock_MultipleAddedMethods_HaveBlankLinesBetweenThem()
        => TestAsync(
            csharpSource: """
                class MyComponent : ComponentBase
                {
                    {|map1:private void M()
                    {
                        FirstNewMethod();
                        SecondNewMethod();
                    }|}
                }
                """,
            razorSource: """
                @code {
                    {|map1:private void M()
                    {
                        FirstNewMethod();
                        SecondNewMethod();
                    }|}
                }
                """,
            newCSharpSource: """
                using System;

                class MyComponent : ComponentBase
                {
                    private void M()
                    {
                        FirstNewMethod();
                        SecondNewMethod();
                    }

                    private void FirstNewMethod()
                    {
                        throw new NotImplementedException();
                    }

                    private void SecondNewMethod()
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
            expectedRazorSource: """
                @using System
                @code {
                    private void M()
                    {
                        FirstNewMethod();
                        SecondNewMethod();
                    }

                    private void FirstNewMethod()
                    {
                        throw new NotImplementedException();
                    }

                    private void SecondNewMethod()
                    {
                        throw new NotImplementedException();
                    }
                }
                """);

    [Fact]
    public Task GenerateMethod_ExistingCodeBlock_UsesTabsWithMixedIndentation()
    {
        ClientSettingsManager.Update(new ClientSpaceSettings(IndentWithTabs: true, IndentSize: 3));

        return TestAsync(
            csharpSource: """
                class MyComponent : ComponentBase
                {
                    {|map1:private void M()
                    {
                        NewMethod();
                    }|}
                }
                """,
            razorSource: """
                @code {
                    {|map1:private void M()
                    {
                        NewMethod();
                    }|}
                }
                """,
            newCSharpSource: """
                using System;

                class MyComponent : ComponentBase
                {
                    private void M()
                    {
                        NewMethod();
                    }

                    private void NewMethod()
                    {
                        if (true)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                """,
            expectedRazorSource: """
                @using System
                @code {
                    private void M()
                    {
                        NewMethod();
                    }

                	private void NewMethod()
                	{
                		 if (true)
                		 {
                			  throw new NotImplementedException();
                		 }
                	}
                }
                """);
    }

    [Fact]
    public Task GenerateMethod_ExistingCodeBlock_UsesConfiguredTabSizeForTabIndentedSource()
    {
        ClientSettingsManager.Update(new ClientSpaceSettings(IndentWithTabs: true, IndentSize: 3));

        return TestAsync(
            csharpSource: """
                class MyComponent : ComponentBase
                {
                    {|map1:private void M()
                    {
                        NewMethod();
                    }|}
                }
                """,
            razorSource: """
                @code {
                    {|map1:private void M()
                    {
                        NewMethod();
                    }|}
                }
                """,
            newCSharpSource: """
                using System;

                class MyComponent : ComponentBase
                {
                    private void M()
                    {
                        NewMethod();
                    }

                	private void NewMethod()
                	{
                		if (true)
                		{
                			throw new NotImplementedException();
                		}
                	}
                }
                """,
            expectedRazorSource: """
                @using System
                @code {
                    private void M()
                    {
                        NewMethod();
                    }

                	private void NewMethod()
                	{
                		if (true)
                		{
                			throw new NotImplementedException();
                		}
                	}
                }
                """);
    }

    [Fact]
    public Task GenerateMethod_ExistingCodeBlock_UsesConfiguredSpaceIndentation()
    {
        // This test is a test for our indentation handling code, but the end result could be better if we knew what Roslyn
        // was using as the tabsize for C# files.

        ClientSettingsManager.Update(new ClientSpaceSettings(IndentWithTabs: false, IndentSize: 2));

        return TestAsync(
            csharpSource: """
                class MyComponent : ComponentBase
                {
                  {|map1:private void M()
                  {
                    NewMethod();
                  }|}
                }
                """,
            razorSource: """
                @code {
                  {|map1:private void M()
                  {
                    NewMethod();
                  }|}
                }
                """,
            newCSharpSource: """
                using System;

                class MyComponent : ComponentBase
                {
                  private void M()
                  {
                    NewMethod();
                  }

                    private void NewMethod()
                    {
                        if (true)
                        {
                            throw new NotImplementedException();
                        }
                    }
                }
                """,
            expectedRazorSource: """
                @using System
                @code {
                  private void M()
                  {
                    NewMethod();
                  }

                  private void NewMethod()
                  {
                      if (true)
                      {
                          throw new NotImplementedException();
                      }
                  }
                }
                """);
    }

    [Fact]
    public Task GenerateMethod_ExistingSingleLineCodeBlockAtTopOfFile_Applies()
        => TestAsync(
            csharpSource: """
                class MyComponent : ComponentBase
                {
                }
                """,
            razorSource: """
                @code {}
                """,
            newCSharpSource: """
                class MyComponent : ComponentBase
                {
                    private void NewMethod()
                    {
                    }
                }
                """,
            expectedRazorSource: """
                @code {
                    private void NewMethod()
                    {
                    }
                }
                """);

    [Fact]
    public Task EditInUnmappedMethod_NotAdded()
       => TestAsync(
           csharpSource: """
                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}

                    void UnmappedMethod()
                    {
                    }
                }
                """,
           razorSource: """
                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
           newCSharpSource: """
                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }

                    void UnmappedMethod()
                    {
                        // This is new, but the Razor doc shouldn't change.
                    }
                }
                """,
           expectedRazorSource: """
                @code {
                    public int NewCounter { get; set; } 
                }
                """);

    [Fact]
    public Task NewMethod_ExpressionBodied()
      => TestAsync(
          csharpSource: """
                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
          razorSource: """
                @code {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
          newCSharpSource: """
                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }

                    int UnmappedMethod()
                        => 5;
                }
                """,
          expectedRazorSource: """
                @code {
                    public int NewCounter { get; set; }

                    int UnmappedMethod()
                        => 5;
                }
                """);

    [Fact]
    public async Task NewMethod_NoCodeBlock_CodeBlockBraceOnNextLine()
    {
        ClientSettingsManager.Update(ClientSettingsManager.GetClientSettings().AdvancedSettings with { CodeBlockBraceOnNextLine = true });

        await TestAsync(
             csharpSource: """
                class MyComponent : ComponentBase
                {
                }
                """,
             razorSource: """
                <div></div>
                """,
             newCSharpSource: """
                class MyComponent : ComponentBase
                {
                    private int UnmappedMethod()
                    {
                        return 5;
                    }
                }
                """,
             expectedRazorSource: """
                <div></div>
                @code
                {
                    private int UnmappedMethod()
                    {
                        return 5;
                    }
                }
                """);
    }

    [Fact]
    public Task NewMethod_ExpressionBodied_NoCodeBlock()
      => TestAsync(
          csharpSource: """
                class MyComponent : ComponentBase
                {
                }
                """,
          razorSource: """
                <div></div>
                """,
          newCSharpSource: """
                class MyComponent : ComponentBase
                {
                    int UnmappedMethod()
                        => 5;
                }
                """,
          expectedRazorSource: """
                <div></div>
                @code {
                    int UnmappedMethod()
                        => 5;
                }
                """);

    [Fact]
    public Task NewMethod_Incomplete()
      => TestAsync(
          csharpSource: """
                class MyComponent : ComponentBase
                {
                }
                """,
          razorSource: """
                <div></div>
                """,
          newCSharpSource: """
                class MyComponent : ComponentBase
                {
                    int UnmappedMethod()
                }
                """,
          expectedRazorSource: """
                <div></div>
                @code {
                    int UnmappedMethod()
                }
                """);

    [Fact]
    public Task NewMethod_ExistingIncomplete()
      => TestAsync(
          csharpSource: """
                class MyComponent : ComponentBase
                {
                    {|map1:int UnmappedMethod()|}
                }
                """,
          razorSource: """
                <div></div>
                @code
                {
                    {|map1:int UnmappedMethod()|}
                }
                """,
          newCSharpSource: """
                class MyComponent : ComponentBase
                {
                    int UnmappedMethod()

                    void M()
                    {
                    }
                }
                """,
          expectedRazorSource: """
                <div></div>
                @code
                {
                    int UnmappedMethod()

                    void M()
                    {
                    }
                }
                """);

    [Fact]
    public Task NewUsing_TopOfFile()
        => TestAsync(
            csharpSource: """
                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
            razorSource: """
                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
            newCSharpSource: """
                using System;

                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }
                }
                """,
            expectedRazorSource: """
                @using System
                @code {
                    public int NewCounter { get; set; } 
                }
                """);

    [Fact]
    public Task RemovedUsing_IsRemoved()
        => TestAsync(
            csharpSource: """
                {|mapUsing:using System;|}

                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
            razorSource: """
                {|mapUsing:@using System|}

                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
            newCSharpSource: """
                class MyComponent : ComponentBase
                {
                    public int Counter { get; set; }
                }
                """,
            expectedRazorSource: """

                @code {
                    public int Counter { get; set; } 
                }
                """);

    [Fact]
    public Task NewUsing_AfterExisting()
        => TestAsync(
            csharpSource: """
                {|mapUsing:using System;|}

                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
            razorSource: """
                {|mapUsing:@using System|}

                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
            newCSharpSource: """
                using System;
                using System.Collections.Generic;

                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }
                }
                """,
            expectedRazorSource: """
                @using System
                @using System.Collections.Generic

                @code {
                    public int NewCounter { get; set; } 
                }
                """);

    [Fact]
    public Task NewUsing_AfterPage()
        => TestAsync(
            csharpSource: """
                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
            razorSource: """
                @page "/counter"

                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
            newCSharpSource: """
                using System;
                using System.Collections.Generic;

                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }
                }
                """,
            expectedRazorSource: """
                @page "/counter"
                @using System
                @using System.Collections.Generic

                @code {
                    public int NewCounter { get; set; } 
                }
                """);

    [Fact]
    public Task NewUsing_AfterPage2()
        => TestAsync(
            csharpSource: """
                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
            razorSource: """
                @page "/counter"

                <h3>Counter</h3>
                <p>Current count: @Counter</p>

                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
            newCSharpSource: """
                using System;
                using System.Collections.Generic;

                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }
                }
                """,
            expectedRazorSource: """
                @page "/counter"
                @using System
                @using System.Collections.Generic

                <h3>Counter</h3>
                <p>Current count: @Counter</p>

                @code {
                    public int NewCounter { get; set; } 
                }
                """);

    [Fact]
    public Task RenamedUsing_Applies()
        => TestAsync(
            csharpSource: """
                {|mapUsing:using System;|}
                {|mapUsing2:using System.Collections.Generic;|}
                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
            razorSource: """
                @page "/counter"
                {|mapUsing:@using System|}
                {|mapUsing2:@using System.Collections.Generic|}

                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
            newCSharpSource: """
                using Renamed;

                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }
                }
                """,
            expectedRazorSource: """
                @page "/counter"
                @using Renamed

                @code {
                    public int NewCounter { get; set; } 
                }
                """);

    [Fact]
    public Task AddUsing_AfterSystem()
        => TestAsync(
            csharpSource: """
                {|mapUsing:using System;|}
                {|mapUsing2:using System.Collections.Generic;|}
                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
            razorSource: """
                @page "/counter"
                {|mapUsing:@using System|}
                {|mapUsing2:@using System.Collections.Generic|}

                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
            newCSharpSource: """
                using OtherNamespace;
                using System;

                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }
                }
                """,
            expectedRazorSource: """
                @page "/counter"
                @using System
                @using OtherNamespace

                @code {
                    public int NewCounter { get; set; } 
                }
                """);

    [Fact]
    public Task AddUsing_OrdersSystemCorrectly()
        => TestAsync(
            csharpSource: """
                {|mapUsing:using System;|}
                {|mapUsing2:using MyNamespace;|}
                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
            razorSource: """
                @page "/counter"
                {|mapUsing:@using System|}
                {|mapUsing2:@using MyNamespace|}

                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
            newCSharpSource: """
                using OtherNamespace;
                using System;
                using System.Collections.Generic;

                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }
                }
                """,
            expectedRazorSource: """
                @page "/counter"
                @using System
                @using System.Collections.Generic
                @using OtherNamespace

                @code {
                    public int NewCounter { get; set; } 
                }
                """);

    [Fact]
    public Task UsingIndentation_DoesNotApply()
        => TestAsync(
            csharpSource: """
                {|mapUsing:using System;|}
                {|mapUsing2:using System.Collections.Generic;|}
                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
            razorSource: """
                @page "/counter"
                {|mapUsing:@using System|}
                {|mapUsing2:@using System.Collections.Generic|}

                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
            newCSharpSource: """
                    using System;
                    using System.Collections.Generic;

                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }
                }
                """,
            expectedRazorSource: """
                @page "/counter"
                @using System
                @using System.Collections.Generic

                @code {
                    public int NewCounter { get; set; } 
                }
                """);

    [Fact]
    public Task UsingAliasRemoved_HandledCorrectly()
        => TestAsync(
            csharpSource: """
                {|mapUsing:using System;|}
                {|mapUsing2:using System.Collections.Generic;|}
                {|mapUsing3:using Goo = Bar;|}
                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
            razorSource: """
                @page "/counter"
                {|mapUsing:@using System|}
                {|mapUsing2:@using System.Collections.Generic|}
                {|mapUsing3:@using Goo = Bar|}

                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
            newCSharpSource: """
                using System;
                using System.Collections.Generic;

                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }
                }
                """,
            expectedRazorSource: """
                @page "/counter"
                @using System
                @using System.Collections.Generic

                @code {
                    public int NewCounter { get; set; } 
                }
                """);

    [Fact]
    public Task UsingAliasAdded_HandledCorrectly()
        => TestAsync(
            csharpSource: """
                {|mapUsing:using System;|}
                {|mapUsing2:using System.Collections.Generic;|}
                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
            razorSource: """
                @page "/counter"
                {|mapUsing:@using System|}
                {|mapUsing2:@using System.Collections.Generic|}

                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
            newCSharpSource: """
                using System;
                using System.Collections.Generic;
                using Goo = Bar;

                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }
                }
                """,
            expectedRazorSource: """
                @page "/counter"
                @using System
                @using System.Collections.Generic
                @using Goo = Bar

                @code {
                    public int NewCounter { get; set; } 
                }
                """);

    [Fact]
    public Task UsingStaticRemoved_HandledCorrectly()
        => TestAsync(
            csharpSource: """
                {|mapUsing:using System;|}
                {|mapUsing2:using System.Collections.Generic;|}
                {|mapUsing3:using static Test.Bar;|}
                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
            razorSource: """
                @page "/counter"
                {|mapUsing:@using System|}
                {|mapUsing2:@using System.Collections.Generic|}
                {|mapUsing3:@using static Test.Bar|}

                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
            newCSharpSource: """
                using System;
                using System.Collections.Generic;

                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }
                }
                """,
            expectedRazorSource: """
                @page "/counter"
                @using System
                @using System.Collections.Generic

                @code {
                    public int NewCounter { get; set; } 
                }
                """);

    [Fact]
    public Task UsingStaticAdded_HandledCorrectly()
        => TestAsync(
            csharpSource: """
                {|mapUsing:using System;|}
                {|mapUsing2:using static System.Collections.Generic;|}
                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
            razorSource: """
                @page "/counter"
                {|mapUsing:@using System|}
                {|mapUsing2:@using System.Collections.Generic|}

                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
            newCSharpSource: """
                using System;
                using System.Collections.Generic;
                using static Test.Bar;

                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }
                }
                """,
            expectedRazorSource: """
                @page "/counter"
                @using System
                @using System.Collections.Generic
                @using static Test.Bar

                @code {
                    public int NewCounter { get; set; } 
                }
                """);

    [Fact]
    public Task AddUsingMultipleUsingGroups_AppliesCurrently()
        => TestAsync(
            csharpSource: """
                {|mapUsing:using System;|}
                {|mapUsing2:using MyNamespace;|}
                {|mapUsing3:using MyNamespace2;|}
                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
            razorSource: """
                @page "/counter"
                {|mapUsing:@using System|}
                {|mapUsing2:@using MyNamespace|}

                <p></p>

                {|mapUsing3:@using MyNamespace2|}

                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
            newCSharpSource: """
                using OtherNamespace;
                using System;
                using System.Collections.Generic;
                using MyNamespace;
                using MyNamespace2;

                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }
                }
                """,
            expectedRazorSource: """
                @page "/counter"
                @using System
                @using System.Collections.Generic
                @using MyNamespace
                @using OtherNamespace

                <p></p>

                @using MyNamespace2

                @code {
                    public int NewCounter { get; set; } 
                }
                """);

    [Fact]
    public Task RemovingMultipleUsingGroups_AppliesCurrently()
        => TestAsync(
            csharpSource: """
                {|mapUsing:using System;|}
                {|mapUsing2:using MyNamespace;|}
                {|mapUsing3:using MyNamespace2;|}
                class MyComponent : ComponentBase
                {
                    {|map1:public int Counter { get; set; }|}
                }
                """,
            razorSource: """
                @page "/counter"
                {|mapUsing:@using System|}

                <p></p>

                {|mapUsing2:@using MyNamespace|}

                <p></p>

                {|mapUsing3:@using MyNamespace2|}

                @code {
                    {|map1:public int Counter { get; set; }|} 
                }
                """,
            newCSharpSource: """
                class MyComponent : ComponentBase
                {
                    public int NewCounter { get; set; }
                }
                """,
            expectedRazorSource: """
                @page "/counter"

                <p></p>


                <p></p>


                @code {
                    public int NewCounter { get; set; } 
                }
                """);

    private async Task TestAsync(
        TestCode csharpSource,
        TestCode razorSource,
        string newCSharpSource,
        string expectedRazorSource)
    {
        var csharpSpans = csharpSource.NamedSpans;
        var razorSpans = razorSource.NamedSpans;

        AssertEx.SetEqual(csharpSpans.Keys.OrderAsArray(), razorSpans.Keys.OrderAsArray());

        var csharpPath = @"C:\path\to\document.razor.g.cs";
        var razorPath = @"C:\path\to\document.razor";
        var csharpSourceText = SourceText.From(csharpSource.Text);
        var razorSourceText = SourceText.From(razorSource.Text);

        var sourceMappings = new List<SourceMapping>();
        foreach (var key in csharpSpans.Keys)
        {
            var csharpSpan = csharpSpans[key].Single();
            var razorSpan = razorSpans[key].Single();

            var csharpLinePosition = csharpSourceText.GetLinePositionSpan(csharpSpan);
            var razorLinePosition = razorSourceText.GetLinePositionSpan(razorSpan);

            var csharpSourceSpan = new SourceSpan(
                csharpPath,
                csharpSpan.Start,
                csharpLinePosition.Start.Line,
                csharpLinePosition.Start.Character,
                csharpSpan.Length);

            var razorSourceSpan = new SourceSpan(
                razorPath,
                razorSpan.Start,
                razorLinePosition.Start.Line,
                razorLinePosition.Start.Character,
                razorSpan.Length);

            sourceMappings.Add(new SourceMapping(razorSourceSpan, csharpSourceSpan));
        }

        var newCSharpSourceText = SourceText.From(newCSharpSource);
        var changes = GetChanges(csharpSource.Text, newCSharpSource);
        Assert.NotEmpty(changes);

        var document = CreateProjectAndRazorDocument(razorSource.Text, documentFilePath: razorPath);

        var snapshotManager = OOPExportProvider.GetExportedValue<RemoteSnapshotManager>();
        var codeDocument = await snapshotManager.GetSnapshot(document).GetGeneratedOutputAsync(DisposalToken);

        var csharpDocument = TestRazorCSharpDocument.Create(
            codeDocument,
            csharpSource.Text,
            sourceMappings.OrderByAsArray(s => s.GeneratedSpan.AbsoluteIndex));

        codeDocument = codeDocument.WithCSharpDocument(csharpDocument);
        var snapshot = TestDocumentSnapshot.Create(razorPath, codeDocument);

        var responseTextChanges = await _razorEditService.AssumeNotNull().MapCSharpEditsAsync(
            changes,
            snapshot,
            CancellationToken.None);

        Assert.NotEmpty(responseTextChanges);

        var newRazorSourceText = razorSourceText.WithChanges(responseTextChanges);
        AssertEx.EqualOrDiff(expectedRazorSource, newRazorSourceText.ToString());
    }

    private static ImmutableArray<TextChange> GetChanges(string csharpSource, string newCSharpSource)
    {
        var tree = CSharpSyntaxTree.ParseText(csharpSource);
        var newTree = CSharpSyntaxTree.ParseText(newCSharpSource);

        return newTree.GetChanges(tree).ToImmutableArray();
    }
}
