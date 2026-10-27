using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace ScrollV.Core
{
    /// <summary>
    /// Engine that handles smooth scroll animation with high-precision timing
    /// </summary>
    public class ScrollVEngine : IDisposable
    {
        #region Win32 API

        [DllImport("winmm.dll")]
        private static extern uint timeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll")]
        private static extern uint timeEndPeriod(uint uMilliseconds);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_HWHEEL = 0x01000;

        #endregion

        private readonly object _stateLock = new();
        private Thread? _animationThread;
        private volatile bool _isRunning;

        private double _remainingScroll;
        private double _accumulatedDelta;
        private double _currentVelocity;
        private double _peakVelocity;
        private volatile bool _isScrolling;
        private bool _isHorizontal;
        private bool _isGliding;
        private bool _disposed;
        private DateTime _lastScrollTime = DateTime.MinValue;

        internal double RemainingScroll
        {
            get { lock (_stateLock) return _remainingScroll; }
        }

        // Scroll settings
        public double SmoothnessFactor { get; set; } = 0.05;
        public double AccelerationFactor { get; set; } = 1.2;
        public double FrictionFactor { get; set; } = 0.97;
        public double MinVelocityThreshold { get; set; } = 0.05;
        public EasingType EasingFunction { get; set; } = EasingType.EaseOutQuad;
        public bool UseAcceleration { get; set; } = true;
        public double MaxVelocity { get; set; } = 400;
        public bool IsEnabled { get; set; } = true;

        // Smooth stopping settings
        public double SmoothStopThreshold { get; set; } = 30;
        public double MinScrollStep { get; set; } = 0.3;

        // Momentum/Inertia settings
        public double MomentumFactor { get; set; } = 3.2;
        public double GlideDecay { get; set; } = 0.992;
        private const double GLIDE_TRIGGER_DELAY = 80;

        // Stall detection - handles hook chain delays (e.g., from IDM)
        private const double MAX_FRAME_TIME = 100;
        private const double STALL_RECOVERY_FACTOR = 0.5;

        public ScrollVEngine()
        {
            timeBeginPeriod(1);
            StartAnimationLoop();
        }

        private void StartAnimationLoop()
        {
            _isRunning = true;
            _animationThread = new Thread(AnimationLoop)
            {
                IsBackground = true,
                Name = "ScrollVAnimationThread",
                Priority = ThreadPriority.Highest
            };
            _animationThread.Start();
        }

        private void AnimationLoop()
        {
            var stopwatch = Stopwatch.StartNew();
            double lastFrameTime = stopwatch.Elapsed.TotalMilliseconds;

            while (_isRunning)
            {
                double currentTime = stopwatch.Elapsed.TotalMilliseconds;
                double deltaTime = currentTime - lastFrameTime;
                lastFrameTime = currentTime;

                if (_isScrolling && IsEnabled && deltaTime > 0)
                {
                    UpdateAnimation(deltaTime);
                    Thread.Sleep(1);
                }
                else
                {
                    Thread.Sleep(5);
                }
            }
        }

        private void UpdateAnimation(double deltaTime)
        {
            int intScroll = 0;
            bool isHorizontal = false;

            lock (_stateLock)
            {
                if (!_isScrolling || Math.Abs(_remainingScroll) < MinVelocityThreshold)
                {
                    StopScrollingLocked();
                    return;
                }

                if (deltaTime > MAX_FRAME_TIME)
                {
                    _remainingScroll *= STALL_RECOVERY_FACTOR;
                    _accumulatedDelta = 0;

                    if (Math.Abs(_remainingScroll) < MinVelocityThreshold)
                    {
                        StopScrollingLocked();
                        return;
                    }
                }

                double timeFactor = Math.Min(deltaTime / 8.33, 5.0);

                var timeSinceLastInput = (DateTime.UtcNow - _lastScrollTime).TotalMilliseconds;
                if (!_isGliding && timeSinceLastInput > GLIDE_TRIGGER_DELAY && Math.Abs(_peakVelocity) > 10)
                {
                    _isGliding = true;
                    double momentumBoost = Math.Sign(_remainingScroll) * Math.Abs(_peakVelocity) * MomentumFactor * 0.3;
                    _remainingScroll += momentumBoost;
                }

                double scrollAmount = CalculateAdaptiveScrollStep(timeFactor);
                _accumulatedDelta += scrollAmount;

                intScroll = (int)_accumulatedDelta;
                if (intScroll != 0)
                {
                    _accumulatedDelta -= intScroll;
                    isHorizontal = _isHorizontal;
                }

                _remainingScroll -= scrollAmount;

                double dynamicFriction = _isGliding ? CalculateGlideFriction() : CalculateDynamicFriction();
                double adjustedFriction = Math.Pow(dynamicFriction, timeFactor);
                _remainingScroll *= adjustedFriction;
            }

            if (intScroll != 0)
            {
                ApplyScroll(intScroll, isHorizontal);
            }
        }

        private double CalculateAdaptiveScrollStep(double timeFactor)
        {
            double absRemaining = Math.Abs(_remainingScroll);

            if (absRemaining < SmoothStopThreshold)
            {
                double progress = absRemaining / SmoothStopThreshold;
                double smoothFactor = (0.05 + progress * 0.1) * timeFactor;
                double step = _remainingScroll * smoothFactor;

                if (Math.Abs(step) < MinScrollStep * timeFactor && absRemaining > MinVelocityThreshold)
                {
                    step = Math.Sign(_remainingScroll) * MinScrollStep * timeFactor;
                }
                return step;
            }

            double stepFactor = 1 - Math.Pow(1 - SmoothnessFactor, timeFactor);
            double easedT = ApplyEasing(stepFactor);
            return _remainingScroll * easedT;
        }

        public void AddScrollDelta(double delta, bool isHorizontal)
        {
            if (!IsEnabled) return;

            lock (_stateLock)
            {
                if (_isHorizontal != isHorizontal)
                {
                    _remainingScroll = 0;
                    _accumulatedDelta = 0;
                    _peakVelocity = 0;
                    _isHorizontal = isHorizontal;
                }

                _isGliding = false;

                double acceleratedDelta = delta * AccelerationFactor;

                if (UseAcceleration)
                {
                    var timeSinceLastScroll = (DateTime.UtcNow - _lastScrollTime).TotalMilliseconds;
                    if (timeSinceLastScroll < 150)
                    {
                        double accelerationBoost = 1.0 + Math.Min(1.5, (150 - timeSinceLastScroll) / 100.0);
                        acceleratedDelta *= accelerationBoost;
                    }
                }

                _lastScrollTime = DateTime.UtcNow;

                if (Math.Sign(_remainingScroll) == Math.Sign(acceleratedDelta) || Math.Abs(_remainingScroll) < 1)
                {
                    _remainingScroll += acceleratedDelta;
                }
                else
                {
                    _remainingScroll = acceleratedDelta;
                    _accumulatedDelta = 0;
                    _peakVelocity = 0;
                }

                _currentVelocity = acceleratedDelta;
                if (Math.Abs(_currentVelocity) > Math.Abs(_peakVelocity))
                {
                    _peakVelocity = _currentVelocity;
                }

                _remainingScroll = Math.Clamp(_remainingScroll, -MaxVelocity * 15, MaxVelocity * 15);
                _isScrolling = true;
            }
        }

        private double CalculateGlideFriction()
        {
            double absRemaining = Math.Abs(_remainingScroll);
            if (absRemaining < SmoothStopThreshold * 2)
            {
                double progress = absRemaining / (SmoothStopThreshold * 2);
                return GlideDecay - (1 - progress) * 0.02;
            }
            return GlideDecay;
        }

        private double CalculateDynamicFriction()
        {
            double absRemaining = Math.Abs(_remainingScroll);
            if (absRemaining < SmoothStopThreshold)
            {
                double progress = 1 - (absRemaining / SmoothStopThreshold);
                return FrictionFactor + (0.98 - FrictionFactor) * progress;
            }
            return FrictionFactor;
        }

        private double ApplyEasing(double t)
        {
            return EasingFunction switch
            {
                EasingType.Linear => t,
                EasingType.EaseOutQuad => t * (2 - t),
                EasingType.EaseOutCubic => 1 - Math.Pow(1 - t, 3),
                EasingType.EaseOutExpo => t == 1 ? 1 : 1 - Math.Pow(2, -10 * t),
                EasingType.EaseOutCirc => Math.Sqrt(1 - Math.Pow(t - 1, 2)),
                EasingType.EaseInOutQuad => t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2,
                EasingType.EaseOutElastic => t == 0 ? 0 : t == 1 ? 1 :
                    Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * (2 * Math.PI / 3)) + 1,
                EasingType.EaseOutBack => 1 + 2.70158 * Math.Pow(t - 1, 3) + 1.70158 * Math.Pow(t - 1, 2),
                _ => t * (2 - t)
            };
        }

        private static void ApplyScroll(int delta, bool isHorizontal)
        {
            if (delta == 0) return;
            uint flags = isHorizontal ? MOUSEEVENTF_HWHEEL : MOUSEEVENTF_WHEEL;
            mouse_event(flags, 0, 0, delta, MouseHook.ScrollSignature);
        }

        private void StopScrollingLocked()
        {
            _isScrolling = false;
            _remainingScroll = 0;
            _accumulatedDelta = 0;
            _currentVelocity = 0;
            _peakVelocity = 0;
            _isGliding = false;
        }

        public void Reset()
        {
            lock (_stateLock)
            {
                StopScrollingLocked();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _isRunning = false;
            _animationThread?.Join(100);
            timeEndPeriod(1);
            _disposed = true;
        }
    }

    public enum EasingType
    {
        Linear,
        EaseOutQuad,
        EaseOutCubic,
        EaseOutExpo,
        EaseOutCirc,
        EaseInOutQuad,
        EaseOutElastic,
        EaseOutBack
    }
}
