using FlakeGuard.Core.Services;
using FlakeGuard.Tests.Fakes;
using Xunit;

namespace FlakeGuard.Tests.Services;

public class TestCaseRegistryTests
{
    [Fact]
    public async Task UnseenTest_IsCreatedOnFirstSight()
    {
        var repository = new InMemoryTestCaseRepository();
        var registry = new TestCaseRegistry(repository);
        var repositoryId = Guid.NewGuid();

        var testCase = await registry.GetOrCreateAsync(repositoryId, "Suite", "NewTest");

        var stored = await repository.GetByIdAsync(testCase.Id);
        Assert.NotNull(stored);
        Assert.Equal("Suite", stored!.SuiteName);
        Assert.Equal("NewTest", stored.TestName);
    }

    [Fact]
    public async Task SeenTest_ReturnsTheSameIdentityOnEveryCall()
    {
        var repository = new InMemoryTestCaseRepository();
        var registry = new TestCaseRegistry(repository);
        var repositoryId = Guid.NewGuid();

        var first = await registry.GetOrCreateAsync(repositoryId, "Suite", "RepeatTest");
        var second = await registry.GetOrCreateAsync(repositoryId, "Suite", "RepeatTest");

        Assert.Equal(first.Id, second.Id);
    }
}
