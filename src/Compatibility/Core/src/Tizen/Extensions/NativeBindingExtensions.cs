using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls.Compatibility.Internals;
using EObject = ElmSharp.EvasObject;

namespace Microsoft.Maui.Controls.Compatibility.Platform.Tizen
{
	public static class NativeBindingExtensions
	{
		public static void SetBinding(this EObject view, string propertyName, BindingBase binding, string updateSourceEventName = null)
		{
			PlatformBindingHelpers.SetBinding(view, propertyName, binding, updateSourceEventName);
		}

		public static void SetBinding(this EObject view, BindableProperty targetProperty, BindingBase binding)
		{
			PlatformBindingHelpers.SetBinding(view, targetProperty, binding);
		}

		public static void SetValue(this EObject target, BindableProperty targetProperty, object value)
		{
			PlatformBindingHelpers.SetValue(target, targetProperty, value);
		}

		public static void SetBindingContext(this EObject target, object bindingContext, Func<EObject, IEnumerable<EObject>> getChildren = null)
		{
			PlatformBindingHelpers.SetBindingContext(target, bindingContext, getChildren);
		}

		internal static void TransferBindablePropertiesToWrapper(this EObject target, View wrapper)
		{
			PlatformBindingHelpers.TransferBindablePropertiesToWrapper(target, wrapper);
		}
	}
}
