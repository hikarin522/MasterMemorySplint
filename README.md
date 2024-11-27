# MasterMemorySplint
[![.NET](https://github.com/hikarin522/MasterMemorySplint/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hikarin522/MasterMemorySplint/actions/workflows/dotnet.yml)
[![GitHub License](https://img.shields.io/github/license/hikarin522/MasterMemorySplint)](./LICENSE)
[![NuGet Version](https://img.shields.io/nuget/vpre/MasterMemorySplint)](https://www.nuget.org/packages/MasterMemorySplint)

A source generator for adding useful features to [MasterMemory](https://github.com/Cysharp/MasterMemory).

---

## Features

### Extending the table class
https://github.com/Cysharp/MasterMemory/tree/2.4.4#extend-table

Implement `ITable<TElement>` for table classes to improve extensibility and compatibility with .NET 8.  
For more information on `ITable`, see below.  
[./MasterMemorySplint/ITable.cs](./MasterMemorySplint/ITable.cs)

### Speeding up validation
https://github.com/Cysharp/MasterMemory/tree/2.4.4#validator

Speeding up validation by generating a parallel version of MemoryDatabase.Validate.  
See below for usage examples.
```cs
memoryDataBase.Validate(Environment.ProcessorCount, cancellationToken);
```
