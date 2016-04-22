using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

class C
{
    static void Main()
    {
        MainAsync().GetAwaiter().GetResult();
    }

    static async Task MainAsync()
    {
        // Test ValueTask
        var i1 = await g(0);
        Console.WriteLine(i1);
        var i2 = await g(100);
        Console.WriteLine(i2);

        // Test ITask
        ITask<string> its = f();
        ITask<object> ito = its;
        var io = await ito;
        Console.WriteLine(io);

        // Weaving Task
        await h();

        // IAsyncAction
        await uwp();

        // Test FactoryTask
        var factory = fac("hello");
        var t1 = factory.SpawnInstance();
        var t2 = factory.SpawnInstance();
        await Task.WhenAll(t1, t2);

        // Test IObservable

    }

    static async ITask<string> f()
    {
        await Task.Yield();
        return "hello";
    }

    static async ValueTask<int> g(int delay)
    {
        Task t = null;
        if (delay > 0) t = Task.Delay(delay);
        if (delay > 0) await t;
        return delay;
    }

    static async WeavingTask h()
    {
        await new WeavingConfiguration(() => Console.WriteLine("Weave suspend"), () => Console.WriteLine("Weave resume"));

        Console.WriteLine("h.A");
        await Delay(0);
        Console.WriteLine("h.B");
        await Delay(10);
        Console.WriteLine("h.C");
        await Delay(100);
        Console.WriteLine("h.D");
    }

    static async Task Delay(int i)
    {
        Console.WriteLine($"  about to delay {i}");
        await Task.Delay(i);
        Console.WriteLine($"  done delay {i}");
    }

    static async IAsyncAction uwp()
    {
        Console.WriteLine("uwp0");
        await Task.Delay(100);
        Console.WriteLine("uwp1");
    }

    static int fid = 0;
    static async FactoryTask fac(string msg)
    {
        var id = fid++;
        Console.WriteLine($"Factory instance START {msg} - id {id}");
        await Task.Delay(200 - id * 100);
        Console.WriteLine($"Factory instance END {msg} - id {id}");
    }

}


