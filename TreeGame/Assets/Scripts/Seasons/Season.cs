using System;

namespace TreeGame.Seasons
{
    /// <summary>The four seasons, in the order a year goes through them. A new tree starts in spring.</summary>
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter,
    }

    /// <summary>Each season is split into thirds, so the player can tell how far into it they are.</summary>
    public enum SeasonPart
    {
        Early,
        Mid,
        Late,
    }

    /// <summary>What the seasons speed up or slow down, each by a multiplier (1 = as without seasons).</summary>
    public enum SeasonEffect
    {
        /// <summary>How fast the tree grows, normal and extra growth alike. 0 pauses growth, as in winter.</summary>
        Growth,
        WaterDrain,
        NutrientDrain,
        /// <summary>How often insects come.</summary>
        Insects,
        /// <summary>How often leaves fall.</summary>
        LeafFall,
        /// <summary>How fast the composter turns leaves into compost.</summary>
        Composting,
    }

    /// <summary>A value for each season, e.g. how fast water drains in it. Edited in the Inspector as part of the config.</summary>
    [Serializable]
    public class SeasonValues
    {
        public float spring;
        public float summer;
        public float autumn;
        public float winter;

        /// <summary>Every season the same as without seasons. Also what Unity uses to create one before filling it in.</summary>
        public SeasonValues() : this(1f, 1f, 1f, 1f)
        {
        }

        public SeasonValues(float spring, float summer, float autumn, float winter)
        {
            this.spring = spring;
            this.summer = summer;
            this.autumn = autumn;
            this.winter = winter;
        }

        public float For(Season season)
        {
            return season switch
            {
                Season.Spring => spring,
                Season.Summer => summer,
                Season.Autumn => autumn,
                Season.Winter => winter,
                _ => throw new ArgumentOutOfRangeException(nameof(season), season, null),
            };
        }
    }

    /// <summary>
    /// How the tree's leaves look at a moment of the year, each 0 to 1: how many leaves it has, how far they have turned
    /// to autumn colors, and how fresh and light their spring green still is.
    /// </summary>
    public readonly struct SeasonLook
    {
        public readonly float LeafCover;
        public readonly float Autumn;
        public readonly float Fresh;

        public SeasonLook(float leafCover, float autumn, float fresh)
        {
            LeafCover = leafCover;
            Autumn = autumn;
            Fresh = fresh;
        }

        /// <summary>A full summer crown: every leaf, plain green. What the tree looks like without seasons.</summary>
        public static SeasonLook FullLeaves => new SeasonLook(1f, 0f, 0f);
    }
}
