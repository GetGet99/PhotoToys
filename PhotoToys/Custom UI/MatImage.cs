using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using OpenCvSharp;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT;
namespace PhotoToys;
interface IMatDisplayer
{
    FrameworkElement UIElement { get; }
    Mat? Mat { get; set; }
    MatImage MatImage { get; }
}
class MatImage : IDisposable, IMatDisplayer
{
    MatImage IMatDisplayer.MatImage => this;
    ~MatImage() {
        Dispose();
    }
    public void Dispose()
    {
        Mat_?.Dispose();
    }
    public FrameworkElement UIElement => ImageElement;
    Image ImageElement;
    Mat? Mat_;
    public Mat? Mat
    {
        get => Mat_?.Clone();
        set
        {
            if (Mat_ != null)
            {
                Mat_.Dispose();
            }
            Mat_ = value?.ToBGRA();
            if (Mat_ == null)
            {
                UIElement.ContextFlyout = null;
                BitmapImage = null;
            }
            else
            {
                BitmapImage = Mat_.ToBitmapImage();
                UIElement.ContextFlyout = MenuFlyout;
            }
            ImageElement.Source = BitmapImage;
        }
    }
    public async Task<DataPackage?> GetDataPackage()
    {
        if (Mat_ == null) return null;
        var data = new DataPackage();
        var datacontent = await GetDataPackageContent();
        Debug.Assert(datacontent != null);
        var (bytes, stream, BitmapMemRef, StorageFile) = datacontent.Value;
        try
        {
            data.SetData("PhotoToysImage", stream);
            data.SetData("PNG", stream);
            //data.SetStorageItems(new IStorageItem[] { StorageFile }, readOnly: false);
            data.SetBitmap(BitmapMemRef);
        }
        catch
        {

        }
        return data;
    }
    public async Task<(byte[] bytes, IRandomAccessStream stream, RandomAccessStreamReference BitmapMemRef, StorageFile StorageFile)?> GetDataPackageContent()
    {
        if (Mat_ == null) return null;
        Cv2.ImEncode(".png", Mat_, out var bytes);
        var ms = new MemoryStream(bytes);
        var stream = ms.AsRandomAccessStream();
        var BitmapMemRef = RandomAccessStreamReference.CreateFromStream(stream);
        StorageFile sf = await StorageFile.CreateStreamedFileAsync("Image.png", (e) =>
        {
            try
            {
                using var stream = e.AsStreamForWrite();
                stream.Write(bytes);
            }
            finally
            {
                //e.Dispose();
            }
        }, BitmapMemRef);
        return (bytes, stream, BitmapMemRef, sf);
    }
    public BitmapImage? BitmapImage { get; private set; }
    public MenuFlyout MenuFlyout { get; }
    public bool OverwriteDefaultTooltip { get; set; }
    public event Func<Point, string>? DefaultTooltipOverwriter;
    public MatImage(bool DisableView = false, string AddToInventoryLabel = "Add To Inventory", Symbol AddToInventorySymbol = Symbol.Add)
    {
        ImageElement = new Image
        {
            Source = BitmapImage,
            AllowDrop = true
        }
        .Edit(x =>
        {
            bool PointerPressing = false;
            x.PointerPressed += (o, e) =>
            {
                var pointerPoint = e.GetCurrentPoint(x);
                if (pointerPoint.Properties.IsLeftButtonPressed)
                    PointerPressing = true;
            };
            x.PointerMoved += async (o, e) =>
            {
                if (PointerPressing)
                {
                    var pointerPoint = e.GetCurrentPoint(x);
                    await x.StartDragAsync(pointerPoint);
                    PointerPressing = false;
                }
            };
            x.PointerReleased += (o, e) =>
            {
                PointerPressing = false;
            };
            x.PointerCaptureLost += (o, e) =>
            {
                PointerPressing = false;
            };
            x.PointerCanceled += (o, e) =>
            {
                PointerPressing = false;
            };
            x.DragStarting += async (o, e) =>
            {
                var d = e.GetDeferral();
                try
                {
                    if (Mat_ == null) return;
                    e.AllowedOperations = DataPackageOperation.Copy;
                    var datacontent = await GetDataPackageContent();
                    Debug.Assert(datacontent != null);
                    var (bytes, stream, BitmapMemRef, StorageFile) = datacontent.Value;
                    e.Data.SetData("PhotoToys Image", stream);
                    e.Data.SetData("PNG", stream);
                    e.Data.SetBitmap(BitmapMemRef);
                    e.Data.SetStorageItems(new IStorageItem[] { StorageFile }, readOnly: false);
                } finally
                {
                    d.Complete();
                }
            };
            var tooltip = new ToolTip
            {
                Content = null,
                PlacementTarget = x,
                Placement = Microsoft.UI.Xaml.Controls.Primitives.PlacementMode.Top
            };
            ToolTipService.SetToolTip(x, tooltip);
            x.PointerExited += delegate
            {
                tooltip.IsOpen = false;
            };
            x.PointerMoved += (_, e) =>
            {
                if (Mat_ is not null)
                {
                    try
                    {
                        var uiw = x.ActualWidth;
                        var uih = x.ActualHeight;
                        var pt = e.GetCurrentPoint(x);
                        var pointlocX = Mat_.Width;
                        var UIShowScale = Mat_.Width / x.ActualWidth;
                        string? text = null;
                        if (OverwriteDefaultTooltip)
                        {
                            text = DefaultTooltipOverwriter?.Invoke(new Point(pt.Position.X * UIShowScale, pt.Position.Y * UIShowScale));
                        }
                        else
                        {
                            var pty = (int)(pt.Position.Y * UIShowScale);
                            var ptx = (int)(pt.Position.X * UIShowScale);
                            var value = Mat_.Get<Vec4b>(pty, ptx);
                            text = $"Color: (R: {value.Item0}, G: {value.Item1}, B: {value.Item2}, A: {value.Item3}) (X: {ptx}, Y: {pty})";
                        }
                        tooltip.Content = text;
                        ToolTipService.SetToolTip(x, tooltip);
                        tooltip.IsOpen = true;
                    } catch
                    {
                        tooltip.IsOpen = false;
                    }
                }
            };
        });
        MenuFlyout = new MenuFlyout
        {
            Items =
            {
                new MenuFlyoutItem
                {
                    Text = "Copy",
                    Icon = new SymbolIcon(Symbol.Copy)
                }.Edit(x => x.Click += async (_, _) => await CopyToClipboard()),
                new MenuFlyoutItem
                {
                    Text = "Save",
                    Icon = new SymbolIcon(Symbol.Save)
                }.Edit(x => x.Click += async (_, _) => {
                    if (Mat_ == null) return;
                    await SaveMat(Mat_);
                }),
                new MenuFlyoutItem
                {
                    Text = "Share",
                    Icon = new SymbolIcon(Symbol.Share),
                }.Edit(x => x.Click += async delegate
                {
                    await Share();
                }),
                new MenuFlyoutSeparator(),
                new MenuFlyoutItem
                {
                    Text = "View",
                    Icon = new SymbolIcon(Symbol.View),
                    Visibility = DisableView ? Visibility.Collapsed : Visibility.Visible,
                }.Edit(x => x.Click += async delegate
                {
                    await View();
                }),
                new MenuFlyoutItem
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Icon = new SymbolIcon(AddToInventorySymbol),
                    Text = AddToInventoryLabel,
                }.Edit(x => x.Click += async (_, _) =>
                {
                    await AddToInventory();
                }),
            }
        };
    }
    public async Task CopyToClipboard()
    {
        if (Mat_ != null) Clipboard.SetContent(await GetDataPackage());
    }
    static readonly Guid _dtm_iid =
        new Guid(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);
    public async Task Share()
    {
        var t = new TaskCompletionSource();
        var Handle = App.CurrentWindowHandle;
        IDataTransferManagerInterop interop =
        Windows.ApplicationModel.DataTransfer.DataTransferManager.As
            <IDataTransferManagerInterop>();

        IntPtr result = interop.GetForWindow(Handle, _dtm_iid);
        var dtm = WinRT.MarshalInterface
            <Windows.ApplicationModel.DataTransfer.DataTransferManager>.FromAbi(result);

        async void A(DataTransferManager o, DataRequestedEventArgs e)
        {
            var d = e.Request.GetDeferral();
            try
            {
                if (Mat_ == null) return;
                var datacontent = await GetDataPackageContent();
                Debug.Assert(datacontent != null);
                var (bytes, stream, BitmapMemRef, StorageFile) = datacontent.Value;
                e.Request.Data.Properties.Title = "Share Image";
                e.Request.Data.SetData("PhotoToys Image", stream);
                e.Request.Data.SetData("PNG", stream);
                e.Request.Data.SetBitmap(BitmapMemRef);
                e.Request.Data.SetStorageItems(new IStorageItem[] { StorageFile }, readOnly: false);
            }
            finally
            {
                d.Complete();
                t.SetResult();
            }
        }
        dtm.DataRequested += A;
        interop.ShowShareUIForWindow(Handle);
        await t.Task;
        dtm.DataRequested -= A;
    }
    public async Task Save()
    {
        if (Mat_ != null) await SaveMat(Mat_);
    }
    public async Task View()
    {
        if (Mat_ != null)
            await Mat_.ImShow("View", UIElement.XamlRoot);
    }
    public bool OverwriteAddToInventory = false;
    public event Action? OnAddToInventory;
    public async Task AddToInventory()
    {
        if (OverwriteAddToInventory)
            OnAddToInventory?.Invoke();
        else
            if (Mat_ != null) await Inventory.Add(Mat_);
    }
    public static implicit operator Image(MatImage m) => m.ImageElement;
    public static async Task SaveMat(Mat Mat)
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary
        };

        WinRT.Interop.InitializeWithWindow.Initialize(picker, App.CurrentWindowHandle);

        picker.FileTypeChoices.Add("PNG", new string[] { ".png" });
        picker.FileTypeChoices.Add("JPEG", new string[] { ".jpg", ".jpeg" });

        var sf = await picker.PickSaveFileAsync();
        if (sf != null)
        {
            var s = await sf.OpenAsync(FileAccessMode.ReadWrite);
            var stream = s.AsStream();
            try
            {
                stream.Position = 0;
                Cv2.ImEncode(sf.FileType, Mat, out var bytes);
                stream.Write(bytes);
            }
            finally
            {
                stream.Close();
            }

        }
    }
}

class DoubleMatDisplayer : IDisposable, IMatDisplayer
{
    ToggleMenuFlyoutItem NewChannelMenuFlyoutItem(int index)
    {
        var output = new ToggleMenuFlyoutItem
        {
            Text = $"Channel {index + 1}",
        };
        output.Click += delegate
        {
            foreach (var ele in ChannelSelectionMenu.Items)
                if (ele is ToggleMenuFlyoutItem tmfi && tmfi != output)
                    tmfi.IsChecked = false;
            SelectedChannel = index;
        };
        ChannelSelectionMenu.Items.Add(output);
        return output;
    }
    ToggleMenuFlyoutItem NewColormapFlyoutItem(ColormapTypes? Type)
    {
        var output = new ToggleMenuFlyoutItem
        {
            Text = Type?.ToString() ?? "Grayscale",
        };
        output.Click += delegate
        {
            foreach (var ele in HeatmapSelectionMenu.Items)
                if (ele is ToggleMenuFlyoutItem tmfi && tmfi != output)
                    tmfi.IsChecked = false;
            SelectedColorMap = Type;
        };
        HeatmapSelectionMenu.Items.Add(output);
        return output;
    }
    public FrameworkElement UIElement => MatImage.UIElement;
    readonly MenuFlyoutSubItem ChannelSelectionMenu = new()
    {
        Text = "Show Channel"
    };
    readonly MenuFlyoutSubItem HeatmapSelectionMenu = new()
    {
        Text = "Heatmap View"
    };
    public MatImage MatImage { get; } = new();
    public DoubleMatDisplayer() {
        MatImage.MenuFlyout.Items.Add(ChannelSelectionMenu);
        MatImage.MenuFlyout.Items.Add(HeatmapSelectionMenu);
        NewChannelMenuFlyoutItem(index: 0);
        NewChannelMenuFlyoutItem(index: 1);
        NewChannelMenuFlyoutItem(index: 2);
        NewColormapFlyoutItem(null);
        foreach (var e in Enum.GetValues<ColormapTypes>())
            NewColormapFlyoutItem(e);
        var ui = UIElement;
        MatImage.DefaultTooltipOverwriter += pt =>
        {
            if (Mat_ is not null && _SelectedChannel is not -1 && _SelectedChannelCache is not null)
            {
                return $"Value: {_SelectedChannelCache.Get<double>(pt.Y, pt.X)} (X: {pt.X}, Y: {pt.Y})";
            }
            else return $"[Can't find value] (X: {pt.X}, Y: {pt.Y})";
        };
    }
    int _SelectedChannel;
    int SelectedChannel
    {
        get => _SelectedChannel;
        set
        {
            _SelectedChannel = value;
            OnMatDisplayOptionsChanged();
        }
    }
    ColormapTypes? _SelectedColorMap;
    ColormapTypes? SelectedColorMap
    {
        get => _SelectedColorMap;
        set
        {
            _SelectedColorMap = value;
            OnMatDisplayOptionsChanged();
        }
    }
    Mat? Mat_;
    public Mat? Mat
    {
        get => Mat_?.Clone();
        set
        {
            if (Mat_ != null)
            {
                Mat_.Dispose();
            }
            Mat_ = value?.Clone();
            if (Mat_ is not null && Mat_.IsCompatableImage())
            {
                MatImage.OverwriteDefaultTooltip = false;
                ChannelSelectionMenu.Visibility = Visibility.Collapsed;
                HeatmapSelectionMenu.Visibility = Visibility.Collapsed;
                MatImage.Mat = Mat_;
                ToolTipService.SetToolTip(MatImage.UIElement, null);
            }
            else
            {
                MatImage.OverwriteDefaultTooltip = true;
                ChannelSelectionMenu.Visibility = Visibility.Visible;
                HeatmapSelectionMenu.Visibility = Visibility.Visible;
                OnMatUpdate();
            }
        }
    }
    void OnMatUpdate()
    {
        if (Mat_ == null)
        {
            SelectedChannel = -1;
        } else
        {
            var channels = Mat_.Channels();
            for (int c = 0; c < channels; c++)
            {
                if (ChannelSelectionMenu.Items[c] is UIElement element)
                {
                    element.Visibility = Visibility.Visible;
                }
            }
            for (int c = ChannelSelectionMenu.Items.Count; c < channels; c++)
            {
                NewChannelMenuFlyoutItem(c);
            }
            for (int c = channels; c < ChannelSelectionMenu.Items.Count; c++)
            {
                if (ChannelSelectionMenu.Items[c] is UIElement element)
                {
                    element.Visibility = Visibility.Collapsed;
                }
            }
            if (_SelectedChannel > channels - 1)
            {
                SelectedChannel = channels - 1;
            } else
            {
                OnMatDisplayOptionsChanged();
            }
        }
    }
    Mat? _SelectedChannelCache = null;
    void OnMatDisplayOptionsChanged()
    {
        if (SelectedChannel is not -1 && Mat_ is not null)
            using (var tracker = new ResourcesTracker())
            {
                _SelectedChannelCache?.Dispose();
                _SelectedChannelCache = Mat_.ExtractChannel(_SelectedChannel);
                var displaymat = _SelectedChannelCache.NormalBytes();
                if (ChannelSelectionMenu.Items[_SelectedChannel] is RadioMenuFlyoutItem ele1)
                {
                    ele1.IsChecked = true;
                }
                if (HeatmapSelectionMenu.Items[(int)(_SelectedColorMap ?? (ColormapTypes)(-1))+1] is RadioMenuFlyoutItem ele2)
                {
                    ele2.IsChecked = true;
                }
                if (_SelectedColorMap is ColormapTypes cmt)
                {
                    var newmat = displaymat.Heatmap(cmt);
                    displaymat.Dispose();
                    displaymat = newmat;
                }
                MatImage.Mat = displaymat;
            }     
        else
        {
            _SelectedChannelCache?.Dispose();
            _SelectedChannelCache = null;
        }
    }

    public void Dispose()
    {
        MatImage.Dispose();
    }
}
[System.Runtime.InteropServices.ComImport, System.Runtime.InteropServices.Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
[System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
interface IDataTransferManagerInterop
{
    IntPtr GetForWindow([System.Runtime.InteropServices.In] IntPtr appWindow, [System.Runtime.InteropServices.In] ref Guid riid);
    void ShowShareUIForWindow(IntPtr appWindow);
}