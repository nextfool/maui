﻿using System;
using Android.Content;
using Android.Runtime;
using Android.Webkit;
using Java.Net;
using AWebView = Android.Webkit.WebView;

namespace Microsoft.AspNetCore.Components.WebView.Maui
{
	internal class WebKitWebViewClient : WebViewClient
	{
		// Using an IP address means that WebView doesn't wait for any DNS resolution,
		// making it substantially faster. Note that this isn't real HTTP traffic, since
		// we intercept all the requests within this origin.
		private static readonly string AppOrigin = $"https://{BlazorWebView.AppHostAddress}/";

		private static readonly Uri AppOriginUri = new(AppOrigin);

		private readonly BlazorWebViewHandler? _webViewHandler;

		public WebKitWebViewClient(BlazorWebViewHandler webViewHandler!!)
		{
			_webViewHandler = webViewHandler;
		}

		protected WebKitWebViewClient(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
			// This constructor is called whenever the .NET proxy was disposed, and it was recreated by Java. It also
			// happens when overridden methods are called between execution of this constructor and the one above.
			// because of these facts, we have to check all methods below for null field references and properties.
		}

		public override bool ShouldOverrideUrlLoading(AWebView? view, IWebResourceRequest? request)
			=> ShouldOverrideUrlLoadingCore(request) || base.ShouldOverrideUrlLoading(view, request);

		private bool ShouldOverrideUrlLoadingCore(IWebResourceRequest? request)
		{
			if (_webViewHandler is null || !Uri.TryCreate(request?.Url?.ToString(), UriKind.RelativeOrAbsolute, out var uri))
			{
				return false;
			}

			// This method never gets called for navigation to a new window ('_blank'),
			// so we know we can safely invoke the UrlLoading event.
			var callbackArgs = UrlLoadingEventArgs.CreateWithDefaultLoadingStrategy(uri, AppOriginUri);
			_webViewHandler.UrlLoading(callbackArgs);

			if (callbackArgs.UrlLoadingStrategy == UrlLoadingStrategy.OpenExternally)
			{
				try
				{
					var intent = Intent.ParseUri(uri.OriginalString, IntentUriType.Scheme);
					_webViewHandler.Context.StartActivity(intent);
				}
				catch (URISyntaxException)
				{
					// This can occur if there is a problem with the URI formatting given its specified scheme.
					// Other platforms will silently ignore formatting issues, so we do the same here.
				}
				catch (ActivityNotFoundException)
				{
					// Do nothing if there is no activity to handle the intent. This is consistent with the
					// behavior on other platforms when a URL with an unknown scheme is clicked.
				}
				return true;
			}

			return callbackArgs.UrlLoadingStrategy != UrlLoadingStrategy.OpenInWebView;
		}

		public override WebResourceResponse? ShouldInterceptRequest(AWebView? view, IWebResourceRequest? request)
		{
			if (request is null)
			{
				throw new ArgumentNullException(nameof(request));
			}

			var requestUri = request?.Url?.ToString();
			var allowFallbackOnHostPage = AppOriginUri.IsBaseOfPage(requestUri);
			requestUri = QueryStringHelper.RemovePossibleQueryString(requestUri);

			if (requestUri != null &&
				_webViewHandler != null &&
				_webViewHandler.WebviewManager != null &&
				_webViewHandler.WebviewManager.TryGetResponseContentInternal(requestUri, allowFallbackOnHostPage, out var statusCode, out var statusMessage, out var content, out var headers))
			{
				var contentType = headers["Content-Type"];

				return new WebResourceResponse(contentType, "UTF-8", statusCode, statusMessage, headers, content);
			}

			return base.ShouldInterceptRequest(view, request);
		}

		public override void OnPageFinished(AWebView? view, string? url)
		{
			base.OnPageFinished(view, url);

			// TODO: How do we know this runs only once?
			if (view != null && AppOriginUri.IsBaseOfPage(url))
			{
				// Startup scripts must run in OnPageFinished. If scripts are run earlier they will have no lasting
				// effect because once the page content loads all the document state gets reset.
				RunBlazorStartupScripts(view);
			}
		}

		private void RunBlazorStartupScripts(AWebView view)
		{
			// TODO: we need to protect against double initialization because the
			// OnPageFinished event refires after the app is brought back from the 
			// foreground and the webview is brought back into view, without it actually
			// getting reloaded.


			// Set up JS ports
			view.EvaluateJavascript(@"

		const channel = new MessageChannel();
		var nativeJsPortOne = channel.port1;
		var nativeJsPortTwo = channel.port2;
		window.addEventListener('message', function (event) {
			if (event.data != 'capturePort') {
				nativeJsPortOne.postMessage(event.data)
			}
			else if (event.data == 'capturePort') {
				if (event.ports[0] != null) {
					nativeJsPortTwo = event.ports[0]
				}
			}
		}, false);

		nativeJsPortOne.addEventListener('message', function (event) {
		}, false);

		nativeJsPortTwo.addEventListener('message', function (event) {
			// data from native code to JS
			if (window.external.__callback) {
				window.external.__callback(event.data);
			}
		}, false);
		nativeJsPortOne.start();
		nativeJsPortTwo.start();

		window.external.sendMessage = function (message) {
			// data from JS to native code
			nativeJsPortTwo.postMessage(message);
		};

		window.external.receiveMessage = function (callback) {
			window.external.__callback = callback;
		}

		", new JavaScriptValueCallback(() =>
			{
				// Set up Server ports
				_webViewHandler?.WebviewManager?.SetUpMessageChannel();

				// Start Blazor
				view.EvaluateJavascript(@"
					Blazor.start();
				", new JavaScriptValueCallback(() =>
				{
					// Done; no more action required
				}));
			}));
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				//_webViewManager = null;
			}
		}

		private class JavaScriptValueCallback : Java.Lang.Object, IValueCallback
		{
			private readonly Action _callback;

			public JavaScriptValueCallback(Action callback!!)
			{
				_callback = callback;
			}

			public void OnReceiveValue(Java.Lang.Object? value)
			{
				_callback();
			}
		}
	}
}
