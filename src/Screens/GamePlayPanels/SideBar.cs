// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Drawing;
using System.Linq;
using CivOne.Enums;
using CivOne.Events;
using CivOne.Graphics;
using CivOne.IO;
using CivOne.Graphics.Sprites;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Units;
using CivOne.Players;

namespace CivOne.Screens.GamePlayPanels
{
	internal class SideBar : BaseScreen
	{
		private bool _update = true;
		
		private readonly Picture _miniMap, _demographics;
		private Picture _gameInfo;
		
		private void DrawMiniMap(uint gameTick = 0)
		{
			_miniMap.Clear(5);
			
			if (GamePlay != null)
			{
				IUnit activeUnit = Game.ActiveUnit;
				ITile[,] tiles = Map[GamePlay.X - 30, GamePlay.Y - 18, 78, 48];
				for (int yy = 0; yy < 48; yy++)
				for (int xx = 0; xx < 78; xx++)
				{
					ITile tile = tiles[xx, yy];
					if (tile == null) continue;

					// Flash active unit
					if (activeUnit != null && Human.Is(activeUnit.Owner) && (tile.X == activeUnit.X && tile.Y == activeUnit.Y))
					{
						if (gameTick % 4 <= 1)
						{
							_miniMap[xx + 1, yy + 1] = 15;
						}
						else
						{
							_miniMap[xx + 1, yy + 1] = (byte)(tile.IsOcean ? 1 : 2);
						}
						continue;
					}

					if (Settings.RevealWorld)
					{
						byte colour = 5;
						switch (tile.Type)
						{
							case Terrain.Ocean: colour = 1; break;
							case Terrain.Forest: colour = 2; break;
							case Terrain.Swamp: colour = 3; break;
							case Terrain.Plains: colour = 6; break;
							case Terrain.Tundra: colour = 7; break;
							case Terrain.River: colour = 9; break;
							case Terrain.Grassland1:
							case Terrain.Grassland2: colour = 10; break;
							case Terrain.Jungle: colour = 11; break;
							case Terrain.Hills: colour = 12; break;
							case Terrain.Mountains: colour = 13; break;
							case Terrain.Desert: colour = 14; break;
							case Terrain.Arctic: colour = 15; break;
						}
						_miniMap[xx + 1, yy + 1] = colour;
					}
					else if (Human.Visible(tile.X, tile.Y))
					{
						if (tile.City != null)
						{
							_miniMap[xx + 1, yy + 1] = Common.ColourLight[tile.City.Owner];
						}
						else
						{
							if (tile.IsOcean) _miniMap[xx + 1, yy + 1] = 1;
							else _miniMap[xx + 1, yy + 1] = 2;
						}
					}
				}
			}
			_miniMap.DrawRectangle(31, 18, 18, 11, 15)
				.DrawRectangle3D();
		}

		private void DrawDemographics()
		{
			_demographics.Tile(Pattern.PanelGrey)
				.DrawRectangle3D()
				.FillRectangle(3, 2, 74, 11, 11)
				.FillRectangle(3, 13, 74, 1, 2);
			if (Human.GetPopulation() > 0)
			{
				string population = Common.NumberSeperator(Human.GetPopulation());
				_demographics.DrawText($"{population}#", 0, 5, 2, 15, TextAlign.Left);
			}
			_demographics.DrawText(Game.GameYear, 0, 5, 2, 23, TextAlign.Left);

			int width = Resources.GetTextSize(0, Game.GameYear).Width;
			int stage = (int)Math.Floor(((double)Human.Science / Human.ScienceCost()) * 4);
			_demographics.AddLayer(Icons.Lamp(stage), 4 + width, 22);

			_demographics.DrawText($"{Human.Gold}$ {Human.LuxuriesRate}.{Human.TaxesRate}.{Human.ScienceRate}", 0, 5, 2, 31, TextAlign.Left);
		}
		
		private void DrawGameInfo(uint gameTick = 0)
		{
			IUnit unit = Game.ActiveUnit;
			
			_gameInfo.Tile(Pattern.PanelGrey)
				.DrawRectangle3D();
			
			if (Game.CurrentPlayer != Human || (unit != null && !Human.Is(unit.Owner)) || (GameTask.Any() && !GameTask.Is<Show>() && !GameTask.Is<Message>()))
			{
				_gameInfo.FillRectangle(2, _gameInfo.Height - 8, 6, 6, (byte)((gameTick % 4 < 2) ? 15 : 8));
				return;
			}

			if (unit != null)
			{
				int yy = 2;
				_gameInfo.DrawText(Human.Civilization.Name, 0, 5, 4, 2, TextAlign.Left);
				_gameInfo.DrawText(unit.Name, 0, 5, 4, (yy += 8), TextAlign.Left);
				
				if (unit.Veteran)
				{
					_gameInfo.DrawText("Veteran", 0, 5, 8, (yy += 8), TextAlign.Left);
				}

				if (unit is BaseUnitAir)
				{
					_gameInfo.DrawText($"Moves: {unit.MovesLeft}({(unit as BaseUnitAir).FuelLeft})", 0, 5, 4, (yy += 8), TextAlign.Left);
				}
				else if (unit.PartMoves > 0)
				{
					_gameInfo.DrawText($"Moves: {unit.MovesLeft}.{unit.PartMoves}", 0, 5, 4, (yy += 8), TextAlign.Left);
				}
				else
				{
					_gameInfo.DrawText($"Moves: {unit.MovesLeft}", 0, 5, 4, (yy += 8), TextAlign.Left);
				}
				_gameInfo.DrawText((unit.Home == null ? "NONE" : unit.Home.Name), 0, 5, 4, (yy += 8), TextAlign.Left);
				_gameInfo.DrawText($"({Map[unit.X, unit.Y].Name})", 0, 5, 4, (yy += 8), TextAlign.Left);
				
				if (Map[unit.X, unit.Y].RailRoad)
					_gameInfo.DrawText("(RailRoad)", 0, 5, 4, (yy += 8), TextAlign.Left);
				else if (Map[unit.X, unit.Y].Road)
					_gameInfo.DrawText("(Road)", 0, 5, 4, (yy += 8), TextAlign.Left);
				if (Map[unit.X, unit.Y].Irrigation)
					_gameInfo.DrawText("(Irrigation)", 0, 5, 4, (yy += 8), TextAlign.Left);
				else if (Map[unit.X, unit.Y].Mine)
					_gameInfo.DrawText("(Mining)", 0, 5, 4, (yy += 8), TextAlign.Left);
				
				yy += 11;

				IUnit[] units = Map[unit.X, unit.Y].Units.Where(u => u != unit).Take(8).ToArray();
				for (int i = 0; i < units.Length; i++)
				{
					int ix = 7 + ((i % 4) * 16);
					int iy = yy + (((i - (i % 4)) / 4) * 16);
					_gameInfo.AddLayer(units[i].ToBitmap(), ix, iy);
				}
			}
			else
			{
				if (gameTick % 4 < 2)
					_gameInfo.DrawText($"End of Turn", 0, 5, 4, 26, TextAlign.Left);
				_gameInfo.DrawText($"Press Enter", 0, 5, 4, 42, TextAlign.Left);
				_gameInfo.DrawText($"to continue", 0, 5, 4, 50, TextAlign.Left);
			}
		}
		
		protected override bool HasUpdate(uint gameTick)
		{
			if (_update || (gameTick % 2 == 0))
			{
				if (!(Common.TopScreen is GamePlay))
					gameTick = 0;

				DrawMiniMap(gameTick);
				DrawDemographics();
				DrawGameInfo(gameTick);
				
				this.AddLayer(_miniMap, 0, 0)
					.AddLayer(_demographics, 0, 50)
					.AddLayer(_gameInfo, 0, 89);
				
				_update = false;
				return true;
			}
			return false;
		}
		
		public override bool MouseDown(ScreenEventArgs args)
		{
			if (args.Y <= 50)
			{
				if (args.X < 1 || args.Y < 1 || args.X > 79 || args.Y > 49) return true;
				
				int xx = (args.X - 1) + GamePlay.X - 30;
				int yy = (args.Y - 1) + GamePlay.Y - 18;

				GamePlay.CenterOnPoint(xx, yy);
			}
			if (args.Y > 50 && args.Y < 62)
			{
				Log("Sidebar: Palace View");
				Common.AddScreen(new PalaceView());
			}
			else if (args.Y >= 62)
			{
				if (Game.CurrentPlayer == Human && Game.ActiveUnit == null)
				{
					GameTask.Enqueue(Turn.End());
				}
			}
			return true;
		}

		private GamePlay GamePlay
		{
			get
			{
				IScreen mapScreen = Common.Screens.FirstOrDefault(s => (s is GamePlay));
				if (mapScreen != null)
					return (mapScreen as GamePlay);
				return null;
			}
		}
		
		public void Resize(int height)
		{
			Bitmap = new Bytemap(80, height);
			_gameInfo?.Dispose();
			_gameInfo = new Picture(80, (height - 89), Palette);
			_update = true;
		}

		public SideBar(Palette palette) : base(80, 192)
		{
			_miniMap = new Picture(80, 50, palette);
			_demographics = new Picture(80, 39, palette);
			_gameInfo = new Picture(80, 103, palette);
			
			DrawMiniMap();
			DrawDemographics();
			DrawGameInfo();
			
			Palette = palette.Copy();
			this.AddLayer(_miniMap, 0, 0)
				.AddLayer(_demographics, 0, 50)
				.AddLayer(_gameInfo, 0, 89);
		}

		public override void Dispose()
		{
			_miniMap.Dispose();
			_demographics.Dispose();
			_gameInfo.Dispose();
			base.Dispose();
		}
	}
}