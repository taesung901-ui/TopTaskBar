using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TopTaskBar.Tests;

[TestClass]
public sealed class LauncherTargetHelperTests
{
    private const string MmcPath = @"C:\Windows\System32\mmc.exe";
    private const string SystemDirectory = @"C:\Windows\System32";

    [DataTestMethod]
    [DataRow("Hyper-V 관리자")]
    [DataRow("Hyper-V Manager")]
    public void TryResolveRunningApp_HyperVManager_AddsConsoleArgument(string windowTitle)
    {
        var resolved = LauncherTargetHelper.TryResolveRunningApp(
            windowTitle,
            MmcPath,
            SystemDirectory,
            _ => true,
            out var target,
            out var message);

        Assert.IsTrue(resolved);
        Assert.AreEqual(string.Empty, message);
        Assert.IsNotNull(target);
        Assert.AreEqual("Hyper-V 관리자", target.Name);
        Assert.AreEqual(MmcPath, target.Path);
        Assert.AreEqual(@"""C:\Windows\System32\virtmgmt.msc""", target.Arguments);
        Assert.AreEqual(SystemDirectory, target.WorkingDirectory);
        Assert.IsTrue(target.ReplaceBareHostEntry);
    }

    [TestMethod]
    public void TryResolveRunningApp_UnrecognizedMmc_IsRejected()
    {
        var resolved = LauncherTargetHelper.TryResolveRunningApp(
            "컴퓨터 관리",
            MmcPath,
            SystemDirectory,
            _ => true,
            out var target,
            out var message);

        Assert.IsFalse(resolved);
        Assert.IsNull(target);
        StringAssert.Contains(message, "MMC 기반 앱");
    }

    [TestMethod]
    public void TryResolveRunningApp_HyperVManagerWithoutConsole_IsRejected()
    {
        var resolved = LauncherTargetHelper.TryResolveRunningApp(
            "Hyper-V 관리자",
            MmcPath,
            SystemDirectory,
            _ => false,
            out var target,
            out var message);

        Assert.IsFalse(resolved);
        Assert.IsNull(target);
        StringAssert.Contains(message, "virtmgmt.msc");
    }

    [TestMethod]
    public void TryResolveRunningApp_RegularExecutable_KeepsExistingBehavior()
    {
        const string executablePath = @"C:\Apps\Sample\sample.exe";

        var resolved = LauncherTargetHelper.TryResolveRunningApp(
            "Document - Sample",
            executablePath,
            SystemDirectory,
            _ => false,
            out var target,
            out var message);

        Assert.IsTrue(resolved);
        Assert.AreEqual(string.Empty, message);
        Assert.IsNotNull(target);
        Assert.AreEqual("Sample", target.Name);
        Assert.AreEqual(executablePath, target.Path);
        Assert.AreEqual(string.Empty, target.Arguments);
        Assert.AreEqual(@"C:\Apps\Sample", target.WorkingDirectory);
        Assert.IsFalse(target.ReplaceBareHostEntry);
    }
}
