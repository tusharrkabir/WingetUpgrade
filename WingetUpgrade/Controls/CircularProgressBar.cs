using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WingetUpgrade.Controls
{
    public class CircularProgressBar : Control
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(CircularProgressBar),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(CircularProgressBar),
                new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(CircularProgressBar),
                new FrameworkPropertyMetadata(6.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TrackBrushProperty =
            DependencyProperty.Register(nameof(TrackBrush), typeof(Brush), typeof(CircularProgressBar),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0x1E, 0x24, 0x2C)), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IndicatorBrushProperty =
            DependencyProperty.Register(nameof(IndicatorBrush), typeof(Brush), typeof(CircularProgressBar),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E)), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CenterTextProperty =
            DependencyProperty.Register(nameof(CenterText), typeof(string), typeof(CircularProgressBar),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public Brush TrackBrush
        {
            get => (Brush)GetValue(TrackBrushProperty);
            set => SetValue(TrackBrushProperty, value);
        }

        public Brush IndicatorBrush
        {
            get => (Brush)GetValue(IndicatorBrushProperty);
            set => SetValue(IndicatorBrushProperty, value);
        }

        public string CenterText
        {
            get => (string)GetValue(CenterTextProperty);
            set => SetValue(CenterTextProperty, value);
        }

        static CircularProgressBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CircularProgressBar),
                new FrameworkPropertyMetadata(typeof(CircularProgressBar)));
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double size = Math.Min(ActualWidth, ActualHeight);
            if (size <= 0) return;

            double thickness = StrokeThickness;
            double radius = (size - thickness) / 2.0;
            if (radius <= 0) return;

            Point center = new Point(ActualWidth / 2.0, ActualHeight / 2.0);

            // Draw full background track circle
            var trackPen = new Pen(TrackBrush, thickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            if (trackPen.CanFreeze) trackPen.Freeze();
            dc.DrawEllipse(null, trackPen, center, radius, radius);

            // Compute percentage
            double max = Maximum <= 0 ? 100.0 : Maximum;
            double pct = Math.Clamp(Value / max, 0.0, 1.0);

            if (pct > 0.001)
            {
                var indicatorPen = new Pen(IndicatorBrush, thickness)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round
                };
                if (indicatorPen.CanFreeze) indicatorPen.Freeze();

                if (pct >= 0.999)
                {
                    dc.DrawEllipse(null, indicatorPen, center, radius, radius);
                }
                else
                {
                    // Draw arc starting from top (-90 degrees)
                    double startAngle = -90.0;
                    double sweepAngle = pct * 360.0;
                    double endAngle = startAngle + sweepAngle;

                    Point startPoint = PointOnCircle(center, radius, startAngle);
                    Point endPoint = PointOnCircle(center, radius, endAngle);

                    var figure = new PathFigure
                    {
                        StartPoint = startPoint,
                        IsClosed = false
                    };

                    figure.Segments.Add(new ArcSegment
                    {
                        Point = endPoint,
                        Size = new Size(radius, radius),
                        SweepDirection = SweepDirection.Clockwise,
                        IsLargeArc = sweepAngle > 180.0
                    });

                    var geometry = new PathGeometry();
                    geometry.Figures.Add(figure);
                    if (geometry.CanFreeze) geometry.Freeze();
                    dc.DrawGeometry(null, indicatorPen, geometry);
                }
            }

            // Draw Center Text
            string text = !string.IsNullOrEmpty(CenterText) ? CenterText : $"{(int)Math.Round(pct * 100)}%";
            var formattedText = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                18,
                Foreground ?? Brushes.White,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );

            Point textLocation = new Point(
                center.X - formattedText.Width / 2.0,
                center.Y - formattedText.Height / 2.0
            );
            dc.DrawText(formattedText, textLocation);
        }

        private static Point PointOnCircle(Point center, double radius, double angleInDegrees)
        {
            double angleInRadians = angleInDegrees * Math.PI / 180.0;
            return new Point(
                center.X + radius * Math.Cos(angleInRadians),
                center.Y + radius * Math.Sin(angleInRadians)
            );
        }
    }
}
