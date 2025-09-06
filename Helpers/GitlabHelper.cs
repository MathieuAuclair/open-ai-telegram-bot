using System.Text;
using System.Text.Json;

public class GitLabHelper
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

    public async Task<bool> WaitForPipelineAsync(string branch)
    {
        while (true)
        {
            // Get latest pipeline
            var resp = await _http.GetAsync($"/api/v4/projects/{_projectId}/pipelines?ref={branch}");
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var pipelineId = doc.RootElement[0].GetProperty("id").GetInt32();

            // Get pipeline status
            var statusResp = await _http.GetAsync($"/api/v4/projects/{_projectId}/pipelines/{pipelineId}");
            statusResp.EnsureSuccessStatusCode();
            var statusJson = await statusResp.Content.ReadAsStringAsync();

            using var statusDoc = JsonDocument.Parse(statusJson);
            var status = statusDoc.RootElement.GetProperty("status").GetString();

            Console.WriteLine($"Pipeline {pipelineId} status: {status}");

            if (status is "success")
            {
                return true;
            }
            else if (status is "failed" or "canceled")
            {
                return false;
            }

            await Task.Delay(5000);
        }
    }
}
