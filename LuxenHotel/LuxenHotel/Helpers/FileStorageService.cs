namespace LuxenHotel.Helpers;

public static class FileStorageService
{
    public static async Task<List<string>> UploadFilesAsync(List<IFormFile> files, IWebHostEnvironment environment)
    {
        var mediaPaths = new List<string>();
        if (files == null || !files.Any())
            return mediaPaths;

        var uploadsFolder = Path.Combine(environment.WebRootPath, "media");
        Directory.CreateDirectory(uploadsFolder);

        foreach (var file in files)
        {
            if (file.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                mediaPaths.Add($"/media/{fileName}");
            }
        }

        return mediaPaths;
    }

    public static async Task<string?> UploadSingleFileAsync(IFormFile file, IWebHostEnvironment environment)
    {
        if (file == null || file.Length == 0)
            return null;

        var uploadsFolder = Path.Combine(environment.WebRootPath, "media");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/media/{fileName}";
    }

    public static void DeleteFile(string filePath, IWebHostEnvironment environment)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        var fullPath = Path.Combine(environment.WebRootPath, filePath.TrimStart('/'));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    public static void DeleteFiles(IEnumerable<string> filePaths, IWebHostEnvironment environment)
    {
        if (filePaths == null || !filePaths.Any())
            return;

        foreach (var filePath in filePaths)
        {
            DeleteFile(filePath, environment);
        }
    }
}