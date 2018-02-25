using System;
using System.Collections.Generic;
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
        private TranslateTransform dragTranslation;
        private TranslateTransform dragTranslationCapture;

        public MainPage()
        {
            this.InitializeComponent();
            _previewer = new MediaCapturePreviewer(captureElement, Dispatcher);

            touchRectangle.ManipulationDelta += touchRectangle_ManipulationDelta;
            dragTranslation = new TranslateTransform();
            touchRectangle.RenderTransform = this.dragTranslation;

            captureElement.ManipulationDelta += captureElement_ManipulationDelta;
            dragTranslationCapture = new TranslateTransform();
            captureElement.RenderTransform = this.dragTranslationCapture;
        }

        void touchRectangle_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // Move the rectangle.
            dragTranslation.X += e.Delta.Translation.X;
            dragTranslation.Y += e.Delta.Translation.Y;
        }

        private void captureElement_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // Move the rectangle.
            dragTranslationCapture.X += e.Delta.Translation.X;
            dragTranslationCapture.Y += e.Delta.Translation.Y;
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
            // Query all properties of the device
            IEnumerable<StreamResolution> allProperties = _previewer.MediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview).Select(x => new StreamResolution(x));

            // Order them by resolution then frame rate
            allProperties = allProperties.OrderByDescending(x => x.Height * x.Width).ThenByDescending(x => x.FrameRate);

            // Populate the combo box with the entries
            foreach (var property in allProperties)
            {
                if ((property.Width > 4000) && (property.FrameRate > 25))
                {
                    ComboBoxItem comboBoxItem = new ComboBoxItem();
                    comboBoxItem.Content = property.GetFriendlyName();
                    comboBoxItem.Tag = property;
                    resolutions.Items.Add(comboBoxItem);
                }
            }
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
    }
}
