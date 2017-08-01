using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            OutputTextBlock.Text = "";
            Task.Run(RunTests);
        }

        private async Task RunTests()
        {
            await WriteLine("Running tests...");

            var succeededCount = 0;
            var failedCount = 0;
            var testContainer = new Tests(WriteLine);

            foreach (var testMethod in testContainer.GetType().GetTypeInfo().GetRuntimeMethods()
                .Where(m => m.ReturnType == typeof(Task) && m.IsPublic))
            {
                try
                {
                    await (Task)testMethod.Invoke(testContainer, new object[0]);
                    succeededCount++;
                }
                catch (Exception ex)
                {
                    await WriteLine($"Test {testMethod.Name} failed:");
                    await WriteLine(ex.ToString());
                    await WriteLine("");
                    failedCount++;
                }
            }

            await WriteLine($"Succeeded: {succeededCount}. Failed: {failedCount}");
        }

        private Task WriteLine(string text)
            => Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { OutputTextBlock.Text += text + Environment.NewLine; }).AsTask();
    }
}
