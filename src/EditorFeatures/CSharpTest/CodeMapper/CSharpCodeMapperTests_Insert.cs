// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeMapper;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeMapper;

[UseExportProvider]
public partial class CSharpCodeMapperTests : TestBase
{
    private readonly TestFixtureHelper<CSharpTestWorkspaceFixture> _fixtureHelper = new();

    private protected ReferenceCountedDisposable<CSharpTestWorkspaceFixture> GetOrCreateWorkspaceFixture()
        => _fixtureHelper.GetOrCreateFixture();

    protected async Task TestAsync(string text, ImmutableArray<string> contents, string expected, bool useSelection)
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();

        MarkupTestFile.GetSpans(text.NormalizeLineEndings(), out text, out ImmutableArray<TextSpan> textSpans);
        var document = workspaceFixture.Target.UpdateDocument(text, SourceCodeKind.Regular);
        var codeMapper = document.GetRequiredLanguageService<ICodeMapper>();

        var focusLocation = textSpans.Length == 0 || !useSelection ? ImmutableArray<DocumentSpan>.Empty : ImmutableArray.Create(new DocumentSpan(document, textSpans.Single()));
        var textChanges = await codeMapper.MapCodeAsync(document, contents, focusLocation, formatMappedCode:true, CancellationToken.None).ConfigureAwait(false);
        var oldText = await document.GetTextAsync(CancellationToken.None).ConfigureAwait(false);
        var newText = oldText.WithChanges(textChanges);

        Assert.Equal(expected, newText.ToString());
    }

    [Fact]
    public Task InsertConsoleWriteLineArgsInMain()
    {
        var code = """
            namespace ConsoleApp1
            {
                class Program
                {
                    [|static void Main(string[] args)
                    {
            
                    }|]
                }
            }
            """;

        var codeBlock = @"Console.WriteLine(string.Join("", "", args));";

        var expected = """
            namespace ConsoleApp1
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        Console.WriteLine(string.Join(", ", args));
                    }
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }

    [Theory, CombinatorialData]
    public Task ReadonlyPropertyWithinClass(bool useSelection)
    {
        var code = """
            using System.Threading.Tasks;
            using System.Linq;
            namespace MyNamespace;
            [|public class MyClass
            {
            
            }|]
            """;

        var codeBlock = @"private readonly int myValue = 0;";

        var expected = """
            using System.Threading.Tasks;
            using System.Linq;
            namespace MyNamespace;
            public class MyClass
            {
                private readonly int myValue = 0;
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection);
    }

    [Theory, CombinatorialData]
    public Task ReadonlyPropertyWithinClassWithoutLineBreak(bool useSelection)
    {
        var code = """
            [|public class MyClass
            {
            }|]
            """;

        var codeBlock = @"private readonly int myValue = 0;";

        var expected = """
            public class MyClass
            {
                private readonly int myValue = 0;
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection);
    }
    
    [Theory, CombinatorialData]
    public Task PublicPropertyInsertion(bool useSelection)
    {
        var code = """
            using System.Threading.Tasks;
            namespace My.Namespace
            {
                [|public class TaskifyManager : ITaskifyManager
                {
                    private readonly ITaskRepository Tasks;
                    private readonly ILogger _logger;
                    public TaskifyManager(ITaskRepository tasks, ILogger logger)
                    {
                        Tasks = tasks;
                        _logger = logger;
                    }
                }|]
            }
            """;

        var codeBlock = @"public int MaxTasks { get; set; }";

        var expected = useSelection ? 
           """
           using System.Threading.Tasks;
           namespace My.Namespace
           {
               public class TaskifyManager : ITaskifyManager
               {
                   public int MaxTasks { get; set; }
                   private readonly ITaskRepository Tasks;
                   private readonly ILogger _logger;
                   public TaskifyManager(ITaskRepository tasks, ILogger logger)
                   {
                       Tasks = tasks;
                       _logger = logger;
                   }
               }
           }
           """ :
           """
            using System.Threading.Tasks;
            namespace My.Namespace
            {
                public class TaskifyManager : ITaskifyManager
                {
                    private readonly ITaskRepository Tasks;
                    private readonly ILogger _logger;
                    public int MaxTasks { get; set; }
                    public TaskifyManager(ITaskRepository tasks, ILogger logger)
                    {
                        Tasks = tasks;
                        _logger = logger;
                    }
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection);
    }    

    [Fact]
    public Task ConstructorOverloadInsertion()
    {
        var code = """
            using System.Threading.Tasks;
            namespace My.Namespace
            {
                public class TaskifyManager : ITaskifyManager
                {
                    private readonly ITaskRepository Tasks;
                    private readonly ILogger _logger;
                    public TaskifyManager(ITaskRepository tasks, ILogger logger)
                    {
                        Tasks = tasks;
                        _logger = logger;
                    }
                }
            }
            """;

        var codeBlock = """
            public TaskifyManager(ITaskRepository tasks, ILogger logger, IUserInterface userInterface)
            {
                Tasks = tasks;
                _logger = logger;
                _userInterface = userInterface;
            }
            """;

        var expected = """
            using System.Threading.Tasks;
            namespace My.Namespace
            {
                public class TaskifyManager : ITaskifyManager
                {
                    private readonly ITaskRepository Tasks;
                    private readonly ILogger _logger;
                    public TaskifyManager(ITaskRepository tasks, ILogger logger)
                    {
                        Tasks = tasks;
                        _logger = logger;
                    }
                    public TaskifyManager(ITaskRepository tasks, ILogger logger, IUserInterface userInterface)
                    {
                        Tasks = tasks;
                        _logger = logger;
                        _userInterface = userInterface;
                    }
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }    

    [Fact]
    public Task MultiFieldWithSingleLineGetter()
    {
        var code = """
            using System.Threading.Tasks;
            namespace My.Namespace;
            
            public class TaskifyManager : ITaskifyManager
            {
                private readonly ITaskRepository Tasks;
                private readonly ILogger _logger;
                public TaskifyManager(ITaskRepository tasks, ILogger logger)
                {
                    Tasks = tasks;
                    _logger = logger;
                }
            }
            """;

        var codeBlock = """
            private const int MaxTasks = 100;
            public int MaxTasksAllowed
            {
                get { return MaxTasks; }
            }
            """;

        var expected = """
            using System.Threading.Tasks;
            namespace My.Namespace;
            
            public class TaskifyManager : ITaskifyManager
            {
                private readonly ITaskRepository Tasks;
                private readonly ILogger _logger;
                private const int MaxTasks = 100;
                public int MaxTasksAllowed
                {
                    get { return MaxTasks; }
                }
                public TaskifyManager(ITaskRepository tasks, ILogger logger)
                {
                    Tasks = tasks;
                    _logger = logger;
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }

    [Fact]
    public Task MultiFieldWithMultiLineGetter()
    {
        var code = """
            public class TaskifyManager : ITaskifyManager
            {
                private readonly ITaskRepository Tasks;
                private readonly ILogger _logger;
                public TaskifyManager(ITaskRepository tasks, ILogger logger)
                {
                    Tasks = tasks;
                    _logger = logger;
                }
            }
            """;

        var codeBlock = """
            private const int MaxTasks = 100;
            public int MaxTasksAllowed
            {
                get
                {
                    return MaxTasks;
                }
            }
            """;

        var expected = """
            public class TaskifyManager : ITaskifyManager
            {
                private readonly ITaskRepository Tasks;
                private readonly ILogger _logger;
                private const int MaxTasks = 100;
                public int MaxTasksAllowed
                {
                    get
                    {
                        return MaxTasks;
                    }
                }
                public TaskifyManager(ITaskRepository tasks, ILogger logger)
                {
                    Tasks = tasks;
                    _logger = logger;
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }

    [Fact]
    public Task InsertsPublicAndPrivateProperties()
    {
        var code = """
            public class TaskifyManager : ITaskifyManager
            {
                private readonly ITaskRepository Tasks;
                private readonly ILogger _logger;
                public TaskifyManager(ITaskRepository tasks, ILogger logger)
                {
                    Tasks = tasks;
                    _logger = logger;
                }
            }
            """;

        var codeBlock = """
            private readonly int _maxTasks;
            public int MaxTasks => _maxTasks;
            """;

        var expected = """
            public class TaskifyManager : ITaskifyManager
            {
                private readonly ITaskRepository Tasks;
                private readonly ILogger _logger;
                private readonly int _maxTasks;
                public int MaxTasks => _maxTasks;
                public TaskifyManager(ITaskRepository tasks, ILogger logger)
                {
                    Tasks = tasks;
                    _logger = logger;
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }

    [Fact]
    public Task InsertsMethodAsChildOfClass()
    {
        var code = """
            public class TaskifyManager : ITaskifyManager
            {
                private readonly ITaskRepository Tasks;
                private readonly ILogger _logger;
                public TaskifyManager(ITaskRepository tasks, ILogger logger)
                {
                    Tasks = tasks;
                    _logger = logger;
                }
            
                [HttpPost]
                public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
                {
                    var task = await FetchTaskOrThrowAsync(key);
                    var subTasks = await Tasks.GetChildrenAsync(task.Id);
                    return new TaskDetailsDto(task, subTasks);
                }
            }
            """;

        var codeBlock = """
            private async Task<Task> FetchTaskOrThrowAsync(TaskKey? key)
            {
                if (key == null)
                {
                    throw new ArgumentException("Task key is null");
                }
                var task = await Tasks.GetAsync(key.Value.Id);
                if (task == null)
                {
                    throw new ArgumentException($"Task not found for id: {key.Value.Id}");
                }
                return task;
            }
            """;

        var expected = """
            public class TaskifyManager : ITaskifyManager
            {
                private readonly ITaskRepository Tasks;
                private readonly ILogger _logger;
                public TaskifyManager(ITaskRepository tasks, ILogger logger)
                {
                    Tasks = tasks;
                    _logger = logger;
                }
            
                [HttpPost]
                public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
                {
                    var task = await FetchTaskOrThrowAsync(key);
                    var subTasks = await Tasks.GetChildrenAsync(task.Id);
                    return new TaskDetailsDto(task, subTasks);
                }
                private async Task<Task> FetchTaskOrThrowAsync(TaskKey? key)
                {
                    if (key == null)
                    {
                        throw new ArgumentException("Task key is null");
                    }
                    var task = await Tasks.GetAsync(key.Value.Id);
                    if (task == null)
                    {
                        throw new ArgumentException($"Task not found for id: {key.Value.Id}");
                    }
                    return task;
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }

    [Fact]
    public Task InsertsMethodWithAttributeAsChildOfClass()
    {
        var code = """
            public class TaskifyManager : ITaskifyManager
            {
                private readonly ITaskRepository Tasks;
                private readonly ILogger _logger;
                public TaskifyManager(ITaskRepository tasks, ILogger logger)
                {
                    Tasks = tasks;
                    _logger = logger;
                }
            
                [HttpPost]
                public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
                {
                    var task = await FetchTaskOrThrowAsync(key);
                    var subTasks = await Tasks.GetChildrenAsync(task.Id);
                    return new TaskDetailsDto(task, subTasks);
                }
            }
            """;

        var codeBlock = """
            [HttpPut]
            public async Task UpdateTaskAsync(TaskDto taskDto)
            {
                var task = await FetchTaskOrThrowAsync(taskDto.Key);
                task.UpdateFromDto(taskDto);
                await Tasks.UpdateAsync(task);
            }
            """;

        var expected = """
            public class TaskifyManager : ITaskifyManager
            {
                private readonly ITaskRepository Tasks;
                private readonly ILogger _logger;
                public TaskifyManager(ITaskRepository tasks, ILogger logger)
                {
                    Tasks = tasks;
                    _logger = logger;
                }
            
                [HttpPost]
                public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
                {
                    var task = await FetchTaskOrThrowAsync(key);
                    var subTasks = await Tasks.GetChildrenAsync(task.Id);
                    return new TaskDetailsDto(task, subTasks);
                }
                [HttpPut]
                public async Task UpdateTaskAsync(TaskDto taskDto)
                {
                    var task = await FetchTaskOrThrowAsync(taskDto.Key);
                    task.UpdateFromDto(taskDto);
                    await Tasks.UpdateAsync(task);
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }

    [Fact]
    public Task EventHandlerOnTopOnCtor()
    {
        var code = """
            public class Manager : ITaskifyManager
            {
                private readonly ITaskRepository Tasks;
                private readonly ILogger _logger;
                public TaskifyManager(ITaskRepository tasks, ILogger logger)
                {
                    Tasks = tasks;
                    _logger = logger;
                }
            }
            """;

        var codeBlock = """
            public event EventHandler MyNewEvent;
            """;

        var expected = """
            public class Manager : ITaskifyManager
            {
                private readonly ITaskRepository Tasks;
                private readonly ILogger _logger;
                public event EventHandler MyNewEvent;
                public TaskifyManager(ITaskRepository tasks, ILogger logger)
                {
                    Tasks = tasks;
                    _logger = logger;
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }
    

    [Fact]
    public Task InvalidNodesAreNotInserted()
    {
        var code = """
            public class MathHelperTests
            {
                [Test]
                public void Add_WhenGivenTwoIntegers_ReturnsTheSum()
                {
                    // Arrange
                    int a = 5;
                    int b = 10;
                    int expected = 15;
            
                    // Act
                    int actual = MathHelper.Add(a, b);
            
                    // Assert
                    Assert.Equal(expected, actual);
                }
            }
            """;

        var codeBlock = """
            [Test]
            public void Multiply_WhenGivenTwoLargeIntegers_ReturnsTheProduct()
            {
                // Arrange
                int a = int.MaxValue / 2;
                int b = 2;
                int expected = int.MaxValue - 1;
            
                // Act
                int actual = MathHelper.Multiply(a, b);
            
                // Assert
                Assert.AreEqual(expected, actual);
            }
            
            [Test]
            public void Multiply_WhenGivenTwoSmallIntegers_ReturnsTheProduct()
            {
                // Arrange
                int a = int.MinValue + 1;
                int b = int.MinValue + 1;
                int expected = 0;
            
                // Act
                int actual = MathHelper.Multiply(a, b);
            
                // Assert
                Assert.AreEqual(expected, actual);
            }
            
            [Test]
            public void Divide_WhenGivenTwoLargeIntegers_ReturnsTheQuotient()
            {
                // Arrange
                int
            """;

        var expected = """
            public class MathHelperTests
            {
                [Test]
                public void Add_WhenGivenTwoIntegers_ReturnsTheSum()
                {
                    // Arrange
                    int a = 5;
                    int b = 10;
                    int expected = 15;
            
                    // Act
                    int actual = MathHelper.Add(a, b);
            
                    // Assert
                    Assert.Equal(expected, actual);
                }
                [Test]
                public void Multiply_WhenGivenTwoLargeIntegers_ReturnsTheProduct()
                {
                    // Arrange
                    int a = int.MaxValue / 2;
                    int b = 2;
                    int expected = int.MaxValue - 1;
            
                    // Act
                    int actual = MathHelper.Multiply(a, b);
            
                    // Assert
                    Assert.AreEqual(expected, actual);
                }
                [Test]
                public void Multiply_WhenGivenTwoSmallIntegers_ReturnsTheProduct()
                {
                    // Arrange
                    int a = int.MinValue + 1;
                    int b = int.MinValue + 1;
                    int expected = 0;
            
                    // Act
                    int actual = MathHelper.Multiply(a, b);
            
                    // Assert
                    Assert.AreEqual(expected, actual);
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }

    [Fact]
    public Task FocusedScopedInsertionForLoop()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;
            
            namespace ConsoleApp1
            {
                internal class Class1
                {
                    int Add(int a, int b)
                    {
                        return a + b;
                    }
            
                    [|int Multiply(int a, int b)
                    {
                        return a * b;
                    }|]
                }
            }
            """;

        var codeBlock = """
            for (int i = 1; i <= 10; i++)
            {
                Console.WriteLine(i);
            }
            """;

        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;
            
            namespace ConsoleApp1
            {
                internal class Class1
                {
                    int Add(int a, int b)
                    {
                        return a + b;
                    }
            
                    int Multiply(int a, int b)
                    {
                        for (int i = 1; i <= 10; i++)
                        {
                            Console.WriteLine(i);
                        }
                        return a * b;
                    }
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }
}
