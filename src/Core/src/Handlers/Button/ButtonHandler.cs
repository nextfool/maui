#if __IOS__ || MACCATALYST
using PlatformView = UIKit.UIButton;
#elif MONOANDROID
using PlatformView = Google.Android.Material.Button.MaterialButton;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.Controls.Button;
#elif NETSTANDARD || (NET6_0 && !IOS && !ANDROID)
using PlatformView = System.Object;
#endif

namespace Microsoft.Maui.Handlers
{
	public partial class ButtonHandler : IButtonHandler
	{
		ImageSourcePartLoader? _imageSourcePartLoader;
		public ImageSourcePartLoader ImageSourceLoader =>
			_imageSourcePartLoader ??= new ImageSourcePartLoader(this, () => (VirtualView as IImageButton), OnSetImageSource);

		public static IPropertyMapper<IImage, IButtonHandler> ImageButtonMapper = new PropertyMapper<IImage, IButtonHandler>()
		{
			[nameof(IImage.Source)] = MapImageSource
		};

		public static IPropertyMapper<ITextButton, IButtonHandler> TextButtonMapper = new PropertyMapper<ITextButton, IButtonHandler>()
		{
			[nameof(ITextStyle.CharacterSpacing)] = MapCharacterSpacing,
			[nameof(ITextStyle.Font)] = MapFont,
			[nameof(ITextStyle.TextColor)] = MapTextColor,
			[nameof(IText.Text)] = MapText
		};

		public static IPropertyMapper<IButton, IButtonHandler> Mapper = new PropertyMapper<IButton, IButtonHandler>(TextButtonMapper, ImageButtonMapper, ViewHandler.ViewMapper)
		{
			[nameof(IButton.Background)] = MapBackground,
			[nameof(IButton.Padding)] = MapPadding,
			[nameof(IButtonStroke.StrokeThickness)] = MapStrokeThickness,
			[nameof(IButtonStroke.StrokeColor)] = MapStrokeColor,
			[nameof(IButtonStroke.CornerRadius)] = MapCornerRadius
		};

		public static CommandMapper<IButton, IButtonHandler> CommandMapper = new(ViewCommandMapper);

		public ButtonHandler() : base(Mapper, CommandMapper)
		{

		}

		public ButtonHandler(IPropertyMapper? mapper = null) : base(mapper ?? Mapper, CommandMapper)
		{
		}

		IButton IButtonHandler.VirtualView => VirtualView;

		PlatformView IButtonHandler.PlatformView => PlatformView;
	}
}