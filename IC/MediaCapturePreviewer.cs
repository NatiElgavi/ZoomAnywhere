//The MIT License(MIT)

//Copyright(c) Microsoft Corporation

//Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using System;
using Windows.Devices.Enumeration;
using System.Diagnostics;

namespace IC
{
    internal class MediaCapturePreviewer
    {
        CoreDispatcher _dispatcher;
        CaptureElement _previewControl;

        public MediaCapturePreviewer(CaptureElement previewControl, CoreDispatcher dispatcher)
        {
            _previewControl = previewControl;
            _dispatcher = dispatcher;
        }

        public bool IsPreviewing { get; private set; }
        public bool IsRecording { get; set; }
        public MediaCapture MediaCapture { get; private set; }
        DeviceInformationCollection devices;

        /// <summary>
        /// Sets encoding properties on a camera stream. Ensures CaptureElement and preview stream are stopped before setting properties.
        /// </summary>
        public async Task SetMediaStreamPropertiesAsync(MediaStreamType streamType, IMediaEncodingProperties encodingProperties)
        {
            // Stop preview and unlink the CaptureElement from the MediaCapture object
            await MediaCapture.StopPreviewAsync();
            _previewControl.Source = null;

            // Apply desired stream properties
            await MediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, encodingProperties);

            // Recreate the CaptureElement pipeline and restart the preview
            _previewControl.Source = MediaCapture;
            await MediaCapture.StartPreviewAsync();
        }

        private void ListDeviceDetails()
        {
            int i = 0;

            foreach (var device in devices)
            {
                Debug.WriteLine("* Device [{0}]", i++);
                Debug.WriteLine("Id: " + device.Id);
            }
        }


        /// <summary>
        /// Initializes the MediaCapture, starts preview.
        /// </summary>
        public async Task InitializeCameraAsync()
        {
            const string logitechBrioVidAndPid = "VID_046D&PID_085E";
            if (devices == null)
            {
                devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                foreach (var device in devices)
                {
                    if (device.Id.Contains(logitechBrioVidAndPid))
                    {
                        MediaCapture = new MediaCapture();
                        MediaCapture.Failed += MediaCapture_Failed;

                        try
                        {
                            //await MediaCapture.InitializeAsync();
                            await MediaCapture.InitializeAsync(
                                new MediaCaptureInitializationSettings
                                {
                                    VideoDeviceId = device.Id
                                }
                            );

                            _previewControl.Source = MediaCapture;
                            await MediaCapture.StartPreviewAsync();
                            IsPreviewing = true;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // This can happen if access to the camera has been revoked.
                            await CleanupCameraAsync();
                        }
                    }
                }
            }
        }

        public async Task CleanupCameraAsync()
        {
            if (IsRecording)
            {
                await MediaCapture.StopRecordAsync();
                IsRecording = false;
            }

            if (IsPreviewing)
            {
                await MediaCapture.StopPreviewAsync();
                IsPreviewing = false;
            }

            _previewControl.Source = null;

            if (MediaCapture != null)
            {
                MediaCapture.Dispose();
                MediaCapture = null;
            }
        }

        private void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs e)
        {
            var task = _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                IsRecording = false;
                IsPreviewing = false;
                await CleanupCameraAsync();
            });
        }
    }
}