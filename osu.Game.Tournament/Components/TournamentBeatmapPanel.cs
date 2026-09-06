// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics;
using osu.Game.Tournament.Models;
using osuTK.Graphics;

namespace osu.Game.Tournament.Components
{
    public partial class TournamentBeatmapPanel : CompositeDrawable
    {
        public readonly IBeatmapInfo? Beatmap;

        private readonly string mod;

        public const float HEIGHT = 50;

        private readonly Bindable<TournamentMatch?> currentMatch = new Bindable<TournamentMatch?>();

        private Box flash = null!;
        private Container protectBadge = null!;
        private Box protectBadgeBackground = null!;
        private TournamentSpriteText protectBadgeText = null!;

        public TournamentBeatmapPanel(IBeatmapInfo? beatmap, string mod = "")
        {
            Beatmap = beatmap;
            this.mod = mod;

            Width = 400;
            Height = HEIGHT;
        }

        [BackgroundDependencyLoader]
        private void load(LadderInfo ladder)
        {
            currentMatch.BindValueChanged(matchChanged);
            currentMatch.BindTo(ladder.CurrentMatch);

            Masking = true;

            AddRangeInternal(new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                },
                new NoUnloadBeatmapSetCover
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = OsuColour.Gray(0.5f),
                    OnlineInfo = (Beatmap as IBeatmapSetOnlineInfo),
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Padding = new MarginPadding(15),
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        new TournamentSpriteText
                        {
                            Text = Beatmap?.GetDisplayTitleRomanisable(false, false) ?? (LocalisableString)@"unknown",
                            Font = OsuFont.Torus.With(weight: FontWeight.Bold),
                        },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Children = new Drawable[]
                            {
                                new TournamentSpriteText
                                {
                                    Text = "mapper",
                                    Padding = new MarginPadding { Right = 5 },
                                    Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 14)
                                },
                                new TournamentSpriteText
                                {
                                    Text = Beatmap?.Metadata.Author.Username ?? "unknown",
                                    Padding = new MarginPadding { Right = 20 },
                                    Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 14)
                                },
                                new TournamentSpriteText
                                {
                                    Text = "difficulty",
                                    Padding = new MarginPadding { Right = 5 },
                                    Font = OsuFont.Torus.With(weight: FontWeight.Regular, size: 14)
                                },
                                new TournamentSpriteText
                                {
                                    Text = Beatmap?.DifficultyName ?? "unknown",
                                    Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 14)
                                },
                            }
                        }
                    },
                },
                flash = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Gray,
                    Blending = BlendingParameters.Additive,
                    Alpha = 0,
                },
            });

            if (!string.IsNullOrEmpty(mod))
            {
                AddInternal(new TournamentModIcon(mod)
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Margin = new MarginPadding(10),
                    Width = 60,
                    RelativeSizeAxes = Axes.Y,
                });
            }

            AddInternal(protectBadge = new Container
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Margin = new MarginPadding { Bottom = 6, Right = string.IsNullOrEmpty(mod) ? 6 : 74 },
                AutoSizeAxes = Axes.X,
                Height = 14,
                Alpha = 0,
                Children = new Drawable[]
                {
                    protectBadgeBackground = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.55f,
                    },
                    protectBadgeText = new TournamentSpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = "PROTECTED",
                        Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 10),
                        Colour = Color4.Black,
                        Padding = new MarginPadding { Horizontal = 4 },
                    },
                }
            });
        }

        private void matchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            if (match.OldValue != null)
                match.OldValue.PicksBans.CollectionChanged -= picksBansOnCollectionChanged;
            if (match.NewValue != null)
                match.NewValue.PicksBans.CollectionChanged += picksBansOnCollectionChanged;

            Scheduler.AddOnce(updateState);
        }

        private void picksBansOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => Scheduler.AddOnce(updateState);

        private BeatmapChoice? choice;

        private void updateState()
        {
            if (currentMatch.Value == null)
                return;

            var entries = currentMatch.Value.PicksBans.Where(p => p.BeatmapID == Beatmap?.OnlineID).ToList();

            // The protect badge is shown whenever there is a protect entry, regardless of other states.
            var protectEntry = entries.FirstOrDefault(p => p.Type == ChoiceType.Protect);
            bool isProtected = protectEntry != null;
            protectBadge.FadeTo(isProtected ? 1 : 0, 200);

            if (protectEntry != null)
            {
                Color4 protectColour = protectEntry.Team == TeamColour.Red ? TournamentGame.COLOUR_RED : TournamentGame.COLOUR_BLUE;
                protectBadgeBackground.Colour = protectColour;
                protectBadgeBackground.Alpha = 1f;

                string? teamAcronym = protectEntry.Team == TeamColour.Red
                    ? currentMatch.Value.Team1.Value?.Acronym.Value
                    : currentMatch.Value.Team2.Value?.Acronym.Value;

                string teamLabel = protectEntry.Team == TeamColour.Red ? "ORANGE" : "AQUA";

                protectBadgeText.Text = $"{teamLabel} PROTECT";
            }

            // For border/dim state, prefer pick/ban over protect when both exist.
            var newChoice = entries.FirstOrDefault(p => p.Type != ChoiceType.Protect) ?? entries.FirstOrDefault();

            bool shouldFlash = newChoice != choice;

            if (newChoice != null)
            {
                if (shouldFlash)
                    flash.FadeOutFromOne(500).Loop(0, 10);

                BorderThickness = 6;

                switch (newChoice.Type)
                {
                    case ChoiceType.Pick:
                        BorderColour = TournamentGame.GetTeamColour(newChoice.Team);
                        Colour = Color4.White;
                        Alpha = 1;
                        break;

                    case ChoiceType.Ban:
                        BorderColour = TournamentGame.GetTeamColour(newChoice.Team);
                        Colour = Color4.Gray;
                        Alpha = 0.5f;
                        break;

                    case ChoiceType.Protect:
                        BorderColour = TournamentGame.GetTeamColour(newChoice.Team);
                        Colour = Color4.White;
                        Alpha = 1;
                        break;
                }
            }
            else
            {
                Colour = Color4.White;
                BorderThickness = 0;
                Alpha = 1;
            }

            choice = newChoice;
        }

        private partial class NoUnloadBeatmapSetCover : UpdateableOnlineBeatmapSetCover
        {
            // As covers are displayed on stream, we want them to load as soon as possible.
            protected override double LoadDelay => 0;

            // Use DelayedLoadWrapper to avoid content unloading when switching away to another screen.
            protected override DelayedLoadWrapper CreateDelayedLoadWrapper(Func<Drawable> createContentFunc, double timeBeforeLoad)
                => new DelayedLoadWrapper(createContentFunc(), timeBeforeLoad);
        }
    }
}
