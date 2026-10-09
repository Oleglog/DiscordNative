using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DiscordNative.App.Controls;

/// <summary>
/// Hôte du fond d'écran personnalisé de l'appli, inspiré du réglage "Custom background"
/// de Files (files-community/Files) : image utilisateur + voile semi-transparent + flou gaussien
/// appliqué via l'API Composition (nécessite le package Win2D.Uwp pour GaussianBlurEffect).
/// </summary>
public sealed partial class BackgroundImageHost : UserControl
{
    private SpriteVisual? _blurVisual;
    private CompositionEffectBrush? _blurBrush;

    public BackgroundImageHost()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyFromSettings();
        App.Settings.SettingsChanged += ApplyFromSettings;
    }

    public void ApplyFromSettings()
    {
        var settings = App.Settings.Current;

        if (string.IsNullOrEmpty(settings.BackgroundImagePath) || !System.IO.File.Exists(settings.BackgroundImagePath))
        {
            BackgroundImage.Source = null;
            OverlayGrid.Opacity = 0;
            RemoveBlur();
            return;
        }

        BackgroundImage.Source = new BitmapImage(new Uri(settings.BackgroundImagePath));
        OverlayGrid.Opacity = settings.BackgroundOpacity;
        ApplyBlur(settings.BackgroundBlurRadius);
    }

    private void ApplyBlur(double radius)
    {
        RemoveBlur();
        if (radius <= 0) return;

        var hostVisual = ElementCompositionPreview.GetElementVisual(BackgroundImage);
        var compositor = hostVisual.Compositor;

        var graphicsEffect = new GaussianBlurEffect
        {
            Name = "Blur",
            BlurAmount = (float)radius,
            Source = new CompositionEffectSourceParameter("Source"),
            BorderMode = EffectBorderMode.Hard,
        };

        var effectFactory = compositor.CreateEffectFactory(graphicsEffect);
        _blurBrush = effectFactory.CreateBrush();
        _blurBrush.SetSourceParameter("Source", compositor.CreateBackdropBrush());

        _blurVisual = compositor.CreateSpriteVisual();
        _blurVisual.Brush = _blurBrush;
        _blurVisual.Size = new System.Numerics.Vector2((float)ActualWidth, (float)ActualHeight);
        SizeChanged += (_, _) =>
        {
            if (_blurVisual is not null)
                _blurVisual.Size = new System.Numerics.Vector2((float)ActualWidth, (float)ActualHeight);
        };

        ElementCompositionPreview.SetElementChildVisual(OverlayGrid, _blurVisual);
    }

    private void RemoveBlur()
    {
        ElementCompositionPreview.SetElementChildVisual(OverlayGrid, null);
        _blurBrush?.Dispose();
        _blurBrush = null;
        _blurVisual?.Dispose();
        _blurVisual = null;
    }
}
