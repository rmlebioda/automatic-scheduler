using OpenQA.Selenium;

namespace PriceCheckWebScrapper.Extensions;

public static class ITakesScreenshotExtensions
{
    public static void TakeScreenshot(this ITakesScreenshot takesScreenshot, string directory)
    {
        takesScreenshot
            .GetScreenshot()
            .SaveAsFile(MakeUnique(Path.Combine(directory, DateTime.Now.ToString("yyyyMMdd_HHmmssffff") + ".png")), ScreenshotImageFormat.Png);
    }

    private static string MakeUnique(string path, Func<int, string>? suffix = null)
    {
        var dir = Path.GetDirectoryName(path)!;
        var fileName = Path.GetFileNameWithoutExtension(path);
        var fileExt = Path.GetExtension(path);
        var uniqueSuffixFunc = suffix ?? new Func<int, string>(i => $"_{i}");

        for (int i = 1;; ++i)
        {
            if (!File.Exists(path))
                return path;

            path = Path.Combine(dir, fileName + uniqueSuffixFunc(i) + fileExt);
        }
    }
}