// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeMapper;

public partial class CSharpCodeMapperTests
{
    [Fact]
    public Task SimpleReplaceMethodBySignature()
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
            public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
            {
                if (key == null)
                {
                    return null;
                }
            
                var task = await FetchTaskOrThrowAsync(key);
                var subTasks = await Tasks.GetChildrenAsync(task.Id);
                return new TaskDetailsDto(task, subTasks);
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
                    if (key == null)
                    {
                        return null;
                    }
            
                    var task = await FetchTaskOrThrowAsync(key);
                    var subTasks = await Tasks.GetChildrenAsync(task.Id);
                    return new TaskDetailsDto(task, subTasks);
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }
    
    [Fact]
    public Task ReplaceMethodBySignatureWithAddedParameters()
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
            public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key, CancellationToken cancellationToken)
            {
                if (key == null)
                {
                    return null;
                }
            
                var task = await FetchTaskOrThrowAsync(key, cancellationToken);
                var subTasks = await Tasks.GetChildrenAsync(task.Id, cancellationToken);
                return new TaskDetailsDto(task, subTasks);
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
                public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key, CancellationToken cancellationToken)
                {
                    if (key == null)
                    {
                        return null;
                    }
            
                    var task = await FetchTaskOrThrowAsync(key, cancellationToken);
                    var subTasks = await Tasks.GetChildrenAsync(task.Id, cancellationToken);
                    return new TaskDetailsDto(task, subTasks);
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }
    
    
    [Fact]
    public Task MethodBySignatureWithExtraLinebreaksAndIndentation()
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
            public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
            {
                if (key == null)
                {
                    return null;
                }
            
                var task = await FetchTaskOrThrowAsync(key);
                var subTasks = await Tasks.GetChildrenAsync(task.Id);
                return new TaskDetailsDto(task, subTasks);
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
                    if (key == null)
                    {
                        return null;
                    }
            
                    var task = await FetchTaskOrThrowAsync(key);
                    var subTasks = await Tasks.GetChildrenAsync(task.Id);
                    return new TaskDetailsDto(task, subTasks);
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }    

    [Fact]
    public Task MethodBySignatureWithNamespaceScope()
    {
        var code = """
            namespace Taskify.Managers
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
            
                    [HttpPost]
                    public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
                    {
                        var task = await FetchTaskOrThrowAsync(key);
                        var subTasks = await Tasks.GetChildrenAsync(task.Id);
                        return new TaskDetailsDto(task, subTasks);
                    }
                }
            }
            """;

        var codeBlock = """
            public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
            {
                if (key == null)
                {
                    return null;
                }
            
                var task = await FetchTaskOrThrowAsync(key);
                var subTasks = await Tasks.GetChildrenAsync(task.Id);
                return new TaskDetailsDto(task, subTasks);
            }
            """;

        var expected = """
            namespace Taskify.Managers
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
            
                    [HttpPost]
                    public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
                    {
                        if (key == null)
                        {
                            return null;
                        }
            
                        var task = await FetchTaskOrThrowAsync(key);
                        var subTasks = await Tasks.GetChildrenAsync(task.Id);
                        return new TaskDetailsDto(task, subTasks);
                    }
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }    

    [Fact]
    public Task MethodBySignatureIgnoreUnchangedAttributes()
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
            [HttpPost]
            public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }
            
                var task = await FetchTaskOrThrowAsync(key);
                var subTasks = await Tasks.GetChildrenAsync(task.Id);
                return new TaskDetailsDto(task, subTasks);
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
                    if (key == null)
                    {
                        throw new ArgumentNullException(nameof(key));
                    }
            
                    var task = await FetchTaskOrThrowAsync(key);
                    var subTasks = await Tasks.GetChildrenAsync(task.Id);
                    return new TaskDetailsDto(task, subTasks);
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }    

    [Fact]
    public Task MethodBySignatureWithSingleLineIfStatement()
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
            [HttpPost]
            public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
            {
                if (key == null) throw new ArgumentNullException(nameof(key));
            
                var task = await FetchTaskOrThrowAsync(key);
                var subTasks = await Tasks.GetChildrenAsync(task.Id);
                return new TaskDetailsDto(task, subTasks);
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
                    if (key == null) throw new ArgumentNullException(nameof(key));
            
                    var task = await FetchTaskOrThrowAsync(key);
                    var subTasks = await Tasks.GetChildrenAsync(task.Id);
                    return new TaskDetailsDto(task, subTasks);
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }    

    [Fact]
    public Task MethodBySignatureWithDocsInCodeBlock()
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
            /// <summary>
            /// These are some good docs on this method.
            /// </summary>
            [HttpPost]
            public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }
            
                var task = await FetchTaskOrThrowAsync(key);
                var subTasks = await Tasks.GetChildrenAsync(task.Id);
                return new TaskDetailsDto(task, subTasks);
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
            
                /// <summary>
                /// These are some good docs on this method.
                /// </summary>
                [HttpPost]
                public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
                {
                    if (key == null)
                    {
                        throw new ArgumentNullException(nameof(key));
                    }
            
                    var task = await FetchTaskOrThrowAsync(key);
                    var subTasks = await Tasks.GetChildrenAsync(task.Id);
                    return new TaskDetailsDto(task, subTasks);
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }    

    [Fact]
    public Task MethodBySignatureWithDocsInCodeBlockWithNamespaceScope()
    {
        var code = """
            namespace Taskify.Managers
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
            
                    [HttpPost]
                    public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
                    {
                        var task = await FetchTaskOrThrowAsync(key);
                        var subTasks = await Tasks.GetChildrenAsync(task.Id);
                        return new TaskDetailsDto(task, subTasks);
                    }
                }
            }
            """;

        var codeBlock = """
            /// <summary>
            /// These are some good docs on this method.
            /// </summary>
            [HttpPost]
            public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }
            
                var task = await FetchTaskOrThrowAsync(key);
                var subTasks = await Tasks.GetChildrenAsync(task.Id);
                return new TaskDetailsDto(task, subTasks);
            }
            """;

        var expected = """
            namespace Taskify.Managers
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
            
                    /// <summary>
                    /// These are some good docs on this method.
                    /// </summary>
                    [HttpPost]
                    public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
                    {
                        if (key == null)
                        {
                            throw new ArgumentNullException(nameof(key));
                        }
            
                        var task = await FetchTaskOrThrowAsync(key);
                        var subTasks = await Tasks.GetChildrenAsync(task.Id);
                        return new TaskDetailsDto(task, subTasks);
                    }
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }    

    [Fact]
    public Task MethodBySignatureWithDocsInCodeBlockWithMissingAttribute()
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
            /// <summary>
            /// These are some good docs on this method.
            /// </summary>
            public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }
            
                var task = await FetchTaskOrThrowAsync(key);
                var subTasks = await Tasks.GetChildrenAsync(task.Id);
                return new TaskDetailsDto(task, subTasks);
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
            
                /// <summary>
                /// These are some good docs on this method.
                /// </summary>
                [HttpPost]
                public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
                {
                    if (key == null)
                    {
                        throw new ArgumentNullException(nameof(key));
                    }
            
                    var task = await FetchTaskOrThrowAsync(key);
                    var subTasks = await Tasks.GetChildrenAsync(task.Id);
                    return new TaskDetailsDto(task, subTasks);
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }    

    [Fact]
    public Task ReplaceConstructorSignatureOnlyWhenArgumentsAreTheSame()
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
            public TaskifyManager(ITaskRepository tasks, ILogger logger)
            {
                Tasks = tasks;
                _logger = logger;
                // This is new code added by the model.
                var myVar = 0;
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
                    // This is new code added by the model.
                    var myVar = 0;
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }  

    [Fact(Skip = "There seems to be an issue with Formatter when handling the doc strings inthis text")]
    public Task EnumByNameShouldAddDocs()
    {
        var code = """
            namespace MyNamespace
            {
                public enum CodeResponseType
                {
                    None,
                    Replace,
                    Insert,
                }
            }
            """;

        var codeBlock = """
            /// <summary>
            /// Represents the type of code response.
            /// </summary>
            public enum CodeResponseType
            {
                /// <summary>
                /// Represents no code response.
                /// </summary>
                None,
                /// <summary>
                /// Represents a code response that replaces existing code.
                /// </summary>
                Replace,
                /// <summary>
                /// Represents a code response that inserts new code.
                /// </summary>
                Insert,
            }
            """;

        var expected = """
            namespace MyNamespace
            {
                /// <summary>
                /// Represents the type of code response.
                /// </summary>
                public enum CodeResponseType
                {
                    /// <summary>
                    /// Represents no code response.
                    /// </summary>
                    None,
                    /// <summary>
                    /// Represents a code response that replaces existing code.
                    /// </summary>
                    Replace,
                    /// <summary>
                    /// Represents a code response that inserts new code.
                    /// </summary>
                    Insert,
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }  

    [Fact]
    public Task ReplaceClassByName()
    {
        var code = """
            namespace SomeNamespace;
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

        var expected = """
            namespace SomeNamespace;
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

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }  

    [Fact]
    public Task ReplaceClassByNameWithNamespaceScope()
    {
        var code = """
            namespace SomeNamespace
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

        var expected = """
            namespace SomeNamespace
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
            
                    [HttpPost]
                    public async Task<TaskDetailsDto> GetTaskDetailsAsync(TaskKey? key)
                    {
                        var task = await FetchTaskOrThrowAsync(key);
                        var subTasks = await Tasks.GetChildrenAsync(task.Id);
                        return new TaskDetailsDto(task, subTasks);
                    }
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }  

    [Fact]
    public Task ReplaceClassIgnoresNamespacingInCodeBlock()
    {
        var code = """
            namespace MyProject.CsharpUnitTest;
            internal class UnitTestMethodNode : ScopedNode
            {
                public ClassDeclarationSyntax? Parent { get; private set; }
                public string? ClassName => Parent?.Identifier.ToString();
                public string Framework { get; private set; }
                public UnitTestMethodNode(SyntaxNode node, ClassDeclarationSyntax parent, string framework) : base(node, Scope.Method)
                {
                    Parent = parent;
                    Framework = framework;
                }
            }
            """;

        var codeBlock = """
            namespace MyProject.CsharpUnitTest
            {
                /// <summary>
                /// Represents a unit test method node in the C# syntax tree.
                /// </summary>
                internal class UnitTestMethodNode : ScopedNode
                {
                    /// <summary>
                    /// Gets the parent class of the unit test method.
                    /// </summary>
                    public ClassDeclarationSyntax? Parent { get; private set; }
            
                    /// <summary>
                    /// Gets the name of the parent class.
                    /// </summary>
                    public string? ClassName => Parent?.Identifier.ToString();
            
                    /// <summary>
                    /// Gets the testing framework used by the unit test method.
                    /// </summary>
                    public string Framework { get; private set; }
            
                    /// <summary>
                    /// Initializes a new instance of the <see cref="UnitTestMethodNode"/> class.
                    /// </summary>
                    /// <param name="node">The syntax node that represents the unit test method.</param>
                    /// <param name="parent">The parent class of the unit test method.</param>
                    /// <param name="framework">The testing framework used by the unit test method.</param>
                    public UnitTestMethodNode(SyntaxNode node, ClassDeclarationSyntax parent, string framework) : base(node, Scope.Method)
                    {
                        Parent = parent;
                        Framework = framework;
                    }
                }
            }
            """;

        var expected = """
            namespace MyProject.CsharpUnitTest;
            /// <summary>
            /// Represents a unit test method node in the C# syntax tree.
            /// </summary>
            internal class UnitTestMethodNode : ScopedNode
            {
                /// <summary>
                /// Gets the parent class of the unit test method.
                /// </summary>
                public ClassDeclarationSyntax? Parent { get; private set; }
            
                /// <summary>
                /// Gets the name of the parent class.
                /// </summary>
                public string? ClassName => Parent?.Identifier.ToString();
            
                /// <summary>
                /// Gets the testing framework used by the unit test method.
                /// </summary>
                public string Framework { get; private set; }
            
                /// <summary>
                /// Initializes a new instance of the <see cref="UnitTestMethodNode"/> class.
                /// </summary>
                /// <param name="node">The syntax node that represents the unit test method.</param>
                /// <param name="parent">The parent class of the unit test method.</param>
                /// <param name="framework">The testing framework used by the unit test method.</param>
                public UnitTestMethodNode(SyntaxNode node, ClassDeclarationSyntax parent, string framework) : base(node, Scope.Method)
                {
                    Parent = parent;
                    Framework = framework;
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }  

    [Fact]
    public Task ReplaceAbstractSignatureWithDocsAtEndOfClass()
    {
        var code = """
            internal abstract class TestAbstractClass
            {
                public ClassDeclarationSyntax? Parent { get; private set; }
                public string? ClassName => Parent?.Identifier.ToString();
                public string Framework { get; private set; }
                public UnitTestMethodNode(SyntaxNode node, ClassDeclarationSyntax parent, string framework) : base(node, Scope.Method)
                {
                    Parent = parent;
                    Framework = framework;
                }
            
                public abstract void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem);
            }
            """;

        var codeBlock = """
            /// <summary>
            /// This method is called when an action item on the info bar is clicked.
            /// </summary>
            public abstract void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem);
            """;

        var expected = """
            internal abstract class TestAbstractClass
            {
                public ClassDeclarationSyntax? Parent { get; private set; }
                public string? ClassName => Parent?.Identifier.ToString();
                public string Framework { get; private set; }
                public UnitTestMethodNode(SyntaxNode node, ClassDeclarationSyntax parent, string framework) : base(node, Scope.Method)
                {
                    Parent = parent;
                    Framework = framework;
                }
            
                /// <summary>
                /// This method is called when an action item on the info bar is clicked.
                /// </summary>
                public abstract void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem);
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }  

    [Fact]
    public Task ReplaceAbstractSignatureWithDocsAtEndOfClassWithNamespaceScope()
    {
        var code = """
            namespace My.Namespace
            {
                internal abstract class TestAbstractClass
                {
                    public ClassDeclarationSyntax? Parent { get; private set; }
                    public string? ClassName => Parent?.Identifier.ToString();
                    public string Framework { get; private set; }
                    public UnitTestMethodNode(SyntaxNode node, ClassDeclarationSyntax parent, string framework) : base(node, Scope.Method)
                    {
                        Parent = parent;
                        Framework = framework;
                    }
            
                    public abstract void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem);
                }
            }
            """;

        var codeBlock = """
            /// <summary>
            /// This method is called when an action item on the info bar is clicked.
            /// </summary>
            public abstract void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem);
            """;

        var expected = """
            namespace My.Namespace
            {
                internal abstract class TestAbstractClass
                {
                    public ClassDeclarationSyntax? Parent { get; private set; }
                    public string? ClassName => Parent?.Identifier.ToString();
                    public string Framework { get; private set; }
                    public UnitTestMethodNode(SyntaxNode node, ClassDeclarationSyntax parent, string framework) : base(node, Scope.Method)
                    {
                        Parent = parent;
                        Framework = framework;
                    }
            
                    /// <summary>
                    /// This method is called when an action item on the info bar is clicked.
                    /// </summary>
                    public abstract void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem);
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }  

    [Fact]
    public Task ReplaceMethodBySignatureBasketServiceOptimize()
    {
        var code = """
            public class BasketService : IBasketService
            {
                private readonly IRepository<Basket> _basketRepository;
                private readonly IAppLogger<BasketService> _logger;
            
                public BasketService(IRepository<Basket> basketRepository,
                    IAppLogger<BasketService> logger)
                {
                    _basketRepository = basketRepository;
                    _logger = logger;
                }
            
                /// <summary>
                /// Add an item to the user's basket or creates a new basket if no basket was found for that user.
                /// </summary>
                /// <param name="username">The username of the basket owner.</param>
                /// <param name="catalogItemId">The ID of the catalog item to add.</param>
                /// <param name="price">The price of the item.</param>
                /// <param name="quantity">The quantity of the item being added.</param>
                /// <returns>The modified basket object.</returns>
                public async Task<Basket> AddItemToBasket(string username, int catalogItemId, decimal price, int quantity = 1)
                {
                    // Retrieve basket from repository
                    var basketSpec = new BasketWithItemsSpecification(username);
                    var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);
            
                    // create new basket if needed
                    if (basket == null)
                    {
                        basket = new Basket(username);
                        await _basketRepository.AddAsync(basket);
                    }
            
                    // add item to basket
                    basket.AddItem(catalogItemId, price, quantity);
            
                    //update and return basket
                    await _basketRepository.UpdateAsync(basket);
                    return basket;
                }
            
                public async Task DeleteBasketAsync(int basketId)
                {
                    var basket = await _basketRepository.GetByIdAsync(basketId);
                    Guard.Against.Null(basket, nameof(basket));
                    await _basketRepository.DeleteAsync(basket);
                }
            
                public async Task<Result<Basket>> SetQuantities(int basketId, Dictionary<string, int> quantities)
                {
                    var basketSpec = new BasketWithItemsSpecification(basketId);
                    var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);
                    if (basket == null) return Result<Basket>.NotFound();
            
                    foreach (var item in basket.Items)
                    {
                        if (quantities.TryGetValue(item.Id.ToString(), out var quantity))
                        {
                            if (_logger != null) _logger.LogInformation($"Updating quantity of item ID:{item.Id} to {quantity}.");
                            item.SetQuantity(quantity);
                        }
                    }
                    basket.RemoveEmptyItems();
                    await _basketRepository.UpdateAsync(basket);
                    return basket;
                }
            }
            """;

        var codeBlock = """
            public async Task<Result<Basket>> SetQuantities(int basketId, Dictionary<string, int> quantities)
            {
                var basketSpec = new BasketWithItemsSpecification(basketId);
                var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);
                if (basket == null) return Result<Basket>.NotFound();
            
                foreach (var item in basket.Items.Where(item => quantities.TryGetValue(item.Id.ToString(), out var quantity)))
                {
                    item.SetQuantity(quantity);
                }
            
                basket.RemoveEmptyItems();
            
                await _basketRepository.UpdateAsync(basket);
            
                return basket;
            }
            """;

        var expected = """
            public class BasketService : IBasketService
            {
                private readonly IRepository<Basket> _basketRepository;
                private readonly IAppLogger<BasketService> _logger;
            
                public BasketService(IRepository<Basket> basketRepository,
                    IAppLogger<BasketService> logger)
                {
                    _basketRepository = basketRepository;
                    _logger = logger;
                }
            
                /// <summary>
                /// Add an item to the user's basket or creates a new basket if no basket was found for that user.
                /// </summary>
                /// <param name="username">The username of the basket owner.</param>
                /// <param name="catalogItemId">The ID of the catalog item to add.</param>
                /// <param name="price">The price of the item.</param>
                /// <param name="quantity">The quantity of the item being added.</param>
                /// <returns>The modified basket object.</returns>
                public async Task<Basket> AddItemToBasket(string username, int catalogItemId, decimal price, int quantity = 1)
                {
                    // Retrieve basket from repository
                    var basketSpec = new BasketWithItemsSpecification(username);
                    var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);
            
                    // create new basket if needed
                    if (basket == null)
                    {
                        basket = new Basket(username);
                        await _basketRepository.AddAsync(basket);
                    }
            
                    // add item to basket
                    basket.AddItem(catalogItemId, price, quantity);
            
                    //update and return basket
                    await _basketRepository.UpdateAsync(basket);
                    return basket;
                }
            
                public async Task DeleteBasketAsync(int basketId)
                {
                    var basket = await _basketRepository.GetByIdAsync(basketId);
                    Guard.Against.Null(basket, nameof(basket));
                    await _basketRepository.DeleteAsync(basket);
                }
            
                public async Task<Result<Basket>> SetQuantities(int basketId, Dictionary<string, int> quantities)
                {
                    var basketSpec = new BasketWithItemsSpecification(basketId);
                    var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);
                    if (basket == null) return Result<Basket>.NotFound();
            
                    foreach (var item in basket.Items.Where(item => quantities.TryGetValue(item.Id.ToString(), out var quantity)))
                    {
                        item.SetQuantity(quantity);
                    }
            
                    basket.RemoveEmptyItems();
            
                    await _basketRepository.UpdateAsync(basket);
            
                    return basket;
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }  

    [Fact]
    public Task ReplaceDeviceControllerIgnoresAttributes()
    {
        var code = """
            namespace MyHome.Web.Server.Controllers
            {
                [ApiController]
                [Route("device")]
                public class DeviceController : ControllerBase
                {
                    private readonly ILogger<DeviceController> _logger;
                    private readonly IDeviceRegistration _deviceRegistration;
            
                    public DeviceController(ILogger<DeviceController> logger, IDeviceRegistration deviceRegistration)
                    {
                        _logger = logger;
                        _deviceRegistration = deviceRegistration;
                    }
            
                    [HttpGet]
                    public GetDeviceResponse Get()
                    {
                        // Check if the device is null
                        if(_deviceRegistration.Device == null)
                        {
                            return new GetDeviceResponse
                            {
                                Registered = false,
                                Device = null,
                            };
                        }
                        var device = _deviceRegistration.Device;
                        // Here we are returning a new response.
                        return new GetDeviceResponse
                        {
                            Registered = true,
                            Device = new DeviceModel()
                            {
                                ModelId = device.ModelId,
                                DeviceId = device.DeviceId,
                                DpsEndpointName = device.DpsEndpointName,
                                DpsIdScope = device.DpsIdScope,
                                SymmetricKey = device.SymmetricKey,
                            }
                        };
                    }
                }
            }
            """;

        var codeBlock = """
            public GetDeviceResponse Get()
            {
                // Check if the device is null
                if (_deviceRegistration.Device == null)
                {
                    _logger.LogInformation("Device not found.");
                    return new GetDeviceResponse
                    {
                        Registered = false,
                        Device = null,
                    };
                }
                var device = _deviceRegistration.Device;
                _logger.LogInformation("Device found.");
                // Here we are returning a new response.
                return new GetDeviceResponse
                {
                    Registered = true,
                    Device = new DeviceModel()
                    {
                        ModelId = device.ModelId,
                        DeviceId = device.DeviceId,
                        DpsEndpointName = device.DpsEndpointName,
                        DpsIdScope = device.DpsIdScope,
                        SymmetricKey = device.SymmetricKey,
                    }
                };
            }
            """;

        var expected = """
            namespace MyHome.Web.Server.Controllers
            {
                [ApiController]
                [Route("device")]
                public class DeviceController : ControllerBase
                {
                    private readonly ILogger<DeviceController> _logger;
                    private readonly IDeviceRegistration _deviceRegistration;
            
                    public DeviceController(ILogger<DeviceController> logger, IDeviceRegistration deviceRegistration)
                    {
                        _logger = logger;
                        _deviceRegistration = deviceRegistration;
                    }
            
                    [HttpGet]
                    public GetDeviceResponse Get()
                    {
                        // Check if the device is null
                        if (_deviceRegistration.Device == null)
                        {
                            _logger.LogInformation("Device not found.");
                            return new GetDeviceResponse
                            {
                                Registered = false,
                                Device = null,
                            };
                        }
                        var device = _deviceRegistration.Device;
                        _logger.LogInformation("Device found.");
                        // Here we are returning a new response.
                        return new GetDeviceResponse
                        {
                            Registered = true,
                            Device = new DeviceModel()
                            {
                                ModelId = device.ModelId,
                                DeviceId = device.DeviceId,
                                DpsEndpointName = device.DpsEndpointName,
                                DpsIdScope = device.DpsIdScope,
                                SymmetricKey = device.SymmetricKey,
                            }
                        };
                    }
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }  

    [Fact]
    public Task UnindentedReplaceShouldIndentValues()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;
            
            namespace ConsoleApp1
            {
                class Program
                {
                    [|static async Task Main(string[] args)
                    {
            
                    }|]
                }
            }
            """;

        var codeBlock = """
            static async Task Main(string[] args)
            {
            foreach (var arg in args)
            {
            Console.WriteLine(arg);
            }
            }
            """;

        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;
            
            namespace ConsoleApp1
            {
                class Program
                {
                    static async Task Main(string[] args)
                    {
                        foreach (var arg in args)
                        {
                            Console.WriteLine(arg);
                        }
                    }
                }
            }
            """;

        return TestAsync(code, ImmutableArray.Create(codeBlock), expected, useSelection: true);
    }
}
