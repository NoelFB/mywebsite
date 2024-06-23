using System.Text.Json;
using System.Text.RegularExpressions;

partial class Entry
{
	public readonly string Name = "";
	public readonly Dictionary<string, string> Variables = [];
	public readonly string SourcePath;
	public readonly bool Valid = false;
	
	public Entry(string path)
	{
		SourcePath = path;

		var index_path = Path.Combine(SourcePath, "index.json");
		if (!File.Exists(index_path))
		{
			index_path = Path.Combine(SourcePath, "index.md");
			if (!File.Exists(index_path))
				return;
		}

		var contents = File.ReadAllText(index_path).Split("---");
		var json = contents[0];
		var desc = (contents.Length > 1 ? contents[1] : "");
		var document = JsonDocument.Parse(json, new JsonDocumentOptions() { AllowTrailingCommas = true }).RootElement;
		if (document.ValueKind != JsonValueKind.Object)
			return;

		// load all values
		foreach (var entry in document.EnumerateObject())
		{
			var key = entry.Name.ToLower();
			if (key == "links" && entry.Value.ValueKind == JsonValueKind.Array)
			{
				string links = "";

				foreach (var link_entry in entry.Value.EnumerateArray())
				{
					if (link_entry.ValueKind != JsonValueKind.Object)
						continue;

					string link_url = "";
					string link_label = "";

					foreach (var pair in link_entry.EnumerateObject())
					{
						if (pair.Name == "url") link_url = pair.Value.GetString() ?? "";
						if (pair.Name == "label") link_label = pair.Value.GetString() ?? "";
					}

					links += $"<a href=\"{link_url}\">{link_label}</a><br />\n";
				}

				Variables.Add("links", links);
			}
			else if (entry.Value.ValueKind == JsonValueKind.String)
			{
				Variables.Add(entry.Name, entry.Value.GetString() ?? "");
			}
			else if (entry.Value.ValueKind == JsonValueKind.Number)
			{
				Variables.Add(entry.Name, entry.Value.GetDouble().ToString());
			}
			else if (entry.Value.ValueKind == JsonValueKind.True)
			{
				Variables.Add(entry.Name, "true");
			}
			else if (entry.Value.ValueKind == JsonValueKind.False)
			{
				Variables.Add(entry.Name, "false");
			}
		}

		// add page title
		if (Variables.TryGetValue("title", out string? title))
			Variables.Add("page_title", " :: " + (title ?? ""));

		// add body, fall back to description
		if (desc.Length <= 0 && Variables.TryGetValue("description", out string? description) && description != null)
			Variables.Add("body", description);
		else
			Variables.Add("body", desc);

		if (Variables.TryGetValue("body", out string? value))
			Variables["body"] = Markdown.Parse(value);

		// preview / postcard
		if (File.Exists(Path.Combine(SourcePath, "preview.png")))
			Variables.Add("preview", "preview.png");
		if (File.Exists(Path.Combine(SourcePath, "postcard.png")))
			Variables.Add("postcard", "postcard.png");

		// adjust name to skip past numerics of name (000_NAME becomes NAME)
		Name = Path.GetFileNameWithoutExtension(path);
		while (Name.Length > 0 && ((Name[0] >= '0' && Name[0] <= '9') || Name[0] == '_'))
			Name = Name[1..];

		// we good here
		Valid = true;
	}

	public string Generate(string template)
	{
		string result = template;
		foreach (var variable in Variables)
			result = result.Replace($"{{{{{variable.Key}}}}}", variable.Value);

		result = TemplateRegex().Replace(result, "");

		return result;
	}

	public void CopyFiles(string destination)
	{
		foreach (var file in Directory.EnumerateFiles(SourcePath))
			if (!file.EndsWith(".json"))
				File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
	}

    [GeneratedRegex(@"\{\{.*?\}\}")]
    private static partial Regex TemplateRegex();
}

class Program
{
	static void Main(string[] args)
	{
		var rel = "/";

		// delete public dir
		if (Directory.Exists("public"))
			Directory.Delete("public", true);
		Directory.CreateDirectory("public");

		// load partials
		var partials = new Dictionary<string, string>();
		foreach (var file in Directory.EnumerateFiles("source/partials"))
			partials.Add(Path.GetFileNameWithoutExtension(file).ToLower(), File.ReadAllText(file));

		// create content types (games, posts)
		var entries = new Dictionary<string, List<Entry>>();
		{
			var template = LoadTemplate("source/post.html", partials);
			var contentTypes = new string[] { "games", "posts" };
			
			foreach (var type in contentTypes)
			{
				var inputPath = Path.Combine($"source/{type}");
				var outputPath = Path.Combine($"public/{type}");
				
				// clear existing
				if (Directory.Exists(outputPath))
					Directory.Delete(outputPath, true);

				// load entries
				entries.Add(type, []);
				foreach (var file in Directory.EnumerateDirectories(inputPath))
					entries[type].Add(new Entry(file));
				entries[type].Reverse();

				// generate entries
				Directory.CreateDirectory(outputPath);

				foreach (var entry in entries[type])
				{
					if (!entry.Valid)
						continue;

					entry.Variables["rel"] = rel;
					entry.Variables["path"] = $"{type}/{entry.Name}";
					entry.Variables["url"] = $"{entry.Variables["path"]}/index.html";
					
					Directory.CreateDirectory($"public/{entry.Variables["path"]}");
					File.WriteAllText($"public/{entry.Variables["url"]}", entry.Generate(template));
					entry.CopyFiles($"public/{entry.Variables["path"]}");
				}
			}
		}

		// construct index.html
		{
			var variables = new Dictionary<string, string>()
			{
				{ "rel", rel },
				{ "page_title", "" },
			};

			var result = LoadTemplate("source/index.html", partials, variables);
			foreach (var type in entries)
			{
				var list = "";

				foreach (var entry in type.Value)
				{
					if (entry.Variables.TryGetValue("visible", out var vis) && vis == "false")
						continue;

					// override specific variables
					foreach (KeyValuePair<string, string> v in variables)
						entry.Variables[v.Key] = v.Value;

					list += entry.Generate(partials[$"{type.Key}_entry"]) + "\n";
				}

				result = result.Replace($"{{{{list:{type.Key}}}}}", list);
			}
			File.WriteAllText("public/index.html", result);
		}

		// copy "content" files 1-1
		var contentSrcPath = Path.Combine(Directory.GetCurrentDirectory(), "source/content"); 
		foreach (var file in Directory.EnumerateFiles(contentSrcPath, "*.*", SearchOption.AllDirectories))
		{
			var fileDst = Path.Combine("public", Path.GetRelativePath(contentSrcPath, file));
			var fileSubDir = Path.GetDirectoryName(fileDst);
			if (!string.IsNullOrWhiteSpace(fileSubDir) && !Directory.Exists(fileSubDir))
				Directory.CreateDirectory(fileSubDir); 
			File.Copy(file, fileDst);
		}
	}

	static string LoadTemplate(string file, Dictionary<string, string> partials, Dictionary<string, string>? variables = null)
	{
		var result = File.ReadAllText(file);

		foreach (var partial in partials)
			result = result.Replace($"{{{{partial:{partial.Key}}}}}", partial.Value);

		if (variables != null)
		{
			foreach (var pair in variables)
				result = result.Replace($"{{{{{pair.Key}}}}}", pair.Value);
		}

		return result;
	}
}

partial class Markdown
{
	public static string Parse(string text)
	{
		// hacky mardown parser
		
		text = text.Replace("\r", "");
		text = H3().Replace(text, "<h3>$1</h3>");
		text = H2().Replace(text, "<h2>$1</h2>");
		text = H1().Replace(text, "<h1>$1</h1>");

		text = text.Replace("\n<h", "<h");
		text = text.Replace("\n<ul>", "<ul>");
		text = text.Replace("</ul>\n", "</ul>");
		text = text.Replace("\n<li>", "<li>");
		text = text.Replace("</li>\n", "</li>");
		text = text.Replace("</h1>\n", "</h1>");
		text = text.Replace("</h2>\n", "</h2>");
		text = text.Replace("</h3>\n", "</h3>");
		text = text.Replace("\n\n", "<br /><br />");

		text = Bold().Replace(text, "<b>$1</b>");
		text = Italic().Replace(text, "<i>$1</i>");
		text = Img().Replace(text, "<img alt='$1' src='$2' />");
		text = Link().Replace(text, "<a href='$2'>$1</a>");

		return text;
	}

    [GeneratedRegex(@"\n\s?### (.*)\n")] private static partial Regex H3();
    [GeneratedRegex(@"\n\s?## (.*)\n")] private static partial Regex H2();
    [GeneratedRegex(@"\n\s?# (.*)\n")] private static partial Regex H1();
    [GeneratedRegex(@"\*\*([^\*]*)\*\*")] private static partial Regex Bold();
    [GeneratedRegex(@"\*([^\*]*)\*")] private static partial Regex Italic();
    [GeneratedRegex(@"!\[(.*?)\]\((.*?)\)")] private static partial Regex Img();
    [GeneratedRegex(@"\[(.*?)\]\((.*?)\)")] private static partial Regex Link();
}