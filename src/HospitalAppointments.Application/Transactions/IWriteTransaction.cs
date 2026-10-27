namespace HospitalAppointments.Application.Transactions;

public interface IWriteTransaction
{
    Task<IWriteScope> BeginAsync(CancellationToken cancellationToken = default);
}

public interface IWriteScope : IAsyncDisposable
{
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseBusyException : Exception
{
    public DatabaseBusyException(Exception inner) : base("The database is busy; start a fresh request.", inner) { }
}

public sealed class PersistenceConflictException(Exception inner)
    : Exception("The record conflicts with an existing or referenced record.", inner);
