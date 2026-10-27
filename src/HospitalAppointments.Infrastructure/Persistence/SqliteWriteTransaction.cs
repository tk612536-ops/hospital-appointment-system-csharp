using HospitalAppointments.Application.Transactions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HospitalAppointments.Infrastructure.Persistence;

public sealed class SqliteWriteTransaction(ApplicationDbContext database) : IWriteTransaction
{
    public async Task<IWriteScope> BeginAsync(CancellationToken cancellationToken = default)
    {
        if (database.Database.CurrentTransaction is not null || database.ChangeTracker.HasChanges())
            throw new InvalidOperationException("A write command requires a fresh transaction and clean context.");
        // Detach any previously read domain state; every authoritative read follows acquisition.
        database.ChangeTracker.Clear();
        SqliteTransaction? transaction = null;
        try
        {
            await database.Database.OpenConnectionAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            transaction = ((SqliteConnection)database.Database.GetDbConnection()).BeginTransaction(deferred: false);
            cancellationToken.ThrowIfCancellationRequested();
            var enlisted = await database.Database.UseTransactionAsync(transaction, cancellationToken)
                ?? throw new InvalidOperationException("EF transaction enlistment failed.");
            return new Scope(database, transaction, enlisted);
        }
        catch (Exception exception)
        {
            if (transaction is not null) await transaction.DisposeAsync();
            await database.Database.CloseConnectionAsync();
            if (exception is SqliteException { SqliteErrorCode: 5 }) throw new DatabaseBusyException(exception);
            throw;
        }
    }

    private sealed class Scope(ApplicationDbContext database, SqliteTransaction transaction, IDbContextTransaction enlisted) : IWriteScope
    {
        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            try { await database.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteErrorCode: 5 })
            { throw new DatabaseBusyException(exception); }
            catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteErrorCode: 19 })
            { throw new PersistenceConflictException(exception); }
        }
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            try { await transaction.CommitAsync(cancellationToken); }
            catch (SqliteException exception) when (exception.SqliteErrorCode == 5) { throw new DatabaseBusyException(exception); }
        }
        public async ValueTask DisposeAsync()
        {
            // An uncommitted native transaction rolls back on disposal.
            try { await enlisted.DisposeAsync(); }
            finally
            {
                try { await transaction.DisposeAsync(); }
                finally
                {
                    database.ChangeTracker.Clear();
                    await database.Database.CloseConnectionAsync();
                }
            }
        }
    }
}
