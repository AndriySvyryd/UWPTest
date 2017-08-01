using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Verification
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void RunTests_Click(object sender, RoutedEventArgs e)
        {
            RunTestsButton.IsEnabled = false;
            OutputTextBlock.Text = "";
            Task.Run(RunTests);
        }

        private async Task RunTests()
        {
            await WriteLine("Running tests...");

            var testContainer = new Tests(WriteLine);

            foreach (var testMethod in testContainer.GetType().GetTypeInfo().GetRuntimeMethods()
                .Where(m => m.ReturnType == typeof(Task)))
            {
                try
                {
                    await (Task)testMethod.Invoke(testContainer, new object[0]);
                }
                catch (Exception ex)
                {
                    await WriteLine($"Test {testMethod.Name} failed:");
                    await WriteLine(ex.ToString());
                    await WriteLine("");
                }
            }

            await WriteLine("Done");

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () => { RunTestsButton.IsEnabled = true; }).AsTask();
        }

        private Task WriteLine(string text)
        {
            return Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { OutputTextBlock.Text += text + Environment.NewLine; }).AsTask();
        }
    }
}
