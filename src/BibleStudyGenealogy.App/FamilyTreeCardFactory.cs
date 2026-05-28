using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Interactivity;

namespace BibleStudyGenealogy.App;

internal sealed record FamilyTreeCardModel(
    double Zoom,
    string DisplayName,
    string BirthText,
    string DeathText,
    string AvatarGlyph,
    IBrush Background,
    IBrush HoverBackground,
    IBrush BorderBrush,
    bool IsSelected,
    bool IsFocus,
    bool IsPlaceholder);

internal static class FamilyTreeCardFactory
{
    public static Control Create(
        FamilyTreeCardModel model,
        Action onSelect,
        Action onEdit,
        Action onAddRelative)
    {
        var card = new Border
        {
            Width = 380 * model.Zoom,
            Height = 182 * model.Zoom,
            Background = model.Background,
            BorderBrush = model.BorderBrush,
            BorderThickness = new Thickness(model.IsSelected || model.IsFocus ? 2.8 * model.Zoom : 1.5 * model.Zoom),
            CornerRadius = new CornerRadius(8 * model.Zoom),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        card.PointerEntered += (_, _) => card.Background = model.HoverBackground;
        card.PointerExited += (_, _) => card.Background = model.Background;
        card.PointerPressed += (_, args) =>
        {
            args.Handled = true;
            onSelect();
        };

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*"),
            ColumnDefinitions = new ColumnDefinitions("92,*,42"),
            Margin = new Thickness(16 * model.Zoom, 14 * model.Zoom, 14 * model.Zoom, 10 * model.Zoom),
            RowSpacing = 5 * model.Zoom,
            ColumnSpacing = 12 * model.Zoom
        };

        var avatar = new Border
        {
            Width = 74 * model.Zoom,
            Height = 74 * model.Zoom,
            CornerRadius = new CornerRadius(37 * model.Zoom),
            Background = Brushes.White,
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = model.AvatarGlyph,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                FontSize = 26 * model.Zoom,
                Foreground = Brushes.LightGray
            }
        };
        var nameText = new TextBlock
        {
            Text = model.DisplayName,
            Foreground = Brushes.Black,
            FontWeight = FontWeight.SemiBold,
            FontSize = 18 * model.Zoom,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 2
        };
        var birthText = new TextBlock
        {
            Text = model.BirthText,
            Foreground = Brushes.DimGray,
            FontSize = 15 * model.Zoom,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var deathText = new TextBlock
        {
            Text = model.DeathText,
            Foreground = Brushes.DimGray,
            FontSize = 15 * model.Zoom,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var editText = new TextBlock
        {
            Text = "✎",
            Foreground = Brushes.DimGray,
            FontSize = 18 * model.Zoom,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        editText.PointerPressed += (_, args) =>
        {
            args.Handled = true;
            onEdit();
        };
        var plusButton = new Button
        {
            Content = "+",
            Width = 70 * model.Zoom,
            Height = 34 * model.Zoom,
            Padding = new Thickness(0),
            FontSize = 22 * model.Zoom,
            Background = Brushes.White,
            BorderBrush = Brushes.Transparent,
            Foreground = Brushes.Black,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom
        };
        plusButton.Click += (_, args) =>
        {
            args.Handled = true;
            onAddRelative();
        };

        Grid.SetRowSpan(avatar, 4);
        Grid.SetColumn(avatar, 0);
        Grid.SetRow(nameText, 0);
        Grid.SetColumn(nameText, 1);
        Grid.SetColumnSpan(nameText, 2);
        Grid.SetRow(birthText, 1);
        Grid.SetColumn(birthText, 1);
        Grid.SetRow(deathText, 2);
        Grid.SetColumn(deathText, 1);
        Grid.SetRow(editText, 2);
        Grid.SetColumn(editText, 2);
        Grid.SetRow(plusButton, 3);
        Grid.SetColumn(plusButton, 1);

        grid.Children.Add(nameText);
        grid.Children.Add(avatar);
        grid.Children.Add(birthText);
        grid.Children.Add(deathText);
        grid.Children.Add(editText);
        grid.Children.Add(plusButton);
        card.Child = grid;
        return card;
    }
}
