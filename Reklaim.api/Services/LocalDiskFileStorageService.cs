namespace Reklaim.api.Services;

public class LocalDiskFileStorageService : IFileStorageService
{
    private readonly string _uploadsFolder;

    internal static string GetUploadsFolder(IWebHostEnvironment env) =>
        Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png"
    };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public LocalDiskFileStorageService(IWebHostEnvironment env)
    {
        _uploadsFolder = GetUploadsFolder(env);
        Directory.CreateDirectory(_uploadsFolder);
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file provided.");

        // Validate file size
        if (file.Length > MaxFileSizeBytes)
            throw new ArgumentException("File size exceeds the maximum limit of 5MB.");

        // Validate extension
        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException($"File type '{extension}' is not allowed. Only .jpg, .jpeg, and .png are accepted.");

        // Generate a unique filename to avoid conflicts
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(_uploadsFolder, uniqueFileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        // Return a relative URL the frontend can use
        return $"/uploads/{uniqueFileName}";
    }

    public Task DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return Task.CompletedTask;

        const string prefix = "/uploads/";
        if (!fileUrl.StartsWith(prefix, StringComparison.Ordinal))
            throw new ArgumentException("Only local upload URLs can be deleted.", nameof(fileUrl));

        var fileName = fileUrl[prefix.Length..];
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains('/') || fileName.Contains('\\')
            || fileName is "." or "..")
            throw new ArgumentException("Invalid upload filename.", nameof(fileUrl));

        var fullPath = Path.Combine(_uploadsFolder, fileName);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
