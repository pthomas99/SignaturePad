using SignaturePad.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace SignatureTest
{
    public class App : Application
    {
        public App()
        {

            var sigview = new SignaturePadView
            {
                BackgroundColor = Color.White,
                CaptionTextColor = Color.Black,
                ClearTextColor = Color.Black,
                PromptTextColor = Color.Black,
                SignatureLineColor = Color.Black,
                StrokeColor = Color.Black,
                CaptionText = "Touch to Enter Signature",
                ClearText = "Start Again",
                PromptText = "Please Sign",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.FillAndExpand,
            };

            // The root page of your application
            MainPage = new ContentPage
            {
                Content = sigview,
            };      
			
		}

		protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}
}
