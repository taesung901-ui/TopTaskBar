using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TopTaskBar.Tests;

[TestClass]
public sealed class SettingsStoreTests
{
    [TestMethod]
    public void InvalidJsonIsPreservedAndRejected()
    {
        var testDirectory = CreateTestDirectory();
        var settingsPath = Path.Combine(testDirectory, "settings.json");

        try
        {
            File.WriteAllText(settingsPath, "{ invalid json");

            var loaded = SettingsStore.TryLoadFromPath(settingsPath, out _);

            Assert.IsFalse(loaded);
            Assert.AreEqual("{ invalid json", File.ReadAllText(settingsPath));
            Assert.AreEqual(1, Directory.GetFiles(testDirectory, "settings.invalid-*.json").Length);
        }
        finally
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void SaveReplacesSettingsWithoutLeavingTemporaryFiles()
    {
        var testDirectory = CreateTestDirectory();
        var settingsPath = Path.Combine(testDirectory, "settings.json");

        try
        {
            var settings = new TopTaskBarSettings { WindowSlotWidth = 84 };

            SettingsStore.SaveToPath(settings, settingsPath);
            var loaded = SettingsStore.TryLoadFromPath(settingsPath, out var reloaded);

            Assert.IsTrue(loaded);
            Assert.AreEqual(84, reloaded.WindowSlotWidth);
            Assert.AreEqual(0, Directory.GetFiles(testDirectory, "*.tmp").Length);
        }
        finally
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    private static string CreateTestDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"TopTaskBar.Tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
