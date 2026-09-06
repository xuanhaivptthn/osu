// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using System.Net.Http;
using Newtonsoft.Json;
using osu.Framework.IO.Network;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Rulesets.Difficulty;

namespace osu.Game.Online.API.Requests
{
    public class GetBeatmapAttributesRequest : APIRequest<BeatmapDifficultyAttributesResponse>
    {
        public readonly int BeatmapId;
        public readonly int? RulesetId;
        public readonly LegacyMods? Mods;

        public GetBeatmapAttributesRequest(int beatmapId, int? rulesetId = null, LegacyMods? mods = null)
        {
            BeatmapId = beatmapId;
            RulesetId = rulesetId;
            Mods = mods;
        }

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();
            req.Method = HttpMethod.Post;

            if (RulesetId.HasValue)
                req.AddParameter(@"ruleset_id", RulesetId.Value.ToString(CultureInfo.InvariantCulture));

            if (Mods.HasValue)
                req.AddParameter(@"mods", ((int)Mods.Value).ToString(CultureInfo.InvariantCulture));

            return req;
        }

        protected override string Target => $@"beatmaps/{BeatmapId}/attributes";
    }

    public class BeatmapDifficultyAttributesResponse
    {
        [JsonProperty("attributes")]
        public DifficultyAttributes? Attributes { get; set; }
    }
}
