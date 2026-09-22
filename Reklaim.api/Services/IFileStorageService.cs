namespace Reklaim.api.Services;

public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file and returns the relative URL to access it.
    /// </summary>
    Task<string> UploadFileAsync(IFormFile file);

    /// <summary>
    /// Deletes a file by its relative URL.
    /// </summary>
    Task DeleteFileAsync(string fileUrl);
}
