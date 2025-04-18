﻿using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

partial class Entry
{
	public readonly string Name = "";
	public readonly string Type;
	public readonly Dictionary<string, string> Variables = [];
	public readonly string SourcePath;
	public readonly bool Valid = false;
	public readonly string DestPath = string.Empty;
	
	public Entry(string path, string rel, string site, string type)
	{
		SourcePath = path;
		Type = type;

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

		// add body, fall back to description
		if (desc.Length <= 0 && Variables.TryGetValue("description", out string? description) && description != null)
			Variables.Add("body", description);
		else
			Variables.Add("body", desc);

		// parse body as markdown
		if (Variables.TryGetValue("body", out string? value))
			Variables["body"] = Markdown.Parse(value);

		// preview image
		if (File.Exists(Path.Combine(SourcePath, "preview.png")))
			Variables.Add("preview", "preview.png");

		// postcard image
		if (File.Exists(Path.Combine(SourcePath, "postcard.png")))
			Variables.Add("postcard", "postcard.png");

		// adjust name to skip past numerics of name (000_NAME becomes NAME)
		Name = Path.GetFileNameWithoutExtension(path);
		while (Name.Length > 0 && ((Name[0] >= '0' && Name[0] <= '9') || Name[0] == '_'))
			Name = Name[1..];

		// add built in vars
		Variables["rel"] = rel;
		Variables["path"] = $"{rel}{type}/{Name}";
		Variables["url"] = $"{rel}{type}/{Name}";
		Variables["site_url"] = $"{site}{rel}{type}/{Name}";

		// we good here
		DestPath = $"{type}/{Name}";
		Valid = true;
	}

	public void CopyFiles(string destination)
	{
		foreach (var file in Directory.EnumerateFiles(SourcePath))
			if (!file.EndsWith(".json"))
				File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
	}
}

class Generator
{
	public readonly Dictionary<string, string> Partials = new(StringComparer.OrdinalIgnoreCase);
	public readonly Dictionary<string, List<Entry>> Entries = new(StringComparer.OrdinalIgnoreCase);

	public Generator(string path, string[] entryTypes, string rel, string site)
	{
		foreach (var file in Directory.EnumerateFiles(Path.Combine(path, "partials")))
		{
			Partials.Add(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file));
		}

		foreach (var type in entryTypes)
		{
			Entries[type] = [];
			foreach (var dir in Directory.EnumerateDirectories(Path.Combine(path, type)))
				Entries[type].Add(new Entry(dir, rel, site, type));
		}
	}

	public string Generate(string template, Dictionary<string, string> variables)
	{
		StringBuilder result = new();

		var src = template.AsSpan();
		for (int i = 0; i < src.Length; i ++)
		{
			if (src[i..].StartsWith("{{"))
			{
				int start = i + 2;
				int end = start;
				while (end < src.Length && !src[end..].StartsWith("}}"))
					end++;
				if (end >= src.Length)
				{
					i = end ;
					continue;
				}

				var cmd = src.Slice(start, end - start);

				// handle if statement
				if (cmd.StartsWith("if:"))
				{
					var condition = cmd[3..].ToString();
					if (!variables.TryGetValue(condition, out var value) || string.IsNullOrEmpty(value) || value == "false")
					{
						// skip to end
						var j = end;
						var d = 1;
						while (j < src.Length)
						{
							if (src[j..].StartsWith("{{if:"))
								d++;
							else if (src[j..].StartsWith("{{end"))
							{
								d--;
								if (d <= 0)
								{
									end = j + 5;
									break;
								}
							}
							j++;
						}
					}
				}
				// ignore end statement
				else if (cmd.StartsWith("end"))
				{
					// ...
				}
				// list entries
				else if (cmd.StartsWith("list:"))
				{
					var list = "";
					var kind = cmd[5..].ToString();
					var entries = Entries[kind];

					for (int j = entries.Count - 1; j >= 0; j --)
					{
						var entry = entries[j];
						if (entry.Variables.TryGetValue("visible", out var vis) && vis == "false")
							continue;

						// override specific variables
						var vars = new Dictionary<string, string>();
						foreach (var kv in variables)
							vars[kv.Key] = kv.Value;
						foreach (var kv in entry.Variables)
							vars[kv.Key] = kv.Value;
						list += Generate(Partials[$"{kind}_entry"], vars) + "\n";
					}

					result.Append(list);
				}
				// embed partial
				else if (cmd.StartsWith("partial:"))
				{
					result.Append(Generate(Partials[cmd[8..].ToString()], variables));
				}
				// embed variable as content (run generator on var)
				else if (cmd.StartsWith("embed:") && variables.TryGetValue(cmd[6..].ToString(), out var embedding))
				{
					result.Append(Generate(embedding, variables));
				}
				// embed variable as-is
				else if (variables.TryGetValue(cmd.ToString(), out var value))
				{
					result.Append(value);
				}
				// missing value
				else
				{
					result.Append("{{MISSING}}");
				}
				
				// skip to end of cmd
				i = end + 2 - 1;
			}
			else
			{
				result.Append(src[i]);
			}
		}

		return result.ToString();
	}
}

class Program
{
	static void Main(string[] args)
	{
		var rel = "/";
		var site = "https://noelberry.ca";

		var root = Directory.GetCurrentDirectory();
		while(!File.Exists(Path.Combine(root, "noelfb2022.csproj")))
			root = Path.Combine(root, "..");
		Directory.SetCurrentDirectory(root);

		// delete public dir
		if (Directory.Exists("public"))
			Directory.Delete("public", true);
		Directory.CreateDirectory("public");

		var generator = new Generator("source", ["games", "posts"], rel, site);

		// templates
		var indexTemplate = File.ReadAllText("source/index.html");
		var postTemplate = File.ReadAllText("source/post.html");

		// construct index.html
		{
			var variables = new Dictionary<string, string>()
			{
				{ "rel", rel },
				{ "url", rel },
				{ "site_url", $"{site}{rel}" },
				{ "postcard", "img/profile.jpg" }
			};

			var result = generator.Generate(indexTemplate, variables);
			File.WriteAllText("public/index.html", result);
		}

		// construct post entries
		foreach (var (type, entries) in generator.Entries)
		{
			var outputPath = Path.Combine($"public/{type}");
			if (Directory.Exists(outputPath))
				Directory.Delete(outputPath, true);
			Directory.CreateDirectory(outputPath);

			foreach (var entry in entries)
			{
				if (!entry.Valid)
					continue;

				Directory.CreateDirectory($"public/{entry.DestPath}");
				File.WriteAllText($"public/{entry.DestPath}/index.html", generator.Generate(postTemplate, entry.Variables));
				entry.CopyFiles($"public/{entry.DestPath}");
			}
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