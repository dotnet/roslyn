using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

class C
{
    static void Main()
    {
        MainAsync().GetAwaiter().GetResult();
    }

    static async Task MainAsync()
    {
        IObservable<string> o1 = obs1();
        // This observable is much like Task<T>.ToObservable() - only runs the async method once, and caches the result
        // so that any .Subscribe() after its return statement will give you back an OnNext instantly.
        var tcs1a = new TaskCompletionSource<object>();
        Console.WriteLine("o1a.subscribe...");
        using (o1.Subscribe(msg => Console.WriteLine(msg), (ex) => tcs1a.SetException(ex), () => tcs1a.SetResult(null)))
        {
            Console.WriteLine("o1a.waiting...");
            await tcs1a.Task;
            Console.WriteLine("o1a.done");
        }
        var tcs1b = new TaskCompletionSource<object>();
        Console.WriteLine("o1b.subscribe...");
        using (o1.Subscribe(msg => Console.WriteLine(msg), (ex) => tcs1b.SetException(ex), () => tcs1b.SetResult(null)))
        {
            Console.WriteLine("o1b.waiting...");
            await tcs1b.Task;
            Console.WriteLine("o1b.done");
        }

        IObservable<string> o2 = obs2();
        // This is an observable that's like Observable.Create - runs an instance of the async method each time you subscribe
        // and can have multiple OnNext come out of it.
        var tcs2a = new TaskCompletionSource<object>();
        Console.WriteLine("o2a.subscribe...");
        using (o2.Subscribe(msg => Console.WriteLine(msg), (ex) => tcs2a.SetException(ex), () => tcs2a.SetResult(null)))
        {
            Console.WriteLine("o2a.waiting...");
            await tcs2a.Task;
            Console.WriteLine("o2a.done");
        }
        var tcs2b = new TaskCompletionSource<object>();
        Console.WriteLine("o2b.subscribe...");
        using (o2.Subscribe(msg => Console.WriteLine(msg), (ex) => tcs2b.SetException(ex), () => tcs2b.SetResult(null)))
        {
            Console.WriteLine("o2b.waiting...");
            await tcs2b.Task;
            Console.WriteLine("o2b.done");
        }

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


    static async IObservable1<string> obs1()
    {
        // This "IObservable" will run only once when you invoke it.
        // Subscribers will see the return as an OnNext at the moment it happens
        // (or, immediately upon subscription if the return has already happened prior to subscribing)
        await Task.Delay(100);
        return "hello";
    }

    static async IObservableN<string> obs2()
    {
        // An instance of this "IObservable" will start running at the moment you subscribe to it.
        // Inelegantly, we can't use "yield return", so we await Yield(...) to fire an OnNext,
        // and there's no compile-time type checking that you've called it with the right
        // argument type, and you're forced to put in a dummy return value that's ignored.
        await Task.Delay(100);
        await AsyncObservable.Yield("hello");
        await Task.Delay(200);
        await AsyncObservable.Yield("world");
        await Task.Delay(300);
        return null; // dummy
    }

}

