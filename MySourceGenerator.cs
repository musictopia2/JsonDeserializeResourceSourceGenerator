namespace JsonDeserializeResourceSourceGenerator;
[Generator]
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
        
        BasicList<string> classes = new();
        files.ForEach(file =>
        {
            string className = file.FileName.Replace(" ", ""); //because class names cannot have spaces.  i like the idea of just ignoring them (well see how that goes when i test).
            classes.Add(className);
            string fins = $"{className}.g.cs";
            string data = file.Content.Replace("\"", "\"\""); //since its not bad, use the standard string and not custom is fine this time.
            string source = @$"using zz1 = CommonBasicLibraries.AdvancedGeneralFunctionsAndProcesses.JsonSerializers.SystemTextJsonStrings;
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
        T output = zz1.DeserializeObject<T>(data);
        return output;
    }}
    public static async Task<T> GetResourceAsync<T>()
    {{
        string data = GetData();
        T output = await zz1.DeserializeObjectAsync<T>(data);
        return output;
    }}
}}";
            context.AddSource(fins, source);
        });
        if (classes.Count >= 5)
        {
            GenerateSummary(context, classes, name); //eventually may find another way to determine if a summary is necessary.
        }
    }
    private void GenerateSummary(SourceProductionContext context, BasicList<string> list, string ns)
    {
        SourceCodeStringBuilder builder = new();
        builder.WriteLine(w =>
        {
            w.Write("namespace ")
            .Write(ns)
            .Write(";");
        })
        .WriteLine("public static class SummaryClass")
        .WriteCodeBlock(w =>
        {
            w.WriteLine("public static T GetResource<T>(string name)")
            .WriteCodeBlock(w =>
            {
                GenerateRegularMethods(w, list, ns);
            })
            .WriteLine("public static async Task<T> GetResourceAsync<T>(string name)")
            .WriteCodeBlock(w =>
            {
                GenerateAsyncMethods(w, list, ns);
            });
        });
        context.AddSource("Summary.g", builder.ToString());
    }
    private void GenerateAsyncMethods(ICodeBlock w, BasicList<string> list, string ns)
    {
        foreach (var item in list)
        {
            w.WriteLine(w =>
            {
                w.Write("if (name == ")
                .AppendDoubleQuote(w =>
                {
                    w.Write(item);
                })
                .Write(")");
            })
            .WriteCodeBlock(w =>
            {
                w.WriteLine(w =>
                {
                    w.Write("return await global::")
                    .Write(ns)
                    .Write(".")
                    .Write(item)
                    .Write(".GetResourceAsync<T>();");
                });
            });
        }
        WriteException(w);
    }
    private void WriteException(ICodeBlock w)
    {
        w.WriteLine(w =>
        {
            w.CustomExceptionLine(w =>
            {
                w.Write("Nothing found with name {name}");
            });
        });
    }
    private void GenerateRegularMethods(ICodeBlock w, BasicList<string> list, string ns)
    {
        foreach (var item in list)
        {
            w.WriteLine(w =>
            {
                w.Write("if (name == ")
                .AppendDoubleQuote(w =>
                {
                    w.Write(item);
                })
                .Write(")");
            })
            .WriteCodeBlock(w =>
            {
                w.WriteLine(w =>
                {
                    w.Write("return global::")
                    .Write(ns)
                    .Write(".")
                    .Write(item)
                    .Write(".GetResource<T>();");
                });
            });
        }
        WriteException(w);
    }
}