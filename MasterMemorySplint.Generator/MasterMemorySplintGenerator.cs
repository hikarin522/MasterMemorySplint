using Microsoft.CodeAnalysis;

namespace MasterMemorySplint.Generator;

[Generator(LanguageNames.CSharp)]
public class MasterMemorySplintGenerator: IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var tableProvider = context.SyntaxProvider.CreateTableProvider();
        var memoryDatabaseProvider = context.SyntaxProvider.CreateMemoryDatabaseProvider();

        context.RegisterTableGenerator(tableProvider);
        context.RegisterMemoryDatabaseExtensionsGenerator(memoryDatabaseProvider);
    }
}
