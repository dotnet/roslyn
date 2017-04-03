using System;
using System.Threading.Tasks;

class Foo {
    async Task<int> Main() {
        return await Task.Factory.StartNew(() => 5);
    }
}
