using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IC
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MediaCapturePreviewer _previewer = null;
        private ScaleTransform scaleTransform;
        private TranslateTransform translateTranslation;
        private TransformGroup transformGroup;

        public MainPage()
        {
            this.InitializeComponent();
            _previewer = new MediaCapturePreviewer(captureElement, Dispatcher);

            captureElement.ManipulationDelta += captureElement_ManipulationDelta;

            scaleTransform = new ScaleTransform();
            translateTranslation = new TranslateTransform();

            transformGroup = new TransformGroup();
            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(translateTranslation);

            captureElement.RenderTransform = this.transformGroup;
        }

        private void captureElement_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // Add translation delta
            translateTranslation.X += e.Delta.Translation.X;
            translateTranslation.Y += e.Delta.Translation.Y;

            if (e.IsInertial)
                return;

            // Keep old scale transform for translation fix (https://social.msdn.microsoft.com/Forums/vstudio/en-US/63ebc273-89bc-431e-a5bd-c014128c7879/scaletransform-and-translatetransform-what-the?forum=wpf)
            double oldCenterX = scaleTransform.CenterX;
            double oldCenterY = scaleTransform.CenterY;

            // Set the new scale center
            scaleTransform.CenterX = e.Position.X;
            scaleTransform.CenterY = e.Position.Y;

            // Apply translation fix for continuos scale
            translateTranslation.X += (scaleTransform.CenterX - oldCenterX) * (scaleTransform.ScaleX - 1);
            translateTranslation.Y += (scaleTransform.CenterY - oldCenterY) * (scaleTransform.ScaleY - 1);

            // Multiply scale delta
            scaleTransform.ScaleX *= e.Delta.Scale;
            scaleTransform.ScaleY *= e.Delta.Scale;

            scaleTransform.ScaleX = Math.Max(scaleTransform.ScaleX, 0.7);
            scaleTransform.ScaleY = Math.Max(scaleTransform.ScaleY, 0.7);

        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await _previewer.InitializeCameraAsync();

            if (_previewer.IsPreviewing)
            {
                PopulateResolutionsComboBox();
                var desiredItem = resolutions.Items[resolutions.Items.Count - 1] as ComboBoxItem;
                var encodingProperties = (desiredItem.Tag as StreamResolution).EncodingProperties;
                await _previewer.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, encodingProperties);
            }
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            await _previewer.CleanupCameraAsync();
        }

        private void PopulateResolutionsComboBox()
        {
            Debug.WriteLine("Iterating resolutions from: {0}", _previewer.MediaCapture.VideoDeviceController.Id);
            // Query all properties of the device
            IEnumerable<StreamResolution> allProperties = _previewer.MediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview).Select(x => new StreamResolution(x));

            // Order them by resolution then frame rate
            allProperties = allProperties.OrderByDescending(x => x.Height * x.Width).ThenByDescending(x => x.FrameRate);
             

            // Populate the combo box with the entries
            foreach (var property in allProperties)
            {
                Debug.WriteLine(" - {0} x {1} @ {2} FPS", property.Height, property.Width, property.FrameRate);

                if ((property.Width > 2500) && (property.FrameRate > 25))
                {
                    ComboBoxItem comboBoxItem = new ComboBoxItem();
                    comboBoxItem.Content = property.GetFriendlyName();
                    comboBoxItem.Tag = property;
                    resolutions.Items.Add(comboBoxItem);
                }
            }

            const string logitechBrioVidAndPid = "VID_046D&PID_085E";

            if (!_previewer.MediaCapture.VideoDeviceController.Id.ToLower().Contains(logitechBrioVidAndPid.ToLower()))
                throw new Exception("Not the Brio!!");

            if (resolutions.Items.Count == 0)
                throw new Exception("Resolution missing");
        }

        private async void resolutions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_previewer.IsPreviewing)
            {
                var selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;
                var encodingProperties = (selectedItem.Tag as StreamResolution).EncodingProperties;
                await _previewer.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, encodingProperties);
            }
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            Splitter.IsPaneOpen = !Splitter.IsPaneOpen;
        }
    }
}
