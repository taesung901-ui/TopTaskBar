using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TopTaskBar.Tests;

[TestClass]
public sealed class WindowClickPolicyTests
{
    [TestMethod]
    public void ActiveWindowRevealedAfterAnotherWindowWasMinimizedIsProtectedOnce()
    {
        var now = new DateTime(2026, 8, 7, 10, 0, 2, DateTimeKind.Utc);

        var protectedClick = WindowClickPolicy.ShouldProtectAutoRevealedWindow(
            isActive: true,
            isMinimized: false,
            clickedWindow: new IntPtr(2),
            lastMinimizedWindow: new IntPtr(1),
            lastMinimizedAtUtc: now.AddSeconds(-2),
            nowUtc: now);

        Assert.IsTrue(protectedClick);
    }

    [TestMethod]
    public void ProtectionExpiresAfterThreeSeconds()
    {
        var now = new DateTime(2026, 8, 7, 10, 0, 4, DateTimeKind.Utc);

        var protectedClick = WindowClickPolicy.ShouldProtectAutoRevealedWindow(
            isActive: true,
            isMinimized: false,
            clickedWindow: new IntPtr(2),
            lastMinimizedWindow: new IntPtr(1),
            lastMinimizedAtUtc: now.AddSeconds(-4),
            nowUtc: now);

        Assert.IsFalse(protectedClick);
    }

    [TestMethod]
    public void SameWindowAndMinimizedWindowKeepExistingBehavior()
    {
        var now = new DateTime(2026, 8, 7, 10, 0, 1, DateTimeKind.Utc);

        Assert.IsFalse(WindowClickPolicy.ShouldProtectAutoRevealedWindow(
            true, false, new IntPtr(1), new IntPtr(1), now.AddSeconds(-1), now));
        Assert.IsFalse(WindowClickPolicy.ShouldProtectAutoRevealedWindow(
            true, true, new IntPtr(2), new IntPtr(1), now.AddSeconds(-1), now));
    }
}
