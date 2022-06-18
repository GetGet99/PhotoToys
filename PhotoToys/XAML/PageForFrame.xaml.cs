using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;

namespace PhotoToys;

public sealed partial class PageForFrame : Page
{
    public PageForFrame()
    {
        InitializeComponent();
    }
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is UIElement uIElement)
        {
            bool success = false;
            while (!success)
            {
                try
                {
                    Content = uIElement;
                    success = true;
                } catch
                {
                    await Task.Delay(300);
                }
            }
        }
    }
    protected override async void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        await Task.Delay(300);
        Content = null;
    }
}
