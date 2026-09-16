using System.Linq;
using AbrCivil.Modules.Core;
using Xunit;

public class CatalogParserTests
{
    private const string Json = @"{
      ""modules"": [
        { ""name"": ""abr-civil-modules"", ""title"": ""Библиотека модулей"", ""version"": ""1.0.0"",
          ""description"": ""Каталог и установка"", ""bundle_url"": ""https://example/a.zip"",
          ""bundle_sha256"": ""AABB"", ""help_url"": """", ""compatibility"": [""2024""] },
        { ""name"": ""abr-hello"", ""title"": ""Проверка связи"", ""version"": ""1.2.3"",
          ""bundle_url"": ""https://example/h.zip"", ""compatibility"": [""2024"", ""2026""] }
      ]}";

    [Fact]
    public void Parse_reads_all_entries()
    {
        var list = CatalogParser.Parse(Json);

        Assert.Equal(2, list.Count);
        Assert.Equal("Библиотека модулей", list[0].Title);
        Assert.Equal("1.2.3", list[1].Version);
        Assert.Equal("https://example/h.zip", list[1].BundleUrl);
        Assert.Equal(new[] { "2024", "2026" }, list[1].Compatibility.ToArray());
    }

    [Fact]
    public void Parse_tolerates_missing_optional_fields()
    {
        var entry = CatalogParser.Parse(Json)[1];

        Assert.Equal("", entry.HelpUrl);
        Assert.Equal("", entry.Sha256);
    }

    [Fact]
    public void Parse_returns_empty_list_on_garbage()
    {
        Assert.Empty(CatalogParser.Parse("не json вовсе"));
    }
}
