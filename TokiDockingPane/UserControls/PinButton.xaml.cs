using DependencyPropertyGenerator;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TokiDockingPane.UserControls
{
    /// <summary>
    /// PinButton.xaml の相互作用ロジック
    /// </summary>
    /// 
     
    public partial class PinButton : UserControl
    {
        public static readonly DependencyProperty IsPinnedProperty =
            DependencyProperty.Register(nameof(IsPinned), typeof(bool), typeof(PinButton),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(PinButton), new PropertyMetadata(null));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(PinButton), new PropertyMetadata(null));

        public bool IsPinned
        {
            get => (bool)GetValue(IsPinnedProperty);
            set => SetValue(IsPinnedProperty, value);
        }

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }
 

        public PinButton()
        {
            InitializeComponent();
        }
    }

}
