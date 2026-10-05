using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Reklaim.api.Services;
using Xunit;

namespace Reklaim.SecurityTests;

// <summary>
// Unit tests for the security validation inside LocalDiskFileStorageService:
// extension whitelist, size cap, and path-traversal protection on delete.
// </summary>
public class FileStorageSecurityTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly LocalDiskFileStorageService _service;

    private static IFormFile FormFile(string fileName, long length)
    {
        var bytes = new byte[length];
        return new FormFile(Stream.Null, 0, length, "Image", fileName);
    }

    public FileStorageSecurityTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "reklaim-fs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _service = new LocalDiskFileStorageService(new FakeEnv(_tempRoot));
    }

    [Theory]
    [InlineData("shell.exe")]
    [InlineData("script.php")]
    [InlineData("doc.pdf")]
    [InlineData("image.gif")]
    [InlineData("noext")]
    public async Task UploadFileAsync_RejectsDisallowedExtensions(string fileName)
    {
        var file = FormFile(fileName, 100);
        await Assert.ThrowsAsync<ArgumentException>(() => _service.UploadFileAsync(file));
    }

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("photo.jpeg")]
    [InlineData("photo.png")]
    [InlineData("PHOTO.PNG")] 
    public async Task UploadFileAsync_AcceptsAllowedExtensions(string fileName)
    {
        var file = FormFile(fileName, 100);
        var url = await _service.UploadFileAsync(file);

        Assert.StartsWith("/uploads/", url);
        
        var storedName = url["/uploads/".Length..];
        Assert.True(Guid.TryParse(Path.GetFileNameWithoutExtension(storedName), out _));
        Assert.Equal(Path.GetExtension(fileName), Path.GetExtension(storedName),
            ignoreCase: true);
    }

    [Fact]
    public async Task UploadFileAsync_RejectsOversizedFile()
    {
        var file = FormFile("big.png", 5 * 1024 * 1024 + 1); 
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UploadFileAsync(file));
        Assert.Contains("5MB", ex.Message);
    }

    [Theory]
    [InlineData("/uploads/../../etc/passwd")]
    [InlineData("/uploads/sub/secret.png")]
    [InlineData("/uploads/.")]
    [InlineData("/uploads/..")]
    [InlineData("/uploads/")]
    [InlineData("C:/Windows/system32/config")]
    [InlineData("/static/index.html")]
    public Task DeleteFileAsync_RejectsPathTraversalAndForeignPaths(string fileUrl)
        => Assert.ThrowsAsync<ArgumentException>(() => _service.DeleteFileAsync(fileUrl));

    [Fact]
    public async Task UploadThenDelete_RoundTrip_Works()
    {
        var file = FormFile("ok.png", 50);
        var url = await _service.UploadFileAsync(file);
        await _service.DeleteFileAsync(url); // must not throw
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, recursive: true); } catch { }
    }

    private sealed class FakeEnv : IWebHostEnvironment
    {
        public FakeEnv(string root)
        {
            ContentRootPath = root;
            WebRootPath = root; 
        }
        public string ApplicationName { get; set; } = "test";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ContentRootPath { get; set; } = ".";
        public string EnvironmentName { get; set; } = "Development";
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
