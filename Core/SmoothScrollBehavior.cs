using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScrollV.Core
{
    public class SmoothScrollBehavior
    {
        private readonly ScrollViewer _scrollViewer;
        private readonly Stopwatch _stopwatch = new();
        private double _targetVerticalOffset;
        private double _currentVerticalOffset;
        private double _lastFrameTimeMs;
        private bool _isAnimating;

        private const double ScrollSpeed = 1.0;
        private const double SmoothFactor = 0.14;
        private const double StopThreshold = 0.5;

        public SmoothScrollBehavior(ScrollViewer scrollViewer)
        {
            _scrollViewer = scrollViewer ?? throw new ArgumentNullException(nameof(scrollViewer));
            _targetVerticalOffset = _scrollViewer.VerticalOffset;
            _currentVerticalOffset = _scrollViewer.VerticalOffset;

            _scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
            _scrollViewer.ScrollChanged += OnScrollChanged;
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            double delta = -e.Delta * ScrollSpeed;

            _targetVerticalOffset = Math.Max(0, Math.Min(
                _scrollViewer.ScrollableHeight,
                _targetVerticalOffset + delta));

            StartAnimation();
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!_isAnimating && Math.Abs(e.VerticalChange) > 0.1)
            {
                _targetVerticalOffset = _scrollViewer.VerticalOffset;
                _currentVerticalOffset = _scrollViewer.VerticalOffset;
            }
        }

        private void StartAnimation()
        {
            if (_isAnimating) return;

            _isAnimating = true;
            _currentVerticalOffset = _scrollViewer.VerticalOffset;
            _stopwatch.Restart();
            _lastFrameTimeMs = 0;
            CompositionTarget.Rendering += OnRendering;
        }

        private void StopAnimation()
        {
            if (!_isAnimating) return;
            _isAnimating = false;
            _stopwatch.Stop();
            CompositionTarget.Rendering -= OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            double nowMs = _stopwatch.Elapsed.TotalMilliseconds;
            double dtMs = nowMs - _lastFrameTimeMs;
            _lastFrameTimeMs = nowMs;

            double diff = _targetVerticalOffset - _currentVerticalOffset;
            if (Math.Abs(diff) < StopThreshold)
            {
                _currentVerticalOffset = _targetVerticalOffset;
                _scrollViewer.ScrollToVerticalOffset(_targetVerticalOffset);
                StopAnimation();
                return;
            }

            double timeFactor = dtMs > 0 ? Math.Min(dtMs / 16.67, 4.0) : 1.0;
            double stepFactor = 1.0 - Math.Pow(1.0 - SmoothFactor, timeFactor);
            _currentVerticalOffset += diff * stepFactor;
            _scrollViewer.ScrollToVerticalOffset(_currentVerticalOffset);
        }

        public void Detach()
        {
            StopAnimation();
            _scrollViewer.PreviewMouseWheel -= OnPreviewMouseWheel;
            _scrollViewer.ScrollChanged -= OnScrollChanged;
        }

        public static SmoothScrollBehavior Attach(ScrollViewer scrollViewer)
        {
            return new SmoothScrollBehavior(scrollViewer);
        }
    }
}
