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

namespace PhotoToys;

class MatImage : IDisposable
{
    ~MatImage() {
        Dispose();
    }
    public void Dispose()
    {
        Mat_?.Dispose();
    }
    public Image UIElement { get; }
    Mat? Mat_;
    public Mat? Mat
    {
        get => Mat_?.Clone();
        set
        {
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
            UIElement.Source = BitmapImage;
        }
    }
    public async Task<DataPackage?> GetDataPackage()
    {
        if (Mat_ == null) return null;
        var data = new DataPackage();
        var datacontent = await GetDataPackageContent();
        Debug.Assert(datacontent != null);
        var (stream, BitmapMemRef, StorageFile) = datacontent.Value;
        data.SetData("PNG", stream);
        //data.SetStorageItems(new IStorageItem[] { StorageFile }, readOnly: false);
        data.SetBitmap(BitmapMemRef);
        return data;
    }
    public async Task<(IRandomAccessStream stream, RandomAccessStreamReference BitmapMemRef, StorageFile StorageFile)?> GetDataPackageContent()
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
        return (stream, BitmapMemRef, sf);
    }
    public BitmapImage? BitmapImage { get; private set; }
    public MenuFlyout MenuFlyout { get; }
    public MatImage(bool DisableView = false, string AddToInventoryLabel = "Add To Inventory", Symbol AddToInventorySymbol = Symbol.Add)
    {
        UIElement = new Image
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
                    var (stream, BitmapMemRef, StorageFile) = datacontent.Value;
                    e.Data.SetData("PNG", stream);
                    e.Data.SetBitmap(BitmapMemRef);
                    e.Data.SetStorageItems(new IStorageItem[] { StorageFile }, readOnly: false);
                } finally
                {
                    d.Complete();
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
                    Text = "View",
                    Icon = new SymbolIcon(Symbol.Zoom),
                    Visibility = DisableView ? Visibility.Collapsed : Visibility.Visible,
                }.Edit(x => x.Click += async delegate
                {
                    if (Mat_ != null)
                        await Mat_.ImShow("View", UIElement.XamlRoot);
                }),
                new MenuFlyoutItem
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Icon = new SymbolIcon(AddToInventorySymbol),
                    Text = AddToInventoryLabel,
                }.Edit(x => x.Click += async (_, _) =>
                {
                    await AddToInventory();
                })
            }
        };
    }
    public async Task CopyToClipboard()
    {
        if (Mat_ != null) Clipboard.SetContent(await GetDataPackage());
    }
    public async Task Save()
    {
        if (Mat_ != null) await SaveMat(Mat_);
    }
    public async Task AddToInventory()
    {
        if (Mat_ != null) await Inventory.Add(Mat_);
    }
    public static implicit operator Image(MatImage m) => m.UIElement;
    public static async Task SaveMat(Mat Mat)
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary
        };

        WinRT.Interop.InitializeWithWindow.Initialize(picker, App.CurrentWindowHandle);

        picker.FileTypeChoices.Add("JPEG", new string[] { ".jpg", ".jpeg" });
        picker.FileTypeChoices.Add("PNG", new string[] { ".png" });

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
