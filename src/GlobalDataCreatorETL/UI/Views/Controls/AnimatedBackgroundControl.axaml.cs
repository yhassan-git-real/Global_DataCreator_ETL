using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using System;

namespace GlobalDataCreatorETL.UI.Views.Controls;

/// <summary>
/// Cosmic animated overlay (rendered on top at Opacity=0.13, IsHitTestVisible=False).
/// Three layers:
///   1. Nebula orbs     — large slow-drifting radial-gradient blobs
///   2. Twinkling stars — small dots that pulse opacity independently
///   3. Shooting stars  — brief lit streaks that fire every few seconds
/// </summary>
public sealed partial class AnimatedBackgroundControl : UserControl
{
    // ── data records ─────────────────────────────────────────────────────────
    private sealed record Orb(Ellipse El,
        double BaseX, double BaseY,
        double PhaseX, double PhaseY,
        double SpdX, double SpdY,
        double AmpX, double AmpY);

    private sealed record Star(Ellipse El,
        double NormX, double NormY,
        double Phase, double TwinkleSpeed,
        byte   BaseAlpha);

    private sealed class Shooter
    {
        public Line   Line   = null!;
        public double X,  Y, NormDx, NormDy;
        public double Life, MaxLife;
        public bool   Active;
    }

    // ── nebula orb palette ───────────────────────────────────────────────────
    private static readonly (string Hex, double Size, byte Alpha)[] OrbDefs =
    [
        ("#4A8EE8", 850, 200),   // sky-blue
        ("#7C3AED", 680, 185),   // violet
        ("#1C50C0", 1050, 165),  // deep navy (full-width ambient)
        ("#5B9BF5", 620, 195),   // bright blue
        ("#5B21B6", 780, 175),   // indigo
    ];

    private Orb[]     _orbs = null!;
    private Star[]    _stars = null!;
    private Shooter[] _shooters = null!;
    private readonly Random    _rng     = new(42);
    private double             _t;
    private double             _nextShot;

    public AnimatedBackgroundControl()
    {
        InitializeComponent();
        BuildOrbs();
        BuildStars();
        BuildShooters();
        _nextShot = 2.5 + _rng.NextDouble() * 3.0;

        var timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(33)   // ~30 fps
        };
        timer.Tick += Tick;
        timer.Start();
        Unloaded += (_, _) => timer.Stop();
    }

    // ── builders ─────────────────────────────────────────────────────────────

    private void BuildOrbs()
    {
        _orbs = new Orb[OrbDefs.Length];
        for (int i = 0; i < OrbDefs.Length; i++)
        {
            var (hex, size, alpha) = OrbDefs[i];
            var c     = Color.Parse(hex);
            var brush = new RadialGradientBrush();
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(alpha, c.R, c.G, c.B), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0,     c.R, c.G, c.B), 1.0));

            var el = new Ellipse { Width = size, Height = size, Fill = brush };
            _orbs[i] = new Orb(el,
                BaseX:  0.15 + _rng.NextDouble() * 0.70,
                BaseY:  0.15 + _rng.NextDouble() * 0.70,
                PhaseX: _rng.NextDouble() * MathF.PI * 2,
                PhaseY: _rng.NextDouble() * MathF.PI * 2,
                SpdX:   0.05  + _rng.NextDouble() * 0.08,
                SpdY:   0.04  + _rng.NextDouble() * 0.07,
                AmpX:   140   + _rng.NextDouble() * 200,
                AmpY:   110   + _rng.NextDouble() * 160);
            PART_Canvas.Children.Add(el);
        }
    }

    private void BuildStars()
    {
        _stars = new Star[90];
        for (int i = 0; i < _stars.Length; i++)
        {
            double sz    = 0.8  + _rng.NextDouble() * 2.8;
            byte   alpha = (byte)(70 + _rng.Next(160));
            var    el    = new Ellipse
            {
                Width  = sz,
                Height = sz,
                Fill   = new SolidColorBrush(Color.FromArgb(alpha, 190, 220, 255))
            };
            _stars[i] = new Star(el,
                NormX:        _rng.NextDouble(),
                NormY:        _rng.NextDouble(),
                Phase:        _rng.NextDouble() * MathF.PI * 2,
                TwinkleSpeed: 0.3 + _rng.NextDouble() * 1.5,
                BaseAlpha:    alpha);
            PART_Canvas.Children.Add(el);
        }
    }

    private void BuildShooters()
    {
        _shooters = new Shooter[3];
        for (int i = 0; i < _shooters.Length; i++)
        {
            var line = new Line
            {
                StrokeThickness = 1.4,
                Stroke  = new SolidColorBrush(Color.FromArgb(230, 200, 230, 255)),
                Opacity = 0
            };
            _shooters[i] = new Shooter { Line = line };
            PART_Canvas.Children.Add(line);
        }
    }

    // ── tick ─────────────────────────────────────────────────────────────────

    private void Tick(object? sender, EventArgs e)
    {
        _t += 0.033;

        double w = PART_Canvas.Bounds.Width;
        double h = PART_Canvas.Bounds.Height;
        if (w <= 0 || h <= 0) return;

        TickOrbs(w, h);
        TickStars(w, h);
        TickShooters(w, h);
    }

    private void TickOrbs(double w, double h)
    {
        foreach (var o in _orbs)
        {
            double cx = o.BaseX * w + Math.Sin(_t * o.SpdX + o.PhaseX) * o.AmpX;
            double cy = o.BaseY * h + Math.Cos(_t * o.SpdY + o.PhaseY) * o.AmpY;
            Canvas.SetLeft(o.El, cx - o.El.Width  / 2);
            Canvas.SetTop (o.El, cy - o.El.Height / 2);
        }
    }

    private void TickStars(double w, double h)
    {
        foreach (var s in _stars)
        {
            // each star pulses between 20% and 100% of its base alpha
            double t = 0.5 + 0.5 * Math.Sin(_t * s.TwinkleSpeed + s.Phase);
            s.El.Opacity = 0.20 + 0.80 * t;
            Canvas.SetLeft(s.El, s.NormX * w);
            Canvas.SetTop (s.El, s.NormY * h);
        }
    }

    private void TickShooters(double w, double h)
    {
        // spawn if due
        if (_t >= _nextShot)
        {
            TrySpawn(w, h);
            _nextShot = _t + 2.0 + _rng.NextDouble() * 4.0;
        }

        foreach (var ss in _shooters)
        {
            if (!ss.Active) continue;
            ss.Life += 0.033;

            if (ss.Life >= ss.MaxLife)
            {
                ss.Active = false;
                ss.Line.Opacity = 0;
                continue;
            }

            double prog    = ss.Life / ss.MaxLife;
            // fade in fast, hold, fade out
            double fadeIn  = Math.Min(1.0, prog  * 5.0);
            double fadeOut = Math.Max(0.0, 1.0 - Math.Max(0.0, prog - 0.55) / 0.45);
            ss.Line.Opacity = fadeIn * fadeOut;

            // advance head position
            double speed  = 320.0;
            double tailLen = 50 + 140 * prog;
            ss.X += ss.NormDx * speed * 0.033;
            ss.Y += ss.NormDy * speed * 0.033;

            ss.Line.StartPoint = new Point(ss.X - ss.NormDx * tailLen,
                                           ss.Y - ss.NormDy * tailLen);
            ss.Line.EndPoint   = new Point(ss.X, ss.Y);
        }
    }

    private void TrySpawn(double w, double h)
    {
        foreach (var ss in _shooters)
        {
            if (ss.Active) continue;

            // angle: 15-40° below horizontal, always left→right
            double deg = 15 + _rng.NextDouble() * 25;
            double rad = deg * Math.PI / 180.0;
            ss.NormDx = Math.Cos(rad);
            ss.NormDy = Math.Sin(rad);

            // start left half of screen, upper 70%
            ss.X      = _rng.NextDouble() * w * 0.55;
            ss.Y      = _rng.NextDouble() * h * 0.70;
            ss.Life   = 0;
            ss.MaxLife = 0.7 + _rng.NextDouble() * 0.5;
            ss.Active = true;
            break;
        }
    }
}
