using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OctoFixFlow
{
    public partial class NotificationControl : UserControl
    {
        public enum NotificationType
        {
            Info,
            Warn,
            Error
        }

        private readonly DoubleAnimation _slideInAnimation;
        private readonly DoubleAnimation _slideOutAnimation;

        public NotificationControl(string message, NotificationType type, int duration = 3000)
        {
            InitializeComponent();

            MessageText.Text = message;
            ApplyNotificationStyle(type);

            // 设置动画
            _slideInAnimation = new DoubleAnimation
            {
                From = 400,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            _slideOutAnimation = new DoubleAnimation
            {
                From = 0,
                To = 400,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            // 应用滑入动画
            var transform = new TranslateTransform();
            RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.XProperty, _slideInAnimation);

            // 设置自动关闭
            if (duration > 0)
            {
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(duration)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    Close();
                };
                timer.Start();
            }
        }

        private void ApplyNotificationStyle(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Info:
                    NotificationBorder.Style = (Style)FindResource("InfoStyle");
                    IconPath.Data = (Geometry)FindResource("InfoIcon");
                    break;
                case NotificationType.Warn:
                    NotificationBorder.Style = (Style)FindResource("WarnStyle");
                    IconPath.Data = (Geometry)FindResource("WarnIcon");
                    break;
                case NotificationType.Error:
                    NotificationBorder.Style = (Style)FindResource("ErrorStyle");
                    IconPath.Data = (Geometry)FindResource("ErrorIcon");
                    break;
            }
        }

        public void Close()
        {
            _slideOutAnimation.Completed += (s, e) =>
            {
                if (Parent is Panel panel)
                {
                    panel.Children.Remove(this);
                }
            };

            RenderTransform.BeginAnimation(TranslateTransform.XProperty, _slideOutAnimation);
        }
    }
}