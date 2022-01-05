namespace JsonDeserializeResourceSourceGenerator;
public class MySourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var firsts = context.AdditionalTextsProvider
           .Where(xx => Path.GetExtension(xx.Path) == ".json");
        var nexts = firsts.Collect();
        var finals = context.CompilationProvider.Combine(nexts);
        context.RegisterSourceOutput(finals, (spc, source) =>
        {
            Execute(source.Left, spc, source.Right);
        });
    }
    private BasicList<FileInformation> GetFiles(ImmutableArray<AdditionalText> files)
    {
        BasicList<FileInformation> output = new();
        foreach (AdditionalText file in files)
        {
            string name = Path.GetFileNameWithoutExtension(file.Path);
            string details = file.GetText()!.ToString();
            output.Add(new FileInformation(name, details));
        }
        return output;
    }
    private void Execute(Compilation compilation, SourceProductionContext context, ImmutableArray<AdditionalText> list)
    {
        var files = GetFiles(list);
        var name = $"{compilation.AssemblyName}.Resources";
        files.ForEach(file =>
        {
            string className = file.FileName;
            string fins = $"{className}.g.cs";
            string data = file.Content.Replace("\"", "\"\""); //since its not bad, use the standard string and not custom is fine this time.
            string source = @$"using zz = CommonBasicLibraries.AdvancedGeneralFunctionsAndProcesses.JsonSerializers.SystemTextJsonStrings;
namespace {name};
public static class {className}
{{
    private static string GetData()
    {{
        string data = @""{ data}"";
        return data;
    }}
    public static T GetResource<T>()
    {{
        string data = GetData();
        T output = zz.DeserializeObject<T>(data);
        return output;
    }}
    public static async Task<T> GetResourceAsync<T>()
    {{
        string data = GetData();
        T output = await zz.DeserializeObjectAsync<T>(data);
        return output;
    }}
}}";
            context.AddSource(fins, source);
        });
    }
}