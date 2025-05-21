using System.Text;
using System.Text.Json;
using Markdig;

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
				var links = new StringBuilder();

				foreach (var linkEntry in entry.Value.EnumerateArray())
				{
					if (linkEntry.ValueKind != JsonValueKind.Object)
						continue;

					var linkUrl = "";
					var linkLabel = "";

					foreach (var pair in linkEntry.EnumerateObject())
					{
						if (pair.Name == "url")
							linkUrl = pair.Value.GetString() ?? "";
						if (pair.Name == "label")
							linkLabel = pair.Value.GetString() ?? "";
					}

					links.AppendLine($"<a href=\"{linkUrl}\">{linkLabel}</a><br />");
				}

				Variables.Add("links", links.ToString());
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
		if (desc.Length <= 0 && Variables.TryGetValue("description", out var description) &&
			description != null)
			Variables.Add("body", description);
		else
			Variables.Add("body", desc);

		// parse body as markdown
		if (Variables.TryGetValue("body", out var value))
			Variables["body"] = value;

		// preview image
		if (File.Exists(Path.Combine(SourcePath, "preview.png")))
			Variables.Add("preview", "preview.png");

		// postcard image
		if (File.Exists(Path.Combine(SourcePath, "postcard.png")))
		{
			Variables.Add("postcard", "postcard.png");
			Variables.TryAdd("postcard_visible", "true");
		}
		else
			Variables.Add("postcard_visible", "false");

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
			if (!src[i..].StartsWith("{{"))
			{
				result.Append(src[i]);
				continue;
			}

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
				if (!variables.TryGetValue(condition, out var value) ||
					string.IsNullOrEmpty(value) || value == "false")
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
				result.Append(Markdown.ToHtml(Generate(embedding, variables)));
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

		return result.ToString();
	}
}

class Program
{
	const string Rel = "/";
	const string Site = "https://noelberry.ca";
	const string PublishPath = "public";

	static void Main(string[] args)
	{
		var root = Directory.GetCurrentDirectory();
		while(!File.Exists(Path.Combine(root, "noelfb2022.csproj")))
			root = Path.Combine(root, "..");
		Directory.SetCurrentDirectory(root);

		// delete public dir
		if (Directory.Exists(PublishPath))
			Directory.Delete(PublishPath, true);
		Directory.CreateDirectory(PublishPath);

		var generator = new Generator("source", ["games", "posts"], Rel, Site);

		// templates
		var indexTemplate = File.ReadAllText(Path.Combine("source", "index.html"));
		var postTemplate = File.ReadAllText(Path.Combine("source", "post.html"));

		// construct index.html
		{
			var variables = new Dictionary<string, string>()
			{
				{ "rel", Rel },
				{ "url", Rel },
				{ "site_url", $"{Site}{Rel}" },
				{ "postcard", "img/profile.jpg" }
			};

			var result = generator.Generate(indexTemplate, variables);
			File.WriteAllText(Path.Combine(PublishPath, "index.html"), result);
		}

		// construct game/post entries
		foreach (var (type, entries) in generator.Entries)
		{
			var outputPath = Path.Combine(PublishPath, type);
			if (Directory.Exists(outputPath))
				Directory.Delete(outputPath, true);
			Directory.CreateDirectory(outputPath);

			foreach (var entry in entries)
			{
				if (!entry.Valid)
					continue;

				Directory.CreateDirectory(Path.Combine(PublishPath, entry.DestPath));
				File.WriteAllText(
					Path.Combine(PublishPath, entry.DestPath, "index.html"),
					generator.Generate(postTemplate, entry.Variables));
				entry.CopyFiles(Path.Combine(PublishPath, entry.DestPath));
			}
		}

		// copy "content" files 1-1
		var contentSrcPath = Path.Combine(Directory.GetCurrentDirectory(), "source", "content"); 
		foreach (var file in Directory.EnumerateFiles(contentSrcPath, "*.*", SearchOption.AllDirectories))
		{
			var fileDst = Path.Combine(PublishPath, Path.GetRelativePath(contentSrcPath, file));
			var fileSubDir = Path.GetDirectoryName(fileDst);
			if (!string.IsNullOrWhiteSpace(fileSubDir) && !Directory.Exists(fileSubDir))
				Directory.CreateDirectory(fileSubDir); 
			File.Copy(file, fileDst);
		}

		// create RSS feed
		{
			var rss = new StringBuilder();
			rss.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
			rss.AppendLine("<rss version=\"2.0\">");
			rss.AppendLine("\t<channel>");
			rss.AppendLine("\t\t<title>Noel Berry</title>");
			rss.AppendLine("\t\t<link>https://noelberry.ca</link>");
			rss.AppendLine("\t\t<language>en-us</language>");
			rss.AppendLine("\t\t<description>Sometimes, when I'm in the right mood, I make video games and art. Or something.</description>");
			foreach (var post in generator.Entries["posts"].Reverse<Entry>())
			{
				if (!post.Valid || 
					post.Variables.GetValueOrDefault("visible") == "false")
					continue;

				var date = DateTime.Parse(post.Variables["date"]);
				var rssDate = date.ToString("ddd, dd MMM yyyy HH:mm:ss zzz");

				rss.AppendLine("\t\t<item>");
				rss.AppendLine($"\t\t\t<title>{post.Variables["title"]}</title>");
				rss.AppendLine($"\t\t\t<link>{Site}/{post.DestPath}/index.html</link>");
				rss.AppendLine($"\t\t\t<description>{post.Variables.GetValueOrDefault("description")}</description>");
				rss.AppendLine($"\t\t\t<pubDate>{rssDate}</pubDate>");
				rss.AppendLine("\t\t</item>");
			}
			rss.AppendLine("\t</channel>");
			rss.AppendLine("</rss>");
			File.WriteAllText(Path.Combine(PublishPath, "rss.xml"), rss.ToString());
		}
	}
}