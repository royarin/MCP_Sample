using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.ML.Tokenizers;

namespace OpsBot.Shared;

/// <summary>
/// How many tokens do the tool definitions cost on EVERY model call? Serialises each tool the way the
/// chat API sends it (name + description + JSON schema) and counts tokens with the model's tokenizer.
/// Demo 3's opening number: "11 tools · ~1.9k tokens of definitions per call".
/// </summary>
public static class ToolCatalogMeter
{
    private static Tokenizer? _tokenizer;

    private static Tokenizer GetTokenizer(string model)
    {
        if (_tokenizer is not null) return _tokenizer;
        try { _tokenizer = TiktokenTokenizer.CreateForModel(model); }
        catch { _tokenizer = TiktokenTokenizer.CreateForEncoding("o200k_base"); } // unknown deployment name → GPT-4o family
        return _tokenizer;
    }

    public static (int Tools, int Tokens) Measure(IEnumerable<AITool> tools, string model)
    {
        var tk = GetTokenizer(model);
        var sb = new StringBuilder();
        int n = 0;
        foreach (var f in tools.OfType<AIFunction>())
        {
            n++;
            sb.Append(f.Name).Append('\n').Append(f.Description).Append('\n').Append(f.JsonSchema.GetRawText()).Append('\n');
        }
        return (n, tk.CountTokens(sb.ToString()));
    }
}
