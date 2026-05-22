using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace KNARZHelper.Controls
{
    [ContentProperty("SettingControl")]
    public partial class SettingsOption : UserControl
    {
        //NEXT: The binding doesn't work. It shows the right value, but when you change it, it doesn't update the value of the property.
        //NEXT: Find out how to embed a control in a user control and bind to the properties of the user control. Maybe I need to set the DataContext of the user control to itself?

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(
                  "Description",
                   typeof(string),
                   typeof(SettingsOption));

        public static readonly DependencyProperty SettingControlProperty =
            DependencyProperty.Register("SettingControl", typeof(object), typeof(SettingsOption),
              new PropertyMetadata(null));

        public static readonly DependencyProperty TitleProperty =
                    DependencyProperty.Register(
                  "Title",
                   typeof(string),
                   typeof(SettingsOption));

        public SettingsOption()
        {
            InitializeComponent();
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public Visibility DescriptionVisibility => string.IsNullOrEmpty(Description) ? Visibility.Collapsed : Visibility.Visible;

        public object SettingControl
        {
            get => GetValue(SettingControlProperty);
            set => SetValue(SettingControlProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
    }
}
