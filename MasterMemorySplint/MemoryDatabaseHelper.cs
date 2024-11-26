using System;
using System.Collections.Generic;

using MasterMemory;
using MasterMemory.Validation;

using MessagePack;

namespace MasterMemorySplint;

public interface IMemoryDatabaseHelper
{
#if NET8_0_OR_GREATER
    static abstract IMemoryDatabaseHelper Instance { get; }

    static abstract TView ExtractTableData<T, TView>(
        Dictionary<string, (int offset, int count)> header,
        ReadOnlyMemory<byte> databaseBinary,
        MessagePackSerializerOptions options,
        Func<T[], TView> createView
    );
#endif

    void ValidateTable<TElement>(IReadOnlyList<TElement> table, ValidationDatabase database, string pkName, Delegate pkSelector, ValidateResult result);
}

public class MemoryDatabaseHelper: MemoryDatabaseBase, IMemoryDatabaseHelper
{
    public static MemoryDatabaseHelper Instance { get; } = new MemoryDatabaseHelper();

#if NET8_0_OR_GREATER
    static IMemoryDatabaseHelper IMemoryDatabaseHelper.Instance => Instance;
#endif

    protected override void Init(
        Dictionary<string, (int offset, int count)> header,
        ReadOnlyMemory<byte> databaseBinary,
        MessagePackSerializerOptions options,
        int maxDegreeOfParallelism
    ) => throw new NotSupportedException();

    public static new TView ExtractTableData<T, TView>(
        Dictionary<string, (int offset, int count)> header,
        ReadOnlyMemory<byte> databaseBinary,
        MessagePackSerializerOptions options,
        Func<T[], TView> createView
    ) => MemoryDatabaseBase.ExtractTableData(header, databaseBinary, options, createView);

    public new virtual void ValidateTable<TElement>(IReadOnlyList<TElement> table, ValidationDatabase database, string pkName, Delegate pkSelector, ValidateResult result)
        => base.ValidateTable(table, database, pkName, pkSelector, result);
}

public static class MemoryDatabaseHelperExtensions
{
    public static void ValidateTable<TTable, TElement, TPrimaryKey>(
        this IMemoryDatabaseHelper @this,
        ITable<TTable, TElement, TPrimaryKey> table,
        ValidationDatabase database,
        string pkName,
        ValidateResult result
    )
        where TTable : class, ITable<TTable, TElement, TPrimaryKey>
        where TElement : class
        where TPrimaryKey : notnull
    {
        ArgumentNullException.ThrowIfNull(@this);
        ArgumentNullException.ThrowIfNull(table);

        @this.ValidateTable(table.All, database, pkName, table.PrimaryKeySelector, result);
    }
}
