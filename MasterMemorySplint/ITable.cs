using System;
using System.Collections.Generic;

using MasterMemory;
using MasterMemory.Meta;
using MasterMemory.Validation;

namespace MasterMemorySplint;

public interface ITable: ITableUniqueValidate
{
#if NET8_0_OR_GREATER
    static abstract Type ElementType { get; }

    static abstract Type PrimaryKeyType { get; }

    static abstract string TableName { get; }
#endif

    Delegate PrimaryKeySelector { get; }

    int Count { get; }

    object[] GetRawDataUnsafe();

#if NET8_0_OR_GREATER
    static abstract object SelectPrimaryKey(object item);

    static abstract ITable CreateFromSortedData(object[] sortedData);

    static abstract ITable Create(object[] data);

    static abstract MetaTable CreateMetaTable();
#endif
}

public interface ITable<TElement>: ITable
    where TElement : class
{
#if NET8_0_OR_GREATER
    static Type ITable.ElementType => typeof(TElement);
#endif

    RangeView<TElement> All { get; }

    RangeView<TElement> AllReverse { get; }

    new TElement[] GetRawDataUnsafe();

#if NET8_0_OR_GREATER
    object[] ITable.GetRawDataUnsafe() => this.GetRawDataUnsafe();

    static abstract ITable<TElement> CreateFromSortedData(TElement[] sortedData);

    static abstract ITable<TElement> Create(TElement[] data);
#endif
}

public interface IKeyedTable<TElement, TPrimaryKey>: ITable<TElement>
    where TElement : class
    where TPrimaryKey : notnull
{
    new Func<TElement, TPrimaryKey> PrimaryKeySelector { get; }

#if NET8_0_OR_GREATER
    Delegate ITable.PrimaryKeySelector => this.PrimaryKeySelector;

    static Type ITable.PrimaryKeyType => typeof(TPrimaryKey);

    static abstract TPrimaryKey SelectPrimaryKey(TElement item);
#endif
}

public interface ITable<TTable, TElement>: ITable<TElement>
    where TTable : class, ITable<TTable, TElement>
    where TElement : class
{
#if NET8_0_OR_GREATER
    static new abstract TTable CreateFromSortedData(TElement[] sortedData);

    static ITable<TElement> ITable<TElement>.CreateFromSortedData(TElement[] sortedData)
        => TTable.CreateFromSortedData(sortedData);

    static ITable ITable.CreateFromSortedData(object[] sortedData)
        => TTable.CreateFromSortedData((TElement[])sortedData);

    static new abstract TTable Create(TElement[] data);

    static ITable<TElement> ITable<TElement>.Create(TElement[] data)
        => TTable.Create(data);

    static ITable ITable.Create(object[] data)
        => TTable.Create((TElement[])data);
#endif
}

public interface ITable<TTable, TElement, TPrimaryKey>: ITable<TTable, TElement>, IKeyedTable<TElement, TPrimaryKey>
    where TTable : class, ITable<TTable, TElement, TPrimaryKey>
    where TElement : class
    where TPrimaryKey : notnull
{
#if NET8_0_OR_GREATER
    static object ITable.SelectPrimaryKey(object item) => TTable.SelectPrimaryKey((TElement)item);

    static TTable ITable<TTable, TElement>.Create(TElement[] data)
    {
        var keys = new TPrimaryKey[data.Length];
        for (var i = 0; i < data.Length; ++i) {
            keys[i] = TTable.SelectPrimaryKey(data[i]);
        }
        Array.Sort(keys, data, 0, data.Length, Comparer<TPrimaryKey>.Default);
        return TTable.CreateFromSortedData(data);
    }
#endif
}
