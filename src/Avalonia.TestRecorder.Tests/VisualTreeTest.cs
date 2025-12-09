using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.HeadlessTestKit;
using System;
using Xunit;

namespace Avalonia.TestRecorder.Tests
{
    public class VisualTreeTest
    {
        [AvaloniaFact]
        public void PrintVisualTreeStructure()
        {
            // Arrange
            var window = new Window
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "Test" },
                        new Button { Content = "Click Me" }
                    }
                }
            };
            window.Show();

            // Print the visual tree to see what it looks like in headless mode
            Console.WriteLine("Visual Tree Structure:");
            Ui.PrintVisualTree(window);
        }
    }
}