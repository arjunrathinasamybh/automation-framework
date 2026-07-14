using NUnit.Framework;

// Scenarios run sequentially. Entra ID throttles rapid repeated sign-ins from one account, so parallel
// scenarios against a single test account produce authentication failures that look like product bugs.
// Raise this only alongside a pool of distinct test accounts.
[assembly: LevelOfParallelism(1)]
[assembly: Parallelizable(ParallelScope.None)]
