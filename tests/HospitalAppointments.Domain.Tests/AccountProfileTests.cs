using HospitalAppointments.Domain.Accounts;

namespace HospitalAppointments.Domain.Tests;

public sealed class AccountProfileTests
{
    [Fact]
    public void RolesAreComposedAndDefensivelyCopied()
    {
        var original = new HashSet<AccountRole> { AccountRole.Patient, AccountRole.Staff };
        var profile = new AccountProfile(1, "  Demo Hasta  ", original);
        original.Clear();
        Assert.Equal("Demo Hasta", profile.DisplayName);
        Assert.True(profile.Roles.SetEquals([AccountRole.Patient, AccountRole.Staff]));
        Assert.False(profile.Roles is ISet<AccountRole> mutable && !mutable.IsReadOnly);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidAccountIdentityIsRejected(long id) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new AccountProfile(id, "Demo", [AccountRole.Patient]));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BlankDisplayNamesAreRejected(string? name) =>
        Assert.ThrowsAny<ArgumentException>(() => new AccountProfile(1, name!, [AccountRole.Patient]));

    [Fact]
    public void EmptyAndUnknownRolesAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new AccountProfile(1, "Demo", []));
        Assert.Throws<ArgumentException>(() => new AccountProfile(1, "Demo", [(AccountRole)99]));
    }

    [Fact]
    public void DomainDoesNotReferenceWebOrPersistenceFrameworks()
    {
        var references = typeof(AccountProfile).Assembly.GetReferencedAssemblies().Select(a => a.Name!);
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
            || name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
            || name.StartsWith("HospitalAppointments.", StringComparison.Ordinal));
    }
}
