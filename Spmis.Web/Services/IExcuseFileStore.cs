namespace Spmis.Web.Services;

public interface IExcuseFileStore
{
    // Saves the upload under a random name and returns that stored name.
    Task<string> SaveAsync(IFormFile file, string extension);

    // null when the name is not valid or the file does not exist.
    Stream? OpenRead(string storedName);

    void Delete(string storedName);

    static string ContentTypeFor(string storedName) =>
        Path.GetExtension(storedName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            _ => "image/jpeg"
        };
}