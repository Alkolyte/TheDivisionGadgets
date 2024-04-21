using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using RimWorld;
using HarmonyLib;
using Verse.AI.Group;
using Verse.Sound;
using System.Linq;
using Verse.AI;
using System.Reflection;

namespace TheDivisionGadgets
{
    public class TheDivisionGadgetsSettings : ModSettings
    {
        /// <summary>
        /// The settings our mod has.
        /// </summary>
        ///public bool isBuildable;
        public bool hasBattery = true;
        public bool enemyPacks = false;
        public bool enemySingles = true;
        public bool wipeDefaultFaction = false;


        /// <summary>
        /// The part that writes our settings to file. Note that saving is by ref.
        /// </summary>
        public override void ExposeData()
        {
            ///Scribe_Values.Look(ref isBuildable, "isBuildable");
            Scribe_Values.Look(ref hasBattery, "hasBattery");
            Scribe_Values.Look(ref enemyPacks, "enemyPacks");
            Scribe_Values.Look(ref enemySingles, "enemySingles");
            Scribe_Values.Look(ref wipeDefaultFaction, "wipeDefaultFaction");
            base.ExposeData();
        }
    }
    [StaticConstructorOnStartup]
    public class TheDivisionGadgets : Mod
    {
        /// <summary>
        /// A reference to our settings.
        /// </summary>
        TheDivisionGadgetsSettings settings;

        /// <summary>
        /// A mandatory constructor which resolves the reference to our settings.
        /// </summary>
        /// <param name="content"></param>
        public TheDivisionGadgets(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<TheDivisionGadgetsSettings>();
            var harmony = new Harmony("Alkolyte.TheDivisionGadgets");
            harmony.PatchAll();
        }

        [StaticConstructorOnStartup] // this makes the static constructor get called AFTER defs are loaded
        public static class OnDefsLoaded
        {
            static OnDefsLoaded()
            {
                // apply settings to defs now that defs are loaded:
                ApplySettingsToDefs();
            }
            public static void ApplySettingsToDefs()
            {  // a public static method that can be called from anywhere
                String[] packnames = new String[4] {"TDG_Apparel_SeekerBelt", "TDG_Apparel_IncendiaryBelt", "TDG_Apparel_ClusterBelt", "TDG_Apparel_StrikerBelt"};
                String[] singlenames = new String[4] { "TDG_Apparel_SeekerSingle", "TDG_Apparel_IncendiarySingle", "TDG_Apparel_ClusterSingle", "TDG_Apparel_StrikerSingle" };
                String[] dronenames = new String[5] { "TDG_Seeker", "TDG_Incendiary", "TDG_Cluster", "TDG_Bomblet", "TDG_Striker" };
                bool enemyPacks = LoadedModManager.GetMod<TheDivisionGadgets>().GetSettings<TheDivisionGadgetsSettings>().enemyPacks;
                if (!enemyPacks)
                {
                    for (int i = 0; i < packnames.Length; i++)
                    {
                        DefDatabase<ThingDef>.GetNamed(packnames[i]).apparel.tags.Clear();
                    }
                }
                bool enemySingles = LoadedModManager.GetMod<TheDivisionGadgets>().GetSettings<TheDivisionGadgetsSettings>().enemySingles;
                if (!enemySingles)
                {
                    for (int i = 0; i < packnames.Length; i++)
                    {
                        DefDatabase<ThingDef>.GetNamed(singlenames[i]).apparel.tags.Clear();
                    }
                }
                bool wipeDefaultFaction = LoadedModManager.GetMod<TheDivisionGadgets>().GetSettings<TheDivisionGadgetsSettings>().wipeDefaultFaction;
                if (wipeDefaultFaction)
                {
                    for (int i = 0; i < dronenames.Length; i++)
                    {
                        DefDatabase<PawnKindDef>.GetNamed(dronenames[i]).defaultFactionType = null;
                    }
                }


            }
        }
        /// <summary>
        /// The (optional) GUI part to set your settings.
        /// </summary>
        /// <param //name="inRect">A Unity Rect with the size of the settings window.</param>

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("Drones have lifespan", ref settings.hasBattery, "only applies to deployed drones from packs and turrets, default true");
            listingStandard.Label("Settings below require restart");
            listingStandard.CheckboxLabeled("Drone belts appear on enemies", ref settings.enemyPacks, "default false");
            listingStandard.CheckboxLabeled("Single drones appear on enemies", ref settings.enemySingles, "default true");
            listingStandard.CheckboxLabeled("Remove default faction of drones", ref settings.wipeDefaultFaction, "drones are by default hostile ancients, won't affect already generated drones, default false");
            listingStandard.End();
            OnDefsLoaded.ApplySettingsToDefs();
            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// Override SettingsCategory to show up in the list of settings.
        /// Using .Translate() is optional, but does allow for localisation.
        /// </summary>
        /// <returns>The (translated) mod name.</returns>
        public override string SettingsCategory()
        {
            return "The Division Gadgets";
        }

    }

    // Death On Downed
    public class DeathOnDowned : ThingComp
    {
        public CompProperties_DeathOnDowned Props
        {
            get
            {
                return (CompProperties_DeathOnDowned)this.props;
            }
        }
    }

    public class CompProperties_DeathOnDowned : CompProperties
    {
        // public float DeathChance;
        // (<DeathChance>###</DeathChance>)

        public CompProperties_DeathOnDowned()
        {
            // (redundancy) - this.compClass = typeof(DeathOnDownedChance);
            compClass = typeof(DeathOnDowned);
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    class DownedPatch
    {
        public static bool Prefix(Pawn_HealthTracker __instance, DamageInfo? dinfo, Hediff hediff, Pawn ___pawn)
        {
            DeathOnDowned comp = ___pawn.GetComp<DeathOnDowned>();
            if (comp != null & !___pawn.Dead)
            {
                ___pawn.Kill(dinfo, null);
                return false;
                //if (___pawn.Downed) // (Rand.Chance(comp.Props.DeathChance ) && ___pawn.Downed)
                //{
                //    ___pawn.Kill(dinfo, null);
                //    Log.Warning("hello3");
                //    return false;
                //}
            }
            return true;
        }
    }

    // For is you want the relations object to be created on spawn <laggy probably>
    [HarmonyPatch(typeof(PawnComponentsUtility), "CreateInitialComponents")]
    class AddFields
    {
        public static bool Prefix(Pawn pawn)
        {
            DeathOnDowned comp = pawn.GetComp<DeathOnDowned>();
            if (comp != null)
            {
                //            if (pawn.relations == null)
                //            {
                //                pawn.relations = new Pawn_RelationsTracker(pawn);
                //            }
                if (pawn.drafter == null)
                {
                    pawn.drafter = new Pawn_DraftController(pawn);
                }
            }
            return true;
        }
    }

    // if you just want to suppress the error, instead of doing this hacky way of adding it in the prefix, 
    // you can use a finalizer patch and it will wrap the original method in a try catch, and you can handle the exception yourself
    // The current fix causes melee pawns to freeze (maybe set up relations on spawn)
    [HarmonyPatch(typeof(Pawn_HealthTracker), "NotifyPlayerOfKilled")] class DeathLetterPatch
    {
        public static bool Prefix(Pawn ___pawn)
        {
            DeathOnDowned comp = ___pawn.GetComp<DeathOnDowned>();
            if (comp != null)
            {
                if (___pawn.relations == null)
                {
                    ___pawn.relations = new Pawn_RelationsTracker(___pawn);
                }
                ___pawn.SetFaction(null, null);
            }
            return true;
        }
    }

    // Explosive Melee

    public class ExplosiveMelee : ThingComp
    {
        public CompProperties_ExplosiveMelee Props
        {
            get
            {
                return (CompProperties_ExplosiveMelee)this.props;
            }
        }
    }

    public class CompProperties_ExplosiveMelee : CompProperties
    {
        // (<DeathChance>###</DeathChance>)
        public float radius;
        public int damageAmount;
        public DamageDef damageDef;
        public float armorPenetration;
        public SoundDef soundExplode;
        public ThingDef postExplosionSpawnThingDef;
        public float postExplosionSpawnChance = 0f;
        public int postExplosionSpawnThingCount = 1;
        public ThingDef preExplosionSpawnThingDef;
        public float preExplosionSpawnChance = 0f;
        public int preExplosionSpawnThingCount = 1;
        public float explosionChanceToStartFire = 0f;
        public bool explosionDamageFalloff;

        public CompProperties_ExplosiveMelee()
        {
            // (redundancy) - this.compClass = typeof(DeathOnDownedChance);
            compClass = typeof(ExplosiveMelee);
        }
    }

    [HarmonyPatch(typeof(Verb_MeleeAttack), "TryCastShot")]
    class ExplosiveMeleePatch
    {
        public static void Postfix(Verb_MeleeAttack __instance, ref bool __result)
        {
            Pawn casterPawn = __instance.CasterPawn;
            ExplosiveMelee comp = casterPawn.GetComp<ExplosiveMelee>();
            if (comp != null)
            {
                GenExplosion.DoExplosion(casterPawn.Position, casterPawn.Map, comp.Props.radius, comp.Props.damageDef, casterPawn, comp.Props.damageAmount, comp.Props.armorPenetration, comp.Props.soundExplode, null, null, null, comp.Props.postExplosionSpawnThingDef, comp.Props.postExplosionSpawnChance, comp.Props.postExplosionSpawnThingCount, null, false, comp.Props.preExplosionSpawnThingDef, comp.Props.preExplosionSpawnChance, comp.Props.preExplosionSpawnThingCount, comp.Props.explosionChanceToStartFire, comp.Props.explosionDamageFalloff = false, null, null);
                casterPawn.health.AddHediff(HediffDefOf.TDG_Destroyed);
                //casterPawn.DeSpawn();
                //casterPawn.Destroy();
                __result = true;
            }
        }
    }

    // Queue Destroy
    public class HediffCompProperties_QueueDestroy : HediffCompProperties
    {
        public HediffCompProperties_QueueDestroy()
        {
            this.compClass = typeof(HediffComp_QueueDestroy);
        }
        public bool destroyBody;
        public bool clearFaction;
    }

    public class HediffComp_QueueDestroy : HediffComp
    {
        public HediffCompProperties_QueueDestroy Props
        {
            get
            {
                return (HediffCompProperties_QueueDestroy)this.props;
            }
        }

        public override void CompPostMake()
        {
            {
                if (this.Props.clearFaction)
                {
                    base.Pawn.SetFaction(null, null);
                }
            }
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            if (this.Props.destroyBody)
            {
                base.Pawn.Corpse.Destroy(DestroyMode.Vanish);
            }
        }
    }

    // Hediff Defs
    [DefOf]
    public static class HediffDefOf
    {
        static HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HediffDefOf));
        }

        public static HediffDef TDG_Destroyed;
        public static HediffDef TDG_Battery;
    }

    // Projectile Pawn
    public class Projectile_Pawn_Comp : ThingComp
    {
        public CompProperties_Projectile_Pawn_Comp Props
        {
            get
            {
                return (CompProperties_Projectile_Pawn_Comp)this.props;
            }
        }
    }

    public class CompProperties_Projectile_Pawn_Comp : CompProperties
    {
        public string pawnType;

        public CompProperties_Projectile_Pawn_Comp()
        {
            // (redundancy) - this.compClass = typeof(DeathOnDownedChance);
            compClass = typeof(Projectile_Pawn_Comp);
        }
    }

    public class Projectile_Pawn : Projectile_Explosive
    {

        public Faction faction;

        public override void Tick()
        {
            if (this.faction == null)
            {
                this.faction = launcher.Faction;
            }
            base.Tick();
        }
        
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            IntVec3 position = base.Position;
            Projectile_Pawn_Comp comp = this.GetComp<Projectile_Pawn_Comp>();
            if (this.faction == null)
            {
                this.faction = FactionUtility.DefaultFactionFrom(PawnKindDef.Named(comp.Props.pawnType).defaultFactionType);
            }
            Pawn pawn = GenSpawn.Spawn(PawnGenerator.GeneratePawn(PawnKindDef.Named(comp.Props.pawnType), this.faction), position, map, WipeMode.Vanish) as Pawn;
            pawn.pather.TryRecoverFromUnwalkablePosition(false);
            bool hasBattery = LoadedModManager.GetMod<TheDivisionGadgets>().GetSettings<TheDivisionGadgetsSettings>().hasBattery;
            if (hasBattery)
            {
                pawn.health.AddHediff(HediffDefOf.TDG_Battery);
            }
            if (this.faction == null)
            {
                pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, null, false, false, false, null, false, false);
            }
            Lord lord = this.Lord;
            if (lord == null)
            {
                lord = Projectile_Pawn.CreateNewLord(pawn, true, 0, typeof(LordJob_AssaultColony));
            }
            lord.AddPawn(pawn);
            GenClamor.DoClamor(this, 2.1f, ClamorDefOf.Impact);
            SoundDefOf.BulletImpact_Ground.PlayOneShot(new TargetInfo(position, map, false));
            if (position.GetTerrain(map).takeSplashes)
            {
                FleckMaker.WaterSplash(this.ExactPosition, map, Mathf.Sqrt((float)base.DamageAmount) * 1f, 4f);
            }
            this.Destroy(DestroyMode.Vanish);
        }

        public Lord Lord
        {
            get
            {
                return Projectile_Pawn.FindLordToJoin(this.Launcher, typeof(LordJob_AssaultColony), true, null);
            }
        }

        public static Lord FindLordToJoin(Thing spawner, Type lordJobType, bool shouldTryJoinParentLord, Func<Thing, List<Pawn>> spawnedPawnSelector = null)
        {
            if (spawner.Spawned)
            {
                if (shouldTryJoinParentLord)
                {
                    Pawn pawn = spawner as Pawn;
                    Lord lord = (pawn != null) ? pawn.GetLord() : null;
                    if (lord != null)
                    {
                        return lord;
                    }
                }
                if (spawnedPawnSelector == null)
                {
                    spawnedPawnSelector = delegate (Thing s)
                    {
                        CompSpawnerPawn compSpawnerPawn = s.TryGetComp<CompSpawnerPawn>();
                        if (compSpawnerPawn != null)
                        {
                            return compSpawnerPawn.spawnedPawns;
                        }
                        return null;
                    };
                }
                Predicate<Pawn> hasJob = delegate (Pawn x)
                {
                    Lord lord2 = x.GetLord();
                    return lord2 != null && lord2.LordJob.GetType() == lordJobType;
                };
                Pawn foundPawn = null;
                RegionTraverser.BreadthFirstTraverse(spawner.GetRegion(RegionType.Set_Passable), (Region from, Region to) => true, delegate (Region r)
                {
                    List<Thing> list = r.ListerThings.ThingsOfDef(spawner.def);
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].Faction == spawner.Faction)
                        {
                            List<Pawn> list2 = spawnedPawnSelector(list[i]);
                            if (list2 != null)
                            {
                                foundPawn = list2.Find(hasJob);
                            }
                            if (foundPawn != null)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }, 40, RegionType.Set_Passable);
                if (foundPawn != null)
                {
                    return foundPawn.GetLord();
                }
            }
            return null;
        }

        public static Lord CreateNewLord(Thing byThing, bool aggressive, float defendRadius, Type lordJobType)
        {
            IntVec3 invalid;
            if (!CellFinder.TryFindRandomCellNear(byThing.Position, byThing.Map, 5, (IntVec3 c) => c.Standable(byThing.Map) && byThing.Map.reachability.CanReach(c, byThing, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false, false, false)), out invalid, -1))
            {
                invalid = IntVec3.Invalid;
            }
            return LordMaker.MakeNewLord(byThing.Faction, Activator.CreateInstance(lordJobType, new object[]
            {
                new SpawnedPawnParams
                {
                    aggressive = aggressive,
                    defendRadius = defendRadius,
                    defSpot = invalid,
                    spawnerThing = byThing
                }
            }) as LordJob, byThing.Map, null);
        }
    }

    public class Projectile_InstaPawn : Projectile_Pawn
    {
        public override void Tick()
        {
            base.Tick();
            this.Impact(null);
        }
    }

    // Verb_Scatter
    public class Verb_Scatter : Verb_LaunchProjectile
    {
        protected override int ShotsPerBurst
        {
            get
            {
                return this.verbProps.burstShotCount;
            }
        }

        public override void WarmupComplete()
        {
            base.WarmupComplete();
            Pawn pawn = this.currentTarget.Thing as Pawn;
            if (pawn != null && !pawn.Downed && this.CasterIsPawn && this.CasterPawn.skills != null)
            {
                float num = pawn.HostileTo(this.caster) ? 170f : 20f;
                float num2 = this.verbProps.AdjustedFullCycleTime(this, this.CasterPawn);
            }
        }
        protected override bool TryCastShot()
        {
            if (this.currentTarget.HasThing && this.currentTarget.Thing.Map != this.caster.Map)
            {
                return false;
            }
            ThingDef projectile = this.Projectile;
            if (projectile == null)
            {
                return false;
            }
            ShootLine shootLine = new ShootLine(this.caster.Position, this.caster.Position);
            if (this.verbProps.stopBurstWithoutLos)
            {
                return false;
            }
            if (base.EquipmentSource != null)
            {
                CompChangeableProjectile comp = base.EquipmentSource.GetComp<CompChangeableProjectile>();
                if (comp != null)
                {
                    comp.Notify_ProjectileLaunched();
                }
                CompApparelReloadable comp2 = base.EquipmentSource.GetComp<CompApparelReloadable>();
                if (comp2 != null)
                {
                    comp2.UsedOnce();
                }
            }
            this.lastShotTick = Find.TickManager.TicksGame;
            Thing thing = this.caster;
            Thing equipment = base.EquipmentSource;
            CompMannable compMannable = this.caster.TryGetComp<CompMannable>();
            if (compMannable != null && compMannable.ManningPawn != null)
            {
                thing = compMannable.ManningPawn;
                equipment = this.caster;
            }
            Vector3 drawPos = this.caster.DrawPos;
            Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, shootLine.Source, this.caster.Map, WipeMode.Vanish);
            if (this.verbProps.ForcedMissRadius > 0.5f)
            {
                float num = this.verbProps.ForcedMissRadius;
                Pawn caster;
                if (thing != null && (caster = (thing as Pawn)) != null)
                {
                    num *= this.verbProps.GetForceMissFactorFor(equipment, caster);
                }
                float num2 = VerbUtility.CalculateAdjustedForcedMiss(num, this.currentTarget.Cell - this.caster.Position);
                if (num2 > 0.5f)
                {
                    int max = GenRadial.NumCellsInRadius(num2);
                    int num3 = Rand.Range(0, max);
                    if (num3 > 0)
                    {
                        IntVec3 c = this.caster.Position + GenRadial.RadialPattern[num3];
                        this.ThrowDebugText("ToRadius");
                        this.ThrowDebugText("Rad\nDest", c);
                        ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
                        if (Rand.Chance(0.5f))
                        {
                            projectileHitFlags = ProjectileHitFlags.All;
                        }
                        if (!this.canHitNonTargetPawnsNow)
                        {
                            projectileHitFlags &= ~ProjectileHitFlags.NonTargetPawns;
                        }
                        projectile2.Launch(thing, drawPos, c, this.currentTarget, projectileHitFlags, this.preventFriendlyFire, equipment, null);
                        return true;
                    }
                }
            }
            ShotReport shotReport = ShotReport.HitReportFor(this.caster, this, this.currentTarget);
            Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
            ThingDef targetCoverDef = (randomCoverToMissInto != null) ? randomCoverToMissInto.def : null;
            if (!Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture))
            {
                shootLine.ChangeDestToMissWild_NewTemp(shotReport.AimOnTargetChance_StandardTarget, false, caster.Map);
                this.ThrowDebugText("ToWild" + (this.canHitNonTargetPawnsNow ? "\nchntp" : ""));
                this.ThrowDebugText("Wild\nDest", shootLine.Dest);
                ProjectileHitFlags projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
                if (Rand.Chance(0.5f) && this.canHitNonTargetPawnsNow)
                {
                    projectileHitFlags2 |= ProjectileHitFlags.NonTargetPawns;
                }
                projectile2.Launch(thing, drawPos, shootLine.Dest, this.currentTarget, projectileHitFlags2, this.preventFriendlyFire, equipment, targetCoverDef);
                return true;
            }
            if (this.currentTarget.Thing != null && this.currentTarget.Thing.def.category == ThingCategory.Pawn && !Rand.Chance(shotReport.PassCoverChance))
            {
                this.ThrowDebugText("ToCover" + (this.canHitNonTargetPawnsNow ? "\nchntp" : ""));
                this.ThrowDebugText("Cover\nDest", randomCoverToMissInto.Position);
                ProjectileHitFlags projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
                if (this.canHitNonTargetPawnsNow)
                {
                    projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
                }
                projectile2.Launch(thing, drawPos, randomCoverToMissInto, this.currentTarget, projectileHitFlags3, this.preventFriendlyFire, equipment, targetCoverDef);
                return true;
            }
            ProjectileHitFlags projectileHitFlags4 = ProjectileHitFlags.IntendedTarget;
            if (this.canHitNonTargetPawnsNow)
            {
                projectileHitFlags4 |= ProjectileHitFlags.NonTargetPawns;
            }
            if (!this.currentTarget.HasThing || this.currentTarget.Thing.def.Fillage == FillCategory.Full)
            {
                projectileHitFlags4 |= ProjectileHitFlags.NonTargetWorld;
            }
            this.ThrowDebugText("ToHit" + (this.canHitNonTargetPawnsNow ? "\nchntp" : ""));
            if (this.currentTarget.Thing != null)
            {
                projectile2.Launch(thing, drawPos, shootLine.Dest, this.currentTarget, projectileHitFlags4, this.preventFriendlyFire, equipment, targetCoverDef);
                this.ThrowDebugText("Hit\nDest", this.currentTarget.Cell);
            }
            else
            {
                projectile2.Launch(thing, drawPos, shootLine.Dest, this.currentTarget, projectileHitFlags4, this.preventFriendlyFire, equipment, targetCoverDef);
                this.ThrowDebugText("Hit\nDest", shootLine.Dest);
            }
            return true;
        }

        private void ThrowDebugText(string text)
        {
            if (DebugViewSettings.drawShooting)
            {
                MoteMaker.ThrowText(this.caster.DrawPos, this.caster.Map, text, -1f);
            }
        }

        private void ThrowDebugText(string text, IntVec3 c)
        {
            if (DebugViewSettings.drawShooting)
            {
                MoteMaker.ThrowText(c.ToVector3Shifted(), this.caster.Map, text, -1f);
            }
        }
    }

    public class Verb_ScatterOneUse : Verb_Scatter
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                if (this.burstShotsLeft <= 1)
                {
                    this.SelfConsume();
                }
                return true;
            }
            if (this.burstShotsLeft < this.verbProps.burstShotCount)
            {
                this.SelfConsume();
            }
            return false;
        }

        public override void Notify_EquipmentLost()
        {
            base.Notify_EquipmentLost();
            if (this.state == VerbState.Bursting && this.burstShotsLeft < this.verbProps.burstShotCount)
            {
                this.SelfConsume();
            }
        }

        private void SelfConsume()
        {
            if (base.EquipmentSource != null && !base.EquipmentSource.Destroyed)
            {
                base.EquipmentSource.Destroy(DestroyMode.Vanish);
            }
        }
    }

    // Verb_Drop
    public class Verb_Drop : Verb
    {
        public virtual ThingDef Projectile
        {
            get
            {
                return this.verbProps.defaultProjectile;
            }
        }

        protected override bool TryCastShot()
        {
            return Drop(this.Projectile, this.caster, base.ReloadableCompSource);
        }

        public static bool Drop(ThingDef projectile, Thing caster, CompApparelReloadable comp)
        {
            if (projectile == null)
            {
                return false;
            }
            if (comp != null)
            {
                //if (!comp.CanBeUsed())
                //{
                //    return true;
                //}
                comp.UsedOnce();
            }
            ShootLine shootLine = new ShootLine(caster.Position, caster.Position);
            Thing thing = caster;
            Vector3 drawPos = caster.DrawPos;
            Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, shootLine.Source, caster.Map, WipeMode.Vanish);
            ProjectileHitFlags projectileHitFlags4 = ProjectileHitFlags.IntendedTarget;
            projectile2.Launch(thing, drawPos, shootLine.Dest, null, projectileHitFlags4, true, caster, null);
            return true;
        }
    }

    // Effector Gas
    public class DamageWorker_EffectorGas : DamageWorker
    {
        public override void ExplosionStart(Explosion explosion, List<IntVec3> cellsToAffect)
        {
            if (this.def.explosionHeatEnergyPerCell > 1.401298E-45f)
            {
                GenTemperature.PushHeat(explosion.Position, explosion.Map, this.def.explosionHeatEnergyPerCell * (float)cellsToAffect.Count);
            }
            this.ExplosionVisualEffectCenter(explosion);
        }

        public override DamageWorker.DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            DamageWorker.DamageResult damageResult = new DamageWorker.DamageResult();
            if (victim.def.category == ThingCategory.Pawn && victim.def.useHitPoints && dinfo.Def.harmsHealth)
            {
                float amount = dinfo.Amount;
                damageResult.totalDamageDealt = (float)Mathf.Min(victim.HitPoints, GenMath.RoundRandom(amount));
                victim.HitPoints -= Mathf.RoundToInt(damageResult.totalDamageDealt);
                if (victim.HitPoints <= 0)
                {
                    victim.HitPoints = 0;
                    victim.Kill(new DamageInfo?(dinfo), null);
                }
            }
            return damageResult;
        }
    }

    public class GasEffectorDefs : DefModExtension
    {
        public string Hediff;

        public float Severity;

        public int EffectTickPeriod = 120;

        public int RepairAmount = 0;

        public bool Inhaled = false;
    }

    public class EffectorGasDefGetValue
    {
        public static string HediffGasGetHediff(ThingDef thingdef)
        {
            if (thingdef.HasModExtension<GasEffectorDefs>())
            {
                return thingdef.GetModExtension<GasEffectorDefs>().Hediff;
            }
            return null;
        }

        public static float HediffGasGetSev(ThingDef thingdef)
        {
            if (thingdef.HasModExtension<GasEffectorDefs>())
            {
                return thingdef.GetModExtension<GasEffectorDefs>().Severity;
            }
            return 0f;
        }

        public static int HediffGasGetSevUpVal(ThingDef thingdef)
        {
            if (thingdef.HasModExtension<GasEffectorDefs>())
            {
                return thingdef.GetModExtension<GasEffectorDefs>().EffectTickPeriod;
            }
            return 120;
        }

        public static int EffectorGasGetRepairAmount(ThingDef thingdef)
        {
            if (thingdef.HasModExtension<GasEffectorDefs>())
            {
                return thingdef.GetModExtension<GasEffectorDefs>().RepairAmount;
            }
            return 0;
        }

        public static bool EffectorGasGetInhaled(ThingDef thingdef)
        {
            if (thingdef.HasModExtension<GasEffectorDefs>())
            {
                return thingdef.GetModExtension<GasEffectorDefs>().Inhaled;
            }
            return false;
        }
    }

    public enum GasType : short
    {
        BlindSmoke,
        ToxGas = 8,
        RotStink = 16,
        Unused = 24
    }

    public class EffectorGas : Gas
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, true);
            this.destroyTick = Find.TickManager.TicksGame + this.def.gas.expireSeconds.RandomInRange.SecondsToTicks();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.destroyTick, "destroyTick", 0, false);
        }
        public override void Tick()
        {
            if (this.destroyTick <= Find.TickManager.TicksGame)
            {
                this.Destroy(DestroyMode.Vanish);
            }
            this.graphicRotation += this.graphicRotationSpeed;
            if (!base.Destroyed && Find.TickManager.TicksGame % EffectorGasDefGetValue.HediffGasGetSevUpVal(this.def) == 0)
            {
                Map map = base.Map;
                IntVec3 position = base.Position;
                List<Thing> thingList = position.GetThingList(map);
                if (thingList.Count > 0)
                {
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        if (thingList[i] is Pawn && thingList[i].Position == position)
                        {
                            this.HediffGasApply(this, thingList[i]);
                        }
                        if (EffectorGasDefGetValue.EffectorGasGetRepairAmount(this.def) > 0 && thingList[i] is Building && (thingList[i] as Building).HitPoints < (thingList[i] as Building).MaxHitPoints && thingList[i].Position == position)
                        {
                            Building building = (thingList[i] as Building);
                            building.HitPoints += EffectorGasDefGetValue.EffectorGasGetRepairAmount(this.def);
                            building.HitPoints = Mathf.Min(building.HitPoints, building.MaxHitPoints);
                            this.Map.listerBuildingsRepairable.Notify_BuildingRepaired((Building)building);
                        }
                        else if (EffectorGasDefGetValue.EffectorGasGetRepairAmount(this.def) < 0 && thingList[i] is Building && thingList[i].Position == position)
                        {
                            Building building = (thingList[i] as Building);
                            DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, -(float)EffectorGasDefGetValue.EffectorGasGetRepairAmount(this.def), 0, -1, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, building, true, true);
                            building.TakeDamage(dinfo);
                        }
                    }
                }
            }
        }
        public void HediffGasApply(Thing Gas, Thing targ)
        {
            Pawn pawn = targ as Pawn;
            if (pawn != null && (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Breathing) || !(EffectorGasDefGetValue.EffectorGasGetInhaled(Gas.def))))
            {
                HediffDef namedSilentFail = DefDatabase<HediffDef>.GetNamedSilentFail(EffectorGasDefGetValue.HediffGasGetHediff(Gas.def));
                if (namedSilentFail != null)
                {
                    Pawn_HealthTracker health = pawn.health;
                    Hediff hediff;
                    if (health == null)
                    {
                        hediff = null;
                    }
                    else
                    {
                        HediffSet hediffSet = health.hediffSet;
                        hediff = ((hediffSet != null) ? hediffSet.GetFirstHediffOfDef(namedSilentFail, false) : null);
                    }
                    float num = EffectorGasDefGetValue.HediffGasGetSev(Gas.def);
                    if (num < 0.01f)
                    {
                        num = 0.01f;
                    }
                    float num2 = num;
                    if (hediff != null && num2 > 0f)
                    {
                        hediff.Severity += num2;
                        return;
                    }
                    Hediff hediff2 = HediffMaker.MakeHediff(namedSilentFail, pawn, null);
                    hediff2.Severity = num2;
                    pawn.health.AddHediff(hediff2, null, null, null);
                }
            }
        }
    }

    // Insta Explosive Projectile
    public class Projectile_InstaExplosive : Projectile_Explosive
    {
        public override void Tick()
        {
            base.Tick();
            this.Impact(null);
        }
    }

    // Belts
    public class SeekerBelt : Apparel
    {
        public override void Notify_BulletImpactNearby(BulletImpactData impactData)
        {
            base.Notify_BulletImpactNearby(impactData);
            Pawn wearer = base.Wearer;
            if (wearer == null || wearer.Dead)
            {
                return;
            }
            if (impactData.bullet.Launcher != null && impactData.bullet.Launcher.HostileTo(base.Wearer) && !wearer.IsColonist && wearer.Spawned)
            {
                Verb_Drop.Drop(DefDatabase<ThingDef>.GetNamed("Projectile_Seeker"), wearer, this.TryGetComp<CompApparelReloadable>());
            }
        }
    }

    public class IncendiaryBelt : Apparel
    {
        public override void Notify_BulletImpactNearby(BulletImpactData impactData)
        {
            base.Notify_BulletImpactNearby(impactData);
            Pawn wearer = base.Wearer;
            if (wearer == null || wearer.Dead)
            {
                return;
            }
            if (impactData.bullet.Launcher != null && impactData.bullet.Launcher.HostileTo(base.Wearer) && !wearer.IsColonist && wearer.Spawned)
            {
                Verb_Drop.Drop(DefDatabase<ThingDef>.GetNamed("Projectile_Incendiary"), wearer, this.TryGetComp<CompApparelReloadable>());
            }
        }
    }

    public class ClusterBelt : Apparel
    {
        public override void Notify_BulletImpactNearby(BulletImpactData impactData)
        {
            base.Notify_BulletImpactNearby(impactData);
            Pawn wearer = base.Wearer;
            if (wearer == null || wearer.Dead)
            {
                return;
            }
            if (impactData.bullet.Launcher != null && impactData.bullet.Launcher.HostileTo(base.Wearer) && !wearer.IsColonist && wearer.Spawned)
            {
                Verb_Drop.Drop(DefDatabase<ThingDef>.GetNamed("Projectile_Cluster"), wearer, this.TryGetComp<CompApparelReloadable>());
            }
        }
    }

    public class StrikerBelt : Apparel
    {
        public override void Notify_BulletImpactNearby(BulletImpactData impactData)
        {
            base.Notify_BulletImpactNearby(impactData);
            Pawn wearer = base.Wearer;
            if (wearer == null || wearer.Dead)
            {
                return;
            }
            if (impactData.bullet.Launcher != null && impactData.bullet.Launcher.HostileTo(base.Wearer) && !wearer.IsColonist && wearer.Spawned)
            {
                Verb_Drop.Drop(DefDatabase<ThingDef>.GetNamed("Projectile_Striker"), wearer, this.TryGetComp<CompApparelReloadable>());
            }
        }
    }
}