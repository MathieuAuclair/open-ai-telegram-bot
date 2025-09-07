using System.Text;
using System.Text.Json;

public class GitLabHelper : IGitHelper
{
    private readonly HttpClient _http;
    private readonly string _projectId;

    public GitLabHelper(string baseUrl, string projectId, string token)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _http.DefaultRequestHeaders.Add("PRIVATE-TOKEN", token);
        _projectId = projectId;
    }

    public async Task<string> CommitFileAsync(string branch, string filePath, string content)
    {
        var payload = new
        {
            branch,
            commit_message = $"Add {filePath}",
            actions = new[]
            {
                new {
                    action = "create",
                    file_path = filePath,
                    encoding = "base64",
                    content
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var response = await _http.PostAsync(
            $"/api/v4/projects/{_projectId}/repository/commits",
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine(body); // GitLab tells you *why* it rejected
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
