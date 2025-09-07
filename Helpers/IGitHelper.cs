public interface IGitHelper
{
    public Task<string> CommitFileAsync(string branch, string filePath, string content);
}