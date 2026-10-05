using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Reklaim.api.Controllers;
using Reklaim.api.Data;
using Reklaim.api.Dtos;
using Reklaim.api.Models;
using Reklaim.api.Services;
using Xunit;

namespace Reklaim.SecurityTests;

/// <summary>
/// Unit tests for owner-only authorization on ItemPostsController (status update
/// and delete). A non-owner with a valid JWT must get 403 Forbidden, and the
/// post must be left untouched.
/// </summary>
public class PostOwnershipTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly AppDbContext _db;
    private readonly ItemPostsController _controller;
    private readonly User _owner;
    private readonly User _attacker;
    private readonly ItemPost _post;

    public PostOwnershipTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "reklaim-sec-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("reklaim-own-" + Guid.NewGuid())
            .Options;
        _db = new AppDbContext(options);

        _owner = new User { UserName = "owner@test.io", Email = "owner@test.io", Name = "Owner" };
        _attacker = new User { UserName = "attacker@test.io", Email = "attacker@test.io", Name = "Attacker" };
        _db.Users.AddRange(_owner, _attacker);
        _db.SaveChanges();

        _post = new ItemPost
        {
            Title = "Phone charger",
            Description = "White cable",
            LocationFound = "Block C",
            Category = "Electronics",
            PostType = PostType.Found,
            UserId = _owner.Id,
            User = _owner
        };
        _db.Posts.Add(_post);
        _db.SaveChanges();

        var env = new FakeEnv { ContentRootPath = _tempRoot };
        var storage = new LocalDiskFileStorageService(env);
        _controller = new ItemPostsController(_db, storage)
        {
            ControllerContext = new ControllerContext
            {
                ActionDescriptor = new ControllerActionDescriptor(),
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private void ActAs(User user)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new("sub", user.Id.ToString()),
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString())
        };
        _controller.ControllerContext.HttpContext.User =
            new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims, "test"));
    }

    [Fact]
    public async Task UpdateStatus_ByNonOwner_IsForbidden_AndStatusUnchanged()
    {
        ActAs(_attacker);
        var result = await _controller.UpdateStatus(_post.Id,
            new UpdateItemPostStatusRequest { Status = PostStatus.Claimed });

        Assert.IsType<ForbidResult>(result);

        var post = await _db.Posts.FirstAsync(p => p.Id == _post.Id);
        Assert.Equal(PostStatus.Active, post.Status);
    }

    [Fact]
    public async Task Delete_ByNonOwner_IsForbidden_AndPostSurvives()
    {
        ActAs(_attacker);
        var result = await _controller.Delete(_post.Id);

        Assert.IsType<ForbidResult>(result);
        Assert.True(await _db.Posts.AnyAsync(p => p.Id == _post.Id));
    }

    [Fact]
    public async Task UpdateStatus_ByOwner_Succeeds()
    {
        ActAs(_owner);
        var result = await _controller.UpdateStatus(_post.Id,
            new UpdateItemPostStatusRequest { Status = PostStatus.Claimed });

        Assert.IsType<NoContentResult>(result);
        var post = await _db.Posts.FirstAsync(p => p.Id == _post.Id);
        Assert.Equal(PostStatus.Claimed, post.Status);
    }

    public void Dispose()
    {
        _db.Dispose();
        try { Directory.Delete(_tempRoot, recursive: true); } catch { }
    }

    private sealed class FakeEnv : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
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
