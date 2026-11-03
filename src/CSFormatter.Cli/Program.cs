using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSFormatter.Core;

// Human mode: csformatter --file <path>  (formats in place with default options)
if (args.Length == 2 && args[0] == "--file")
{
    var path = args[1];
    var original = File.ReadAllText(path);
    var formatted = CodeFormatter.Format(original);
    if (!string.Equals(original, formatted, StringComparison.Ordinal))
    {
        File.WriteAllText(path, formatted, new UTF8Encoding(false));
    }

    return 0;
}

// Editor mode: {"text": "...", "options": {...}} on stdin, {"text": "..."} or {"error": "..."} on stdout.
var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };

try
{
    string input;
    using (var stdin = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false)))
    {
        input = stdin.ReadToEnd();
    }

    var request = JsonSerializer.Deserialize<Request>(input, Json.Options)
                  ?? throw new InvalidOperationException("Empty request.");

    var result = CodeFormatter.Format(request.Text ?? string.Empty, request.Options ?? new FormatOptions());

    stdout.Write(JsonSerializer.Serialize(new Response { Text = result }, Json.Options));
    return 0;
}
catch (Exception ex)
{
    stdout.Write(JsonSerializer.Serialize(new Response { Error = ex.Message }, Json.Options));
    return 1;
}

internal sealed class Request
{
    public string? Text { get; set; }

    public FormatOptions? Options { get; set; }
}

internal sealed class Response
{
    public string? Text { get; set; }

    public string? Error { get; set; }
}

internal static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
