using System.Text.RegularExpressions;

namespace simple_crawler;

public partial class Crawler
{
    protected string? basedFolder = null;
    protected int maxLinksPerPage = 3;

    public void SetBasedFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder))
        {
            throw new ArgumentNullException(nameof(folder));
        }
        basedFolder = folder;
    }

    public void SetMaxLinksPerPage(int max)
    {
        maxLinksPerPage = max;
    }

    public async Task GetPage(string url, int level)
    {
        if (basedFolder == null)
        {
            throw new Exception("Please set the value of base folder using SetBasedFolder method first.");
        }
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentNullException(nameof(url));
        }

        // stop recursion
        if (level <= 0)
        {
            return;
        }

        HttpClient client = new();

        try
        {
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();

                string fileName = url.Replace(":", "_")
                                     .Replace("/", "_")
                                     .Replace(".", "_") + ".html";

                File.WriteAllText(basedFolder + "/" + fileName, responseBody);

                ISet<string> links = GetLinksFromPage(responseBody);
                int count = 0;

                foreach (string link in links)
                {
                    if (link.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                    {
                        await GetPage(link, level - 1);

                        if (++count >= maxLinksPerPage)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Can't load content with return status {0}", response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine("\nException caught:");
            Console.WriteLine("Message :{0}", ex.Message);
        }
    }

    [GeneratedRegex("(?<=<a\\s*?href=(?:'|\"))[^'\"]*?(?=(?:'|\"))")]
    private static partial Regex MyRegex();

    public static ISet<string> GetLinksFromPage(string content)
    {
        Regex regexLink = MyRegex();
        HashSet<string> newLinks = [];

        foreach (var match in regexLink.Matches(content))
        {
            string? link = match.ToString();
            if (!string.IsNullOrEmpty(link))
            {
                newLinks.Add(link);
            }
        }
        return newLinks;
    }
}

class Program
{
    static void Main(string[] args)
    {
        // Improved: avoid hard-coded values
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: dotnet run <url> <level> <maxLinksPerPage>");
            return;
        }

        Crawler cw = new();
        cw.SetBasedFolder(".");
        cw.SetMaxLinksPerPage(int.Parse(args[2]));
        cw.GetPage(args[0], int.Parse(args[1])).Wait();
    }
}
