// Integration tests involve async message brokering, in-memory buses, and shared mocks.
// Parallel execution between test classes causes NSubstitute call-count interference
// when multiple harnesses handle the same message types concurrently.
// Disabling collection-level parallelism is standard practice for integration test suites.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
