//
// SignaturePad.cs: User Control subclass for Windows Phone to allow users to draw their signature on 
//				   the device to be captured as an image or vector.
//
// Author:
//   Timothy Risi (timothy.risi@gmail.com)
//
// Copyright (C) 2012 Timothy Risi
//
using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Xamarin.Controls
{
    public partial class SignaturePad : UserControl
    {
        List<Point> currentPoints;
        List<Point[]> points;

        //Create an array containing all of the points used to draw the signature.  Uses (0, 0)
        //to indicate a new line.
        public Point[] Points
        {
            get
            {
                if (points == null || points.Count() == 0)
                    return new Point[0];

                IEnumerable<Point> pointsList = points[0];

                for (var i = 1; i < points.Count; i++)
                {
                    pointsList = pointsList.Concat(new[] { new Point(0, 0) });
                    pointsList = pointsList.Concat(points[i]);
                }

                return pointsList.ToArray();
            }
        }

        public bool IsBlank
        {
            get { return points == null || points.Count() == 0 || !(points.Where(p => p.Any()).Any()); }
        }

        Color strokeColor;
        public Color StrokeColor
        {
            get { return strokeColor; }
            set
            {
                strokeColor = value;
                InkDrawingAttributes drawingAttributes = inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
                drawingAttributes.Color = strokeColor;
                inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
            }
        }

        Color backgroundColor;
        public Color BackgroundColor
        {
            get { return backgroundColor; }
            set
            {
                backgroundColor = value;
                LayoutRoot.Background = new SolidColorBrush(value);
            }
        }

        float lineWidth;
        public float StrokeWidth
        {
            get { return lineWidth; }
            set
            {
                lineWidth = value;
                InkDrawingAttributes drawingAttributes = inkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
                drawingAttributes.Size = new Windows.Foundation.Size(lineWidth, lineWidth);
                inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
            }
        }

        public TextBlock Caption
        {
            get { return captionLabel; }
        }

        public string CaptionText
        {
            get { return captionLabel.Text; }
            set { captionLabel.Text = value; }
        }

        public TextBlock ClearLabel
        {
            get { return btnClear; }
        }

        public string ClearLabelText
        {
            get { return btnClear.Text; }
            set { btnClear.Text = value; }
        }

        public TextBlock SignaturePrompt
        {
            get { return textBlock1; }
        }

        public string SignaturePromptText
        {
            get { return textBlock1.Text; }
            set { textBlock1.Text = value; }
        }

        public Border SignatureLine
        {
            get { return border1; }
        }

        public Brush SignatureLineBrush
        {
            get { return border1.Background; }
            set { border1.Background = value; }
        }

        public SignaturePad()
        {
            InitializeComponent();
            Initialize();
        }

        void Initialize()
        {
            image.Visibility = Visibility.Visible;
            currentPoints = new List<Point>();
            points = new List<Point[]>();
            strokeColor = Colors.White;
            backgroundColor = Colors.Black;
            LayoutRoot.Background = new SolidColorBrush(backgroundColor);
            lineWidth = 3f;
            SizeChanged += SignaturePad_SizeChanged;

            inkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Touch;
            inkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;

            CoreInkIndependentInputSource core = CoreInkIndependentInputSource.Create(inkCanvas.InkPresenter);
            core.PointerPressing += Core_PointerPressing;
            core.PointerReleasing += Core_PointerReleasing;
            core.PointerMoving += Core_PointerMoving;
        }


        private async void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            image.Source = await GetBitmap();
        }


        private async void SignaturePad_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            image.Source = await GetBitmap();
        }


        //Delete the current signature
        public void Clear()
        {
            currentPoints.Clear();
            points.Clear();
            btnClear.Visibility = Visibility.Collapsed;
            inkCanvas.InkPresenter.StrokeContainer.Clear();
            image.Source = null;
        }

        private void btnClear_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Clear();
        }

        #region GetImage

        private async Task<WriteableBitmap> GetBitmap()
        {
            var memoryStream = GetFormatedImage(CanvasBitmapFileFormat.Bmp);
            WriteableBitmap writeableBitmap = await new WriteableBitmap(1, 1).FromStream(memoryStream, BitmapPixelFormat.Bgra8);
            return writeableBitmap;
        }

        //Create a WriteableBitmap of the currently drawn signature with default colors.
        public WriteableBitmap GetImage(bool shouldCrop = true, bool keepAspectRatio = true)
        {
            return image.Source as WriteableBitmap;
        }

        private InMemoryRandomAccessStream GetFormatedImage(CanvasBitmapFileFormat fmt)
        {
            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasRenderTarget renderTarget = new CanvasRenderTarget(device, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight, 96);
            using (var ds = renderTarget.CreateDrawingSession())
            {
                ds.Clear(BackgroundColor);
                ds.DrawInk(inkCanvas.InkPresenter.StrokeContainer.GetStrokes());
            }
            InMemoryRandomAccessStream memoryStream;
            memoryStream = new InMemoryRandomAccessStream();
            renderTarget.SaveAsync(memoryStream, fmt, 1f).AsTask().Wait();
            return memoryStream;
        }
        #endregion



        Rect getCroppedRectangle(Point[] cachedPoints)
        {
            var xMin = cachedPoints.Where(point => point != new Point(0, 0)).Min(point => point.X) - StrokeWidth / 2;
            var xMax = cachedPoints.Where(point => point != new Point(0, 0)).Max(point => point.X) + StrokeWidth / 2;
            var yMin = cachedPoints.Where(point => point != new Point(0, 0)).Min(point => point.Y) - StrokeWidth / 2;
            var yMax = cachedPoints.Where(point => point != new Point(0, 0)).Max(point => point.Y) + StrokeWidth / 2;

            xMin = Math.Max(xMin, 0);
            xMax = Math.Min(xMax, ActualWidth);
            yMin = Math.Max(yMin, 0);
            yMax = Math.Min(yMax, ActualHeight);

            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        float getScaleFromSize(Size size, Size original)
        {
            double scaleX = size.Width / original.Width;
            double scaleY = size.Height / original.Height;

            return (float)Math.Min(scaleX, scaleY);
        }

        Size getSizeFromScale(float scale, Size original)
        {
            double width = original.Width * scale;
            double height = original.Height * scale;

            return new Size(width, height);
        }


        #region Touch Events

        private void UpdateCurrentPoints(Point point)
        {
            // Only add the point to the stroke if it is on the current view.
            if (point.X < 0 || point.Y < 0 || point.X > ActualWidth || point.Y > ActualHeight)
                return;
            currentPoints.Add(point);
        }

        private async void Core_PointerPressing(CoreInkIndependentInputSource sender, Windows.UI.Core.PointerEventArgs args)
        {
            Debug.WriteLine("Core_PointerPressing");
            if (this.Dispatcher != null)
            {
                Point point = new Point(args.CurrentPoint.Position.X, args.CurrentPoint.Position.Y);
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    currentPoints.Clear();
                    UpdateCurrentPoints(point);
                    btnClear.Visibility = Visibility.Visible;
                });
            }
        }

        private async void Core_PointerMoving(CoreInkIndependentInputSource sender, Windows.UI.Core.PointerEventArgs args)
        {
            if (this.Dispatcher != null)
            {
                Point point = new Point(args.CurrentPoint.Position.X, args.CurrentPoint.Position.Y);
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    UpdateCurrentPoints(point);
                });
            }
        }

        private async void Core_PointerReleasing(CoreInkIndependentInputSource sender, Windows.UI.Core.PointerEventArgs args)
        {
            if (this.Dispatcher != null)
            {
                Point point = new Point(args.CurrentPoint.Position.X, args.CurrentPoint.Position.Y);
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    InkStrokeContainer container = inkCanvas.InkPresenter.StrokeContainer;
                    UpdateCurrentPoints(point);
                    points.Add(currentPoints.ToArray());
                });
            }
        }

        #endregion

        //Allow the user to import an array of points to be used to draw a signature in the view, with new
        //lines indicated by a PointF.Empty in the array.
        public void LoadPoints(Point[] loadedPoints)
        {
            throw new NotImplementedException();
        }



    }
}
