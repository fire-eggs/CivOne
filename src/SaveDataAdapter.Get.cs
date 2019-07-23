// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CivOne.IO;

namespace CivOne
{
	internal partial class SaveDataAdapter
	{
		private void GetByteArray<T>(T structure, string fieldName, ref byte[] bytes) where T : struct
		{
			IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
			Marshal.StructureToPtr(structure, ptr, false);
			IntPtr offset = IntPtr.Add(ptr, (int)Marshal.OffsetOf<T>(fieldName));
			Marshal.Copy(offset, bytes, 0, bytes.Length);
			Marshal.FreeHGlobal(ptr);
		}

		private void GetByteArray(string fieldName, ref byte[] bytes) => GetByteArray<SaveData>(_saveData, fieldName, ref bytes);

		private byte[] GetBytes<T>(T structure, string fieldName, int length) where T : struct
		{
			byte[] output = new byte[length];
			GetByteArray<T>(structure, fieldName, ref output);
			return output;
		}

		private byte[] GetArray(string fieldName, int length)
		{
			byte[] output = new byte[length];
			GetByteArray(fieldName, ref output);
			return output;
		}

		private string[] GetArray(string fieldName, int itemLength, int itemCount)
		{
			byte[] bytes = GetArray(fieldName, itemLength * itemCount);
			string[] output = new string[itemCount];
			for (int i = 0; i < itemCount; i++)
				output[i] = bytes.ToString(i * itemLength, itemLength);
			return output;
		}

		private T[] GetArray<T>(string fieldName, int length)
		{
			T[] output = new T[length];
			int itemSize = Marshal.SizeOf<T>();
			switch (output)
			{
				case SaveData.City[] _:
				case SaveData.Unit[] _:
					byte[] buffer = new byte[length * itemSize];
					Buffer.BlockCopy(GetArray(fieldName, buffer.Length), 0, buffer, 0, buffer.Length);

					IntPtr ptr = Marshal.AllocHGlobal(itemSize);
					for (int i = 0; i < output.Length; i++)
					{
						Marshal.Copy(buffer, (i * itemSize), ptr, itemSize);
						output[i] = Marshal.PtrToStructure<T>(ptr);
					}
					Marshal.FreeHGlobal(ptr);
					break;
				default:
					Buffer.BlockCopy(GetArray(fieldName, length * itemSize), 0, output, 0, length * itemSize);
					break;
			}
			return output;
		}

		private unsafe byte[][] GetDiscoveredAdvanceIDs()
		{
			byte[] bytes = GetArray(nameof(SaveData.DiscoveredAdvances), 8 * 10);
			byte[][] output = new byte[8][];
			for (int i = 0; i < 8; i++)
				output[i] = bytes.FromBitIds(i * 10, 10).ToArray();
			return output;
		}

		private unsafe string[] GetLeaderNames() => GetArray(nameof(SaveData.LeaderNames), 14, 8);

		private unsafe string[] GetCivilizationNames() => GetArray(nameof(SaveData.CivilizationNames), 12, 8);

		private unsafe string[] GetCitizenNames() => GetArray(nameof(SaveData.CitizensName), 11, 8);

		private unsafe string[] GetCityNames() => GetArray(nameof(SaveData.CityNames), 13, 256);

		private unsafe short[] GetPlayerGold() => GetArray<short>(nameof(SaveData.PlayerGold), 8);

		private unsafe short[] GetResearchProgress() => GetArray<short>(nameof(SaveData.ResearchProgress), 8);

		private unsafe ushort[] GetTaxRate() => GetArray<ushort>(nameof(SaveData.TaxRate), 8);

		private unsafe ushort[] GetScienceRate() => GetArray<ushort>(nameof(SaveData.ScienceRate), 8);
		
		private unsafe ushort[] GetStartingPositionX() => GetArray<ushort>(nameof(SaveData.StartingPositionX), 8);

		private unsafe ushort[] GetGovernment() => GetArray<ushort>(nameof(SaveData.Government), 8);

		private unsafe CityData[] GetCities()
		{
			SaveData.City[] cities = GetArray<SaveData.City>(nameof(SaveData.Cities), 128);

			List<CityData> output = new List<CityData>();

			for (byte c = 0; c < cities.Length; c++)
			{
				SaveData.City city = cities[c];
				if (city.Status == 0xFF) continue;

				output.Add(new CityData()
				{
					Id = c,
					NameId = city.NameId,
					Buildings = GetBytes<SaveData.City>(city, nameof(SaveData.City.Buildings), 4).FromBitIds(0, 4).ToArray(),
					X = city.X,
					Y = city.Y,
					Status = city.Status,
					ActualSize = city.ActualSize,
					CurrentProduction = city.CurrentProduction,
					Owner = city.Owner,
					Food = city.Food,
					Shields = city.Shields,
					ResourceTiles = GetBytes<SaveData.City>(city, nameof(SaveData.City.ResourceTiles), 6),
					FortifiedUnits = GetBytes<SaveData.City>(city, nameof(SaveData.City.FortifiedUnits), 2).Where(x => x != 0xFF).ToArray()
				});
			}
			return output.ToArray();
		}

		private unsafe UnitData[][] GetUnits()
		{
			SaveData.Unit[] units = GetArray<SaveData.Unit>(nameof(SaveData.Units), 8 * 128);
			UnitData[][] output = new UnitData[8][];

			for (int p = 0; p < 8; p++)
			{
				List<UnitData> unitData = new List<UnitData>();
				for (byte u = 0; u < 128; u++)
				{
					SaveData.Unit unit = units[(p * 128) + u];
					
					if (unit.Type == 0xFF) continue;
					unitData.Add(new UnitData()
					{
						Id = u,
						Status = unit.Status,
						X = unit.X,
						Y = unit.Y,
						TypeId = unit.Type,
						RemainingMoves = unit.RemainingMoves,
						SpecialMoves = unit.SpecialMoves,
						GotoX = unit.GotoX,
						GotoY = unit.GotoY,
						Visibility = unit.Visibility,
						NextUnitId = unit.NextUnitId,
						HomeCityId = unit.HomeCityId
					});
				}
				output[p] = unitData.ToArray();
			}
			return output;
		}

		private unsafe ushort[] GetWonders() => GetArray<ushort>(nameof(SaveData.Wonders), 22);

		private unsafe ushort[] GetAdvanceFirstDiscovery() => GetArray<ushort>(nameof(SaveData.AdvanceFirstDiscovery), 72);

		private unsafe bool[][,] GetTileVisibility()
		{
			byte[] bytes = GetArray(nameof(SaveData.MapVisibility), (80 * 50));
			bool[][,] output = new bool[8][,];
			for (int p = 0; p < 8; p++)
			{
				output[p] = new bool[80, 50];
				for (int i = 0; i < (80 * 50); i++)
				{
					int y = (i % 50), x = (i - y) / 50;
					output[p][x, y] = ((bytes[i] & (1 << p)) > 0);
				}
			}
			return output;
		}

		private unsafe List<ReplayData> GetReplayData()
		{
			ushort replayLength = _saveData.ReplayLength;
			byte[] bytes = GetArray(nameof(SaveData.ReplayData), 4096);

			List<ReplayData> output = new List<ReplayData>();
			for (int i = 0; i < replayLength; i++)
			{
				byte entryCode = (byte)((bytes[i] & 0xF0) >> 4);
				int turn = bytes[i + 1] + ((bytes[i] & 0x0F) << 8);

				switch (entryCode)
				{
					case 0x1: i += 5; continue; // TODO: City built or destroyed
					case 0x2: i += 2; continue; // TODO: Declaration of war
					case 0x3: i += 2; continue; // TODO: Declaration of peace
					case 0x5: i += 3; continue; // TODO: Advance discovery
					case 0x6: i += 3; continue; // TODO: Unit first built
					case 0x8: i += 3; continue; // TODO: Government change
					case 0x9: i += 5; continue; // TODO: City captured
					case 0xA: i += 3; continue; // TODO: Wonder built
					case 0xB: i += 4; continue; // TODO: Replay summary
					case 0xC: i += 5; continue; // TODO: Civilization ranking
					case 0xD:
						output.Add(new ReplayData.CivilizationDestroyed(turn, bytes[i + 2], bytes[i + 3]));
                        i += 3;
						continue;
					default:
                        break;
				}
			}

            return output;
        }
	}
}