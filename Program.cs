using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

class Entry
{
	public String name = "";
	public Dictionary<String, String> variables = new Dictionary<string, string>();
	public String source_path;
	public bool valid = false;
	
	public Entry(String path)
	{
		source_path = path;

		var index_path = Path.Combine(source_path, "index.json");
		if (!File.Exists(index_path))
		{
			index_path = Path.Combine(source_path, "index.md");
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
				String links = "";

				foreach (var link_entry in entry.Value.EnumerateArray())
				{
					if (link_entry.ValueKind != JsonValueKind.Object)
						continue;

					String link_url = "";
					String link_label = "";

					foreach (var pair in link_entry.EnumerateObject())
					{
						if (pair.Name == "url") link_url = pair.Value.GetString() ?? "";
						if (pair.Name == "label") link_label = pair.Value.GetString() ?? "";
					}

					links += $"<a href=\"{link_url}\">{link_label}</a><br />\n";
				}

				variables.Add("links", links);
			}
			else if (entry.Value.ValueKind == JsonValueKind.String)
			{
				variables.Add(entry.Name, entry.Value.GetString() ?? "");
			}
		}

		// add page title
		if (variables.TryGetValue("title", out string? title))
			variables.Add("page_title", " :: " + (title ?? ""));

		// add body, fall back to description
		if (desc.Length <= 0 && variables.TryGetValue("description", out string? description) && description != null)
			variables.Add("body", description);
		else
			variables.Add("body", desc);

		if (variables.ContainsKey("body"))
			variables["body"] = Markdown.Parse(variables["body"]);

		// preview / postcard
		if (File.Exists(Path.Combine(source_path, "preview.png")))
			variables.Add("preview", "preview.png");
		if (File.Exists(Path.Combine(source_path, "postcard.png")))
			variables.Add("postcard", "postcard.png");

		// adjust name to skip past numerics of name (000_NAME becomes NAME)
		name = Path.GetFileNameWithoutExtension(path);
		while (name.Length > 0 && ((name[0] >= '0' && name[0] <= '9') || name[0] == '_'))
			name = name.Substring(1);

		// we good here
		valid = true;
	}

	public String Generate(String template)
	{
		String result = template;
		foreach (var variable in variables)
			result = result.Replace($"{{{{{variable.Key}}}}}", variable.Value);

		result = Regex.Replace(result, @"\{\{.*?\}\}", "");

		return result;
	}

	public void CopyFiles(String destination)
	{
		foreach (var file in Directory.EnumerateFiles(source_path))
			if (!file.EndsWith(".json"))
				File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
	}
}

class Program
{
	static void Main(string[] args)
	{
		String rel = "/";
		List<String> content_types = new List<String> { "games", "posts" };
		Dictionary<String, List<Entry>> entries = new Dictionary<string, List<Entry>>();
		Dictionary<String, String> partials = new Dictionary<string, string>();

		// delete public dir
		if (Directory.Exists("public"))
			Directory.Delete("public", true);
		Directory.CreateDirectory("public");

		// load partials
		foreach (var file in Directory.EnumerateFiles("source/partials"))
			partials.Add(Path.GetFileNameWithoutExtension(file).ToLower(), File.ReadAllText(file));

		// load template file for games/posts
		var template = LoadTemplate("source/post.html", partials);

		// construct all content types
		foreach (var type in content_types)
		{
			var input_path = Path.Combine($"source/{type}");
			var output_path = Path.Combine($"public/{type}");
			
			// clear existing
			if (Directory.Exists(output_path))
				Directory.Delete(output_path, true);

			// load entries
			entries.Add(type, new List<Entry>());
			foreach (var file in Directory.EnumerateDirectories(input_path))
				entries[type].Add(new Entry(file));
			entries[type].Reverse();

			// generate entries
			Directory.CreateDirectory(output_path);

			foreach (var entry in entries[type])
			{
				if (!entry.valid)
					continue;

				entry.variables["rel"] = rel;
				entry.variables["path"] = $"{type}/{entry.name}";
				entry.variables["url"] = $"{entry.variables["path"]}/index.html";
				
				Directory.CreateDirectory($"public/{entry.variables["path"]}");
				File.WriteAllText($"public/{entry.variables["url"]}", entry.Generate(template));
				entry.CopyFiles($"public/{entry.variables["path"]}");
			}
		}

		// construct index.html
		{
			Dictionary<String, String> variables = new () {
				{ "rel", rel },
				{ "page_title", "" },
			};

			String result = LoadTemplate("source/index.html", partials, variables);
			foreach (var type in entries)
			{
				String list = "";

				foreach (var entry in type.Value)
				{
					// override specific variables
					foreach (KeyValuePair<String, String> v in variables)
						entry.variables[v.Key] = v.Value;

					list += entry.Generate(partials[$"{type.Key}_entry"]) + "\n";
				}

				result = result.Replace($"{{{{list:{type.Key}}}}}", list);
			}
			File.WriteAllText("public/index.html", result);
		}

		// copy "content" files 1-1
		var content_src_path = Path.Combine(Directory.GetCurrentDirectory(), "source/content"); 
		foreach (var file in Directory.EnumerateFiles(content_src_path, "*.*", SearchOption.AllDirectories))
		{
			var file_dest = Path.Combine("public", Path.GetRelativePath(content_src_path, file));
			var file_subdir = Path.GetDirectoryName(file_dest);
			if (!String.IsNullOrWhiteSpace(file_subdir) && !Directory.Exists(file_subdir))
				Directory.CreateDirectory(file_subdir); 
			File.Copy(file, file_dest);
		}
	}

	static String LoadTemplate(String file, Dictionary<String, String> partials, Dictionary<String, String>? variables = null)
	{
		String result = File.ReadAllText(file);

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

class Markdown
{
	public static string Parse(string text)
	{
		// hacky mardown parser
		
		text = text.Replace("\r", "");
		text = Regex.Replace(text, @"\n\s?### (.*)\n", "<h3>$1</h3>");
		text = Regex.Replace(text, @"\n\s?## (.*)\n", "<h2>$1</h2>");
		text = Regex.Replace(text, @"\n\s?# (.*)\n", "<h1>$1</h1>");

		text = text.Replace("\n<h", "<h");
		text = text.Replace("\n<ul>", "<ul>");
		text = text.Replace("</ul>\n", "</ul>");
		text = text.Replace("\n<li>", "<li>");
		text = text.Replace("</li>\n", "</li>");
		text = text.Replace("</h1>\n", "</h1>");
		text = text.Replace("</h2>\n", "</h2>");
		text = text.Replace("</h3>\n", "</h3>");
		text = text.Replace("\n\n", "<br /><br />");

		text = Regex.Replace(text, @"\*\*([^\*]*)\*\*", "<b>$1</b>");
		text = Regex.Replace(text, @"\*([^\*]*)\*", "<i>$1</i>");
		text = Regex.Replace(text, @"!\[(.*?)\]\((.*?)\)", "<img alt='$1' src='$2' />");
		text = Regex.Replace(text, @"\[(.*?)\]\((.*?)\)", "<a href='$2'>$1</a>");

		return text;
	}
}