// Project:         Daggerfall Unity - ProjectN Fork
// Copyright:       
// Web Site:        
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     
// Original Author: Arneb Nihal
// Contributors:    
// 
// Notes:
//

#region Using Statements

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using DaggerfallConnect;
using DaggerfallConnect.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;
using Newtonsoft.Json;
using System.Linq;
using DaggerfallWorkshop.Game.Guilds;
using DaggerfallWorkshop.Game;
using Unity.QuickSearch.Providers;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;

#endregion

namespace DaggerfallConnect.Arena2
{
    /// <summary>
    /// File containing regions and locations data
    /// </summary>
    public class WorldMap
    {
        #region Class Variables

        public string Name;
        public int LocationCount;
        public string[] MapNames;
        public DFRegion.RegionMapTable[] MapTable;
        public Dictionary<ulong, int> MapIdLookup;
        public Dictionary<string, int> MapNameLookup;
        public DFLocation[] Locations;

        #endregion
    }

    public static class WorldMaps
    {
        #region Common Fields

        public static PlayerGPS LocalPlayerGPS = GameManager.Instance.PlayerGPS;
        // {
        //     get { return GetPosition(); }
        // }
        // public static string DaggerfallPath = DaggerfallUnity.Settings.MyDaggerfallPath;

        #endregion

        #region Class Fields

        public static WorldMap[] WorldMap;
        public static Dictionary<ulong, MapSummary> mapDict;
        public static string mapPath =  "/home/arneb/Games/daggerfall/DaggerfallGameFiles/arena2/Maps";
        public static string tilesPath = "/home/arneb/Games/daggerfall/DaggerfallGameFiles/arena2/Maps/Tiles";

        static WorldMaps()
        {
            WorldMap = JsonConvert.DeserializeObject<WorldMap[]>(File.ReadAllText(Path.Combine(mapPath, "Maps.json")));
            mapDict = EnumerateMaps();
        }        

        #endregion

        #region Variables

        public static Dictionary<ulong, ulong> locationIdToMapIdDict;

        #endregion

        #region Public Methods

        // public static DFPosition GetPosition()
        // {
        //     DFPosition defaultPosition = new DFPosition(1746, 2330);

        //     if (GameManager.HasInstance)
        //     {
        //         return GameManager.Instance.PlayerGPS.CurrentMapPixel;
        //     }
            
        //     return defaultPosition;
        // }

        public static ulong ReadLocationIdFast(int region, int location)
        {
            // Added new locations will put the LocationId in regions map table, since it doesn't exist in classic data
            if (WorldMaps.WorldMap[region].MapTable[location].LocationId != 0)
                return WorldMaps.WorldMap[region].MapTable[location].LocationId;

            // Get datafile location count (excluding added locations)
            int locationCount = WorldMaps.WorldMap[region].LocationCount;

            // Read the LocationId
            ulong locationId = WorldMaps.WorldMap[region].Locations[location].Exterior.RecordElement.Header.LocationId;

            return locationId;
        }

        public static DFRegion ConvertWorldMapsToDFRegion(int currentRegionIndex)
        {
            DFRegion dfRegion = new DFRegion();

            if (currentRegionIndex < WorldMaps.WorldMap.Length)
            {
                dfRegion.Name = WorldMaps.WorldMap[currentRegionIndex].Name;
                dfRegion.LocationCount = WorldMaps.WorldMap[currentRegionIndex].LocationCount;
                dfRegion.MapNames = WorldMaps.WorldMap[currentRegionIndex].MapNames;
                dfRegion.MapTable = WorldMaps.WorldMap[currentRegionIndex].MapTable;
                dfRegion.MapIdLookup = WorldMaps.WorldMap[currentRegionIndex].MapIdLookup;
                dfRegion.MapNameLookup = WorldMaps.WorldMap[currentRegionIndex].MapNameLookup;
            }
            else
            {
                dfRegion.Name = WorldData.WorldSetting.RegionNames[currentRegionIndex];
                dfRegion.LocationCount = 0;
                dfRegion.MapNames = new string[0];
                dfRegion.MapTable = new DFRegion.RegionMapTable[0];
                dfRegion.MapIdLookup = new Dictionary<ulong, int>();
                dfRegion.MapNameLookup = new Dictionary<string, int>();
            }

            return dfRegion;
        }

        /// <summary>
        /// Gets a DFLocation representation of a location.
        /// </summary>
        /// <param name="region">Index of region.</param>
        /// <param name="location">Index of location.</param>
        /// <returns>DFLocation.</returns>
        public static DFLocation GetLocation(int region, int location)
        {
            // Read location
            DFLocation dfLocation = new DFLocation();
            dfLocation = WorldMaps.WorldMap[region].Locations[location];

            // Store indices
            dfLocation.RegionIndex = region;
            dfLocation.LocationIndex = location;

            // Generate smaller dungeon when possible
            // if (UseSmallerDungeon(dfLocation))
            //     GenerateSmallerDungeon(ref dfLocation);

            return dfLocation;
        }

        /// <summary>
        /// Attempts to get a Daggerfall location from MAPS.BSA.
        /// </summary>
        /// <param name="regionIndex">Index of region.</param>
        /// <param name="locationIndex">Index of location.</param>
        /// <param name="locationOut">DFLocation data out.</param>
        /// <returns>True if successful.</returns>
        public static bool GetLocation(int regionIndex, int locationIndex, out DFLocation locationOut)
        {
            locationOut = new DFLocation();

            // Get location data
            locationOut = WorldMaps.GetLocation(regionIndex, locationIndex);
            if (!locationOut.Loaded)
            {
                DaggerfallUnity.LogMessage(string.Format("Unknown location RegionIndex='{0}', LocationIndex='{1}'.", regionIndex, locationIndex), true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets DFLocation representation of a location.
        /// </summary>
        /// <param name="regionName">Name of region.</param>
        /// <param name="locationName">Name of location.</param>
        /// <returns>DFLocation.</returns>
        public static DFLocation GetLocation(string regionName, string locationName)
        {
            // Load region
            int Region = GetRegionIndex(regionName);

            // Check location exists
            if (!WorldMaps.WorldMap[Region].MapNameLookup.ContainsKey(locationName))
                return new DFLocation();

            // Get location index
            int Location = WorldMaps.WorldMap[Region].MapNameLookup[locationName];

            return GetLocation(Region, Location);
        }

        /// <summary>
        /// Attempts to get a Daggerfall location from MAPS.BSA.
        /// </summary>
        /// <param name="regionName">Name of region.</param>
        /// <param name="locationName">Name of location.</param>
        /// <param name="locationOut">DFLocation data out.</param>
        /// <returns>True if successful.</returns>
        public static bool GetLocation(string regionName, string locationName, out DFLocation locationOut)
        {
            locationOut = new DFLocation();

            // Get location data
            locationOut = GetLocation(regionName, locationName);
            if (!locationOut.Loaded)
            {
                DaggerfallUnity.LogMessage(string.Format("Unknown location RegionName='{0}', LocationName='{1}'.", regionName, locationName), true);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets index of region with specified name. Does not change the currently loaded region.
        /// </summary>
        /// <param name="name">Name of region.</param>
        /// <returns>Index of found region, or -1 if not found.</returns>
        public static int GetRegionIndex(string name)
        {
            // Search for region name
            for (int i = 0; i < MapsFile.TempRegionCount; i++)
            {
                if (WorldMaps.WorldMap[i].Name == name)
                {
                    Debug.Log("Region index: " + i);
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Determines if the current WorldCoord has a location.
        /// </summary>
        /// <param name="mapPixelX">Map pixel X.</param>
        /// <param name="mapPixelY">Map pixel Y.</param>
        /// <returns>True if there is a location at this map pixel.</returns>
        public static bool HasLocation(int mapPixelX, int mapPixelY, out MapSummary summaryOut)
        {
            ulong id = MapsFile.GetMapPixelID(mapPixelX, mapPixelY);
            if (mapDict.ContainsKey(id))
            {
                summaryOut = mapDict[id];
                return true;
            }

            summaryOut = new MapSummary();
            return false;
        }

        /// <summary>
        /// Determines if the current WorldCoord has a location.
        /// </summary>
        /// <param name="mapPixelX">Map pixel X.</param>
        /// <param name="mapPixelY">Map pixel Y.</param>
        /// <returns>True if there is a location at this map pixel.</returns>
        public static bool HasLocation(int mapPixelX, int mapPixelY)
        {
            ulong id = MapsFile.GetMapPixelID(mapPixelX, mapPixelY);
            if (mapDict.ContainsKey(id))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to get a Daggerfall location from MAPS.BSA using a locationId from quest system.
        /// The locationId is different to the mapId, which is derived from location coordinates in world.
        /// At this time, best known way to determine locationId is from LocationRecordElementHeader data.
        /// This is linked to mapId at in EnumerateMaps().
        /// Note: Not all locations have a locationId, only certain key locations
        /// </summary>
        /// <param name="locationId">LocationId of map from quest system.</param>
        /// <param name="locationOut">DFLocation data out.</param>
        /// <returns>True if successful.</returns>
        public static bool GetQuestLocation(ulong locationId, out DFLocation locationOut)
        {
            locationOut = new DFLocation();            

            // MapDictCheck();

            // Get mapId from locationId
            ulong mapId = LocationIdToMapId(locationId);
            if (WorldMaps.mapDict.ContainsKey(mapId))
            {
                MapSummary summary = WorldMaps.mapDict[mapId];
                return GetLocation(summary.RegionIndex, summary.MapIndex, out locationOut);
            }

            return false;
        }

        /// <summary>
        /// Converts LocationId from quest system to a MapId for map lookups.
        /// </summary>
        /// <param name="locationId">LocationId from quest system.</param>
        /// <returns>MapId if present or -1.</returns>
        public static ulong LocationIdToMapId(ulong locationId)
        {
            if (locationIdToMapIdDict.ContainsKey(locationId))
            {
                return locationIdToMapIdDict[locationId];
            }

            return 0;
        }

        // private static void MapDictCheck()
        // {
        //     // Build map lookup dictionary
        //     if (mapDict == null)
        //         EnumerateMaps();
        // }

        /// <summary>
        /// Build dictionary of locations.
        /// </summary>
        private static Dictionary<ulong, MapSummary> EnumerateMaps()
        {
            //System.Diagnostics.Stopwatch s = System.Diagnostics.Stopwatch.StartNew();
            //long startTime = s.ElapsedMilliseconds;

            Dictionary<ulong, MapSummary> mapDictSwap = new Dictionary<ulong, MapSummary>();
            // locationIdToMapIdDict = new Dictionary<ulong, ulong>();

            for (int region = 0; region < MapsFile.TempRegionCount; region++)
            {
                DFRegion dfRegion = WorldMaps.ConvertWorldMapsToDFRegion(region);
                for (int location = 0; location < dfRegion.LocationCount; location++)
                {
                    MapSummary summary = new MapSummary();
                    // Get map summary
                    DFRegion.RegionMapTable mapTable = dfRegion.MapTable[location];
                    summary.ID = mapTable.MapId;
                    summary.MapID = WorldMaps.WorldMap[region].Locations[location].Exterior.RecordElement.Header.LocationId;
                    summary.RegionIndex = region;
                    summary.MapIndex = WorldMaps.WorldMap[region].Locations[location].LocationIndex;

                    summary.LocationType = mapTable.LocationType;
                    summary.DungeonType = mapTable.DungeonType;

                    // TODO: This by itself doesn't account for DFRegion.LocationTypes.GraveyardForgotten locations that start the game discovered in classic
                    summary.Discovered = mapTable.Discovered;

                    mapDictSwap.Add(summary.ID, summary);

                    // Link locationId with mapId - adds ~25ms overhead
                    // ulong locationId = WorldMaps.ReadLocationIdFast(region, location);
                    // locationIdToMapIdDict.Add(locationId, summary.ID);
                }
            }

            string fileDataPath = Path.Combine(mapPath, "mapDict.json");
            var json = JsonConvert.SerializeObject(mapDictSwap, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            File.WriteAllText(fileDataPath, json);

            return mapDictSwap;
        }

        /// <summary>
        /// Lookup block name for exterior block from location data provided.
        /// </summary>
        /// <param name="dfLocation">DFLocation to read block name.</param>
        /// <param name="x">Block X coordinate.</param>
        /// <param name="y">Block Y coordinate.</param>
        /// <returns>Block name.</returns>
        public static string GetRmbBlockName(in DFLocation dfLocation, int x, int y)
        {
            int index = y * dfLocation.Exterior.ExteriorData.Width + x;
            return dfLocation.Exterior.ExteriorData.BlockNames[index];
        }

        #endregion
    }

    public struct MapSummary
    {
        public ulong ID;                  // mapTable.MapId & 0x000fffff for dict key and matching with ExteriorData.MapId
        public ulong MapID;               // Full mapTable.MapId for matching with localization key
        public int RegionIndex;
        public int MapIndex;
        public DFRegion.LocationTypes LocationType;
        public DFRegion.DungeonTypes DungeonType;
        public bool Discovered;
    }

    public class ClimateData
    {
        #region Class Fields

        public static int[,] Climate;

        static ClimateData()        
        {
            Climate = new int[MapsFile.TileDim * 3, MapsFile.TileDim * 3];
            Climate = GetTextureMatrix("climate");
        }

        public static int[,] ClimateModified;

        #endregion

        #region Public Methods

        public static int GetClimateValue(int mapPixelX, int mapPixelY)
        {
            // Clamp X
            if (mapPixelX < 0) mapPixelX = 0;
            if (mapPixelX >= MapsFile.WorldWidth) mapPixelX = MapsFile.WorldWidth - 1;

            // Clamp Y
            if (mapPixelY < 0) mapPixelY = 0;
            if (mapPixelY >= MapsFile.WorldHeight) mapPixelY = MapsFile.WorldHeight - 1;

            // Convert to relative coordinates
            DFPosition currentMP = MapsFile.ConvertToRelative(mapPixelX, mapPixelY);
            mapPixelX = currentMP.X;
            mapPixelY = currentMP.Y;

            return ClimateData.Climate[mapPixelX, mapPixelY];
        }

        public static void ConvertToMatrix(Texture2D imageToTranslate, out int[,] matrix)
        {
            Color32[] grayscaleMap = imageToTranslate.GetPixels32();
            // byte[] buffer = imageToTranslate.GetRawTextureData();
            // Debug.Log("buffer.Length: " + buffer.Length);
            matrix = new int[imageToTranslate.width, imageToTranslate.height];
            for (int x = 0; x < imageToTranslate.width; x++)
            {
                for (int y = 0; y < imageToTranslate.height; y++)
                {
                    int offset = (((imageToTranslate.height - y - 1) * imageToTranslate.width) + x);
                    byte value = grayscaleMap[offset].g;
                    matrix[x, y] = (int)value;
                }
            }
        }

        public static int[,] GetTextureMatrix(string matrixType)
        {
            DFPosition position = WorldMaps.LocalPlayerGPS.CurrentMapPixel;
            int xPos = position.X / MapsFile.TileDim;
            int yPos = position.Y / MapsFile.TileDim;
            int[,] matrix = new int[MapsFile.TileDim * 3, MapsFile.TileDim * 3];
            int[][,] textureArray = new int[9][,];


            for (int i = 0; i < 9; i++)
            {
                int posX = xPos + (i % 3 - 1);
                int posY = yPos + (i / 3 - 1);
                if (posX < 0) posX = 0;
                if (posY < 0) posY = 0;
                if (posX >= MapsFile.TileX) posX = MapsFile.TileX - 1;
                if (posY >= MapsFile.TileY) posY = MapsFile.TileY - 1;

                Texture2D textureTiles = new Texture2D(MapsFile.TileDim, MapsFile.TileDim);
                if (!ImageConversion.LoadImage(textureTiles, File.ReadAllBytes(Path.Combine(WorldMaps.tilesPath, matrixType + "_" + posX + "_" + posY + ".png"))))
                    Debug.Log("Failed to load tile " + matrixType + "_" + posX + "_" + posY + ".png");
                else Debug.Log("Tile " + matrixType + "_" + posX + "_" + posY + ".png succesfully loaded");

                if (matrixType == "climate")
                    ConvertToMatrix(textureTiles, out textureArray[i]);
                else PoliticData.ConvertToMatrix(textureTiles, out textureArray[i]);
            }

            for (int x = 0; x < MapsFile.TileDim * 3; x++)
            {
                for (int y = 0; y < MapsFile.TileDim * 3; y++)
                {
                    int xTile = x / MapsFile.TileDim;
                    int yTile = y / MapsFile.TileDim;
                    int X = x % MapsFile.TileDim;
                    int Y = y % MapsFile.TileDim;

                    matrix[x, y] = textureArray[yTile * 3 + xTile][X, Y];
                }
            }

            return matrix;
        }

        #endregion
    }

    public static class PoliticData
    {
        #region Class Fields

        public static int[,] Politic;

        static PoliticData()
        {
            Politic = new int[MapsFile.TileDim * 3, MapsFile.TileDim * 3];
            Politic = ClimateData.GetTextureMatrix("politic");
        }

        #endregion

        #region Public Methods

        public static bool IsBorderPixel(int x, int y, int actualPolitic)
        {
            int politicIndex = Politic[x, y];

            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    int X = x + i;
                    int Y = y + j;

                    if ((X < 0 || X > MapsFile.WorldWidth || Y < 0 || Y > MapsFile.WorldHeight) ||
                        (i == 0 && j == 0))
                        continue;

                    if (politicIndex != Politic[X, Y] && 
                        politicIndex != actualPolitic && 
                        (WoodsData.GetHeightMapValue(x, y) > 3) && 
                        (WoodsData.GetHeightMapValue(X, Y) > 3))
                        return true;
                }
            }

            return false;
        }

        public static int GetPoliticValue(int x, int y, bool convertToRegionIndex = true)
        {
            int regionIndex = 0;

            // Clamp X
            if (x < 0) x = 0;
            if (x >= MapsFile.WorldWidth) x = MapsFile.WorldWidth - 1;

            // Clamp Y
            if (y < 0) y = 0;
            if (y >= MapsFile.WorldHeight) y = MapsFile.WorldHeight - 1;

            // Convert to relative coordinates
            DFPosition currentMP = MapsFile.ConvertToRelative(x, y);
            x = currentMP.X;
            y = currentMP.Y;

            regionIndex = PoliticData.Politic[x, y];

            if (convertToRegionIndex)
            {
                if (regionIndex == 0)
                    return 31;

                regionIndex -= 128;
            }
            return regionIndex;
        }

        public static void ConvertToMatrix(Texture2D imageToTranslate, out int[,] matrix)
        {
            Color32[] grayscaleMap = imageToTranslate.GetPixels32();
            // byte[] buffer = imageToTranslate.GetRawTextureData();
            // Debug.Log("buffer.Length: " + buffer.Length);
            matrix = new int[imageToTranslate.width, imageToTranslate.height];
            for (int x = 0; x < imageToTranslate.width; x++)
            {
                for (int y = 0; y < imageToTranslate.height; y++)
                {
                    int offset = (((imageToTranslate.height - y - 1) * imageToTranslate.width) + x);

                    int value = 0;

                    if (grayscaleMap[offset].g == grayscaleMap[offset].r && grayscaleMap[offset].g == grayscaleMap[offset].b)
                        value = (int)grayscaleMap[offset].g;
                    else if (grayscaleMap[offset].g == 255 && grayscaleMap[offset].b == 255)
                        value = (int)grayscaleMap[offset].g + grayscaleMap[offset].r;
                    else if (grayscaleMap[offset].r == 255 && grayscaleMap[offset].b == 255)
                        value = (int)grayscaleMap[offset].g + 509;
                    matrix[x, y] = value;
                }
            }
        }

        #endregion
    }

    public static class WoodsData
    {
        #region Class Fields

        public static byte[,] Woods;

        static WoodsData()
        {
            Woods = new byte[MapsFile.TileDim * 3, MapsFile.TileDim * 3];
            Woods = GetTextureMatrix("woods");
        }

        #endregion

        #region Public Methods

        public static Byte[] GetHeightMapValuesRange1Dim(int mapPixelX, int mapPixelY, int dim)
        {
            Byte[] dstData = new Byte[dim * dim];
            for (int y = 0; y < dim; y++)
            {
                for (int x = 0; x < dim; x++)
                {
                    dstData[x + (y * dim)] = GetHeightMapValue(mapPixelX + x, mapPixelY + y);
                }
            }
            return dstData;
        }

        public static Byte GetHeightMapValue(int mapPixelX, int mapPixelY)
        {
            // Clamp X
            if (mapPixelX < 0) mapPixelX = 0;
            if (mapPixelX >= MapsFile.WorldWidth) mapPixelX = MapsFile.WorldWidth - 1;

            // Clamp Y
            if (mapPixelY < 0) mapPixelY = 0;
            if (mapPixelY >= MapsFile.WorldHeight) mapPixelY = MapsFile.WorldHeight - 1;

            // Convert to relative coordinates
            DFPosition currentMP = MapsFile.ConvertToRelative(mapPixelX, mapPixelY);
            mapPixelX = currentMP.X;
            mapPixelY = currentMP.Y;

            return WoodsData.Woods[mapPixelX, mapPixelY];
        }

        public static void ConvertToMatrix(Texture2D imageToTranslate, out byte[,] matrix)
        {
            Color32[] grayscaleMap = imageToTranslate.GetPixels32();
            // byte[] buffer = imageToTranslate.GetRawTextureData();
            // Debug.Log("buffer.Length: " + buffer.Length);
            matrix = new byte[imageToTranslate.width, imageToTranslate.height];
            for (int x = 0; x < imageToTranslate.width; x++)
            {
                for (int y = 0; y < imageToTranslate.height; y++)
                {
                    int offset = (((imageToTranslate.height - y - 1) * imageToTranslate.width) + x);
                    byte value = grayscaleMap[offset].g;
                    matrix[x, y] = value;
                }
            }
        }

        public static byte[,] GetTextureMatrix(string matrixType = "woods")
        {
            DFPosition position = WorldMaps.LocalPlayerGPS.CurrentMapPixel;
            Debug.Log("position: " + position);
            int xPos = position.X / MapsFile.TileDim;
            int yPos = position.Y / MapsFile.TileDim;
            byte[,] matrix = new byte[MapsFile.TileDim * 3, MapsFile.TileDim * 3];
            byte[][,] textureArray = new byte[9][,];


            for (int i = 0; i < 9; i++)
            {
                int posX = xPos + (i % 3 - 1);
                int posY = yPos + (i / 3 - 1);
                if (posX < 0) posX = 0;
                if (posY < 0) posY = 0;
                if (posX >= MapsFile.TileX) posX = MapsFile.TileX - 1;
                if (posY >= MapsFile.TileY) posY = MapsFile.TileY - 1;

                Texture2D textureTiles = new Texture2D(MapsFile.TileDim, MapsFile.TileDim);
                Debug.Log("Loading tile " + posX + ", " + posY);
                ImageConversion.LoadImage(textureTiles, File.ReadAllBytes(Path.Combine(WorldMaps.tilesPath, matrixType + "_" + posX + "_" + posY + ".png")));
                ConvertToMatrix(textureTiles, out textureArray[i]);
            }

            for (int x = 0; x < MapsFile.TileDim * 3; x++)
            {
                for (int y = 0; y < MapsFile.TileDim * 3; y++)
                {
                    int xTile = x / MapsFile.TileDim;
                    int yTile = y / MapsFile.TileDim;
                    int X = x % MapsFile.TileDim;
                    int Y = y % MapsFile.TileDim;

                    matrix[x, y] = textureArray[yTile * 3 + xTile][X, Y];
                }
            }

            return matrix;
        }

        #endregion
    }

    public static class WoodsLargeData
    {
        #region Class Variables

        public static byte[,] WoodsLarge;

        static WoodsLargeData()
        {
            WoodsLarge = new byte[MapsFile.TileDim * 15, MapsFile.TileDim * 15];
            WoodsLarge = GetLargeHeightmapMatrix();
            //WoodsLarge = JsonConvert.DeserializeObject<byte[,]>(File.ReadAllText(Path.Combine(WorldMaps.mapPath, "WoodsLarge.json")));
        }

        #endregion

        #region Public Methods

        public static void ConvertToMatrix(Texture2D imageToTranslate, out byte[,] matrix)
        {
            Color32[] grayscaleMap = imageToTranslate.GetPixels32();
            // byte[] buffer = imageToTranslate.GetRawTextureData();
            // Debug.Log("buffer.Length: " + buffer.Length);
            matrix = new byte[MapsFile.TileDim * 5, MapsFile.TileDim * 5];
            for (int x = 0; x < MapsFile.TileDim * 5; x++)
            {
                for (int y = 0; y < MapsFile.TileDim * 5; y++)
                {
                    int offset = (((MapsFile.TileDim * 5 - y - 1) * (MapsFile.TileDim * 5)) + x);
                    Debug.Log("offset: " + offset);
                    byte value = grayscaleMap[offset].g;
                    matrix[x, y] = value;
                }
            }
        }

        public static byte[,] GetLargeHeightmapMatrix()
        {
            DFPosition position = WorldMaps.LocalPlayerGPS.CurrentMapPixel;
            int xPos = position.X / MapsFile.TileDim;
            int yPos = position.Y / MapsFile.TileDim;
            byte[,] matrix = new byte[MapsFile.TileDim * 15, MapsFile.TileDim * 15];
            byte[][,] textureArray = new byte[9][,];


            for (int i = 0; i < 9; i++)
            {
                int posX = xPos + (i % 3 - 1);
                int posY = yPos + (i / 3 - 1);
                if (posX < 0) posX = 0;
                if (posY < 0) posY = 0;
                if (posX >= MapsFile.TileX) posX = MapsFile.TileX - 1;
                if (posY >= MapsFile.TileY) posY = MapsFile.TileY - 1;

                textureArray[i] = JsonConvert.DeserializeObject<byte[,]>(File.ReadAllText(Path.Combine(WorldMaps.tilesPath, "woodsLarge_" + posX + "_" + posY + ".json")));
                // Texture2D textureTiles = new Texture2D(MapsFile.TileDim * 5, MapsFile.TileDim * 5);
                // ImageConversion.LoadImage(textureTiles, File.ReadAllBytes(Path.Combine(WorldMaps.tilesPath, "woodsLarge_" + posX + "_" + posY + ".json")));
                // ConvertToMatrix(textureTiles, out textureArray[i]);
            }

            for (int x = 0; x < MapsFile.TileDim * 15; x++)
            {
                for (int y = 0; y < MapsFile.TileDim * 15; y++)
                {
                    int xTile = x / (MapsFile.TileDim * 5);
                    int yTile = y / (MapsFile.TileDim * 5);
                    int X = x % (MapsFile.TileDim * 5);
                    int Y = y % (MapsFile.TileDim * 5);

                    matrix[x, y] = textureArray[yTile * 3 + xTile][X, Y];
                }
            }

            return matrix;
        }

        public static Byte[,] GetLargeHeightMapValuesRange(int mapPixelX, int mapPixelY, int dim)
        {
            const int offsetx = 1;
            const int offsety = 1;
            const int len = 3;

            Byte[,] dstData = new Byte[dim * len, dim * len];
            for (int y = 0; y < dim; y++)
            {
                for (int x = 0; x < dim; x++)
                {
                    // Get source 5x5 data
                    Byte[,] srcData = GetLargeMapData(mapPixelX + x, mapPixelY - y);

                    // Write 3x3 heightmap pixels to destination array
                    int startX = x * len;
                    int startY = y * len;
                    for (int iy = offsety; iy < offsety + len; iy++)
                    {
                        for (int ix = offsetx; ix < offsetx + len; ix++)
                        {
                            dstData[startX + ix - offsetx, startY + iy - offsety] = srcData[ix, 4 - iy];
                        }
                    }
                }
            }

            return dstData;
        }

        public static Byte[,] GetLargeMapData(int mapPixelX, int mapPixelY)
        {
            // Clamp X
            if (mapPixelX < 0) mapPixelX = 0;
            if (mapPixelX >= MapsFile.WorldWidth - 1) mapPixelX = MapsFile.WorldWidth - 1;

            // Clamp Y
            if (mapPixelY < 0) mapPixelY = 0;
            if (mapPixelY >= MapsFile.WorldHeight - 1) mapPixelY = MapsFile.WorldHeight - 1;

            // Offset directly to this pixel data
            // BinaryReader reader = managedFile.GetReader();
            // reader.BaseStream.Position = dataOffsets[mapPixelY * mapWidthValue + mapPixelX] + 22;

            DFPosition relativeCoordinates = MapsFile.ConvertToRelative(mapPixelX, mapPixelY);

            // Read 5x5 data
            Byte[,] data = new Byte[5, 5];
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    data[x, y] = WoodsLargeData.WoodsLarge[relativeCoordinates.X * 5 + x, relativeCoordinates.Y * 5 + y];
                }
            }

            return data;
        }

        #endregion
    }

    public static class WorldData
    {
        public static WorldSettings WorldSetting;

        static WorldData()
        {
            WorldSetting = JsonConvert.DeserializeObject<WorldSettings>(File.ReadAllText(Path.Combine("/home/arneb/Games/daggerfall/DaggerfallGameFiles/arena2/Maps", "WorldData.json")));
        }
    }

    public class WorldSettings
    {
        public int Regions;
        public string[] RegionNames;
        public int WorldWidth;
        public int WorldHeight;
        public int[] regionRaces;
        public int[] regionTemples;
        public byte[] regionBorders;

        public WorldSettings()
        {
            // Regions = 62;
            // RegionNames = MapsFile.RegionNames;
            // WorldWidth = 1115;
            // WorldHeight = 932;
            // regionRaces = MapsFile.RegionRaces;
            // regionTemples = MapsFile.RegionTemples;
            // Debug.Log("WorldSettings set");
        }
    }

    public static class FactionsAtlas
    {
        public static Dictionary<int, FactionFile.FactionData> FactionDictionary = new Dictionary<int, FactionFile.FactionData>();
        public static Dictionary<string, int> FactionToId = new Dictionary<string, int>();

        static FactionsAtlas()
        {
            FactionDictionary = JsonConvert.DeserializeObject<Dictionary<int, FactionFile.FactionData>>(File.ReadAllText(Path.Combine("/home/arneb/Games/daggerfall/DaggerfallGameFiles/arena2/Maps", "Faction.json")));

            foreach (KeyValuePair<int, FactionFile.FactionData> faction in FactionDictionary)
            {
                if (!FactionToId.ContainsKey(faction.Value.name))
                    FactionToId.Add(faction.Value.name, faction.Key);
                else {
                    FactionToId.Add(faction.Value.name + "_(" + ((FactionFile.FactionIDs)faction.Value.parent).ToString() + ")", faction.Key);
                    int indexSwap = FactionToId[faction.Value.name];
                    FactionFile.FactionData factionSwap = FactionDictionary[indexSwap];
                    FactionToId.Remove(faction.Value.name);
                    FactionToId.Add(factionSwap.name + "_(" + ((FactionFile.FactionIDs)factionSwap.parent).ToString() + ")", indexSwap);
                }
            }
        }

        /// <summary>
        /// Gets faction data from faction ID.
        /// </summary>
        /// <param name="factionID">Faction ID.</param>
        /// <param name="factionDataOut">Receives faction data.</param>
        /// <returns>True if successful.</returns>
        public static bool GetFactionData(int factionID, out FactionFile.FactionData factionDataOut)
        {
            factionDataOut = new FactionFile.FactionData();
            if (FactionsAtlas.FactionDictionary.ContainsKey(factionID))
            {
                factionDataOut = FactionsAtlas.FactionDictionary[factionID];
                return true;
            }

            return false;
        }
    }
}