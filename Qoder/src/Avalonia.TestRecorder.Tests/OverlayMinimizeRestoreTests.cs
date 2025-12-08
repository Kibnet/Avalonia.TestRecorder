using Avalonia.Controls;
using Avalonia.TestRecorder.UI;
using Xunit;

namespace Avalonia.TestRecorder.Tests
{
    public class OverlayMinimizeRestoreTests
    {
        [Fact]
        public void TestOverlayMinimizeRestore()
        {
            // Create overlay control
            var overlay = new RecorderOverlay();
            
            // Initially should not be minimized
            Assert.False(overlay.Classes.Contains("minimized"));
            
            // Test minimize
            overlay.Minimize();
            Assert.True(overlay.Classes.Contains("minimized"));
            
            // Test restore
            overlay.Restore();
            Assert.False(overlay.Classes.Contains("minimized"));
        }
        
        [Fact]
        public void TestMinimizeWhenAlreadyMinimized()
        {
            // Create overlay control
            var overlay = new RecorderOverlay();
            
            // Minimize twice
            overlay.Minimize();
            overlay.Minimize(); // Should not throw or cause issues
            
            Assert.True(overlay.Classes.Contains("minimized"));
        }
        
        [Fact]
        public void TestRestoreWhenNotMinimized()
        {
            // Create overlay control
            var overlay = new RecorderOverlay();
            
            // Restore when not minimized
            overlay.Restore(); // Should not throw or cause issues
            
            Assert.False(overlay.Classes.Contains("minimized"));
        }
    }
}