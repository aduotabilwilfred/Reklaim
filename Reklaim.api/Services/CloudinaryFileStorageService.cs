using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Reklaim.api.Services;

public class CloudinaryFileStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png"
    };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public CloudinaryFileStorageService(IConfiguration configuration)
    {
        var cloudinaryUrl = configuration["CLOUDINARY_URL"] ?? configuration["Cloudinary:Url"];

        if (!string.IsNullOrWhiteSpace(cloudinaryUrl))
        {
            _cloudinary = new Cloudinary(cloudinaryUrl);
        }
        else
        {
            var cloudName = configuration["Cloudinary:CloudName"] ?? configuration["CLOUDINARY_CLOUD_NAME"];
            var apiKey = configuration["Cloudinary:ApiKey"] ?? configuration["CLOUDINARY_API_KEY"];
            var apiSecret = configuration["Cloudinary:ApiSecret"] ?? configuration["CLOUDINARY_API_SECRET"];

            if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            {
                throw new InvalidOperationException("Cloudinary configuration is missing. Please provide CLOUDINARY_URL or Cloudinary:CloudName, Cloudinary:ApiKey, and Cloudinary:ApiSecret.");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        _cloudinary.Api.Secure = true;
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

        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "reklaim/uploads"
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl.ToString();
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return;

        var publicId = ExtractPublicIdFromUrl(fileUrl);
        if (string.IsNullOrEmpty(publicId))
            return;

        var deletionParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deletionParams);
    }

    private static string? ExtractPublicIdFromUrl(string url)
    {
        // Example URL: https://res.cloudinary.com/<cloud_name>/image/upload/v1234567890/reklaim/uploads/sample.jpg
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var uploadIndex = Array.IndexOf(segments, "upload");
            if (uploadIndex == -1 || uploadIndex >= segments.Length - 1)
                return null;

            // Skip version segment if present (starts with 'v' followed by digits)
            var startIndex = uploadIndex + 1;
            if (startIndex < segments.Length && System.Text.RegularExpressions.Regex.IsMatch(segments[startIndex], @"^v\d+$"))
            {
                startIndex++;
            }

            if (startIndex >= segments.Length)
                return null;

            var publicIdWithExt = string.Join('/', segments.Skip(startIndex));
            var lastDot = publicIdWithExt.LastIndexOf('.');
            return lastDot > 0 ? publicIdWithExt[..lastDot] : publicIdWithExt;
        }
        catch
        {
            return null;
        }
    }
}
