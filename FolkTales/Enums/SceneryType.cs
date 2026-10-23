/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     FolkTales.Enums
 * FILE:        SceneryType.cs
 * PURPOSE:     Define the different types of scenery objects that can be placed in the dungeon, such as bonsai trees, jagged rocks, ancient pillars, and crystal shards.
 *              seed == 0: Facing North (Clamps to the top edge of a tile)
 *              seed == 1: Facing South (Clamps to the bottom edge of a tile)
 *              seed == 2: Facing West (Clamps to the left edge of a tile)
 *              seed == 3: Facing East (Clamps to the right edge of a tile)
 *              seed used for random variation in geometry, color, and orientation of the object.
 *              Prisonbars, Lever, IronRing, HangingChain: These objects are typically attached to walls or ceilings and may have specific orientation requirements based on their placement. 
 *              The seed can be used to determine their facing direction and how they interact with the environment.
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales.Enums
{
    /// <summary>
    /// Collection of current prefabs.
    /// </summary>
    public enum SceneryType
    {
        BonsaiTree = 0,
        JaggedRock = 1,
        AncientPillar = 2,
        CrystalShard = 3,
        AncientArch = 4,
        Stool = 5,
        Table = 6,
        ShelfEmpty = 7,
        WallTorch = 8,
        StandingTorch = 9,
        Bed = 10,
        Campfire = 11,
        PrisonBars = 12,
        StrawBed = 13,
        Ladder = 14,
        Lever = 15,
        IronRing = 16,
        HangingChain = 17,
        SupplyCrate = 18,
        Fountain = 19,
        RitualAltar = 20,
        SkeletalRemains = 21,
        RoundVase = 22,
        Mushrooms = 23,
        Fireplace = 24,
        Machinery = 25,
        PineTree = 26,
        MushroomTree = 27,
        Flowers = 28,
        Hedges = 29,
        DoorWoodClosed = 30,
        DoorWoodOpen = 31,
        DoorPrisonClosed = 32,
        DoorPrisonOpen = 33,
        CurvedWall = 34,
        LargeGableRoof = 35,
        ChinesePagoda = 36,
        WindChime = 37,
        DreamCatcher = 38,
        Bramble = 39,
        Bamboo = 40,
        Oak = 41,
        DeadTree = 42,
        MammothTree = 43,
        WaterLily = 44,
        Lotus = 45,
        Seagrass = 46,
        ElevatedWaterLily = 47,
        LilacRuin = 48,
        BasaltColumn = 49,
        TallGrass = 50,
        WoodenHorse = 51,
        StretchingRack = 52,
        PilloryClosed = 53,
        PilloryOpen = 54,
        HangingCageClosed = 55,
        HangingCageOpen = 56,
        TreasureChestClosed = 57,
        TreasureChestOpen = 58,
        ShelfBooks = 59,
        Pillar = 60,
        StoneArch = 61,
        IronMaiden = 62,
        XCross = 63,
        DogBowlFull = 64,
        DogBowlEmpty = 65,
        WheelStanding = 66,
        WheelLaying = 67,
        TortureChair = 68,
        Barrel = 69,
        SupplyCrateDestroyed = 70,
        BarrelClosed = 71,
        BarrelOpen = 72,
        WeaponRackFull = 73,
        WeaponRackEmpty = 74,

        /// <summary>
        /// One CELL of a straight staircase that climbs one level. A full level takes <c>StairLayout.CellsLong()</c> of these,
        /// placed one per cell, each one <c>StairLayout.RisePerCell</c> higher. Sized from <c>SceneryPlacement.FitScale</c>.
        /// </summary>
        StairFlight = 75,
        TrainingDummy = 76,
        RoundTable = 77,
        ArmorRackEmpty = 78,
        ChairWithBackrest = 79,
        ArmorRackFull = 80,
        SculptureFemaleIntact = 81,
        SculptureFemaleBroken = 82,
        CryptSarcophagusClosed = 83,
        SculptureAngelBroken = 84,
        SculptureAngelIntact = 85,
        SculptureHoodedIntact = 86,
        TownWell = 87,
        SculptureHoodedBroken = 88,
        CryptSarcophagusOpen = 89,
        NoticeBoard = 90,
        AlchemyStation = 91,
        RuinedColumnIntact = 92,
        RuinedColumnBroken = 93,
        TableCluttered = 94,
        ShelfCluttered = 95,
        HandheldCandle = 96,
        Smithy = 97,
        Thimble = 98,
        Iris = 99,
        Delphinium = 100,
        Sunflower = 101,
        Rose = 102,
        GnarlyOak = 103,
    }
}