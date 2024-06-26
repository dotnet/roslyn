using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.UtilityTest;

public class WeakEventTests
{
    [Fact]
    public void AddAndRemove()
    {
        var e = new WeakEvent<int>();

        var sender = new object();
        var target = new List<int>();
        var handler1 = new WeakEventHandler<int>((sender, target, arg) => Assert.IsType<List<int>>(target).Add(arg * 10));
        var handler2 = new WeakEventHandler<int>((sender, target, arg) => Assert.IsType<List<int>>(target).Add(arg * 20));
        var handler3 = new WeakEventHandler<int>((sender, target, arg) => Assert.IsType<List<int>>(target).Add(arg * 30));

        e.AddHandler(target, handler1);
        e.AddHandler(target, handler2);
        e.AddHandler(target, handler3);

        e.RemoveHandler(target, handler2);

        e.RaiseEvent(sender, 1);

        AssertEx.Equal([10, 30], target);
        target.Clear();

        e.RemoveHandler(target, handler1);
        e.RemoveHandler(target, handler3);

        e.RaiseEvent(sender, 1);
        Assert.Empty(target);
    }
}
