using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TopTaskBar.Tests;

[TestClass]
public sealed class InteractionLoggerTests
{
    [TestMethod]
    public void AppendLineRotatesAndKeepsConfiguredArchiveCount()
    {
        var testDirectory = Path.Combine(Path.GetTempPath(), $"TopTaskBar.LogTests-{Guid.NewGuid():N}");
        var logPath = Path.Combine(testDirectory, "interaction.log");

        try
        {
            InteractionLogger.AppendLine(logPath, "first-entry", maxLogBytes: 16, maxArchiveCount: 2);
            InteractionLogger.AppendLine(logPath, "second-entry", maxLogBytes: 16, maxArchiveCount: 2);
            InteractionLogger.AppendLine(logPath, "third-entry", maxLogBytes: 16, maxArchiveCount: 2);

            Assert.IsTrue(File.Exists(logPath));
            Assert.IsTrue(File.Exists(Path.Combine(testDirectory, "interaction.1.log")));
            Assert.IsTrue(File.Exists(Path.Combine(testDirectory, "interaction.2.log")));
            Assert.IsFalse(File.Exists(Path.Combine(testDirectory, "interaction.3.log")));
            StringAssert.Contains(File.ReadAllText(logPath), "third-entry");
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }
}
