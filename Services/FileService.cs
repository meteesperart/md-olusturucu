using System.IO;
using MDOlusturucu.Models;

namespace MDOlusturucu.Services;

public class FileService : IFileService
{
    public IEnumerable<FileModel> GetFiles(string directory)
    {
        if (!Directory.Exists(directory))
            return Enumerable.Empty<FileModel>();

        return Directory.GetFiles(directory)
            .Where(p => p.EndsWith(".md",   StringComparison.OrdinalIgnoreCase) ||
                        p.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p)
            .Select(path => new FileModel
            {
                Name = Path.GetFileName(path),
                Path = path,
                Content = File.ReadAllText(path)
            });
    }

    public FileModel Read(string path)
    {
        return new FileModel
        {
            Name = Path.GetFileName(path),
            Path = path,
            Content = File.ReadAllText(path)
        };
    }

    public void Write(FileModel file)
    {
        var dir = Path.GetDirectoryName(file.Path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(file.Path, file.Content);
    }

    public FileModel CopyToAppDirectory(string sourcePath, string appDirectory)
    {
        Directory.CreateDirectory(appDirectory);

        var fileName = Path.GetFileName(sourcePath);
        var destPath = Path.Combine(appDirectory, fileName);

        // Aynı isimde dosya varsa üzerine yazmadan benzersiz isim üret
        if (File.Exists(destPath) && !string.Equals(sourcePath, destPath, StringComparison.OrdinalIgnoreCase))
        {
            var nameWithout = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            var counter = 1;
            do
            {
                destPath = Path.Combine(appDirectory, $"{nameWithout}_{counter}{ext}");
                counter++;
            } while (File.Exists(destPath));
        }

        File.Copy(sourcePath, destPath, overwrite: false);

        return new FileModel
        {
            Name = Path.GetFileName(destPath),
            Path = destPath,
            Content = File.ReadAllText(destPath)
        };
    }

    public void Rename(FileModel file, string newName)
    {
        newName = newName.Trim();
        if (string.IsNullOrWhiteSpace(newName)) return;
        if (!Path.HasExtension(newName))
            newName += Path.GetExtension(file.Path);

        var dir     = Path.GetDirectoryName(file.Path)!;
        var newPath = Path.Combine(dir, newName);

        if (string.Equals(file.Path, newPath, StringComparison.OrdinalIgnoreCase)) return;
        if (File.Exists(newPath)) return;

        File.Move(file.Path, newPath);
        file.Path = newPath;
        file.Name = newName;
    }
}
