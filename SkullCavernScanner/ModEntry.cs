using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using SObject = StardewValley.Object;

namespace SkullCavernScanner;

internal sealed class ModEntry : Mod
{
    private ModConfig Config = new();
    private Texture2D? Pixel;
    private readonly List<CandidateRock> Candidates = new();
    private Vector2? GeneratedExitTile;
    private string LastLocationName = "";
    private int LastMineLevel = int.MinValue;
    private bool WaitingForCandidates;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.Player.Warped += OnWarped;
        helper.Events.World.ObjectListChanged += OnObjectListChanged;
        helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;
        helper.Events.Display.RenderedWorld += OnRenderedWorld;
        helper.Events.Display.RenderedHud += OnRenderedHud;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        Monitor.Log("Loaded. In Skull Cavern, press F8 to toggle the scanner.", LogLevel.Info);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // Create the pixel after the game has initialized its graphics device.
        if (Pixel == null)
        {
            Pixel = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            Pixel.SetData(new[] { Color.White });
        }
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) => RefreshCurrentFloor();
    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e) => Clear();

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (e.IsLocalPlayer)
            RefreshCurrentFloor();
    }

    private static bool IsSkullCavern(MineShaft mine) => mine.getMineArea() == MineShaft.desertArea;

    private void RefreshCurrentFloor()
    {
        if (Game1.currentLocation is MineShaft mine && IsSkullCavern(mine))
            ScanFloor(mine);
        else
            Clear();
    }

    private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
    {
        if (!e.IsCurrentLocation || e.Location is not MineShaft mine || !IsSkullCavern(mine))
            return;

        // Recalculate after each rock removal: the remaining-rock count changes
        // the game's per-rock ladder/shaft chance.
        ScanFloor(mine);
    }

    private void OnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
    {
        if (!WaitingForCandidates || !Config.Enabled || !Context.IsWorldReady)
            return;
        if (Game1.currentLocation is not MineShaft mine || !IsSkullCavern(mine))
        {
            WaitingForCandidates = false;
            return;
        }
        ScanFloor(mine);
    }

    private void ScanFloor(MineShaft mine)
    {
        Candidates.Clear();
        GeneratedExitTile = null;
        LastLocationName = mine.NameOrUniqueName;
        LastMineLevel = mine.mineLevel;
        WaitingForCandidates = false;

        if (!Config.Enabled)
            return;

        try
        {
            bool ladderHasSpawned = Helper.Reflection
                .GetField<bool>(mine, "ladderHasSpawned", required: false)?.GetValue() ?? false;

            if (ladderHasSpawned)
            {
                if (Config.ShowGeneratedExit)
                {
                    // This is the game's authoritative tile for the generated
                    // descent. The game uses the same field for its ladder/shaft exit.
                    NetVector2? netTile = Helper.Reflection
                        .GetField<NetVector2>(mine, "netTileBeneathLadder", required: false)?.GetValue();
                    if (netTile != null && netTile.Value != Vector2.Zero)
                        GeneratedExitTile = netTile.Value;
                }
                return;
            }

            if (mine.mustKillAllMonstersToAdvance() || !mine.shouldCreateLadderOnThisLevel())
                return;

            int stonesLeft = Helper.Reflection
                .GetField<NetIntDelta>(mine, "netStonesLeftOnThisLevel", required: false)?.GetValue().Value ?? 0;

            // This mirrors the published 1.6-compatible LadderLocator candidate
            // calculation. A highlighted rock is eligible to create a descent;
            // it is NOT a guarantee that the descent will be a shaft.
            double chance = 0.02
                + 1.0 / Math.Max(1, stonesLeft)
                + Game1.player.LuckLevel / 100.0
                + Game1.player.DailyLuck / 5.0;

            if (mine.EnemyCount == 0)
                chance += 0.04;

            foreach (KeyValuePair<Vector2, SObject> pair in mine.Objects.Pairs)
            {
                SObject obj = pair.Value;
                if (!obj.Name.Equals("Stone", StringComparison.OrdinalIgnoreCase))
                    continue;

                Random random = Utility.CreateDaySaveRandom(pair.Key.X * 1000, pair.Key.Y, mine.mineLevel);
                random.NextDouble();
                double roll = random.NextDouble();

                if ((stonesLeft == 0 || roll < chance)
                    && obj.ParentSheetIndex >= 0
                    && obj.ParentSheetIndex / 24 < Game1.objectSpriteSheet.Height / 16)
                {
                    Candidates.Add(new CandidateRock(pair.Key, obj));
                }
            }

            WaitingForCandidates = Candidates.Count == 0;
            Monitor.Log($"Skull Cavern floor {mine.mineLevel - 120}: {Candidates.Count} possible descent rock(s).", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            Candidates.Clear();
            WaitingForCandidates = false;
            Monitor.Log($"Couldn't scan this floor safely: {ex}", LogLevel.Warn);
        }
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Config.Enabled || !Context.IsWorldReady || Game1.currentLocation is not MineShaft mine || !IsSkullCavern(mine))
            return;

        if (LastLocationName != mine.NameOrUniqueName || LastMineLevel != mine.mineLevel)
            ScanFloor(mine);

        if (Pixel == null)
            return;

        foreach (CandidateRock candidate in Candidates)
        {
            Rectangle rect = candidate.Object.GetBoundingBox();
            rect.Offset(-Game1.viewport.X, -Game1.viewport.Y);
            DrawOutline(e.SpriteBatch, rect, Color.Gold * ClampAlpha(), 4);
            if (Config.ShowTileCoordinates)
                DrawText(e.SpriteBatch, $"CÓ THỂ: {candidate.Tile.X:0},{candidate.Tile.Y:0}", new Vector2(rect.X, rect.Y - 20), Color.Yellow);
        }

        if (GeneratedExitTile.HasValue && Config.ShowGeneratedExit)
        {
            Vector2 tile = GeneratedExitTile.Value;
            Rectangle rect = new((int)tile.X * 64 - Game1.viewport.X, (int)tile.Y * 64 - Game1.viewport.Y, 64, 64);
            DrawOutline(e.SpriteBatch, rect, Color.LimeGreen * ClampAlpha(), 5);
            DrawText(e.SpriteBatch, "LỐI XUỐNG ĐÃ XUẤT HIỆN", new Vector2(rect.X - 12, rect.Y - 22), Color.LimeGreen);
        }
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!Config.Enabled || !Context.IsWorldReady || Game1.currentLocation is not MineShaft mine || !IsSkullCavern(mine))
            return;

        int floor = Math.Max(1, mine.mineLevel - 120);
        Vector2 pos = new(16, 16);
        DrawText(e.SpriteBatch, $"SKULL CAVERN SCANNER  |  TẦNG {floor}", pos, Color.Gold);

        if (GeneratedExitTile.HasValue)
            DrawText(e.SpriteBatch, $"Lối xuống đã tạo tại: {GeneratedExitTile.Value.X:0},{GeneratedExitTile.Value.Y:0}", pos + new Vector2(0, 26), Color.LimeGreen);
        else
            DrawText(e.SpriteBatch, $"Đá có khả năng tạo lối xuống: {Candidates.Count}", pos + new Vector2(0, 26), Color.White);

        if (Config.ShowDropPossibilities)
        {
            string drops = string.Join(", ", ShaftDropCalculator.GetPossibleDropDistances(floor));
            DrawText(e.SpriteBatch, $"Hố rơi có thể xuống: {drops} tầng", pos + new Vector2(0, 50), Color.White);
            DrawText(e.SpriteBatch, "Đây là các mức hợp lệ, chưa phải kết quả seed chính xác.", pos + new Vector2(0, 72), Color.LightGray);
        }
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button != Config.ToggleKey || !Context.IsWorldReady || !Context.IsPlayerFree)
            return;

        Config.Enabled = !Config.Enabled;
        Helper.WriteConfig(Config);
        Game1.addHUDMessage(new HUDMessage($"Skull Cavern Scanner {(Config.Enabled ? "bật" : "tắt")}", 2));
        if (Config.Enabled)
            RefreshCurrentFloor();
        else
            ClearHighlightsOnly();
    }

    private float ClampAlpha() => Math.Clamp(Config.HighlightAlpha, 0.1f, 1f);

    private static void DrawText(SpriteBatch batch, string text, Vector2 position, Color color)
    {
        batch.DrawString(Game1.dialogueFont, text, position, color, 0f, Vector2.Zero, 0.72f, SpriteEffects.None, 1f);
    }

    private void DrawOutline(SpriteBatch batch, Rectangle rect, Color color, int thickness)
    {
        if (Pixel == null || rect.Width <= 0 || rect.Height <= 0)
            return;
        batch.Draw(Pixel, new Rectangle(rect.Left, rect.Top, rect.Width, thickness), color);
        batch.Draw(Pixel, new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), color);
        batch.Draw(Pixel, new Rectangle(rect.Left, rect.Top, thickness, rect.Height), color);
        batch.Draw(Pixel, new Rectangle(rect.Right - thickness, rect.Top, thickness, rect.Height), color);
    }

    private void ClearHighlightsOnly()
    {
        Candidates.Clear();
        GeneratedExitTile = null;
        WaitingForCandidates = false;
    }

    private void Clear()
    {
        ClearHighlightsOnly();
        LastLocationName = "";
        LastMineLevel = int.MinValue;
    }

    private sealed record CandidateRock(Vector2 Tile, SObject Object);
}
