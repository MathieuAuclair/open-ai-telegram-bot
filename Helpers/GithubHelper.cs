
using System.Text;
using System.Text.Json;

public class GitHubHelper : IGitHelper
{
    private readonly HttpClient _http;
    private readonly string _owner;
    private readonly string _repo;

    public GitHubHelper(string baseUrl, string owner, string repo, string token)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        _http.DefaultRequestHeaders.Add("User-Agent", "GitHubHelper");
        _owner = owner;
        _repo = repo;
    }

    public async Task<string> CommitFileAsync(string branch, string filePath, string content)
    {
        var payload = new
        {
            message = $"Add {filePath}",
            content,
            branch
        };

        var response = await _http.PutAsync(
            $"repos/{_owner}/{_repo}/contents/{filePath}",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        );

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(body);
            response.EnsureSuccessStatusCode();
        }

        return body;
    }
}