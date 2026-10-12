using System.Text.RegularExpressions;

namespace Spmis.Web.Services;

public class ExcuseFileStore : IExcuseFileStore
{
    // The only shape a stored name can have: 32 hex characters plus an allowed extension.
    private static readonly Regex StoredNamePattern =
        new("^[0-9a-f]{32}\\.(pdf|jpg|png)$", RegexOptions.Compiled);

    private readonly string _root;

    public ExcuseFileStore(IWebHostEnvironment env, IConfiguration config)
    {
        // Outside wwwroot: the web server never serves this folder directly.
        _root = config["Uploads:ExcuseLetterPath"]
                ?? Path.Combine(env.ContentRootPath, "App_Data", "excuse-letters");
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(IFormFile file, string extension)
    {
        var storedName = Guid.NewGuid().ToString("N") + extension;     // e.g. 3f2a...c1.pdf
        var path = Path.Combine(_root, storedName);
        await using var target = File.Create(path);
        await file.CopyToAsync(target);
        return storedName;
    }

    public Stream? OpenRead(string storedName)
    {
        if (!StoredNamePattern.IsMatch(storedName)) return null;       // blocks "../" tricks
        var path = Path.Combine(_root, storedName);
        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    public void Delete(string storedName)
    {
        if (!StoredNamePattern.IsMatch(storedName)) return;
        var path = Path.Combine(_root, storedName);
        if (File.Exists(path)) File.Delete(path);
    }
}