namespace ExBuddy.Helpers
{
    using ff14bot;
    using ff14bot.Enums;
    using System.Collections.Generic;

    internal static class Abilities
    {
        internal static uint GetAbilityId(this Ability ability)
        {
            return Map[Core.Player.CurrentJob][ability];
        }

        internal static readonly Dictionary<ClassJobType, Dictionary<Ability, uint>> Map =
            new Dictionary<ClassJobType, Dictionary<Ability, uint>>
            {
                {
                    ClassJobType.Botanist,
                    new Dictionary<Ability, uint>
                    {
                        {Ability.IncreaseGatherChance5, 218},
                        {Ability.IncreaseGatherChance15, 220},
                        {Ability.Sneak, 304},
                        {Ability.IncreaseGatherChance50, 294},
                        {Ability.Preparation, 213},
                        {Ability.IncreaseGatherQuality10, 225},
                        {Ability.IncreaseElementalGatherYield, 282},
                        {Ability.IncreaseGatherChanceOnce15, 4086},
                        {Ability.IncreaseGatherYieldOnce, 4087},
                        {Ability.AdditionalAttempt, 215},
                        {Ability.IncreaseGatherChanceQuality100, 216},
                        {Ability.IncreaseGatherYield, 222},
                        {Ability.IncreaseGatherQuality30, 226},
                        {Ability.IncreaseGatherYield2, 224},
                        {Ability.Truth, 221},
                        {Ability.CollectorsGlove, 4088},
                        {Ability.MethodicalAppraisal, 4089},
                        {Ability.DiscerningEye, 4092},
                        {Ability.InstinctualAppraisal, 4090},
                        {Ability.UtmostCaution, 4093},
                        {Ability.ImpulsiveAppraisal, 4091},
                        {Ability.Luck, 4095},
                        {Ability.SingleMind, 4098},
                        {Ability.IncreaseGatherQualityOnce10, 4096},
                        {Ability.IncreaseGatherQualityOnce20, 4097},
                        {Ability.ImpulsiveAppraisalII, 302},
                        {Ability.IncreaseGatherQuality30100, 271},
                        {Ability.IncreaseGatherQualityRandomOnce, 275},
                        {Ability.IncreaseGatherYieldOnce2, 273},
                        {Ability.Stickler, 4594},
                        {Ability.TheGivingLand, 4590},
                        {Ability.PickClean, 4588},
                        {Ability.Mind, 4606}
                    }
                },
                {
                    ClassJobType.Miner,
                    new Dictionary<Ability, uint>
                    {
                        {Ability.IncreaseGatherChance5, 235},
                        {Ability.IncreaseGatherChance15, 237},
                        {Ability.Sneak, 303},
                        {Ability.IncreaseGatherChance50, 295},
                        {Ability.Preparation, 230},
                        {Ability.IncreaseGatherQuality10, 242},
                        {Ability.IncreaseElementalGatherYield, 280},
                        {Ability.IncreaseGatherChanceOnce15, 4072},
                        {Ability.IncreaseGatherYieldOnce, 4073},
                        {Ability.AdditionalAttempt, 232},
                        {Ability.IncreaseGatherChanceQuality100, 233},
                        {Ability.IncreaseGatherYield, 239},
                        {Ability.IncreaseGatherQuality30, 243},
                        {Ability.IncreaseGatherYield2, 241},
                        {Ability.Truth, 238},
                        {Ability.CollectorsGlove, 4074},
                        {Ability.MethodicalAppraisal, 4075},
                        {Ability.DiscerningEye, 4078},
                        {Ability.InstinctualAppraisal, 4076},
                        {Ability.UtmostCaution, 4079},
                        {Ability.ImpulsiveAppraisal, 4077},
                        {Ability.Luck, 4081},
                        {Ability.SingleMind, 4084},
                        {Ability.IncreaseGatherQualityOnce10, 4082},
                        {Ability.IncreaseGatherQualityOnce20, 4083},
                        {Ability.ImpulsiveAppraisalII, 301},
                        {Ability.IncreaseGatherQuality30100, 270},
                        {Ability.IncreaseGatherQualityRandomOnce, 274},
                        {Ability.IncreaseGatherYieldOnce2, 272},
                        {Ability.Stickler, 4593},
                        {Ability.TheGivingLand, 4589},
                        {Ability.PickClean, 4587},
                        {Ability.Mind, 4605},
                    }
                },
                {
                    ClassJobType.Fisher,
                    new Dictionary<Ability, uint>
                    {
                        {Ability.Bait, 288},
                        {Ability.Cast, 289},
                        {Ability.Hook, 296},
                        {Ability.Quit, 299},
                        {Ability.CastLight, 2135},
                        {Ability.Sneak, 305},
                        {Ability.Release, 300},
                        {Ability.Mooch, 297},
                        {Ability.Snagging, 4100},
                        {Ability.CollectorsGlove, 4101},
                        {Ability.Patience, 4102},
                        {Ability.PowerfulHookset, 4103},
                        {Ability.Chum, 4104},
                        {Ability.PrecisionHookset, 4179},
                        {Ability.FishEyes, 4105},
                        {Ability.Patience2, 4106},
                        {Ability.SharkEye, 7904},
                        {Ability.Gig, 7632},
                        {Ability.GigHead, 7634},
                        {Ability.Mooch2, 268},
                        {Ability.VeteranTrade, 7906},
                        {Ability.CalmWaters, 7908},
                        {Ability.SharkEye2, 7905},
                        {Ability.Truth, 7911},
                        {Ability.DoubleHook, 269},
                        {Ability.Salvage, 7910},
                        {Ability.BountifulCatch, 7907},
                        {Ability.NaturesBounty, 7909},
                        {Ability.SurfaceSlap, 4595},
                        {Ability.IdenticalGig, 4591},
                        {Ability.IdenticalCast, 4596},


                    }
                }
            };
    }

    internal enum AbilityAura : short
    {
        None = -1,
        Sneak = 47,
        TruthOfForests = 221,
        TruthOfMountains = 222,
        DiscerningEye = 757,
        CollectorsGlove = 805,
        TruthOfOceans = 1173
    }

    internal enum Ability : byte
    {
        None,

        IncreaseGatherChance5, // 218, 235
        IncreaseGatherChance15, // 220, 235
        Sneak, // = 304, 303, 305
        IncreaseGatherChance50, // 294, 295
        Preparation, // 213, 230
        IncreaseGatherQuality10, // 225, 242
        IncreaseElementalGatherYield, // 282, 280
        IncreaseGatherChanceOnce15, // 4086, 4072
        IncreaseGatherYieldOnce, // 40887, 8073
        AdditionalAttempt, // 215, 232
        IncreaseGatherChanceQuality100, // 216, 233
        IncreaseGatherYield, // 222, 239
        IncreaseGatherQuality30, // 226, 243
        IncreaseGatherYield2, // 224, 241
        Truth, // 221, 238, 7911
        CollectorsGlove, // 4088, 4074, 4101
        MethodicalAppraisal, // 4089, 4075
        DiscerningEye, // 4092, 4078
        InstinctualAppraisal, // 4090, 4076
        UtmostCaution, // 4093, 4079
        ImpulsiveAppraisal, // 4091, 4077
        Luck, // 4095, 4081
        SingleMind, // 4098, 4084
        IncreaseGatherQualityOnce10, // 4096, 4082
        IncreaseGatherQualityOnce20, // 4097, 4083
        ImpulsiveAppraisalII, // 302, 301
        IncreaseGatherQuality30100, // 271, 270
        IncreaseGatherQualityRandomOnce, // 275, 274
        IncreaseGatherYieldOnce2, // 273, 272
        Stickler, // 4594, 4593
        TheGivingLand, // 4590, 4589
        PickClean, // 4588, 4587
        Mind, // 4606, 4605
        // Fisher
        Bait,
        Cast,
        Hook,
        Quit,
        CastLight,
        Release,
        Mooch,
        Snagging,
        Patience,
        PowerfulHookset,
        Chum,
        PrecisionHookset,
        FishEyes,
        Patience2,
        SharkEye,
        Gig,
        GigHead,
        Mooch2,
        VeteranTrade,
        CalmWaters,
        SharkEye2,
        DoubleHook,
        Salvage,
        BountifulCatch,
        NaturesBounty,
        SurfaceSlap,
        IdenticalGig,
        IdenticalCast,
    }
}