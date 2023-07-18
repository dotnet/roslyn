The EndToEnd tests are isolated from other compiler test to reduce variability.

Specifically, other tests could:
1. affect JIT ordering,
2. cause GC side-effects,
3. affect implicit caching,
4. change the starting stack size.

Because EndToEnd tests are in a single test class, they also don't get parallelized.
