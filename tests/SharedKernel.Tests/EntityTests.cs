using SharedKernel;

namespace SharedKernel.Tests;

// Concrete entity for testing the abstract base class
file sealed class TestEntity : Entity<Guid>
{
    public TestEntity(Guid id) { Id = id; }

    public void Raise(IDomainEvent e) => RaiseDomainEvent(e);
}

file record TestEvent(string Name) : IDomainEvent;

public class EntityTests
{
    [Fact]
    public void Entities_WithSameId_AreEqual()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        Assert.Equal(a, b);
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void Entities_WithDifferentIds_AreNotEqual()
    {
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.NewGuid());

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GetHashCode_MatchesForEqualEntities()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void RaiseDomainEvent_AppearsInCollection()
    {
        var entity = new TestEntity(Guid.NewGuid());
        entity.Raise(new TestEvent("created"));

        Assert.Single(entity.DomainEvents);
        Assert.IsType<TestEvent>(entity.DomainEvents.First());
    }

    [Fact]
    public void ClearDomainEvents_EmptiesCollection()
    {
        var entity = new TestEntity(Guid.NewGuid());
        entity.Raise(new TestEvent("created"));
        entity.ClearDomainEvents();

        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        var entity = new TestEntity(Guid.NewGuid());

        Assert.False(entity.Equals(null));
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var entity = new TestEntity(Guid.NewGuid());

        Assert.True(entity.Equals(entity));
    }
}
